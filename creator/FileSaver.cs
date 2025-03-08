namespace Creator;

public class FileSaver
{
    private readonly string _outputDir = "C:\\Users\\alexe\\Desktop\\OutputAI\\Game";

    public FileSaver()
    {
        Directory.CreateDirectory(_outputDir);
    }

    public void SaveFile(string fileName, string content)
    {
        File.WriteAllText(Path.Combine(_outputDir, fileName + ".txt"), content);
        SaveCSharp(fileName, content);
    }
    
    public void SaveFile(string fileName , string extension, string content)
    {
        File.WriteAllText(Path.Combine(_outputDir, fileName + "." + extension), content);
    }

    public void SaveCSharp(string fileName, string content)
    {
        string[] code = OutputParser.Parse(content, "csharp");
        if (code.Length > 0)
            SaveFile(fileName, "cs", string.Join("\n", code));
    }

    public bool TryLoadFile(string fileName, out string fileContent)
    {
        if (File.Exists(Path.Combine(_outputDir, fileName + ".txt")))
        {
            fileContent = File.ReadAllText(Path.Combine(_outputDir, fileName + ".txt"));
            return true;
        }

        fileContent = string.Empty;  
        return false;
    }
}