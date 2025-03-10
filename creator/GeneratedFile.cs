namespace Creator;

public record GeneratedFile(string FullPath, string Content)
{
    private const string Separator = "```";
    public override string ToString()
    {
        return Separator + " " + FullPath + Environment.NewLine +
            Content + Environment.NewLine + Separator;
    }
}



public record ContextOfFile(string Name, GeneratedFile ProjectFile, GeneratedFile AiText);
