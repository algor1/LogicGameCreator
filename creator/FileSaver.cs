namespace Creator;

public class FileSaver
{
    private readonly string _outputDir = "C:\\Users\\alexe\\Desktop\\OutputAI";
    public readonly string SolutionDir;
    private readonly string _projectDir;

    public FileSaver(string solutionName, string projectName)
    {
        SolutionDir = Path.Combine(_outputDir,solutionName);
        _projectDir = Path.Combine(SolutionDir, projectName);
        Directory.CreateDirectory(_projectDir);
    }

    public void SaveFile(string fileName, string content)
    {
        File.WriteAllText(Path.Combine(_projectDir, fileName + ".txt"), content);
        SaveCSharp(fileName, content);
    }
    
    public void SaveFile(string fileName , string extension, string content)
    {
        File.WriteAllText(Path.Combine(_projectDir, fileName + "." + extension), content);
    }

    public void SaveCSharp(string fileName, string content)
    {
        string[] code = OutputParser.Parse(content, "csharp");
        if (code.Length > 0)
            SaveFile(fileName, "cs", string.Join("\n", code));
    }

    public bool TryLoadTxtFile(string fileName, out string fileContent)
    {
        return TryLoadFile(fileName +".txt", out fileContent);
    }

    public bool TryLoadFile(string fileName, out string fileContent)
    {
        if (File.Exists(Path.Combine(_projectDir, fileName)))
        {
            fileContent = File.ReadAllText(Path.Combine(_projectDir, fileName));
            return true;
        }

        fileContent = string.Empty;  
        return false;
    }
}