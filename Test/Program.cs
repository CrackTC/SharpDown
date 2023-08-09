using CrackTC.SharpDown.Parsing;
using System.Text.Json;
using System.Text.Json.Nodes;

var testCases = File.ReadAllText("spec.json");
var json = JsonSerializer.Deserialize<JsonNode>(testCases);

foreach (var testCase in json!.AsArray().Where(testCase => (int)testCase!["example"]! >= 0))
{
    int id = (int)testCase!["example"]!;
    string markdown = testCase!["markdown"]!.ToString();
    string answer = testCase!["html"]!.ToString();
    var result = MarkdownParser.Parse(markdown);
    string output = result.ToHtml();
    if (answer.Equals(output) is false)
    {
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
