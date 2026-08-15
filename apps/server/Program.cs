var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

builder.Services.AddCors(options =>
{
	options.AddPolicy("DevCorsPolicy", policy =>
	{
		policy.WithOrigins("http://localhost:3000", "http://127.0.0.1:3000")
			.AllowAnyHeader()
			.AllowAnyMethod();
	});
});

var app = builder.Build();

app.UseCors("DevCorsPolicy");

app.MapHealthChecks("/health");

app.Run();

