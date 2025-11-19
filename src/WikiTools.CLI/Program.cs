using System;
using System.CommandLine;
using System.IO;
using WikiTools.Core.Converters;

namespace WikiTools.CLI;

class Program
{
    static int Main(string[] args)
    {
        var rootCommand = new RootCommand("WikiTools - Convert between different wiki formats");

        // Create convert command
        var convertCommand = new Command("convert", "Convert from one wiki format to another");

        // Add options
        var sourceOption = new Option<string>(
            name: "--source",
            description: "Source wiki directory path")
        {
            IsRequired = true
        };
        sourceOption.AddAlias("-s");

        var destOption = new Option<string>(
            name: "--dest",
            description: "Destination directory path")
        {
            IsRequired = true
        };
        destOption.AddAlias("-d");

        var formatOption = new Option<string>(
            name: "--from",
            description: "Source wiki format (wikidpad)")
        {
            IsRequired = true
        };
        formatOption.AddAlias("-f");

        var toFormatOption = new Option<string>(
            name: "--to",
            description: "Destination wiki format (obsidian)")
        {
            IsRequired = true
        };
        toFormatOption.AddAlias("-t");

        convertCommand.AddOption(sourceOption);
        convertCommand.AddOption(destOption);
        convertCommand.AddOption(formatOption);
        convertCommand.AddOption(toFormatOption);

        convertCommand.SetHandler((string source, string dest, string from, string to) =>
        {
            try
            {
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during conversion: {ex.Message}");
            }
        }, sourceOption, destOption, formatOption, toFormatOption);

        rootCommand.AddCommand(convertCommand);

        return rootCommand.Invoke(args);
    }
}