namespace server.Middleware;

using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using server.Data;
using System.Text;

public class ApiKeyMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext db)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            var authHeader = context.Request.Headers.Authorization.ToString();
            if (authHeader.StartsWith("Bearer ecjc_live_", StringComparison.OrdinalIgnoreCase))
            {
                var apiKey = authHeader.Substring("Bearer ".Length).Trim();

                using var sha256 = SHA256.Create();
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
                var keyHash = Convert.ToBase64String(hashBytes);

                var keyRecord = await db.ApiKeys.FirstOrDefaultAsync(k => k.KeyHash == keyHash);

                if (keyRecord != null)
                {
                    var claims = new[] { new Claim(ClaimTypes.NameIdentifier, keyRecord.UserId) };
                    var identity = new ClaimsIdentity(claims, "ApiKey");
                    context.User = new ClaimsPrincipal(identity);
                }
            }
        }
        await _next(context);
    }
}
