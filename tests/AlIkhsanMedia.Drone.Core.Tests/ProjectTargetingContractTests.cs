namespace AlIkhsanMedia.Drone.Core.Tests;
public sealed class ProjectTargetingContractTests
{
    [Fact] public void WpfProjectAndWindowsWorkflowRemainAuthoritative()
    {
        var root = FindRoot(); var project = File.ReadAllText(Path.Combine(root, "src", "AlIkhsanMedia.Drone.App", "AlIkhsanMedia.Drone.App.csproj"));
        Assert.Contains("<TargetFramework>net8.0-windows</TargetFramework>", project); Assert.Contains("<UseWPF>true</UseWPF>", project); Assert.Contains("<EnableWindowsTargeting>true</EnableWindowsTargeting>", project); Assert.DoesNotContain("net8.0</TargetFramework>", project);
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "ci.yml")); Assert.Contains("runs-on: windows-latest", workflow); Assert.Contains("dotnet build AlIkhsanMedia.Drone.sln", workflow); Assert.Contains("dotnet test AlIkhsanMedia.Drone.sln", workflow); Assert.Contains("verify-vendor.ps1", workflow);
        var release = File.ReadAllText(Path.Combine(root, ".github", "workflows", "release.yml")); Assert.Contains("contents: write", release); Assert.Contains("--self-contained true", File.ReadAllText(Path.Combine(root, "eng", "package.ps1"))); Assert.Contains("AlIkhsanMedia-DroneVersion-Setup-", release);
    }
    private static string FindRoot() { var directory = new DirectoryInfo(AppContext.BaseDirectory); while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PRD_AL_IKHSAN_MEDIA_DRONE_VERSION.md"))) directory = directory.Parent; return directory?.FullName ?? throw new DirectoryNotFoundException(); }
}
