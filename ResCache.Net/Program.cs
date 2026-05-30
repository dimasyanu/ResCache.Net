using ResCache.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var revProxyConfigFiles = builder.Configuration.GetSection("ReverseProxyConfigFiles").Get<List<ReverseProxyConfigFile>>() ?? [];
var revProxyBuilder = builder.Services.AddReverseProxy();
foreach (var configFile in revProxyConfigFiles) {
    if (string.IsNullOrEmpty(configFile.FilePath) || !File.Exists(Path.Combine(builder.Environment.ContentRootPath, configFile.FilePath))) {
        Console.WriteLine($"Warning: Reverse proxy configuration file '{configFile.FilePath}' is missing or invalid. Skipping.");
        continue;
    }

    IConfiguration revProxyConfig = new ConfigurationBuilder()
        .SetBasePath(builder.Environment.ContentRootPath)
        .AddJsonFile(configFile.FilePath, configFile.Optional, configFile.ReloadOnChange)
        .Build();
    revProxyBuilder.LoadFromConfig(revProxyConfig);
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapReverseProxy();
app.Run();