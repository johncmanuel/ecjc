using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using server.Data;
using server.Endpoints;
using server.Services;

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

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
	?? throw new InvalidOperationException("Missing ConnectionStrings__DefaultConnection environment variable.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseNpgsql(connectionString));

builder.Services.AddSingleton<IStorageService, LocalStorageService>();

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
app.UseStaticFiles();
app.UseOpenApi();

app.MapHealthChecks("/health");

app.RegisterUserEndpoints();
app.RegisterGroupEndpoints();
app.RegisterEntryEndpoints();
app.RegisterReactionEndpoints();
app.RegisterMediaEndpoints();

app.Run();
