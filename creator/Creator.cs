using System.Text;
using System.Text.Json;
using OllamaSharp;

namespace Creator;

public class Creator
{
    private readonly string _gameName = "Game1";
    private readonly string _outputDir = "C:\\Users\\alexe\\Desktop\\OutputAI\\Game";
    private readonly IChat _chat;
    
    private List<string> _contexts = new List<string>();

    public Creator()
    {
        Directory.CreateDirectory(_outputDir);
        _chat = new OllamaChat();
    }

    public void Run()
    {
        string design = GameDesign();
        _contexts.Add("We are creating a new game with game design: ```GameDesign " + Environment.NewLine + design + Environment.NewLine + " ``` ");
        string modules = CreateModules();
        _contexts.Add("Game will consists these modules: ```Modules " + Environment.NewLine + modules + Environment.NewLine+ " ``` ");
        CreateModulesCode(modules);
    }
    
    private string GameDesign()
    {
        if (TryLoadFile(_outputDir,_gameName, out var gameDesignLoaded)) 
            return gameDesignLoaded;
        
        string prompt = "Come up with a game design for a simple logic game for PC.";
        var design = _chat.SendPrompt(prompt);
        SaveFile(_gameName,design);
        return design;
    }

    private string CreateModules()
    {
        if (TryLoadFile(_outputDir, _gameName + "_Modules" , out var modulesLoaded)) 
            return modulesLoaded;
    
        string phrase = "let's try to write such a game. First, decompose application on into modules and submodules.";
        var giveMeTheAnswerInJsonFormat =
            " Give me the answer in this json format: " +
            "{\"modules\": [{\"name\": \"...\", \"description\": \"...\"}, ..., {\"name\": \"...\", \"description\": \"...\"}] " +
            "description should be detailed, description should include logic of module, description should include data types of input and output, json must contain oly name and description";

        var modulesResult = _chat.SendPrompt(phrase + giveMeTheAnswerInJsonFormat);
        SaveFile(_gameName + "_Modules", modulesResult);
        return modulesResult;
    }

    private void CreateModulesCode(string modulesJson)
    {
        foreach (var module in ParseModules(modulesJson))
        {
            if (TryLoadFile(_outputDir, module.Item1, out var fileContent))
            {
                _contexts.Add($"Code of module: {module.Item1} ```Code" + Environment.NewLine + fileContent + Environment.NewLine + " ```");
                continue;
            }

            string prompt = $"You are professional C# developer. Implement module of the game named: {module.Item1} . Module descripton: {module.Item2}";
            var moduleCode = _chat.ResetAndSendPrompt(GetAllContext() + prompt);
            SaveFile(module.Item1, moduleCode);
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

    private void SaveFile(string fileName, string content)
    {
        File.WriteAllText(Path.Combine(_outputDir, fileName + ".txt"), content);
    }

    private bool TryLoadFile(string s, string fileName, out string fileContent)
    {
        if (File.Exists(Path.Combine(s, fileName + ".txt")))
        {
            fileContent = File.ReadAllText(Path.Combine(s, fileName + ".txt"));
            return true;
        }

        fileContent = string.Empty;  
        return false;
    }



}