# Endpoints code structure

Because we're using minimal APIs and we want to organize our endpoints in a more maintainable manner, structure endpoints using extension methods:

```cs
// Endpoints/SomeEndpoints.cs

public static class SomeEndpoints 
{
    public static void RegisterSomeEndpoints(this WebApplication app) 
    {
        // can apply attributes to all endpoints under this group in one go!
        var endpoints = app.MapGroup("/api/endpoints").WithTags("some endpoints!")

        endpoints.MapPost("/{id}/alpha", CreateAlpha);
        endpoints.MapGet("/beta", GetBeta);
        endpoints.MapPut("/charlie", UpdateCharlie);
        endpoints.MapDelete("/delta", DeleteDelta);
    }

    // can add other "bad" types like UnauthorizedHttpResult in Results<...> 
    public static async Task<Results<Ok<AlphaRes>, BadRequest<string>, NotFound<string>>> CreateAlpha(
        int id, 
        AppDbContext db) 
    {
       if (/* ... */) return TypedResults.BadRequest("error msg");
       if (/* ... */) return TypedResults.NotFound("error msg");
       // ...
       return TypedResults.Ok(new AlphaRes(/* ... */)); 
    }

    // ...

    // set up the res and req types for the endpoint methods
    internal sealed record AlphaRes(/* ... */);

    // ...
}

// Program.cs

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
// ...
app.RegisterSomeEndpoints();
app.Run();
```

See article for a thorough explanation: https://www.tessferrandez.com/blog/2023/10/31/organizing-minimal-apis.html