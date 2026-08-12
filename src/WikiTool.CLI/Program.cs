using System.CommandLine;
using WikiTool.Converters;

var rootCommand = new RootCommand("WikiTools - Convert between different wiki formats");

var convertCommand = new Command("convert", "Convert from one wiki format to another");

var sourceOption   = new Option<string>("--source", ["-s"]) { Description = "Source wiki directory path", Required = true };
var destOption     = new Option<string>("--dest",   ["-d"]) { Description = "Destination directory path", Required = true };
var formatOption   = new Option<string>("--from",   ["-f"]) { Description = "Source wiki format (wikidpad, obsidian)", Required = true };
var toFormatOption = new Option<string>("--to",     ["-t"]) { Description = "Destination wiki format (obsidian, markdown)", Required = true };

var convertCategoriesOption = new Option<bool>("--convert-categories")
    { Description = "wikidpad -> obsidian: convert CategoryName patterns to #Name tags" };
var noScaffoldingOption = new Option<bool>("--no-scaffolding")
    { Description = "obsidian -> markdown: skip generating _config.yml and the page/tag indexes" };
var indexFolderOption = new Option<string>("--index-folder")
    { Description = "obsidian -> markdown: folder for the generated indexes (default 'indexes', empty for the site root)" };
var keepInlineTagsOption = new Option<bool>("--keep-inline-tags")
    { Description = "obsidian -> markdown: leave inline #tags in the body instead of hoisting them to frontmatter" };
var siteTitleOption = new Option<string>("--site-title")
    { Description = "obsidian -> markdown: title for the generated site (defaults to the source folder name)" };
var forceOption = new Option<bool>("--force")
    { Description = "Overwrite generated site files that already exist in the destination" };

convertCommand.Options.Add(sourceOption);
convertCommand.Options.Add(destOption);
convertCommand.Options.Add(formatOption);
convertCommand.Options.Add(toFormatOption);
convertCommand.Options.Add(convertCategoriesOption);
convertCommand.Options.Add(noScaffoldingOption);
convertCommand.Options.Add(indexFolderOption);
convertCommand.Options.Add(keepInlineTagsOption);
convertCommand.Options.Add(siteTitleOption);
convertCommand.Options.Add(forceOption);

convertCommand.SetAction((parseResult) =>
{
    var source = parseResult.GetValue(sourceOption)!;
    var dest   = parseResult.GetValue(destOption)!;
    var from   = parseResult.GetValue(formatOption)!;
    var to     = parseResult.GetValue(toFormatOption)!;

    Console.WriteLine($"Converting from {from} to {to}...");
    Console.WriteLine($"Source: {source}");
    Console.WriteLine($"Destination: {dest}");

    var route = $"{from.ToLowerInvariant()}->{to.ToLowerInvariant()}";

    switch (route)
    {
        case "wikidpad->obsidian":
        {
            var converter = new WikidPadToObsidianConverter(source, dest)
            {
                ConvertCategoryTags = parseResult.GetValue(convertCategoriesOption)
            };
            converter.ConvertAll();
            break;
        }

        case "obsidian->markdown":
        {
            var indexFolder = parseResult.GetValue(indexFolderOption);

            var converter = new ObsidianToMarkdownConverter(source, dest)
            {
                GenerateSiteFiles = !parseResult.GetValue(noScaffoldingOption),
                StripInlineTags   = !parseResult.GetValue(keepInlineTagsOption),
                SiteTitle         = parseResult.GetValue(siteTitleOption),
                IndexFolder       = indexFolder ?? "indexes",
                Force             = parseResult.GetValue(forceOption)
            };
            converter.ConvertAll();
            ReportWarnings(converter.Warnings);
            break;
        }

        default:
            Console.Error.WriteLine($"Error: Conversion from {from} to {to} is not yet supported.");
            Console.Error.WriteLine("Currently supported: wikidpad -> obsidian, obsidian -> markdown");
            return 1;
    }

    Console.WriteLine("Conversion completed successfully!");
    return 0;
});

rootCommand.Subcommands.Add(convertCommand);

return rootCommand.Parse(args).Invoke();

static void ReportWarnings(List<string> warnings)
{
    if (warnings.Count == 0)
    {
        return;
    }

    Console.WriteLine();
    Console.WriteLine($"{warnings.Count} warning(s):");

    foreach (var warning in warnings)
    {
        Console.WriteLine($"  - {warning}");
    }

    Console.WriteLine();
}
