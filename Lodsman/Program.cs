using DotMake.CommandLine;
using Lodsman.CliRunner;

return await Cli.RunAsync<RootCommand>(args, new CliSettings { EnableDefaultExceptionHandler = true });
