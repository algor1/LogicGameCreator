using OpenAI;
using OpenAI.Chat;

namespace Creator;

public class OpenAiChat : IChat
{
    private ChatClient _client;

    public OpenAiChat()
    {
        var key = File.ReadAllText("C:\\Users\\alexe\\Desktop\\AIkey.txt");
        var openAiClientOptions = new OpenAIClientOptions();
        _client = new ChatClient(model: "gpt-4o-mini", apiKey: key);
    }
    public string SendPrompt(string input)
    {
        ChatCompletion completion = _client.CompleteChat(input);
        Console.WriteLine(completion.Content[0].Text);
        return completion.Content[0].Text;
    }

    public void SendContext(string prompt)
    {
        ChatCompletion completion = _client.CompleteChat(prompt);
        Console.WriteLine(completion.Content[0].Text);
    }

    public string ResetAndSendPrompt(string input)
    {
        throw new NotImplementedException();
    }
}