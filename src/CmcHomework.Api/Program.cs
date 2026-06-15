using System.Text.Json;
using CmcHomework.Api.Handlers;
using CmcHomework.Api.Services;
using CmcHomework.Api.Storage;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình JSON trả về snake_case để khớp ví dụ trong đề bài: by_type, asset_count...
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
});

// Khi JSON request sai định dạng, API trả 400 thay vì bung stack trace trong môi trường Development.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);

builder.Services.AddSingleton<AppLifetime>();
builder.Services.AddSingleton<IAssetStorage, InMemoryAssetStorage>();
builder.Services.AddSingleton<IAssetService, AssetService>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new { message = "CMC Homework API is running" }));
app.MapAssetEndpoints();
app.MapHealthEndpoints();

app.Run();

public partial class Program;
