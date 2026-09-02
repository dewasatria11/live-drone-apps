using AlIkhsanMedia.Drone.Core;
namespace AlIkhsanMedia.Drone.Infrastructure.Tests;
public sealed class MediaMtxConfigGeneratorTests
{
    private static EngineConfiguration Config(string hash = "00") => new("mediamtx", hash, Path.GetTempPath(), "drone1-secure", "127.0.0.1:1935", "127.0.0.1:8554", "127.0.0.1:9997", "127.0.0.1:9998");
    [Fact] public void ConfigUsesDirectRemuxBindingsAndOneExclusivePath()
    {
        var yaml = MediaMtxConfigGenerator.Generate(Config());
        Assert.Contains("rtmpAddress: 127.0.0.1:1935", yaml); Assert.Contains("rtspAddress: 127.0.0.1:8554", yaml);
        Assert.Contains("apiAddress: 127.0.0.1:9997", yaml); Assert.Contains("overridePublisher: false", yaml);
        Assert.Contains("hls: false", yaml); Assert.Contains("webrtc: true", yaml); Assert.Contains("webrtcAddress: 127.0.0.1:8889", yaml); Assert.DoesNotContain("runOn", yaml);
    }
    [Fact] public void ConfigContainsAllActivePathsWithoutChangingEngineEndpoint()
    {
        var keys = new[] { "drone-a", "drone-b", "drone-c", "drone-d", "drone-e", "drone-f" };
        var yaml = MediaMtxConfigGenerator.Generate(Config() with { ActivePaths = keys });
        Assert.Equal(6, keys.Count(key => yaml.Contains($"  {key}:")));
        Assert.Contains("  drone-a:", yaml);
        Assert.Contains("  drone-f:", yaml);
        foreach (var key in keys) Assert.Contains($"  {key}:", yaml);
    }
    [Theory] [InlineData("bad/path")] [InlineData("bad key")] public void UnsafePathIsRejected(string path)
    { Assert.Throws<ArgumentException>(() => MediaMtxConfigGenerator.Generate(Config() with { StreamPath = path })); }
    [Fact] public async Task IntegrityMismatchIsRejectedBeforeExecution()
    {
        var file = Path.GetTempFileName(); try { await File.WriteAllTextAsync(file, "not mediamtx"); await Assert.ThrowsAsync<InvalidDataException>(() => BinaryIntegrityVerifier.VerifyAsync(file, new string('0', 64), default)); } finally { File.Delete(file); }
    }
    [Fact] public void RestartPolicyIsBoundedToRequiredBackoff()
    {
        var policy = new EngineRestartPolicy();
        Assert.True(policy.TryGetDelay(0, out var first)); Assert.Equal(TimeSpan.FromSeconds(1), first);
        Assert.True(policy.TryGetDelay(1, out var second)); Assert.Equal(TimeSpan.FromSeconds(3), second);
        Assert.True(policy.TryGetDelay(2, out var third)); Assert.Equal(TimeSpan.FromSeconds(10), third);
        Assert.False(policy.TryGetDelay(3, out _)); Assert.Equal(3, policy.MaximumAttempts);
    }
}
