using System.ComponentModel.Design;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Text.Json.Serialization;
using OllamaSharp;

namespace Creator;

public class Creator
{
    private readonly string _gameName = "Game3";

    private readonly FileSaver _fileSaver;
    private readonly ContextHolder _contextHolder;
    private Ai _ai;

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
                using (_ai = new Ai(_fileSaver, _contextHolder))
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

    

    private string BuildSolution()
    {
        return CommandRunner.RunCommand("dotnet", "build", _fileSaver.SolutionDir);
    }

    private string GameDesign()
    {
        
        string prompt = "Come up with a game design for a very simple game with balls on small board, like Three in a row or etc, for PC. The rules should be very simple. Game should be usable for kids. ";
        string design = _ai.AskOrLoad(_gameName, prompt, ContextType.AllContext);
        return design;
    }

    private string CreateModules()
    {
        string fileName = _gameName + "_Modules";
        string phrase = "let's try to write such a game. First, decompose application on into modules and submodules.";
        var giveMeTheAnswerInJsonFormat =
            " Give me the answer in this json format: " +
            "{\"modules\": [{\"name\": \"...\", \"description\": \"...\"}, ..., {\"name\": \"...\", \"description\": \"...\"}] " +
            "description should be detailed, description should include logic of module, description should include data types of input and output, json must contain oly name and description";
        
        string modulesResult = _ai.AskOrLoad(fileName, phrase + giveMeTheAnswerInJsonFormat, ContextType.AllContext );
        return modulesResult;
    }

    private void CreateModulesCode(string modulesJson)
    {
        foreach (var module in OutputParser.ParseModules(modulesJson))
        {
            string prompt = $"You are professional C# developer. Implement module of the game named: {module.Item1} . Module descripton: {module.Item2}";
            var moduleCode = _ai.AskOrLoad(module.Item1, prompt, ContextType.AllContext );
            
            string[] code = OutputParser.Parse(moduleCode, "csharp");
            if (code.Length > 0)
            {
                var content = string.Join("\n", code);
                string fullPath = _fileSaver.SaveFileInProjectIfNotExixst(module.Item1, "cs", content);
                _contextHolder.SetProjectFilesContext(fullPath, content);
            }
        }
    }

    private void CreateProject()
    {
        var fileName = "project";
        string prompt = $"Create project file {_gameName}.csproj with <OutputType>Exe</OutputType> <TargetFramework>net8.0</TargetFramework> and add all package references from C# code above. Do not include links on files";
 

        string project = _ai.AskOrLoad(fileName, prompt, ContextType.AllContext);
            
        string[] xml = OutputParser.Parse(project, "xml");
        string projectXml = xml.FirstOrDefault(string.Empty);
        if (projectXml.Length > 0 && projectXml.Contains("<Project"))
        {
            string fullPath = _fileSaver.SaveFileInProjectIfNotExixst(_gameName, "csproj", projectXml);
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
    
    private void FixBuild(string buildErrors)
    {
        string fileName = "FilesToFix";
        string prompt = $"I have errors in building the solution. ```Errors {Environment.NewLine}{buildErrors} ```{Environment.NewLine}" + 
                        "Fix only the errors in the affected files without changing anything else in the code." + Environment.NewLine +
                        "Return only the corrected file contents in the following format, preserving all original class structures, methods, and logic:" +
                        Environment.NewLine +
                        "```FILE: <fullPath>" + " " + OutputParser.Delimetr + Environment.NewLine +
                        "<corrected file contents> ```" + Environment.NewLine +
                        "Do not rename or restructure any classes, methods, or variables unless absolutely necessary to fix the errors." +
                        "Do not introduce new namespaces, modify method signatures, or change logic beyond the necessary fixes." +
                        "Ensure that the output preserves all original formatting and structure.";
        
        var aiResult = _ai.AskOrLoad(fileName, prompt, ContextType.ProjectFiles );
        
        var filesToFix = OutputParser.ParseFiles(aiResult);
        foreach (var file in filesToFix)
        {
            if (true)//!HasSignificantChanges(oldContent, file.Item2))
            {
                File.WriteAllText(file.Key, file.Value);
                _contextHolder.SetProjectFilesContext(file.Key, file.Value);
            }
        }
        _fileSaver.RemoveTxtFile(fileName);
    }
}

public enum ContextType
{
    ProjectFiles = 1,
    AllContext,
}