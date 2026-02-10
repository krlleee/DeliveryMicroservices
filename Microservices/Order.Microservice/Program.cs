
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "https://localhost:7142";
        options.RequireHttpsMetadata = false;
        options.Audience = "frontend";
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/orders", (HttpContext ctx) =>
{
    var user = ctx.User.Identity?.Name ?? "unknown";
    return Results.Ok($"Orders endpoint. User: {user}");
}).RequireAuthorization();

app.Run("http://localhost:5005");
