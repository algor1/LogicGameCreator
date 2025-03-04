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

    public void SaveCSharp(string fileName, string content)
    {
        if (content.Contains("```csharp"))
        {
            List<string> code = new List<string>();
            int startIndex = content.IndexOf("```csharp");
            while (true)
            {
                var beginIndex = content.IndexOf("```csharp", startIndex);
                if (beginIndex == -1)
                    break;
                
                var endIndex = content.IndexOf("```", beginIndex + 9);
                if (endIndex == -1)
                    break;
                var line = content.Substring(beginIndex + 9, endIndex - beginIndex - 9);
                code.Add(line);
                startIndex = endIndex;
            }

            File.WriteAllText(Path.Combine(_outputDir, fileName + ".cs"), string.Join("\n", code));
        }
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