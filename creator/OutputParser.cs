namespace Creator;

public static class OutputParser
{
    private const string Separator = "```";

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
}