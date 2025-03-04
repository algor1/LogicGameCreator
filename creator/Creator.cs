using System.Text;
using System.Text.Json;
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
        _fileSaver = new FileSaver();
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

    private List<(string, string)> ParseModules(string json)
    {
        json = json.Replace("```json", "");
        json = json.Replace("```", "");
        using JsonDocument doc = JsonDocument.Parse(json);
        List<(string, string)> modules = new();
        
        foreach (var element in doc.RootElement.GetProperty("modules").EnumerateArray())
        {
            string name = element.GetProperty("name").GetString();
            string description = element.GetProperty("description").GetString();
            modules.Add((name, description));
        }
        
        return modules;
    }
}