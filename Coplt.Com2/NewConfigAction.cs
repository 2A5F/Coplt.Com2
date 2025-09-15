using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;

namespace Coplt.Com2;

public class NewConfigAction(Argument<FileInfo> Path) : AsynchronousCommandLineAction
{
    public override async Task<int> InvokeAsync(ParseResult result, CancellationToken cancel = default)
    {
        var path = result.GetValue(Path)!;
        await using var file = path.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        await JsonSerializer.SerializeAsync(file, new Config(), ConfigSaveJsonContext.Default.Config, cancel);
        return 0;
    }
}
