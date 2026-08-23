using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using server.Data;
using server.Endpoints;
using server.Services;
using server.Middleware;
using System.Security;

var builder = WebApplication.CreateBuilder(args);

// Load .env from workspace root for local development
var envPath = Path.Combine(builder.Environment.ContentRootPath, "../../.env");
if (File.Exists(envPath))
{
    DotNetEnv.Env.Load(envPath);
    builder.Configuration.AddEnvironmentVariables();
}

builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "v1";
    config.Title = "ECJC API";
    config.Version = "v1";
});

builder.Services.AddCors(options =>
{
	options.AddPolicy("DevCorsPolicy", policy =>
	{
		policy.WithOrigins("http://localhost:3000", "http://127.0.0.1:3000")
			.AllowAnyHeader()
			.AllowAnyMethod();
	});
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
	?? throw new InvalidOperationException("Missing ConnectionStrings__DefaultConnection environment variable.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseNpgsql(connectionString));

builder.Services.AddSingleton<IStorageService, LocalStorageService>();
builder.Services.AddSingleton<CentrifugoService>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddScoped<IStreakEvaluationService, StreakEvaluationService>();
builder.Services.AddHostedService<StreakMonitorBackgroundService>();

var betterAuthUrl = builder.Configuration["Auth:BaseUrl"]
	?? Environment.GetEnvironmentVariable("BETTER_AUTH_URL")
	?? "http://localhost:3000";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		// NOTE: better-auth's jwt() plugin signs tokens with EdDSA by default (https://better-auth.com/docs/plugins/jwt#algorithm-of-the-key-pair).
		// Validate them using the JWKS endpoint it exposes.
		options.RequireHttpsMetadata = false;
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuerSigningKey = true,
			ValidateIssuer = false,
			ValidateAudience = false,
			ValidateLifetime = true,
			ClockSkew = TimeSpan.FromMinutes(1),
		};
		options.Events = new JwtBearerEvents
		{
			OnMessageReceived = async ctx =>
			{
				if (ctx.Options.TokenValidationParameters.IssuerSigningKeys == null ||
					!ctx.Options.TokenValidationParameters.IssuerSigningKeys.Any())
				{
					using var http = new HttpClient();
					var jwksJson = await http.GetStringAsync($"{betterAuthUrl}/api/auth/jwks");
					var jwks = new JsonWebKeySet(jwksJson);
					ctx.Options.TokenValidationParameters.IssuerSigningKeys = jwks.GetSigningKeys();
				}
			}
		};
	});

builder.Services.AddAuthorization();

var app = builder.Build();

// auto migrate database on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
		// probably better to skip the migration
        Console.WriteLine($"Skipping migration during build: {ex.Message}");
    }
}

app.UseCors("DevCorsPolicy");
app.UseAuthentication();

app.UseMiddleware<ApiKeyMiddleware>();

app.UseAuthorization();
app.UseStaticFiles();
app.UseOpenApi();

app.MapHealthChecks("/health");

app.RegisterUserEndpoints();
app.RegisterGroupEndpoints();
app.RegisterEntryEndpoints();
app.RegisterReactionEndpoints();
app.RegisterMediaEndpoints();
app.RegisterInviteEndpoints();
app.RegisterSettingsEndpoints();

app.Run();

// needed for integration tests to access the Program class
public partial class Program { }
