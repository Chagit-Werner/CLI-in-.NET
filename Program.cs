
using System.CommandLine;
using System.CommandLine.Parsing;


var bundleOption = new Option<FileInfo>(new string[] { "--output", "--o" }, "File path & name");
var languagesOption = new Option<string>(new string[] { "--languages", "--l" }, "List of developing-languages.")
{
    IsRequired = true,
};
var includeNoteOption = new Option<bool>(new string[] { "--note", "--n" }, "Include a note in the bundled file");
var orderTypeOption = new Option<bool>(new string[] { "--order-type", "--ot" }, "Order files by type");
var creatorOption = new Option<string>(new string[] { "--creator", "--c" }, "Name of the creator");
var removeEmptyLinesOption = new Option<bool>(new string[] { "--remove-empty-lines", "--rel" }, "Remove empty lines from the source code");


var rootCommand = new RootCommand("Root command for file-bundler CLI");
var bundleCommand = new Command("bundle", "Bundle code files to a single file");
bundleCommand.AddOption(bundleOption);
bundleCommand.AddOption(languagesOption);
bundleCommand.AddOption(includeNoteOption);
bundleCommand.AddOption(orderTypeOption);
bundleCommand.AddOption(creatorOption);
bundleCommand.AddOption(removeEmptyLinesOption);

bundleCommand.SetHandler((output, languagesOption1, includeNoteOption1, orderType, creator, remove) =>
{
   
    try
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        Console.WriteLine("Current direc" + currentDirectory);
        string[] selectedFiles;
        //להיות בטוחה שהמשתמש הכניס ניתוב!!!
        while (output == null)
        {
            Console.WriteLine("Error: The path cannot be empty! ");
            output = PromptForFileInfo("Enter the output file path:");
        }
        if (languagesOption1.ToLower() == "all")
        {
            selectedFiles = Directory.GetFiles(currentDirectory, "*.*", SearchOption.AllDirectories)
       .Where(file => IsSupportedLanguageFile(file, "c#", "sql", "java","html","css","h","cpp") && !IsExcludedFolder(file))
       .ToArray();
        }
        else
        {
            // Include files based on selected languages
            var selectedLanguages = languagesOption1.Split(',');
            selectedFiles = selectedLanguages.SelectMany(lang =>
    {
        var langExtension = GetFileExtensionForLanguage(lang);
        if (langExtension != null)
        {

            return Directory.GetFiles(currentDirectory, $"*.{langExtension}", SearchOption.AllDirectories)
            .Where(file => !IsExcludedFolder(file));
        }
        else
        {
            return null;
        }
    })
    .ToArray();
        }
        if (orderType)
        {
            // Order by file type
            selectedFiles = selectedFiles.OrderBy(file => Path.GetExtension(file)).ToArray();
        }
        else
        {
            // Order by file name (default)
            selectedFiles = selectedFiles.OrderBy(file => Path.GetFileName(file)).ToArray();
        }

        File.WriteAllText(output.FullName, string.Join(Environment.NewLine, selectedFiles.SelectMany(file =>
        {
            var comments = new List<string>();

            if (includeNoteOption1)
            {
                // Your existing code for appending a note
                var noteContent = selectedFiles.Select(file =>
                    $"# Source: {Path.GetFileName(file)}, Relative Path: {Path.GetRelativePath(currentDirectory, file)}");
                comments.AddRange(noteContent);
            }

            if (!string.IsNullOrEmpty(creator))
            {
                comments.Add($"# Creator: {creator}");
            }
            Console.Write($"Reading content from file:");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(file);
            Console.ForegroundColor = ConsoleColor.White;
            var lines = File.ReadAllLines(file);
            // Remove empty lines if the option is specified
            if (remove)
            {
                lines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
            }
           return comments.Concat(lines); // Concatenate lines and comments
        })));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}, bundleOption, languagesOption, includeNoteOption, orderTypeOption, creatorOption, removeEmptyLinesOption);

//----------Rsp
var createRspCommand = new Command("create-rsp", "Create a response file with a ready command")
        {
            new Option<FileInfo>(new string[] { "--output", "--o" }, "File path & name"),
            new Option<string>(new string[] { "--languages", "--l" }, "List of developing-languages."),
            new Option<bool>(new string[] { "--note", "--n" }, "Include a note in the bundled file"),
            new Option<bool>(new string[] { "--order-type", "--ot" }, "Order files by type"),
            new Option<string>(new string[] { "--creator", "--c" }, "Name of the creator"),
            new Option<bool>(new string[] { "--remove-empty-lines", "--rel" }, "Remove empty lines from the source code"),
        };

createRspCommand.SetHandler(async (output, languages, includeNote, orderType, creator, remove) =>
{  
        try
        {
            // Prompt the user for each option
            output = output ?? PromptForFileInfo("Enter the output file path:");
        languages = languages != null ? languages : PromptForString("Enter the list of developing languages\n" +
            "The value must be: " + "c#, java, sql, html, css, cpp, h / all to all languages "+
            "( use ',' to separate them):");
        
        includeNote = PromptForBool("Include a note - source code- in the bundled file? (true/false):");
            orderType = PromptForBool("Order files by type of code? (default: in alphabetical order (true/false):");
            creator = creator != null ? creator : PromptForString("Enter the name of the creator:");
            remove = PromptForBool("Remove empty lines from the source code? (true/false):");

            // Create the response file name
            var responseFileName = "rsp.rsp";
        // Create and write the response file based on user input
        using (var writer = new StreamWriter(responseFileName))
            {
                writer.WriteLine($" --output {output} --languages {languages} " +
                    $"--note {includeNote} --order-type {orderType} --creator {creator} " +
                    $"--remove-empty-lines {remove}");
            }

        Console.ForegroundColor = ConsoleColor.Magenta;

        Console.WriteLine($"Response file '{responseFileName}' created successfully!");
        Console.WriteLine("Now, run the following command to execute the bundling:");
        Console.WriteLine($"Files-Bundler bundle @{responseFileName}");
        Console.ForegroundColor = ConsoleColor.White;

    }
    catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
  

}, bundleOption, languagesOption, includeNoteOption, orderTypeOption, creatorOption, removeEmptyLinesOption);


//Prompt functions:
//כאן יש ולידציות בשביל אפשרות - 
//create-rsp
static FileInfo PromptForFileInfo(string prompt)
{
     Console.ForegroundColor = ConsoleColor.Yellow;

    Console.Write(prompt + " ");
    string path = Console.ReadLine();
    while (string.IsNullOrWhiteSpace(path))
    {
        Console.WriteLine("Error: The path cannot be empty.");
        Console.Write(prompt + " ");
        path = Console.ReadLine();
    }
    return new FileInfo(path);
}

static string PromptForString(string prompt)
{
   Console.ForegroundColor = ConsoleColor.Yellow;
   Console.Write(prompt + " ");
    Console.ForegroundColor = ConsoleColor.White;

    return Console.ReadLine();
}

static bool PromptForBool(string prompt)
{
    Console.ForegroundColor = ConsoleColor.Green;

    Console.Write(prompt + " ");
    Console.ForegroundColor = ConsoleColor.White;

    string input = Console.ReadLine();

    while (
        //writing true or false is must?
        //May the user type "Enter"?
        string.IsNullOrWhiteSpace(input)
        || !bool.TryParse(input, out _))
    {
        Console.WriteLine("Error: Invalid input. Please enter 'true' or 'false' ");
        Console.ForegroundColor = ConsoleColor.Green;

        Console.Write(prompt + " ");
        Console.ForegroundColor = ConsoleColor.White;

        input = Console.ReadLine();
    }

    return bool.Parse(input);
}
static string GetFileExtensionForLanguage(string language)
{
    switch (language.ToLower())
    {
        case "c#":
            return "cs";
        case "java":
            return "java";
        case "sql":
            return "sql";
        case "html":
            return "html";
        case "css":
         return "css";
        case "cpp":
            return "cpp";
        case "h":
            return "h";
        default:
            return null; // Handle unrecognized language
    }
}

bool IsSupportedLanguageFile(string filePath, params string[] supportedLanguages)
{
    var fileExtension = Path.GetExtension(filePath)?.ToLower();
    return supportedLanguages.Any(lang => fileExtension == $".{GetFileExtensionForLanguage(lang)}");
}
bool IsExcludedFolder(string filePath)
{
    var excludedFolders = new[] { "bin", "obj", "debug" }; // Add more as needed
    var folderPath = Path.GetDirectoryName(filePath)?.ToLower();
    return excludedFolders.Any(folder => folderPath?.IndexOf(folder.ToLower()) != -1);
}

//Combine command to root:
rootCommand.Add(bundleCommand);
rootCommand.Add(createRspCommand);
rootCommand.InvokeAsync(args);