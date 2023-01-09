namespace FluxGuardian.Helpers;

public static class Logger
{
    public static void Log(string message)
    {
        Console.WriteLine($"({DateTime.UtcNow} UTC) {message}");
    }
}