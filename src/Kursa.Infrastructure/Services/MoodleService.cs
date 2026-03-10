using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Kursa.Application.Common.Interfaces;
using Kursa.Application.Features.Moodle.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Kursa.Infrastructure.Services;

public sealed class MoodleService(
    IHttpClientFactory httpClientFactory,
    IDistributedCache cache,
    ILogger<MoodleService> logger) : IMoodleService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly TimeSpan CourseCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ContentCacheDuration = TimeSpan.FromMinutes(2);

    public async Task<IReadOnlyList<MoodleCourseDto>> GetEnrolledCoursesAsync(
        string moodleUrl, string moodleToken, CancellationToken cancellationToken = default)
    {
        string cacheKey = $"moodle:courses:{HashToken(moodleToken)}";

        var cached = await GetFromCacheAsync<List<MoodleCourseDto>>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var response = await SendMoodleRequestAsync(
            moodleUrl, moodleToken, "/core_enrol_get_users_courses", cancellationToken);

        var courses = JsonSerializer.Deserialize<List<MoodleCourseDto>>(response, JsonOptions)
            ?? [];

        await SetCacheAsync(cacheKey, courses, CourseCacheDuration, cancellationToken);

        return courses;
    }

    public async Task<IReadOnlyList<MoodleCourseSectionDto>> GetCourseContentAsync(
        string moodleUrl, string moodleToken, int courseId, CancellationToken cancellationToken = default)
    {
        string cacheKey = $"moodle:content:{HashToken(moodleToken)}:{courseId}";

        var cached = await GetFromCacheAsync<List<MoodleCourseSectionDto>>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var response = await SendMoodleRequestAsync(
            moodleUrl, moodleToken, $"/core_course_get_contents?courseid={courseId}", cancellationToken);

        var sections = JsonSerializer.Deserialize<List<MoodleCourseSectionDto>>(response, JsonOptions)
            ?? [];

        await SetCacheAsync(cacheKey, sections, ContentCacheDuration, cancellationToken);

        return sections;
    }

    public async Task<MoodleSiteInfoDto> GetSiteInfoAsync(
        string moodleUrl, string moodleToken, CancellationToken cancellationToken = default)
    {
        var response = await SendMoodleRequestAsync(
            moodleUrl, moodleToken, "/core_webservice_get_site_info", cancellationToken);

        return JsonSerializer.Deserialize<MoodleSiteInfoDto>(response, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize Moodle site info.");
    }

    public async Task<MoodleAssignmentsResponseDto> GetAssignmentsAsync(
        string moodleUrl, string moodleToken, IReadOnlyList<int>? courseIds = null,
        CancellationToken cancellationToken = default)
    {
        string courseParam = courseIds is { Count: > 0 }
            ? "?" + string.Join("&", courseIds.Select((id, i) => $"courseids[{i}]={id}"))
            : string.Empty;

        string cacheKey = $"moodle:assignments:{HashToken(moodleToken)}:{courseParam.GetHashCode(StringComparison.Ordinal):x8}";

        var cached = await GetFromCacheAsync<MoodleAssignmentsResponseDto>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var response = await SendMoodleRequestAsync(
            moodleUrl, moodleToken, $"/mod_assign_get_assignments{courseParam}", cancellationToken);

        var result = JsonSerializer.Deserialize<MoodleAssignmentsResponseDto>(response, JsonOptions)
            ?? new MoodleAssignmentsResponseDto();

        await SetCacheAsync(cacheKey, result, ContentCacheDuration, cancellationToken);

        return result;
    }

    public async Task<MoodleGradeReportDto> GetGradesAsync(
        string moodleUrl, string moodleToken, int courseId, CancellationToken cancellationToken = default)
    {
        string cacheKey = $"moodle:grades:{HashToken(moodleToken)}:{courseId}";

        var cached = await GetFromCacheAsync<MoodleGradeReportDto>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var response = await SendMoodleRequestAsync(
            moodleUrl, moodleToken, $"/gradereport_user_get_grade_items?courseid={courseId}", cancellationToken);

        var result = JsonSerializer.Deserialize<MoodleGradeReportDto>(response, JsonOptions)
            ?? new MoodleGradeReportDto();

        await SetCacheAsync(cacheKey, result, ContentCacheDuration, cancellationToken);

        return result;
    }

    private async Task<string> SendMoodleRequestAsync(
        string moodleUrl, string moodleToken, string endpoint, CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient("Moodle");
        client.BaseAddress = new Uri(moodleUrl.TrimEnd('/'));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", moodleToken);
        client.Timeout = TimeSpan.FromSeconds(30);

        logger.LogDebug("Calling MoodlewareAPI: {Endpoint}", endpoint);

        using var response = await client.PostAsync(endpoint, null, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            logger.LogWarning("Moodle API returned 401 Unauthorized for endpoint {Endpoint}", endpoint);
            throw new UnauthorizedAccessException("Moodle token is invalid or expired. Please re-link your Moodle account.");
        }

        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Moodle API error {StatusCode} for {Endpoint}: {Body}",
                (int)response.StatusCode, endpoint, body);
            throw new HttpRequestException(
                $"Moodle API returned {(int)response.StatusCode}: {response.ReasonPhrase}");
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private async Task<T?> GetFromCacheAsync<T>(string key, CancellationToken cancellationToken) where T : class
    {
        try
        {
            byte[]? data = await cache.GetAsync(key, cancellationToken);
            if (data is null)
                return null;

            return JsonSerializer.Deserialize<T>(data, JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read from cache for key {Key}", key);
            return null;
        }
    }

    private async Task SetCacheAsync<T>(string key, T value, TimeSpan duration, CancellationToken cancellationToken)
    {
        try
        {
            byte[] data = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = duration,
            };
            await cache.SetAsync(key, data, options, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to write to cache for key {Key}", key);
        }
    }

    private static string HashToken(string token)
    {
        int hash = token.GetHashCode(StringComparison.Ordinal);
        return hash.ToString("x8");
    }
}
