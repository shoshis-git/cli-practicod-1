using System;
using System.CommandLine;
using System.Text;

// ---------------------------------------------------------------
// 🔹 פקודת bundle
// ---------------------------------------------------------------
var bundleCommand = new Command("bundle", "אורז קבצי קוד לקובץ אחד");


var languageOption = new Option<string>(new[] { "--language", "-l" }, "רשימת שפות תכנות (cs, js, py, או all)") { IsRequired = true };
var outputOption = new Option<string>(new[] { "--output", "-o" }, "שם קובץ ה-bundle המיוצא");
var noteOption = new Option<bool>(new[] { "--note", "-n" }, "האם לרשום הערות מקור בקובץ ה-bundle");
var sortOption = new Option<string>(new[] { "--sort", "-s" }, () => "name", "סדר המיון: name או type");
var removeEmptyLinesOption = new Option<bool>(new[] { "--remove-empty-lines", "-r" }, "האם להסיר שורות ריקות");
var authorOption = new Option<string>(new[] { "--author", "-a" }, "שם יוצר הקובץ (יופיע כהערה בראש הקובץ)");

bundleCommand.AddOption(languageOption);
bundleCommand.AddOption(outputOption);
bundleCommand.AddOption(noteOption);
bundleCommand.AddOption(sortOption);
bundleCommand.AddOption(removeEmptyLinesOption);
bundleCommand.AddOption(authorOption);


sortOption.AddValidator(result =>
{
    var value = result.GetValueOrDefault<string>();
    if (value != "name" && value != "type")
        result.ErrorMessage = "ערך לא תקין לאפשרות --sort. הערכים התקינים הם: name או type בלבד.";
});

languageOption.AddValidator(result =>
{
    var value = result.GetValueOrDefault<string>();
    if (string.IsNullOrWhiteSpace(value))
        result.ErrorMessage = "יש לציין לפחות שפה אחת או all.";
});

bundleCommand.SetHandler(async (language, output, note, sort, removeEmpty, author) =>
{
    try
    {
        var currentDir = Directory.GetCurrentDirectory();
        var extensions = language == "all"
            ? new[] { "*" }
            : language.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                      .Select(l => $"*.{l}")
                      .ToArray();

        var files = new List<FileInfo>();
        foreach (var ext in extensions)
        {
            files.AddRange(Directory.GetFiles(currentDir, ext, SearchOption.AllDirectories)
                                    .Select(f => new FileInfo(f)));
        }

        
        var excludedDirs = new[] { "bin", "obj", "debug", "release", "node_modules" };
        files = files
            .Where(f => !excludedDirs.Any(ex =>
                f.FullName.Contains(Path.DirectorySeparatorChar + ex + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (!files.Any())
        {
            Console.WriteLine("⚠️ לא נמצאו קבצים תואמים בתיקייה.");
            return;
        }

        files = sort.ToLower() switch
        {
            "type" => files.OrderBy(f => f.Extension).ThenBy(f => f.Name).ToList(),
            _ => files.OrderBy(f => f.Name).ToList()
        };

        var outputPath = output ?? Path.Combine(currentDir, "bundle.txt");
        using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);

        if (!string.IsNullOrWhiteSpace(author))
        {
            await writer.WriteLineAsync($"// Author: {author}");
            await writer.WriteLineAsync($"// Created: {DateTime.Now}");
            await writer.WriteLineAsync();
        }

        foreach (var file in files)
        {
            if (note)
            {
                var relative = Path.GetRelativePath(currentDir, file.FullName);
                await writer.WriteLineAsync($"// Source: {relative}");
            }

            var lines = await File.ReadAllLinesAsync(file.FullName, Encoding.UTF8);
            if (removeEmpty)
                lines = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();

            foreach (var line in lines)
                await writer.WriteLineAsync(line);

            await writer.WriteLineAsync();
        }

        Console.WriteLine($"✅ קובץ ה-bundle נוצר בהצלחה: {outputPath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ שגיאה: {ex.Message}");
    }

}, languageOption, outputOption, noteOption, sortOption, removeEmptyLinesOption, authorOption);


// ---------------------------------------------------------------
// 🔹 פקודת create-rsp
// ---------------------------------------------------------------
var createRspCommand = new Command("create-rsp", "יוצר קובץ תגובה (.rsp) עם פקודה מוכנה ל-bundle");

createRspCommand.SetHandler(() =>
{
    Console.WriteLine("👋 ניצור יחד קובץ תגובה להפעלת פקודת bundle.\n");

    Console.Write("שם קובץ התגובה (ללא סיומת): ");
    var rspName = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(rspName))
    {
        Console.WriteLine("❌ שם קובץ לא תקין.");
        return;
    }

    Console.Write("איזה שפות לכלול (למשל cs,js או all): ");
    var language = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(language))
    {
        Console.WriteLine("❌ חובה להזין שפה או all.");
        return;
    }

    Console.Write("שם קובץ הפלט (למשל output.txt או ניתוב מלא): ");
    var output = Console.ReadLine()?.Trim();

    Console.Write("האם להוסיף הערות מקור? (y/n): ");
    var noteInput = Console.ReadLine()?.Trim().ToLower();
    var note = noteInput == "y" || noteInput == "yes";

    Console.Write("סדר מיון (name/type): ");
    var sort = Console.ReadLine()?.Trim().ToLower();
    if (sort != "name" && sort != "type")
    {
        Console.WriteLine("⚠️ ערך לא תקין, נשתמש בברירת מחדל (name).");
        sort = "name";
    }

    Console.Write("האם להסיר שורות ריקות? (y/n): ");
    var removeEmptyInput = Console.ReadLine()?.Trim().ToLower();
    var removeEmpty = removeEmptyInput == "y" || removeEmptyInput == "yes";

    Console.Write("שם היוצר (אופציונלי): ");
    var author = Console.ReadLine()?.Trim();

    var commandBuilder = new StringBuilder();
    commandBuilder.Append("bundle ");
    commandBuilder.Append($"--language {language} ");
    if (!string.IsNullOrWhiteSpace(output))
        commandBuilder.Append($"--output \"{output}\" ");
    if (note)
        commandBuilder.Append("--note ");
    commandBuilder.Append($"--sort {sort} ");
    if (removeEmpty)
        commandBuilder.Append("--remove-empty-lines ");
    if (!string.IsNullOrWhiteSpace(author))
        commandBuilder.Append($"--author \"{author}\"");

    var rspContent = commandBuilder.ToString();
    var rspPath = $"{rspName}.rsp";
    File.WriteAllText(rspPath, rspContent);

    Console.WriteLine($"\n✅ קובץ התגובה נוצר בהצלחה: {rspPath}");
    Console.WriteLine($"כדי להריץ את הפקודה:\n dotnet run -- @{rspPath}");
});


// ---------------------------------------------------------------
// 🔹 Root Command
// ---------------------------------------------------------------
var rootCommand = new RootCommand("כלי לאריזת קבצי קוד לקובץ אחד");
rootCommand.AddCommand(bundleCommand);
rootCommand.AddCommand(createRspCommand);


await rootCommand.InvokeAsync(args);

