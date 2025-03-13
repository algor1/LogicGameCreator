using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Text.Json.Serialization;
using OllamaSharp;

namespace Creator;

public class Creator
{
    private readonly string _gameName = "Game2";
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
                using (_chat = new OpenAiChat())
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
                        if (!buildResult.Contains("Build FAILED"))
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
        if (!_fileSaver.TryLoadTxtFile("FilesToFix", out var aiResult))
        {
            string prompt =
                $"I have errors in building the solution. ```Errors {Environment.NewLine}{buildErrors} ```{Environment.NewLine}" +
                "Fix the errors and provide the full corrected content of the affected files." + Environment.NewLine +
                "Return only the corrected file contents in the following format without examples of usage, explanations, comments, or any additional text:" +
                Environment.NewLine +
                "```FILE: <fullPath>" + " " + OutputParser.Delimetr + Environment.NewLine +
                "<file contents> ```" + Environment.NewLine +
                "Ensure that the format is clean and properly structured.";

            var allProjectFilesContext = _contextHolder.GetAllProjectFilesContext();
            aiResult = _chat.ResetAndSendPrompt(allProjectFilesContext + prompt);
            _fileSaver.SaveTxtFile("FilesToFix", aiResult);
        }

        var filesToFix = OutputParser.ParseFiles(aiResult);
        foreach (var file in filesToFix)
        {
            if (true)//!HasSignificantChanges(oldContent, file.Item2))
            {
                File.WriteAllText(file.Key, file.Value);
                _contextHolder.SetProjectFilesContext(file.Key, file.Value);
            }
        }
        _fileSaver.RemoveTxtFile("FilesToFix");
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
        
        string prompt = "Come up with a game design for a very simple game with balls on small board, like Three in a row or etc, for PC. The rules should be very simple. Game should be usable for kids. ";
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

        var modulesResult = _chat.SendPrompt(_contextHolder.GetAllContext()+ phrase + giveMeTheAnswerInJsonFormat);
        _fileSaver.SaveTxtFile(_gameName + "_Modules", modulesResult);
        return modulesResult;
    }

    private void CreateModulesCode(string modulesJson)
    {
        foreach (var module in ParseModules(modulesJson))
        {
            if (!_fileSaver.TryLoadTxtFile(module.Item1, out var moduleCode))
            {
                string prompt = $"You are professional C# developer. Implement module of the game named: {module.Item1} . Module descripton: {module.Item2}";
                moduleCode = _chat.ResetAndSendPrompt(_contextHolder.GetAllContext() + prompt);
                _fileSaver.SaveTxtFile(module.Item1, moduleCode);
            }

            _contextHolder._contexts.Add($"Code of module: {module.Item1} ```Code" + Environment.NewLine + moduleCode + Environment.NewLine + " ```");
            
            string[] code = OutputParser.Parse(moduleCode, "csharp");
            if (code.Length > 0)
            {
                var content = string.Join("\n", code);
                string fullPath = _fileSaver.SaveFileInProject(module.Item1, "cs", content);
                _contextHolder.SetProjectFilesContext(fullPath, content);
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
            string prompt = $"Create project file {_gameName}.csproj with <OutputType>Exe</OutputType> <TargetFramework>net8.0</TargetFramework> and add all package references from C# code above. Do not include links on files";
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