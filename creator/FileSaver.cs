namespace Creator;

public class FileSaver
{
    private readonly string _outputDir = "C:\\Users\\alexe\\Desktop\\OutputAI";
    private readonly string _aiTextAnswerstDir;
    public readonly string SolutionDir;
    private readonly string _projectDir;

    public FileSaver(string solutionName, string projectName)
    {
        SolutionDir = Path.Combine(_outputDir,solutionName);
        _projectDir = Path.Combine(SolutionDir, projectName);
        Directory.CreateDirectory(_projectDir);
        _aiTextAnswerstDir = Path.Combine(_outputDir, solutionName + "_AItext");
        Directory.CreateDirectory(_aiTextAnswerstDir);
    }

    public void SaveTxtFile(string fileName, string content)
    {
        File.WriteAllText(Path.Combine(_aiTextAnswerstDir, fileName + ".txt"), content);
    }
    
    public string SaveFileInProject(string fileName , string extension, string content)
    {
        var fullPath = Path.Combine(_projectDir, fileName + "." + extension);
        if (!File.Exists(fullPath))
        {
            File.WriteAllText(fullPath, content);
        }
        return fullPath;
    }

    public string SaveCSharp(string fileName, string content)
    {
        string[] code = OutputParser.Parse(content, "csharp");
        if (code.Length > 0)
            return SaveFileInProject(fileName, "cs", string.Join("\n", code));
        
        return string.Empty;
    }

    public bool TryLoadTxtFile(string fileName, out string fileContent)
    {
        return TryLoadFile(Path.Combine(_aiTextAnswerstDir, fileName) +".txt", out fileContent);
    }

    public bool TryLoadFile(string fullPath, out string fileContent)
    {
        if (File.Exists( fullPath))
        {
            fileContent = File.ReadAllText(fullPath);
            return true;
        }

        fileContent = string.Empty;  
        return false;
    }
}