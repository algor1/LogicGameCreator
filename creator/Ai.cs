namespace Creator;

public class Ai: IDisposable
{
    public IChat _chat;
    private FileSaver _fileSaver;
    ContextHolder _contextHolder;

    public Ai(FileSaver fileSaver, ContextHolder contextHolder)
    {
        _fileSaver = fileSaver;
        _contextHolder = contextHolder;
        _chat = new OpenAiChat();
    }

    public string AskOrLoad(string fileName, string prompt, ContextType contextType)
    {
        if (!_fileSaver.TryLoadTxtFile(fileName, out var aiResult))
        {
            var context = _contextHolder.GetContext(contextType);
            aiResult = _chat.ResetAndSendPrompt(context + prompt);
            _fileSaver.SaveTxtFile(fileName, aiResult);
        }

        return aiResult;
    }
    
    public bool HasSignificantChanges(string oldContent, string newContent)
    {
        string result = _chat.ResetAndSendPrompt($"{ContextHolder.separator}OldFile"+Environment.NewLine + oldContent + Environment.NewLine + 
                                                     $"{ContextHolder.separator}NewFile"+Environment.NewLine + newContent + Environment.NewLine + "Are logic in NewFile changed dramatically against OldFile. Answer only one word: \"YES\" or \"NO\"");
        return result.ToUpper().Contains("YES");
    }

    public void Dispose()
    {
        _chat.Dispose();
    }
}