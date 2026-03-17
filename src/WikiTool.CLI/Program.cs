using System.CommandLine;
using WikiTool.Converters;

var rootCommand = new RootCommand("WikiTools - Convert between different wiki formats");

var convertCommand = new Command("convert", "Convert from one wiki format to another");

var sourceOption  = new Option<string>("--source", ["-s"]) { Description = "Source wiki directory path", Required = true };
var destOption    = new Option<string>("--dest",   ["-d"]) { Description = "Destination directory path", Required = true };
var formatOption  = new Option<string>("--from",   ["-f"]) { Description = "Source wiki format (wikidpad)", Required = true };
var toFormatOption = new Option<string>("--to",    ["-t"]) { Description = "Destination wiki format (obsidian)", Required = true };

convertCommand.Options.Add(sourceOption);
convertCommand.Options.Add(destOption);
convertCommand.Options.Add(formatOption);
convertCommand.Options.Add(toFormatOption);

convertCommand.SetAction((parseResult) =>
{
    var source = parseResult.GetValue(sourceOption)!;
    var dest   = parseResult.GetValue(destOption)!;
    var from   = parseResult.GetValue(formatOption)!;
    var to     = parseResult.GetValue(toFormatOption)!;

    Console.WriteLine($"Converting from {from} to {to}...");
    Console.WriteLine($"Source: {source}");
    Console.WriteLine($"Destination: {dest}");

    if (from.ToLower() == "wikidpad" && to.ToLower() == "obsidian")
    {
        var converter = new WikidPadToObsidianConverter(source, dest);
        converter.ConvertAll();
        Console.WriteLine("Conversion completed successfully!");
    }
    else
    {
        Console.WriteLine($"Error: Conversion from {from} to {to} is not yet supported.");
        Console.WriteLine("Currently supported: wikidpad -> obsidian");
    }
});

rootCommand.Subcommands.Add(convertCommand);

return rootCommand.Parse(args).Invoke();
