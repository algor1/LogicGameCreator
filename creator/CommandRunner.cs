using System.Diagnostics;

namespace Creator;

public class CommandRunner
{
    public static string RunCommand(string command, string args, string workingDirectory)
    {
        var process = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            }
        };
        
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        Console.WriteLine(output);
        string error = process.StandardError.ReadToEnd();
        Console.WriteLine(error);
        process.WaitForExit();
        return output + error;
    }
}