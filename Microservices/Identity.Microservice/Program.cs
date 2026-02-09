using Identity.Microservice.Infrastructure.Identity;
using Identity.Microservice.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

//DBCONTEXT
builder.Services.AddDbContext<IdentityDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("identity"));

    options.UseOpenIddict();
});

//Identity
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<IdentityDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddHttpContextAccessor();

//OpenIddict
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<IdentityDbContext>();
    })
    .AddServer(options =>
    {
        // Endpoints
        options.SetTokenEndpointUris("/connect/token");

        // Grant types
        options.AllowPasswordFlow()
               .AllowRefreshTokenFlow();

        // Token lifetimes
        options.SetAccessTokenLifetime(TimeSpan.FromHours(1))
               .SetRefreshTokenLifetime(TimeSpan.FromDays(7));

        // Development certificates
        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        // ASP.NET Core integration
        options.UseAspNetCore()
               .EnableTokenEndpointPassthrough(); // VAŽNO: omogućava custom logiku u kontroleru
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();