using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Text.Json.Serialization;
using OllamaSharp;

namespace Creator;

public class Creator
{
    private readonly string _gameName = "Game1";
    private IChat _chat;

    private readonly FileSaver _fileSaver;
    private readonly ContextHolder _contextHolder;

    public Creator()
    {
        _fileSaver = new FileSaver(_gameName, _gameName);
        _contextHolder = new ContextHolder();
    }

    public void Run()
    {
        bool finished = false;
        while (!finished)
        {
            try
            {
                using (_chat = new OllamaChat())
                {
                    string design = GameDesign();
                    _contextHolder.AddGameDesign(design);
                    string modules = CreateModules();
                    _contextHolder._contexts.Add("Game will consists these modules: ```Modules " + Environment.NewLine + modules +
                                                 Environment.NewLine + " ``` ");
                    CreateModulesCode(modules);
                    CreateProject();
                    CreateSolution();
                    while (true)
                    {
                        string buildResult = BuildSolution();
                        if (!buildResult.ToUpper().Contains("ERROR"))
                            break;
                        FixBuild(buildResult);    
                    }
                    
                    finished = true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                continue;
            }
        }
    }

    private void FixBuild(string buildErrors)
    {
        string prompt = $"I have errors in building solution. Fix bugs and write new content of files in : {Environment.NewLine}{buildErrors}"+
                        Environment.NewLine + "Return only name of files and updated files contents with fixed changes in this json format. I don't need explanation, I dont need examples. Answer only json " +
                        "{\"files\": [{\"fullPath\": \"...\", \"fileContents\": \"...\"}, ..., {\"fileName\": \"...\", \"fileContents\": \"...\"}] ";

        var aiResult = _chat.ResetAndSendPrompt(_contextHolder.GetAllProjectFilesContext() + prompt);
        var jsonText = string.Join(Environment.NewLine, OutputParser.Parse(aiResult, "json"));
        var filesToFix = GetJsonTupleArray(jsonText, "files", "fullPath", "fileContents");
        foreach (var file in filesToFix)
        {
            var oldContent = _contextHolder.GetProjectFilesContext(file.Item1);
            if (true)//!HasSignificantChanges(oldContent, file.Item2))
            {
                File.WriteAllText(file.Item1, file.Item2);
                _contextHolder.SetProjectFilesContext(file.Item1, file.Item2);
            }
        }        
    }

    private bool HasSignificantChanges(string oldContent, string newContent)
    {
        string result = _chat.ResetAndSendPrompt($"{ContextHolder.separator}OldFile"+Environment.NewLine + oldContent + Environment.NewLine + 
                                                 $"{ContextHolder.separator}NewFile"+Environment.NewLine + newContent + Environment.NewLine + "Are logic in NewFile changed dramatically against OldFile. Answer only one word: \"YES\" or \"NO\"");
        return result.ToUpper().Contains("YES");
    }

    private string BuildSolution()
    {
        return CommandRunner.RunCommand("dotnet", "build", _fileSaver.SolutionDir);
    }

    private string GameDesign()
    {
        if (_fileSaver.TryLoadTxtFile(_gameName, out var gameDesignLoaded)) 
            return gameDesignLoaded;
        
        string prompt = "Come up with a game design for a simple game (like Three in a row) for PC.";
        var design = _chat.SendPrompt(prompt);
        _fileSaver.SaveTxtFile(_gameName,design);
        return design;
    }

    private string CreateModules()
    {
        if (_fileSaver.TryLoadTxtFile(_gameName + "_Modules" , out var modulesLoaded)) 
            return modulesLoaded;
    
        string phrase = "let's try to write such a game. First, decompose application on into modules and submodules.";
        var giveMeTheAnswerInJsonFormat =
            " Give me the answer in this json format: " +
            "{\"modules\": [{\"name\": \"...\", \"description\": \"...\"}, ..., {\"name\": \"...\", \"description\": \"...\"}] " +
            "description should be detailed, description should include logic of module, description should include data types of input and output, json must contain oly name and description";

        var modulesResult = _chat.SendPrompt(phrase + giveMeTheAnswerInJsonFormat);
        _fileSaver.SaveTxtFile(_gameName + "_Modules", modulesResult);
        return modulesResult;
    }

    private void CreateModulesCode(string modulesJson)
    {
        foreach (var module in ParseModules(modulesJson))
        {
            if (_fileSaver.TryLoadTxtFile(module.Item1, out var fileContent))
            {
                _contextHolder._contexts.Add($"Code of module: {module.Item1} ```Code" + Environment.NewLine + fileContent + Environment.NewLine + " ```");
                _fileSaver.SaveCSharp(module.Item1, fileContent);
                continue;
            }

            string prompt = $"You are professional C# developer. Implement module of the game named: {module.Item1} . Module descripton: {module.Item2}";
            var moduleCode = _chat.ResetAndSendPrompt(_contextHolder.GetAllContext() + prompt);
            _fileSaver.SaveTxtFile(module.Item1, moduleCode);
            
            string[] code = OutputParser.Parse(moduleCode, "csharp");
            if (code.Length > 0)
            {
                string fullPath = _fileSaver.SaveFileInProject(module.Item1, "cs", string.Join("\n", code));
                _contextHolder.SetProjectFilesContext(fullPath, moduleCode);
            }
        }
    }

    private List<(string, string)> ParseModules(string jsonOutput)
    {
        var jsonText = string.Join(Environment.NewLine, OutputParser.Parse(jsonOutput, "json"));

        return GetJsonTupleArray(jsonText, "modules", "name", "description");
    }

    private static List<(string, string)> GetJsonTupleArray(string jsonText , string arrayName, string propertyName, string proprrtyContent)
    {
        using JsonDocument doc = JsonDocument.Parse(jsonText);
        List<(string, string)> modules = new();
        
        foreach (var element in doc.RootElement.GetProperty(arrayName).EnumerateArray())
        {
            string name = element.GetProperty(propertyName).GetString();
            string description = element.GetProperty(proprrtyContent).GetString();
            modules.Add((name, description));
        }
        
        return modules;
    }

    private void CreateProject()
    {
        if (!_fileSaver.TryLoadTxtFile("project", out var project))
        {
            string prompt = $"Create project file {_gameName}.csproj with <OutputType>Exe</OutputType> <TargetFramework>net8.0</TargetFramework> and add all package references from C# code above";
            project = _chat.ResetAndSendPrompt(_contextHolder.GetAllContext() + prompt);
            _fileSaver.SaveTxtFile("project", project);
        }
        
        string[] xml = OutputParser.Parse(project, "xml");
        string projectXml = xml.FirstOrDefault(string.Empty);
        if (projectXml.Length > 0 && projectXml.Contains("<Project"))
        {
            string fullPath = _fileSaver.SaveFileInProject(_gameName, "csproj", projectXml);
            _contextHolder.SetProjectFilesContext(fullPath, projectXml);
        }
    }

    private void CreateSolution()
    {
        var solutionFileName = Path.Combine(_fileSaver.SolutionDir, _gameName + ".sln");
        if (!_fileSaver.TryLoadFile(solutionFileName, out var solution))
        {
            GenerateSolution();
        }

        _contextHolder._contexts.Add($"```SolutionFile " + Environment.NewLine + solution + Environment.NewLine + " ```");
    }

    private void GenerateSolution()
    {
        CommandRunner.RunCommand("dotnet", $"new sln -n {_gameName}", _fileSaver.SolutionDir);
        CommandRunner.RunCommand("dotnet", $"sln add {_gameName}/{_gameName}.csproj", _fileSaver.SolutionDir);
        
        Console.WriteLine("Solution created successfully!");
    }
}