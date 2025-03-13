namespace Creator;

public static class OutputParser
{
    private const string Separator = "```";
    public const string Delimetr = "54693253-0db9-4334-bf25-b6a83137125a";

    public static string[] Parse(string content, string contentType)
    {
        List<string> code = new List<string>();
        if (content.Contains(Separator + contentType))
        {
            var contentTypeLength = contentType.Length+ Separator.Length;
            int startIndex = content.IndexOf(Separator + contentType);
            while (true)
            {
                var beginIndex = content.IndexOf(Separator + contentType, startIndex)+ contentTypeLength;
                if (beginIndex == -1 + contentTypeLength)
                    break;
                
                var endIndex = content.IndexOf(Separator, beginIndex);
                if (endIndex == -1)
                    break;
                
                var line = content.Substring(beginIndex , endIndex - beginIndex);
                code.Add(line);
                startIndex = endIndex + Separator.Length;
            }
        }
        return code.ToArray();
    }
    
    public static Dictionary<string, string> ParseFiles(string input)
    {
        var files = new Dictionary<string, string>();

        var parts = Parse(input, "FILE:");
        foreach (var part in parts)
        {
            var lines = part.Split(new[] { Delimetr }, 2, StringSplitOptions.None);
            if (lines.Length < 2) continue;

            string filePath = lines[0].Trim();
            string fileContents = lines[1].Trim();

            files[filePath] = fileContents;
        }

        return files;
    }
}