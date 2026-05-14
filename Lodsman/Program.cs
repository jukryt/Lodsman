using DotMake.CommandLine;
using Lodsman.CliRunner;

return await Cli.RunAsync<RootAppRunner>(args, new CliSettings { EnableDefaultExceptionHandler = true });
