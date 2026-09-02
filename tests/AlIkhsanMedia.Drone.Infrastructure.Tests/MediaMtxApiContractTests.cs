using System.Net;
using System.Text;
namespace AlIkhsanMedia.Drone.Infrastructure.Tests;
public sealed class MediaMtxApiContractTests
{
    [Fact] public async Task PinnedApiFixtureMapsKnownFieldsAndIgnoresAdditiveFields()
    {
        const string json = "{\"pageCount\":1,\"items\":[{\"name\":\"drone1\",\"ready\":true,\"readyTime\":\"2026-09-02T10:00:00Z\",\"tracks\":[\"H264\",\"MPEG-4 Audio\"],\"bytesReceived\":12345,\"readers\":[{\"id\":\"r1\"}],\"futureField\":42}]}";
        var client = new MediaMtxApiClient(new HttpClient(new FixtureHandler(json)) { BaseAddress = new Uri("http://127.0.0.1/") });
        var paths = await client.GetPathsAsync(default); var path = Assert.Single(paths);
        Assert.Equal("drone1", path.Name); Assert.True(path.Ready); Assert.Equal(12345, path.BytesReceived); Assert.Equal(2, path.Codecs.Count); Assert.Equal(1, path.ReaderCount);
    }
    private sealed class FixtureHandler(string content) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(content, Encoding.UTF8, "application/json") }); }
}
