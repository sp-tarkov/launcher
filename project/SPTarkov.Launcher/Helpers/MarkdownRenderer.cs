using Markdig;
using Microsoft.AspNetCore.Components;

namespace SPTarkov.Launcher.Helpers;

/// <summary>Renders release-note markdown to display HTML with raw inline HTML stripped.</summary>
public static class MarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().DisableHtml().Build();

    public static MarkupString ToHtml(string? markdown)
    {
        return string.IsNullOrWhiteSpace(markdown) ? default : (MarkupString)Markdown.ToHtml(markdown, Pipeline);
    }
}
