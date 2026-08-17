using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

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

var jwtSecret = builder.Configuration["Auth:Secret"]
	?? Environment.GetEnvironmentVariable("BETTER_AUTH_SECRET")
	?? "dummy_secret_for_build_purposes_only_must_be_long_enough_32bytes_minimum_length_here";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuerSigningKey = true,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
			ValidateIssuer = false,
			ValidateAudience = false,
			ValidateLifetime = true,
			ClockSkew = TimeSpan.FromMinutes(1),
		};
	});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors("DevCorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.UseOpenApi();

app.MapHealthChecks("/health");

// User sync endpoint — called by Better-Auth's sign-in hook to atomically
// create or update the user in the backend database.
app.MapPost("/api/users/sync", (UserSyncRequest userPayload) =>
{
	if (string.IsNullOrWhiteSpace(userPayload.Email))
	{
		return Results.BadRequest(new { error = "Invalid user payload" });
	}

	// TODO: Replace with actual database upsert logic (EF Core / Npgsql)
	// For now, log the sync request and return success.
	Console.WriteLine($"[User Sync] id={userPayload.Id} email={userPayload.Email} name={userPayload.Name}");

	return Results.Ok(new { synced = true, email = userPayload.Email });
});

app.Run();

record UserSyncRequest(string Id, string Email, string? Name, string? Image);
