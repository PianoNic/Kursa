using System.ComponentModel;
using System.Text.RegularExpressions;
using Kursa.Application.Common.Interfaces;
using Kursa.Application.Features.Moodle.Models;
using Kursa.Domain.Entities;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

namespace Kursa.Application.Features.Chat;

/// <summary>
/// Semantic Kernel plugin that exposes course-material search and Moodle browsing
/// as KernelFunctions so the agent can call them autonomously.
/// </summary>
public sealed class KursaAgentPlugin(
    IVectorStore vectorStore,
    ITextEmbeddingGenerationService embeddingService,
    IMoodleService moodleService,
    User user,
    List<CitationDto> citations)
{
    private const string CollectionName = "content_chunks";

    [KernelFunction("search_course_materials")]
    [Description("Semantically searches the user's pinned and indexed course documents. Returns the most relevant chunks of text with source titles and relevance scores. Use this when the user asks about specific course content, topics, or study material.")]
    public async Task<string> SearchCourseMaterialsAsync(
        [Description("The search query optimised for semantic retrieval, e.g. 'learning objectives for marketing' or 'Break-even point formula'")] string query,
        [Description("Maximum number of results to return (1-10, default 5)")] int limit = 5,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 10);

#pragma warning disable SKEXP0001
        ReadOnlyMemory<float> embeddingMemory = await embeddingService.GenerateEmbeddingAsync(query, cancellationToken: cancellationToken);
#pragma warning restore SKEXP0001

        float[] queryVector = embeddingMemory.ToArray();
        IReadOnlyList<VectorSearchResult> results = await vectorStore.SearchAsync(
            CollectionName, queryVector, limit: limit, filterByUserId: user.Id, cancellationToken: cancellationToken);

        if (results.Count == 0)
            return "No relevant course materials found for this query.";

        var parts = new List<string>(results.Count);
        for (int i = 0; i < results.Count; i++)
        {
            VectorSearchResult r = results[i];
            parts.Add($"[Source {i + 1}] {r.ContentTitle} (relevance: {r.Score:F2})\n{r.ChunkText}");
            citations.Add(new CitationDto(r.ContentId, r.ContentTitle ?? "Unknown", r.ChunkText, r.Score, r.SourceUrl));
        }

        return string.Join("\n\n---\n\n", parts);
    }

    [KernelFunction("list_enrolled_courses")]
    [Description("Returns a list of all Moodle courses the user is enrolled in, including course IDs, names, and completion progress. Use this when the user asks what courses they have or needs a course ID.")]
    public async Task<string> ListEnrolledCoursesAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(user.MoodleToken))
            return "User has not linked their Moodle account yet.";

        IReadOnlyList<MoodleCourseDto> courses = await moodleService.GetEnrolledCoursesAsync(
            user.MoodleToken, cancellationToken);

        if (courses.Count == 0)
            return "No enrolled courses found.";

        IEnumerable<string> lines = courses.Select(c =>
            $"- [{c.Id}] {c.FullName} ({c.ShortName})" +
            (c.Progress.HasValue ? $" — {c.Progress:F0}% complete" : string.Empty));

        return $"Enrolled courses ({courses.Count} total):\n{string.Join('\n', lines)}";
    }

    [KernelFunction("get_course_content")]
    [Description("Returns the sections and modules of a specific Moodle course by its numeric ID. Includes module names, types, and descriptions. Use this when the user asks about the structure or contents of a course.")]
    public async Task<string> GetCourseContentAsync(
        [Description("The numeric Moodle course ID (obtain from list_enrolled_courses if unknown)")] int courseId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(user.MoodleToken))
            return "User has not linked their Moodle account yet.";

        IReadOnlyList<MoodleCourseSectionDto> sections = await moodleService.GetCourseContentAsync(
            user.MoodleToken, courseId, cancellationToken);

        if (sections.Count == 0)
            return "No content found for this course.";

        var lines = new List<string>();
        foreach (MoodleCourseSectionDto section in sections.Where(s => s.Visible != 0 && s.Modules.Count > 0))
        {
            lines.Add($"## {section.Name}");
            foreach (MoodleModuleDto mod in section.Modules.Where(m => m.Visible != 0))
            {
                string fileInfo = mod.Contents?.Count > 0 ? $" ({mod.Contents.Count} file(s))" : string.Empty;
                lines.Add($"  - [{mod.ModName}] {mod.Name}{fileInfo}");
                if (!string.IsNullOrWhiteSpace(mod.Description))
                {
                    string desc = Regex.Replace(mod.Description, "<[^>]+>", " ")
                        .Replace("&nbsp;", " ").Trim();
                    if (desc.Length > 200) desc = string.Concat(desc.AsSpan(0, 197), "...");
                    if (!string.IsNullOrWhiteSpace(desc))
                        lines.Add($"    {desc}");
                }
            }
        }

        return string.Join('\n', lines);
    }
}
