using System.Text;
using System.Text.Json;
using OllamaSharp;

namespace Creator;

public class Creator
{
    private readonly string _gameName = "Game1";
    private readonly string _outputDir = "C:\\Users\\alexe\\Desktop\\OutputAI\\Game";
    private readonly IChat _chat;
    
    private List<string> _context = new List<string>();

    public Creator()
    {
        Directory.CreateDirectory(_outputDir);
        _chat = new OllamaChat();
    }

    public void Run()
    {
        string design = GameDesign();
        _context.Add(design);
        string modules = CreateModules(design);
        CreateModulesCode(modules, design);
    }
    
    private string GameDesign()
    {
        if (TryLoadFile(_outputDir,_gameName, out var gameDesignLoaded)) 
            return gameDesignLoaded;
        
        string prompt = "Come up with a design for a simple logic game";
        var design = _chat.SendPrompt(prompt);
        SaveFile(_gameName,design);
        return design;
    }

    private string CreateModules(string gameDesign)
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

    private void CreateModulesCode(string modulesJson, string design)
    {
        foreach (var module in ParseModules(modulesJson))
        {
            if (TryLoadFile(_outputDir, module.Item1 , out var _)) 
                continue;
            string context = "We are creating a new game with game design: ```GameDesign " + design + " ``` ";
            string prompt = $"You are professional C# developer. Implement module of the game named: {module.Item1} . Modole descripton: {module.Item2}";
            var moduleCode = _chat.ResetAndSendPrompt(context+ prompt);
            SaveFile(module.Item1, moduleCode);
        }
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
            string prompt = $"Read this context and don' answer anything : {fileContent}";
            _chat.SendContext(prompt);
            return true;
        }

        fileContent = string.Empty;  
        return false;
    }



}