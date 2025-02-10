using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using RssNewsApp.Services;
using Services;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;

var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

// User Secrets'ı ekle
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>(optional: false)
    .AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddHttpClient();
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<IRssService, RssService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<ISpotifyService, SpotifyService>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient();
    
    // Configuration değerlerini kontrol et
    foreach (var c in configuration.AsEnumerable())
    {
        Console.WriteLine($"Config: {c.Key} = {c.Value}");
    }
    
    string? clientId = configuration["Spotify:ClientId"];
    string? clientSecret = configuration["Spotify:ClientSecret"];
    
    Console.WriteLine($"Okunan değerler - ClientId: {clientId}, ClientSecret: {clientSecret}");
    
    if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
    {
        var message = builder.Environment.IsDevelopment()
            ? "Spotify yapılandırması eksik. Lütfen secrets.json dosyasında Spotify:ClientId ve Spotify:ClientSecret değerlerini ayarlayın."
            : "Spotify yapılandırması eksik. Lütfen CLIENTID ve CLIENTSECRET environment değişkenlerini kontrol edin.";
            
        throw new InvalidOperationException(message);
    }

    return new SpotifyService(httpClient, configuration, clientId, clientSecret);
});

builder.Services.AddHttpClient<IFixtureService, FixtureService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHostedService<RssBackgroundService>();
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// Güvenlik başlıkları
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Content-Security-Policy", 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://platform.twitter.com https://cdn.tailwindcss.com https://code.jquery.com; " +
        "style-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' https://cdnjs.cloudflare.com; " +
        "frame-src https://platform.twitter.com https://open.spotify.com;");
    await next();
});

if (app.Environment.IsDevelopment())
{
    var spotifyClientId = app.Configuration["Spotify:ClientId"];
    Console.WriteLine($"Spotify Client ID mevcut mu: {!string.IsNullOrEmpty(spotifyClientId)}");
}

app.Run();
