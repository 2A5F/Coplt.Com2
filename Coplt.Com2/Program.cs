using System.CommandLine;
using System.CommandLine.Help;
using Coplt.Com2;

RootCommand root = new("Tools for generating COM-related code");

root.Options.Clear();
root.Options.Add(new HelpOption());
root.Options.Add(new VersionOption("--version", "-v"));
var opt_config_path = new Option<FileInfo>("--config", "-c")
{
    Description = "Configuration file",
    DefaultValueFactory = _ => new FileInfo("./co_com.json")
};
root.Options.Add(opt_config_path);
root.Action = new GenAction(opt_config_path);
var arg_new_path = new Argument<FileInfo>("path")
{
    DefaultValueFactory = _ => new FileInfo("./co_com.json"),
};
root.Subcommands.Add(new Command("new", "new default config.json")
{
    Action = new NewConfigAction(arg_new_path),
    Arguments = { arg_new_path },
});

await root.Parse(args).InvokeAsync();
