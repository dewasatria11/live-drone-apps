using System.Net;
using System.Security.Cryptography;
using AlIkhsanMedia.Drone.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AlIkhsanMedia.Drone.SetupPortal;

public sealed class SetupPortalService : IAsyncDisposable
{
    private readonly SetupTokenStore tokens; private readonly IClock clock; private WebApplication? app;
    public SetupPortalService(SetupTokenStore tokens, IClock clock) { this.tokens = tokens; this.clock = clock; }
    public async Task StartAsync(SetupPortalConfiguration configuration, Func<StreamSlotId, SetupPortalData?> resolver, CancellationToken ct)
    {
        if (app is not null) throw new InvalidOperationException("Portal sudah berjalan.");
        var builder = WebApplication.CreateSlimBuilder(); builder.Logging.ClearProviders(); builder.WebHost.UseUrls($"http://{configuration.BindAddress}:{configuration.Port}");
        var web = builder.Build();
        web.Use(async (context, next) => { context.Response.Headers.CacheControl = "no-store, max-age=0"; context.Response.Headers.Pragma = "no-cache"; context.Response.Headers.XContentTypeOptions = "nosniff"; context.Response.Headers["Referrer-Policy"] = "no-referrer"; await next(); });
        web.MapGet("/s/{token}", (HttpContext context, string token) => { if (!tokens.TryGet(token, clock, out var link)) return Results.NotFound(); var data = resolver(link.SlotId); if (data is null) return Results.NotFound(); var rendered = RenderHtml(data); context.Response.Headers.ContentSecurityPolicy = $"default-src 'none';style-src 'unsafe-inline';script-src 'nonce-{rendered.Nonce}';base-uri 'none';frame-ancestors 'none'"; return Results.Content(rendered.Html, "text/html; charset=utf-8"); });
        app = web; await web.StartAsync(ct).ConfigureAwait(false);
    }
    private static (string Html, string Nonce) RenderHtml(SetupPortalData data)
    {
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        var product = WebUtility.HtmlEncode(data.ProductName); var slot = WebUtility.HtmlEncode(data.SlotName); var url = WebUtility.HtmlEncode(data.RtmpUrl);
        return ($"<!doctype html><html lang=\"id\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>{product}</title><style>body{{font:16px system-ui;max-width:42rem;margin:2rem auto;padding:0 1rem;color:#142019}}main{{border:1px solid #d5e0d8;border-radius:12px;padding:1.25rem}}code{{display:block;padding:1rem;background:#eef2ef;overflow-wrap:anywhere;user-select:all}}button{{margin-top:1rem;padding:.7rem 1rem;background:#168653;color:white;border:0;border-radius:8px;font-weight:600}}.warning{{color:#8b3b12}}</style></head><body><main><h1>{product}</h1><h2>{slot}</h2><p>Di DJI Fly, buka Custom RTMP lalu masukkan URL berikut.</p><code id=\"url\">{url}</code><button id=\"copy\" type=\"button\">Salin URL RTMP</button><p id=\"status\" role=\"status\"></p><p class=\"warning\"><strong>Jangan bagikan URL ini.</strong> URL memiliki akses ke slot ini.</p><p>Kedaluwarsa: {data.ExpiresAt:O}</p></main><script nonce=\"{nonce}\">const b=document.getElementById('copy'),u=document.getElementById('url'),s=document.getElementById('status');b.onclick=async()=>{{try{{await navigator.clipboard.writeText(u.textContent);s.textContent='URL berhasil disalin.'}}catch{{u.focus();const r=document.createRange();r.selectNodeContents(u);const x=getSelection();x.removeAllRanges();x.addRange(r);s.textContent='Clipboard tidak tersedia. Teks URL sudah dipilih, tekan Salin.'}}}};</script></body></html>", nonce);
    }
    public async ValueTask DisposeAsync() { if (app is not null) { await app.StopAsync().ConfigureAwait(false); await app.DisposeAsync().ConfigureAwait(false); app = null; } }
}
