using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;
using Coplt.Com2.Symbols;

namespace Coplt.Com2;

public class GenAction(Option<FileInfo> ConfigPath) : AsynchronousCommandLineAction
{
    public override async Task<int> InvokeAsync(ParseResult result, CancellationToken cancel = default)
    {
        Config config;
        {
            var config_path = result.GetValue(ConfigPath)!;
            if (!config_path.Exists)
            {
                await Utils.LogError($"Config not exists; path: {config_path}");
                return -1;
            }
            await using var config_file = config_path.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            config = (await JsonSerializer.DeserializeAsync<Config>(config_file, ConfigLoadJsonContext.Default.Config, cancel))!;
        }

        // Console.WriteLine(config);

        if (config.Inputs.Count == 0)
        {
            await Utils.LogError($"No input in config");
            return -1;
        }

        var db = new SymbolDb();

        foreach (var input in config.Inputs)
        {
            var ext = Path.GetExtension(input);
            switch (Path.GetExtension(input))
            {
                case ".dll":
                    db.Load(input);
                    break;
                case ".json":
                    // todo
                    break;
                default:
                    await Utils.LogError($"unknown file extension: {ext} at {input}");
                    break;
            }
        }

        var com_define = db.ToComDefine();

        return await Task.FromResult(0);
    }
}
