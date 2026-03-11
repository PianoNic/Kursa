using System.Text.Json;
using Kursa.Application.Common.Interfaces;
using Kursa.Application.Features.Moodle.Models;
using Kursa.Infrastructure.MoodlewareAPI;
using Kursa.Infrastructure.MoodlewareAPI.Client.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Serialization.Json;
using AuthRequest = Kursa.Infrastructure.MoodlewareAPI.Client.Models.AuthRequest;

namespace Kursa.Infrastructure.Services;

public sealed class MoodleService(
    MoodlewareClientFactory clientFactory,
    IDistributedCache cache,
    ILogger<MoodleService> logger) : IMoodleService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly TimeSpan CourseCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ContentCacheDuration = TimeSpan.FromMinutes(2);

    public async Task<string?> GetTokenAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = clientFactory.CreateAnonymous();
            var response = await client.Auth.PostAsync(new AuthRequest
            {
                Username = username,
                Password = password,
            }, cancellationToken: cancellationToken);

            return response?.Token;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "MoodlewareAPI /auth failed for user {Username}", username);
            return null;
        }
    }

    public async Task<IReadOnlyList<MoodleCourseDto>> GetEnrolledCoursesAsync(
        string moodleToken, CancellationToken cancellationToken = default)
    {
        string cacheKey = $"moodle:courses:{HashToken(moodleToken)}";
        var cached = await GetFromCacheAsync<List<MoodleCourseDto>>(cacheKey, cancellationToken);
        if (cached is not null) return cached;

        var client = clientFactory.CreateForToken(moodleToken);

        // Resolve the Moodle userid for this token — required by core_enrol_get_users_courses
        var siteInfo = await GetSiteInfoAsync(moodleToken, cancellationToken);
        var body = new CoreEnrolGetUsersCoursesRequest();
        body.AdditionalData["userid"] = siteInfo.UserId;

        var result = await client.Core_enrol_get_users_courses.PostAsync(
            body, cancellationToken: cancellationToken);

        var courses = await DeserializeAsync<List<MoodleCourseDto>>(result, cancellationToken) ?? [];
        await SetCacheAsync(cacheKey, courses, CourseCacheDuration, cancellationToken);
        return courses;
    }

    public async Task<IReadOnlyList<MoodleCourseSectionDto>> GetCourseContentAsync(
        string moodleToken, int courseId, CancellationToken cancellationToken = default)
    {
        string cacheKey = $"moodle:content:{HashToken(moodleToken)}:{courseId}";
        var cached = await GetFromCacheAsync<List<MoodleCourseSectionDto>>(cacheKey, cancellationToken);
        if (cached is not null) return cached;

        var client = clientFactory.CreateForToken(moodleToken);
        var result = await client.Core_course_get_contents.PostAsync(
            new CoreCourseGetContentsRequest { Courseid = courseId }, cancellationToken: cancellationToken);

        var sections = await DeserializeAsync<List<MoodleCourseSectionDto>>(result, cancellationToken) ?? [];
        await SetCacheAsync(cacheKey, sections, ContentCacheDuration, cancellationToken);
        return sections;
    }

    public async Task<MoodleSiteInfoDto> GetSiteInfoAsync(
        string moodleToken, CancellationToken cancellationToken = default)
    {
        var client = clientFactory.CreateForToken(moodleToken);
        var result = await client.Core_webservice_get_site_info.PostAsync(
            new CoreWebserviceGetSiteInfoRequest(), cancellationToken: cancellationToken);

        return await DeserializeParsableAsync<MoodleSiteInfoDto>(result, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize Moodle site info.");
    }

    public async Task<MoodleAssignmentsResponseDto> GetAssignmentsAsync(
        string moodleToken, IReadOnlyList<int>? courseIds = null,
        CancellationToken cancellationToken = default)
    {
        string cacheKey = $"moodle:assignments:{HashToken(moodleToken)}:{(courseIds is not null ? string.Join(",", courseIds) : "all")}";
        var cached = await GetFromCacheAsync<MoodleAssignmentsResponseDto>(cacheKey, cancellationToken);
        if (cached is not null) return cached;

        var client = clientFactory.CreateForToken(moodleToken);
        var body = new ModAssignGetAssignmentsRequest();
        if (courseIds is { Count: > 0 })
        {
            body.AdditionalData["courseids"] = courseIds.ToList();
        }

        var result = await client.Mod_assign_get_assignments.PostAsync(body, cancellationToken: cancellationToken);
        var dto = await DeserializeAsync<MoodleAssignmentsResponseDto>(result, cancellationToken) ?? new MoodleAssignmentsResponseDto();
        await SetCacheAsync(cacheKey, dto, ContentCacheDuration, cancellationToken);
        return dto;
    }

    public async Task<MoodleGradeReportDto> GetGradesAsync(
        string moodleToken, int courseId, CancellationToken cancellationToken = default)
    {
        string cacheKey = $"moodle:grades:{HashToken(moodleToken)}:{courseId}";
        var cached = await GetFromCacheAsync<MoodleGradeReportDto>(cacheKey, cancellationToken);
        if (cached is not null) return cached;

        var client = clientFactory.CreateForToken(moodleToken);
        var result = await client.Gradereport_user_get_grade_items.PostAsync(
            new GradereportUserGetGradeItemsRequest { Courseid = courseId }, cancellationToken: cancellationToken);

        var dto = await DeserializeAsync<MoodleGradeReportDto>(result, cancellationToken) ?? new MoodleGradeReportDto();
        await SetCacheAsync(cacheKey, dto, ContentCacheDuration, cancellationToken);
        return dto;
    }

    public async Task<IReadOnlyList<MoodleForumDto>> GetForumsAsync(
        string moodleToken, int courseId, CancellationToken cancellationToken = default)
    {
        string cacheKey = $"moodle:forums:{HashToken(moodleToken)}:{courseId}";
        var cached = await GetFromCacheAsync<List<MoodleForumDto>>(cacheKey, cancellationToken);
        if (cached is not null) return cached;

        var client = clientFactory.CreateForToken(moodleToken);
        var body = new ModForumGetForumsByCoursesRequest();
        body.AdditionalData["courseids"] = new List<int> { courseId };

        var result = await client.Mod_forum_get_forums_by_courses.PostAsync(body, cancellationToken: cancellationToken);
        var forums = await DeserializeAsync<List<MoodleForumDto>>(result, cancellationToken) ?? [];
        await SetCacheAsync(cacheKey, forums, ContentCacheDuration, cancellationToken);
        return forums;
    }

    public async Task<MoodleForumDiscussionsResponseDto> GetForumDiscussionsAsync(
        string moodleToken, int forumId, CancellationToken cancellationToken = default)
    {
        string cacheKey = $"moodle:discussions:{HashToken(moodleToken)}:{forumId}";
        var cached = await GetFromCacheAsync<MoodleForumDiscussionsResponseDto>(cacheKey, cancellationToken);
        if (cached is not null) return cached;

        var client = clientFactory.CreateForToken(moodleToken);
        var result = await client.Mod_forum_get_forum_discussions.PostAsync(
            new ModForumGetForumDiscussionsRequest { Forumid = forumId }, cancellationToken: cancellationToken);

        var dto = await DeserializeAsync<MoodleForumDiscussionsResponseDto>(result, cancellationToken) ?? new MoodleForumDiscussionsResponseDto();
        await SetCacheAsync(cacheKey, dto, ContentCacheDuration, cancellationToken);
        return dto;
    }

    public async Task<MoodleCalendarEventsResponseDto> GetCalendarEventsAsync(
        string moodleToken, long timeStart, long timeEnd,
        CancellationToken cancellationToken = default)
    {
        string cacheKey = $"moodle:calendar:{HashToken(moodleToken)}:{timeStart}:{timeEnd}";
        var cached = await GetFromCacheAsync<MoodleCalendarEventsResponseDto>(cacheKey, cancellationToken);
        if (cached is not null) return cached;

        var client = clientFactory.CreateForToken(moodleToken);
        var body = new CoreCalendarGetCalendarEventsRequest();
        body.AdditionalData["events"] = new Dictionary<string, long>
        {
            ["timestart"] = timeStart,
            ["timeend"] = timeEnd,
        };

        var result = await client.Core_calendar_get_calendar_events.PostAsync(body, cancellationToken: cancellationToken);
        var dto = await DeserializeAsync<MoodleCalendarEventsResponseDto>(result, cancellationToken) ?? new MoodleCalendarEventsResponseDto();
        await SetCacheAsync(cacheKey, dto, ContentCacheDuration, cancellationToken);
        return dto;
    }

    /// <summary>
    /// Serializes any Kiota <see cref="IParsable"/> (including <see cref="UntypedNode"/> and
    /// typed response objects) to JSON, then deserializes into <typeparamref name="TTarget"/>
    /// using System.Text.Json.
    /// </summary>
    private async Task<TTarget?> DeserializeAsync<TTarget>(UntypedNode? node, CancellationToken cancellationToken)
        where TTarget : class
        => await DeserializeParsableAsync<TTarget>(node, cancellationToken);

    private async Task<TTarget?> DeserializeParsableAsync<TTarget>(IParsable? parsable, CancellationToken cancellationToken)
        where TTarget : class
    {
        if (parsable is null) return null;

        var writer = new JsonSerializationWriter();
        writer.WriteObjectValue(null, parsable);
        using var stream = writer.GetSerializedContent();
        stream.Seek(0, SeekOrigin.Begin);

        return await JsonSerializer.DeserializeAsync<TTarget>(stream, JsonOptions, cancellationToken);
    }

    private async Task<T?> GetFromCacheAsync<T>(string key, CancellationToken cancellationToken) where T : class
    {
        try
        {
            byte[]? data = await cache.GetAsync(key, cancellationToken);
            if (data is null) return null;
            return JsonSerializer.Deserialize<T>(data, JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache read failed for key {Key}", key);
            return null;
        }
    }

    private async Task SetCacheAsync<T>(string key, T value, TimeSpan duration, CancellationToken cancellationToken)
    {
        try
        {
            byte[] data = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
            await cache.SetAsync(key, data, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = duration,
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache write failed for key {Key}", key);
        }
    }

    private static string HashToken(string token)
        => token.GetHashCode(StringComparison.Ordinal).ToString("x8");
}
