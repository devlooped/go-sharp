using Devlooped;

namespace Tests;

public class ProcessRunnerTests
{
    [Fact]
    public void CreateDotNetHostEnvironment_sets_host_path_and_root()
    {
        var muxer = OperatingSystem.IsWindows()
            ? @"C:\Program Files\dotnet\dotnet.exe"
            : "/usr/share/dotnet/dotnet";

        var env = ProcessRunner.CreateDotNetHostEnvironment(muxer);

        Assert.Equal(muxer, env["DOTNET_HOST_PATH"]);
        Assert.Equal(Path.GetDirectoryName(muxer), env["DOTNET_ROOT"]);
    }

    [Fact]
    public void CreateDotNetHostEnvironment_empty_when_muxer_missing()
    {
        var env = ProcessRunner.CreateDotNetHostEnvironment(muxerPath: "");

        Assert.Empty(env);
    }

    [Fact]
    public void CreateDotNetHostEnvironment_uses_resolved_muxer_by_default()
    {
        Assert.NotNull(DotnetMuxer.Path);

        var env = ProcessRunner.CreateDotNetHostEnvironment();

        Assert.Equal(DotnetMuxer.Path.FullName, env["DOTNET_HOST_PATH"]);
        Assert.Equal(Path.GetDirectoryName(DotnetMuxer.Path.FullName), env["DOTNET_ROOT"]);
    }
}
