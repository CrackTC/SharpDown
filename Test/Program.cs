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

string str = """
Course Design
-------------

one CUC student's course design

# Build instructions:

## Linux

```shell
make build run
```

## Windows

- MSVC

  ```shell
  mingw32-make build run
  ```

- MinGW

  ```shell
  mingw32-make MINGW=1 build run
  ```
""";
Console.WriteLine(MarkdownParser.Parse(str).ToHtml());
//var markdown = "  \tfoo\tbaz\t\tbim\n";
//var md = MarkdownParser.Parse(markdown);
//Console.ForegroundColor = ConsoleColor.Green;
//Console.WriteLine(Regex.Escape(md.ToHtml()));
//Console.ForegroundColor = ConsoleColor.Blue;
//Console.WriteLine();
//Console.WriteLine(md.ToAST());
//Console.ForegroundColor = ConsoleColor.Black;

//var matches = Regex.Matches(html, @"<raw>([^<]*)</raw>");

//foreach (Match match in matches)
//{
//	foreach (Group item in match.Groups)
//	{
//    }
//}
