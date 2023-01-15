namespace FluxGuardian.Helpers;

public static class Logger
{
    public static void Log(string message)
    {
        Console.WriteLine($"({DateTime.UtcNow} UTC) {message}");
    }

    public static void LogMessage(string message)
    {
        var msg = $"({DateTime.UtcNow} UTC) {message}";
        File.AppendAllLines(Directory.GetCurrentDirectory() + "/allMessages.log", new [] { msg});
    }
}