using System.Net;
using AlIkhsanMedia.Drone.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace AlIkhsanMedia.Drone.SetupPortal;

public sealed class SetupPortalService : IAsyncDisposable
{
    private readonly SetupTokenStore tokens; private readonly IClock clock; private WebApplication? app;
    public SetupPortalService(SetupTokenStore tokens, IClock clock) { this.tokens = tokens; this.clock = clock; }
    public async Task StartAsync(SetupPortalConfiguration configuration, Func<StreamSlotId, SetupPortalData?> resolver, CancellationToken ct)
    {
        if (app is not null) throw new InvalidOperationException("Portal sudah berjalan.");
        var builder = WebApplication.CreateSlimBuilder(); builder.WebHost.UseUrls($"http://{configuration.BindAddress}:{configuration.Port}");
        var web = builder.Build();
        web.Use(async (context, next) => { context.Response.Headers.CacheControl = "no-store"; context.Response.Headers.XContentTypeOptions = "nosniff"; context.Response.Headers.ContentSecurityPolicy = "default-src 'none'; style-src 'unsafe-inline'; img-src 'self' data:; script-src 'none'; base-uri 'none'; frame-ancestors 'none'"; await next(); });
        web.MapGet("/healthz", () => Results.Text("ok", "text/plain"));
        web.MapGet("/s/{token}", (string token) => { if (!tokens.TryGet(token, clock, out var link)) return Results.NotFound(); var data = resolver(link.SlotId); return data is null ? Results.NotFound() : Results.Content(RenderHtml(data), "text/html; charset=utf-8"); });
        app = web; await web.StartAsync(ct).ConfigureAwait(false);
    }
    private static string RenderHtml(SetupPortalData data) => $"<!doctype html><html lang=\"id\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>{WebUtility.HtmlEncode(data.ProductName)}</title><style>body{{font:16px system-ui;max-width:42rem;margin:2rem auto;padding:0 1rem}}code{{display:block;padding:1rem;background:#eef2ef;overflow-wrap:anywhere}}</style><h1>{WebUtility.HtmlEncode(data.ProductName)}</h1><h2>{WebUtility.HtmlEncode(data.SlotName)}</h2><p>Salin URL ini ke Custom RTMP di DJI Fly. Jangan bagikan URL ini.</p><code>{WebUtility.HtmlEncode(data.RtmpUrl)}</code><p>Kedaluwarsa: {data.ExpiresAt:O}</p></html>";
    public async ValueTask DisposeAsync() { if (app is not null) { await app.StopAsync().ConfigureAwait(false); await app.DisposeAsync().ConfigureAwait(false); app = null; } }
}
