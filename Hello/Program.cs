using Blazored.LocalStorage;
using Hello.Components;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddHealthChecks();

// OpenTelemetry: logs, metrics and traces.
//     Automatically sends everything to the endpoint specified by the OTEL_EXPORTER_OTLP_ENDPOINT env var
builder.Services
    .AddOpenTelemetry()
    .UseOtlpExporter()
    .ConfigureResource(resource => resource.AddService("alb1"))
    .WithLogging()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
    )
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("*")
    );

// Set the requester's IP address using headers set by the reverse proxy
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownProxies.Clear();
    options.KnownIPNetworks.Clear();
});

// Log requester IP and User-Agent
builder.Services.AddHttpLogging(options =>
{
    // nothing to see here
    options.LoggingFields = HttpLoggingFields.RequestMethod
                            | HttpLoggingFields.RequestPath
                            | HttpLoggingFields.RequestQuery
                            | HttpLoggingFields.ResponseStatusCode
                            | HttpLoggingFields.Duration
                            | HttpLoggingFields.RequestHeaders
                            | HttpLoggingFields.ResponseHeaders;
    options.RequestHeaders.Add("Upgrade-Insecure-Requests");
    options.RequestHeaders.Add("Cdn-Loop");
    options.RequestHeaders.Add("Cf-Connecting-Ip");
    options.RequestHeaders.Add("Cf-Ipcountry");
    options.RequestHeaders.Add("Cf-Ray");
    options.RequestHeaders.Add("Cf-Visitor");
    options.RequestHeaders.Add("Cf-Warp-Tag-Id");
    options.RequestHeaders.Add("Priority");
    options.RequestHeaders.Add("Sec-Fetch-Dest");
    options.RequestHeaders.Add("Sec-Fetch-Mode");
    options.RequestHeaders.Add("Sec-Fetch-Site");
    options.RequestHeaders.Add("Sec-Fetch-User");
    options.RequestHeaders.Add("Sec-Gpc");
    options.RequestHeaders.Add("X-Original-Proto");
    options.RequestHeaders.Add("X-Original-For");
    options.RequestHeaders.Add("X-Forwarded-For");
    options.RequestHeaders.Add("X-Forwarded-Proto");
    options.RequestHeaders.Add(HeaderNames.Referer);
});


var app = builder.Build();

app.UseHttpLogging();
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
if (!app.Environment.IsDevelopment() && Directory.Exists("/alb1.hu_files"))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider("/alb1.hu_files"),
        RequestPath = "/files",
        ServeUnknownFileTypes = true,
    });
}
app.UseAntiforgery();

app.Use((context, next) =>
{
    context.Response.OnStarting(() =>
    {
        // When a Blazor page is rendered on the server, IAntiforgery.GetAndStoreTokens is called, which forces
        // Cache-Control to be "no-cache, no-store", thereby disabling the bfcache. Override it.
        // https://web.dev/articles/bfcache
        // https://github.com/dotnet/aspnetcore/issues/54464
        if (context.Response.Headers.TryGetValue(HeaderNames.CacheControl, out var cacheControlHeader)
            && cacheControlHeader == "no-cache, no-store")
            context.Response.Headers.CacheControl = "no-cache";
        return Task.CompletedTask;
    });

    return next();
});

app.MapHealthChecks("/healthz");
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
