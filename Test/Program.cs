using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using CrackTC.SharpDown.Parsing;

var testCases = File.ReadAllText("spec.json");
var json = JsonSerializer.Deserialize<JsonNode>(testCases);
var count = 0;
var watch = Stopwatch.StartNew();

foreach (var testCase in json!.AsArray().Where(testCase => (int)testCase!["example"]! >= 0))
{
    var id = (int)testCase!["example"]!;
    var markdown = testCase["markdown"]!.ToString();
    var answer = testCase["html"]!.ToString();
    var result = MarkdownParser.Parse(markdown);
    var output = result.ToHtml();
    if (answer.Equals(output) is false)
    {
        count++;

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"id: {id}");

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"input:\n{markdown}");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"answer:\n{answer}");

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"output:\n{output}");

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine();
    }
}

watch.Stop();
var elapsedMs = watch.ElapsedMilliseconds;
Console.WriteLine($"{elapsedMs} ms elapsed");
Console.WriteLine($"{count} errors found");