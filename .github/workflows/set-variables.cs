
#:package DotMake.CommandLine@2.8.0
#:package LibGit2Sharp@0.31.0

using System.Text.RegularExpressions;
using DotMake.CommandLine;
using LibGit2Sharp;

return Cli.Run<SetVariablesCommand>(args);

[CliCommand(TreatUnmatchedTokensAsErrors = false)]
public class SetVariablesCommand
{
    [CliOption(Alias = "-debug", Required = false, Arity = CliArgumentArity.ZeroOrOne)]
    public required bool Debug { get; set; }

    [CliOption(Alias = "-appName", Required = true, Arity = CliArgumentArity.ExactlyOne)]
    public required string AppName { get; set; }

    [CliOption(Alias = "-eventName", Required = true, Arity = CliArgumentArity.ExactlyOne)]
    public required string EventName { get; set; }

    [CliOption(Alias = "-refType", Required = true, Arity = CliArgumentArity.ExactlyOne)]
    public required string RefType { get; set; }

    [CliOption(Alias = "-refName", Required = false, Arity = CliArgumentArity.ZeroOrOne)]
    public required string RefName { get; set; }

    [CliOption(Alias = "-major", Required = false, Arity = CliArgumentArity.ZeroOrOne)]
    public required string Major { get; set; }

    [CliOption(Alias = "-minor", Required = false, Arity = CliArgumentArity.ZeroOrOne)]
    public required string Minor { get; set; }

    [CliOption(Alias = "-build", Required = false, Arity = CliArgumentArity.ZeroOrOne)]
    public required string Build { get; set; }

    [CliOption(Alias = "-notes", Required = false, Arity = CliArgumentArity.ZeroOrOne)]
    public required string Notes { get; set; }

    public void Run()
    {
        var githubEnvManager = Debug
            ? GithubEnvManagerDebug.Create()
            : GithubEnvManager.Create();

        if (EventName == "push" && RefType == "tag")
        {
            var tagName = RefName;

            Console.WriteLine("Set environment variables by tag");
            Console.WriteLine($"Tag name: {tagName}");

            var tagRegex = new Regex(@"v(?<major>[^.]+)\.(?<minor>[^.]+)(?:\.(?<build>[^.]+))?");
            var tagMatch = tagRegex.Match(tagName);
            if (!tagMatch.Success)
                throw new ArgumentException(nameof(tagName));

            Major = tagMatch.Groups["major"].Value;
            Minor = tagMatch.Groups["minor"].Value;
            Build = tagMatch.Groups["build"].Value;

            var noteString = GitTools.GetTagMessage(tagName);
            var noteLines = new List<string>();
            using (var noteReader = new StringReader(noteString))
            {
                while (true)
                {
                    var noteLine = noteReader.ReadLine();
                    if (noteLine == null)
                        break;
                    if (!string.IsNullOrEmpty(noteLine))
                        noteLines.Add($"- {noteLine}");
                }
            }

            if (noteLines.Any())
            {
                noteLines.Insert(0, "Release notes:");
                Notes = string.Join(Environment.NewLine, noteLines);
            }
        }

        Console.WriteLine($"major: {Major}");
        Console.WriteLine($"minor: {Minor}");
        Console.WriteLine($"build: {Build}");
        Console.WriteLine($"notes: {Notes}");

        if (string.IsNullOrEmpty(Major) ||
            string.IsNullOrEmpty(Minor))
        {
            throw new Exception("One or more parameters is empty");
        }

        var version = $"{Major}.{Minor}";
        if (!string.IsNullOrEmpty(Build))
            version += $".{Build}";

        var copyright = $"© {DateTime.Now.Year} jukryt";

        if (string.IsNullOrEmpty(Notes))
            Notes = $"{AppName} v{version}";

        var artifactName = $"{AppName}.{version}";
        var artifactFileName = $"{artifactName}.zip";

        githubEnvManager.SetValue("app_major", Major);
        githubEnvManager.SetValue("app_minor", Minor);
        githubEnvManager.SetValue("app_build", Build);
        githubEnvManager.SetValue("app_version", version);
        githubEnvManager.SetValue("copyright", copyright);
        githubEnvManager.SetValue("release_notes", Notes);
        githubEnvManager.SetValue("artifact_name", artifactName);
        githubEnvManager.SetValue("artifact_file_name", artifactFileName);
        githubEnvManager.Save();
    }
}

internal interface IGithubEnvManager
{
    void SetValue(string key, string value);
    void Save();
}

internal class GithubEnvManager : IGithubEnvManager
{
    private const string DELIMITER = "EOF";

    public static IGithubEnvManager Create()
    {
        var githubEnvFilePath = Environment.GetEnvironmentVariable("GITHUB_ENV");
        if (string.IsNullOrEmpty(githubEnvFilePath))
            throw new Exception("GITHUB_ENV is empty");

        if (!File.Exists(githubEnvFilePath))
            throw new Exception("GITHUB_ENV file not found");

        return new GithubEnvManager(githubEnvFilePath);
    }

    private readonly string _githubEnvFilePath;
    private readonly Dictionary<string, string> _variables;

    private GithubEnvManager(string githubEnvFilePath)
    {
        _githubEnvFilePath = githubEnvFilePath;
        _variables = [];
    }

    public void SetValue(string key, string value)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        _variables[key] = value ?? string.Empty;
    }

    public void Save()
    {
        var lines = new List<string>();
        foreach (var (key, value) in _variables)
        {
            if (string.IsNullOrEmpty(value))
                lines.Add($"{key}=");
            else if (value.Contains(Environment.NewLine))
                lines.Add($"{key}<<{DELIMITER}{Environment.NewLine}{value}{Environment.NewLine}{DELIMITER}");
            else
                lines.Add($"{key}={value}");
        }

        File.WriteAllText(_githubEnvFilePath, string.Join(Environment.NewLine, lines));
    }
}

internal class GithubEnvManagerDebug : IGithubEnvManager
{
    public static IGithubEnvManager Create()
    {
        return new GithubEnvManagerDebug();
    }

    public void SetValue(string key, string value)
    {
        Console.WriteLine($"Set: {key}, value: {value}");
    }

    public void Save()
    {
    }
}

internal static class GitTools
{
    public static string GetTagMessage(string tagName)
    {
        GlobalSettings.SetOwnerValidation(false);
        using var repo = new Repository(Path.GetFullPath("."));
        var tag = repo.Tags[tagName];
        var annotation = tag?.Annotation;
        if (annotation == null)
            return string.Empty;

        return annotation.Message ?? string.Empty;
    }
}
