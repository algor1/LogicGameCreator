using System.Text;

namespace Creator;

public class ContextHolder
{
    public List<string> _contexts = new List<string>();
    public List<ContextOfFile> _profectFilesContexts = new List<ContextOfFile>();
    private const string separator = "```";

    public ContextHolder()
    {
    }

    public string GetAllContext()
    {
        StringBuilder result = new StringBuilder();
        foreach (var context in _contexts)
        {
            result.AppendLine(context);
        }
        return result.ToString();
    }

    public void AddGameDesign(string design)
    {
        string prompt = "We are creating a new game with game design:";
        string name = "GameDesign";
        AddAiText(name, prompt, design);
        
    }

    private void AddAiText(string name, string prompt, string content)
    {
        _contexts.Add(prompt + " " + separator + name + Environment.NewLine +
                      content +
                      Environment.NewLine + separator);
    }
    

    public string GetAllProjectFilesContext()
    {
        string prompt = "Here all files in my solution are:" + Environment.NewLine;
        string context = string.Join(Environment.NewLine, _profectFilesContexts.Where(c => c.ProjectFile != null).Select(c => c.ProjectFile.ToString()));
        return prompt + context;
    }
}