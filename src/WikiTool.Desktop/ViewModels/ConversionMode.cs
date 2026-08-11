namespace WikiTool.Desktop.ViewModels;

public enum ConversionMode
{
    WikidPadToObsidian,
    ObsidianToMarkdown
}

public class ConversionModeOption
{
    public required ConversionMode Mode { get; init; }
    public required string DisplayName { get; init; }

    public static readonly ConversionModeOption[] All =
    [
        new() { Mode = ConversionMode.WikidPadToObsidian, DisplayName = "WikidPad → Obsidian" },
        new() { Mode = ConversionMode.ObsidianToMarkdown, DisplayName = "Obsidian → Markdown (GitHub Pages)" }
    ];
}
