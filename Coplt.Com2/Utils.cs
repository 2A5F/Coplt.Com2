namespace Coplt.Com2;

public static class Utils
{
    public static async ValueTask LogError(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        await Console.Error.WriteLineAsync(msg);
        Console.ResetColor();
    }
}
