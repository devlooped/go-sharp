using System.Diagnostics;

namespace Devlooped;

public static class ProcessRunner
{
    public static Task<int> PublishAsync(string dotnet, string cs, string config, string targets, string publishDir, IReadOnlyList<string>? dotnetArgs = null)
    {
        var environment = CreateGoEnvironment(config, targets);

        var arguments = new List<string> { "publish", "--ucr", cs };
        if (dotnetArgs is not null)
            arguments.AddRange(dotnetArgs);
        arguments.Add($"/p:PublishDir={publishDir}");

        return RunAsync(dotnet, arguments, environment);
    }

    public static Task<int> DotnetCleanAsync(string dotnet, string cs)
        => RunAsync(dotnet, ["clean", cs], environment: null);

    public static Task<int> DotnetRunAsync(string dotnet, string cs, string config, string targets, IReadOnlyList<string>? dotnetArgs, IReadOnlyList<string>? appArgs)
    {
        var environment = CreateGoEnvironment(config, targets);

        var arguments = new List<string> { "run", cs };
        if (dotnetArgs is not null)
            arguments.AddRange(dotnetArgs);

        if (appArgs is { Count: > 0 })
        {
            arguments.Add("--");
            arguments.AddRange(appArgs);
        }

        return RunAsync(dotnet, arguments, environment);
    }

    public static Task<int> DotnetExecAsync(string dotnet, string assembly, IReadOnlyList<string>? appArgs)
    {
        var arguments = new List<string> { assembly };
        if (appArgs is not null)
            arguments.AddRange(appArgs);

        return RunAsync(dotnet, arguments, environment: null);
    }

    public static Task<int> RunAsync(string appPath, IReadOnlyList<string>? extraArgs = null)
        => RunAsync(appPath, extraArgs, environment: null);

    public static async Task<int> RunAsync(string fileName, IReadOnlyList<string>? arguments, IReadOnlyDictionary<string, string>? environment)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
        };

        if (arguments is not null)
        {
            foreach (var arg in arguments)
                startInfo.ArgumentList.Add(arg);
        }

        // Always expose the muxer go resolved so apps can re-invoke `dotnet` (incl. native AOT).
        foreach (var (key, value) in CreateDotNetHostEnvironment())
            startInfo.Environment[key] = value;

        if (environment is not null)
        {
            foreach (var (key, value) in environment)
                startInfo.Environment[key] = value;
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start process: {fileName}");

        await process.WaitForExitAsync();

        return process.ExitCode;
    }

    /// <summary>
    /// Environment entries that point child processes at the same <c>dotnet</c> host go uses.
    /// Sets <c>DOTNET_HOST_PATH</c> and <c>DOTNET_ROOT</c> when a muxer path is available.
    /// </summary>
    internal static IReadOnlyDictionary<string, string> CreateDotNetHostEnvironment(string? muxerPath = null)
    {
        muxerPath ??= DotnetMuxer.Path?.FullName;
        if (string.IsNullOrEmpty(muxerPath))
            return new Dictionary<string, string>();

        var env = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["DOTNET_HOST_PATH"] = muxerPath,
        };

        var root = Path.GetDirectoryName(muxerPath);
        if (!string.IsNullOrEmpty(root))
            env["DOTNET_ROOT"] = root;

        return env;
    }

    static Dictionary<string, string> CreateGoEnvironment(string config, string targets) => new()
    {
        ["GoConfig"] = config,
        ["CustomAfterMicrosoftCSharpTargets"] = targets,
    };
}