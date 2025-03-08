using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OllamaSharp;

namespace Creator;

public class Creator
{
    private readonly string _gameName = "Game1";
    private IChat _chat;
    
    private List<string> _contexts = new List<string>();
    private readonly FileSaver _fileSaver;

    public Creator()
    {
        _fileSaver = new FileSaver(_gameName, _gameName);
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
                    _contexts.Add("We are creating a new game with game design: ```GameDesign " + Environment.NewLine +
                                  design +
                                  Environment.NewLine + " ``` ");
                    string modules = CreateModules();
                    _contexts.Add("Game will consists these modules: ```Modules " + Environment.NewLine + modules +
                                  Environment.NewLine + " ``` ");
                    CreateModulesCode(modules);
                    CreateProject();
                    CreateSolution();
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

    private void CreateProject()
    {
        if (!_fileSaver.TryLoadFile("project", out var project))
        {
            string prompt = $"Create project file {_gameName}.csproj with <OutputType>Exe</OutputType> <TargetFramework>net8.0</TargetFramework> and add all package references from C# code above";
            project = _chat.ResetAndSendPrompt(GetAllContext() + prompt);
            _fileSaver.SaveFile("project", project);
        }
        
        string[] xml = OutputParser.Parse(project, "xml");
        string projectXml = xml.FirstOrDefault(string.Empty);
        if (projectXml.Length > 0 && projectXml.Contains("<Project"))
            _fileSaver.SaveFile(_gameName, "csproj", projectXml);
        _contexts.Add($"```ProjectFile " + Environment.NewLine + project + Environment.NewLine + " ```");
    }

    private void CreateSolution()
    {
        var solutionFileName = Path.Combine(_fileSaver.SolutionDir, _gameName + ".sln");
        if (!_fileSaver.TryLoadFile(solutionFileName, out var solution))
        {
            GenerateSolution();
        }
        
        _contexts.Add($"```SolutionFile " + Environment.NewLine + solution + Environment.NewLine + " ```");
    }

    private void GenerateSolution()
    {
        RunCommand("dotnet", $"new sln -n {_gameName}", _fileSaver.SolutionDir);
        RunCommand("dotnet", $"sln add {_gameName}/{_gameName}.csproj", _fileSaver.SolutionDir);
        
        Console.WriteLine("Solution created successfully!");
    }

    private string GameDesign()
    {
        if (_fileSaver.TryLoadFile(_gameName, out var gameDesignLoaded)) 
            return gameDesignLoaded;
        
        string prompt = "Come up with a game design for a simple game (like Three in a row) for PC.";
        var design = _chat.SendPrompt(prompt);
        _fileSaver.SaveFile(_gameName,design);
        return design;
    }

    private string CreateModules()
    {
        if (_fileSaver.TryLoadFile(_gameName + "_Modules" , out var modulesLoaded)) 
            return modulesLoaded;
    
        string phrase = "let's try to write such a game. First, decompose application on into modules and submodules.";
        var giveMeTheAnswerInJsonFormat =
            " Give me the answer in this json format: " +
            "{\"modules\": [{\"name\": \"...\", \"description\": \"...\"}, ..., {\"name\": \"...\", \"description\": \"...\"}] " +
            "description should be detailed, description should include logic of module, description should include data types of input and output, json must contain oly name and description";

        var modulesResult = _chat.SendPrompt(phrase + giveMeTheAnswerInJsonFormat);
        _fileSaver.SaveFile(_gameName + "_Modules", modulesResult);
        return modulesResult;
    }

    private void CreateModulesCode(string modulesJson)
    {
        foreach (var module in ParseModules(modulesJson))
        {
            if (_fileSaver.TryLoadFile(module.Item1, out var fileContent))
            {
                _contexts.Add($"Code of module: {module.Item1} ```Code" + Environment.NewLine + fileContent + Environment.NewLine + " ```");
                _fileSaver.SaveCSharp(module.Item1, fileContent);
                continue;
            }

            string prompt = $"You are professional C# developer. Implement module of the game named: {module.Item1} . Module descripton: {module.Item2}";
            var moduleCode = _chat.ResetAndSendPrompt(GetAllContext() + prompt);
            _fileSaver.SaveFile(module.Item1, moduleCode);
            _contexts.Add($"Code of module: {module.Item1} ```Code" + Environment.NewLine + moduleCode + Environment.NewLine + " ```");
        }
    }

    private string GetAllContext()
    {
        StringBuilder result = new StringBuilder();
        foreach (var context in _contexts)
        {
            result.AppendLine(context);
        }
        return result.ToString();
    }

    private List<(string, string)> ParseModules(string jsonOutput)
    {
        var json = OutputParser.Parse(jsonOutput, "json");
        using JsonDocument doc = JsonDocument.Parse(json.First());
        List<(string, string)> modules = new();
        
        foreach (var element in doc.RootElement.GetProperty("modules").EnumerateArray())
        {
            string name = element.GetProperty("name").GetString();
            string description = element.GetProperty("description").GetString();
            modules.Add((name, description));
        }
        
        return modules;
    }
    
    static void RunCommand(string command, string args, string workingDirectory)
    {
        var process = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            }
        };
        
        process.Start();
        Console.WriteLine(process.StandardOutput.ReadToEnd());
        Console.WriteLine(process.StandardError.ReadToEnd());
        process.WaitForExit();
    }
}