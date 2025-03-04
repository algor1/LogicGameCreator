using System.Text;
using OllamaSharp;

namespace Creator;

public interface IChat: IDisposable
{
    string SendPrompt(string input);
    void SendContext(string prompt);
    string ResetAndSendPrompt(string input);
}

public class OllamaChat : IChat 
{
    private Uri _uri = new Uri("http://localhost:11434");
    private Chat _chat;
    private OllamaApiClient _ollama;
    

    public OllamaChat()
    {
        _chat = ChatCreation();
    }

    public string SendPrompt(string input)
    {
        return SendPromptInternal(input).Result;
    }
    
    public string ResetAndSendPrompt(string input)
    {
        _chat = ChatCreation();
        return SendPromptInternal(input).Result;
    }
    
    private async Task<string> SendPromptInternal(string input)
    {
        var stringBuilder = new StringBuilder();
        await foreach (var answerToken in _chat.SendAsync(input))
            WriteConsole(stringBuilder, answerToken);
        return stringBuilder.ToString();
    }
    
    private Chat ChatCreation()
    {
        _ollama?.Dispose();
        _ollama = new OllamaApiClient(_uri);
        _ollama.SelectedModel = "phi4";
        return new Chat(_ollama);
    }
    
    private void WriteConsole(StringBuilder stringBuilder, string answerToken)
    {
        stringBuilder.Append(answerToken);
        Console.Write(answerToken);
    }

    public void SendContext(string prompt)
    {
        _chat.SendAsync(prompt);
    }

    public void Dispose()
    {
        _ollama.Dispose();
    }
}