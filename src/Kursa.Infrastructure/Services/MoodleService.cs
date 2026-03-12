using System.Text.Json;
using Kursa.Application.Common.Interfaces;
using Kursa.Application.Features.Moodle.Models;
using Kursa.Infrastructure.MoodlewareAPI;
using Kursa.Infrastructure.MoodlewareAPI.Client.Models;
using Kursa.Infrastructure.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Serialization.Json;

namespace Kursa.Infrastructure.Services;

public sealed class MoodleService(
    MoodlewareClientFactory clientFactory,
    IHttpClientFactory httpClientFactory,
    IDistributedCache cache,
    IOptions<MoodleOptions> moodleOptions,
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
            var response = await client.GetToken.PostAsync(new Get_moodle_token_params
            {
                MoodleUrl = moodleOptions.Value.SiteUrl,
                Username = username,
                Password = password,
            }, cancellationToken: cancellationToken);

            if (response?.Success != true || response.Data is not UntypedObject dataObj)
                return null;

            IDictionary<string, UntypedNode?> values = dataObj.GetValue()!;
            if (values.TryGetValue("token", out UntypedNode? tokenNode) && tokenNode is UntypedString tokenStr)
                return tokenStr.GetValue();

            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "MoodlewareAPI /get-token failed for user {Username}", username);
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
        var body = new Get_user_courses_params { MoodleUrl = moodleOptions.Value.SiteUrl };
        body.AdditionalData["userid"] = siteInfo.UserId;

        var response = await client.Core.Enrol.GetUsersCourses.PostAsync(
            body, cancellationToken: cancellationToken);

        var courses = await DeserializeMoodleDataAsync<List<MoodleCourseDto>>(response, cancellationToken) ?? [];
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
        var response = await client.Core.Course.GetContents.PostAsync(
            new Get_course_contents_params { MoodleUrl = moodleOptions.Value.SiteUrl, Courseid = courseId }, cancellationToken: cancellationToken);

        ThrowIfMoodleError(response, "core_course_get_contents");

        List<MoodleCourseSectionDto> sections;
        try
        {
            sections = await DeserializeMoodleDataAsync<List<MoodleCourseSectionDto>>(response, cancellationToken) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize course content for course {CourseId}", courseId);
            throw new InvalidOperationException($"Failed to parse course content response: {ex.Message}", ex);
        }

        await SetCacheAsync(cacheKey, sections, ContentCacheDuration, cancellationToken);
        return sections;
    }

    public async Task<MoodleSiteInfoDto> GetSiteInfoAsync(
        string moodleToken, CancellationToken cancellationToken = default)
    {
        var client = clientFactory.CreateForToken(moodleToken);
        var response = await client.Core.Webservice.GetSiteInfo.PostAsync(
            new Get_site_info_params { MoodleUrl = moodleOptions.Value.SiteUrl }, cancellationToken: cancellationToken);

        return await DeserializeMoodleDataAsync<MoodleSiteInfoDto>(response, cancellationToken)
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
        var body = new Get_assignments_params { MoodleUrl = moodleOptions.Value.SiteUrl };
        if (courseIds is { Count: > 0 })
        {
            body.AdditionalData["courseids"] = courseIds.ToList();
        }

        var response = await client.Mod.Assign.GetAssignments.PostAsync(body, cancellationToken: cancellationToken);
        var dto = await DeserializeMoodleDataAsync<MoodleAssignmentsResponseDto>(response, cancellationToken) ?? new MoodleAssignmentsResponseDto();
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
        var response = await client.Gradereport.User.GetGradeItems.PostAsync(
            new Get_grade_items_params { MoodleUrl = moodleOptions.Value.SiteUrl, Courseid = courseId }, cancellationToken: cancellationToken);

        var dto = await DeserializeMoodleDataAsync<MoodleGradeReportDto>(response, cancellationToken) ?? new MoodleGradeReportDto();
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
        var body = new Get_forums_by_courses_params { MoodleUrl = moodleOptions.Value.SiteUrl };
        body.AdditionalData["courseids"] = new List<int> { courseId };

        var response = await client.Mod.Forum.GetForumsByCourses.PostAsync(body, cancellationToken: cancellationToken);
        var forums = await DeserializeMoodleDataAsync<List<MoodleForumDto>>(response, cancellationToken) ?? [];
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
        var response = await client.Mod.Forum.GetForumDiscussions.PostAsync(
            new Get_forum_discussions_params { MoodleUrl = moodleOptions.Value.SiteUrl, Forumid = forumId }, cancellationToken: cancellationToken);

        var dto = await DeserializeMoodleDataAsync<MoodleForumDiscussionsResponseDto>(response, cancellationToken) ?? new MoodleForumDiscussionsResponseDto();
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
        var body = new Get_calendar_events_params { MoodleUrl = moodleOptions.Value.SiteUrl };
        body.AdditionalData["events"] = new Dictionary<string, long>
        {
            ["timestart"] = timeStart,
            ["timeend"] = timeEnd,
        };

        var response = await client.Core.Calendar.GetCalendarEvents.PostAsync(body, cancellationToken: cancellationToken);
        var dto = await DeserializeMoodleDataAsync<MoodleCalendarEventsResponseDto>(response, cancellationToken) ?? new MoodleCalendarEventsResponseDto();
        await SetCacheAsync(cacheKey, dto, ContentCacheDuration, cancellationToken);
        return dto;
    }

    public async Task<HttpResponseMessage> GetFileAsync(
        string moodleToken, string moodleFileUrl, CancellationToken cancellationToken = default)
    {
        // Append token as a query parameter — Moodle pluginfile.php supports ?token=<wstoken>
        var uri = new UriBuilder(moodleFileUrl);
        string query = string.IsNullOrEmpty(uri.Query)
            ? $"token={Uri.EscapeDataString(moodleToken)}"
            : $"{uri.Query.TrimStart('?')}&token={Uri.EscapeDataString(moodleToken)}";
        uri.Query = query;

        HttpClient client = httpClientFactory.CreateClient("MoodleFile");
        HttpResponseMessage response = await client.GetAsync(
            uri.Uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        return response;
    }

    /// <summary>
    /// Extracts the <c>data</c> field from a <see cref="MoodleResponse"/> and deserializes it
    /// into <typeparamref name="TTarget"/> using System.Text.Json.
    /// </summary>
    private async Task<TTarget?> DeserializeMoodleDataAsync<TTarget>(MoodleResponse? response, CancellationToken cancellationToken)
        where TTarget : class
    {
        UntypedNode? data = response?.Data;
        return await DeserializeParsableAsync<TTarget>(data, cancellationToken);
    }

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

    /// <summary>
    /// Detects Moodle error responses and throws an <see cref="InvalidOperationException"/>
    /// with the Moodle error message so callers don't try to deserialize error objects as content DTOs.
    /// </summary>
    private static void ThrowIfMoodleError(MoodleResponse? response, string operation)
    {
        if (response?.Success == false || response?.Data is not UntypedObject obj) return;

        // Check if the data itself contains a Moodle exception
        if (response.Data is UntypedObject dataObj)
        {
            IDictionary<string, UntypedNode?> values = dataObj.GetValue()!;
            if (!values.ContainsKey("exception")) return;

            string message = "Moodle error";
            if (values.TryGetValue("message", out UntypedNode? msgNode) && msgNode is UntypedString msgStr)
                message = msgStr.GetValue() ?? message;
            else if (values.TryGetValue("errorcode", out UntypedNode? codeNode) && codeNode is UntypedString codeStr)
                message = codeStr.GetValue() ?? message;

            throw new InvalidOperationException($"Moodle returned an error for '{operation}': {message}");
        }
    }
}
