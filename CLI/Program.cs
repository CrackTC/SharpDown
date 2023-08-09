using CrackTC.SharpDown.Parsing;
using System.CommandLine;

namespace CrackTC.SharpDown.Cli;
internal static class Program
{
    private static void Handle(string markdownPath, string? outputPath)
    {
        if (outputPath is not null)
            File.WriteAllText(outputPath, MarkdownParser.Parse(File.ReadAllText(markdownPath)).ToHtml());
        else
            Console.Write(MarkdownParser.Parse(File.ReadAllText(markdownPath)).ToHtml());
    }

    private static void Main(string[] args)
    {
        var markdownFileName = new Argument<string>()
        {
            Arity = ArgumentArity.ExactlyOne
        };
        var option = new Option<string>(new[] { "-o", "--output" }, "Specify output path.");
        var parseCommand = new RootCommand("Parse markdown file, output html.")
        {
            markdownFileName,
            option
        };
        parseCommand.SetHandler(Handle, markdownFileName, option);
        parseCommand.Invoke(args);
    }
}