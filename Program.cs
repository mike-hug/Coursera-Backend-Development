using System.Net;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

Dictionary<int, User> users = [];

// Error-Handling Middleware 
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An unhandled exception has occurred.");

        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";
        var response = new { error = "Internal server error." };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
});
// Authentication Middleware 
app.Use(async (context, next) =>
{
    if (!context.Request.Headers.TryGetValue("Authorization", out var token))
    {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        await context.Response.WriteAsync("Unauthorized");
        return;
    }
    if (!ValidateToken(token))
    {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        await context.Response.WriteAsync("Unauthorized");
        return;
    }
    await next();
});
// Logging Middleware 
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation($"HTTP {context.Request.Method} {context.Request.Path}");
    await next();
    logger.LogInformation($"Response Status Code: {context.Response.StatusCode}");
});



app.MapGet("/users", () =>
{
    return users.Values;
});

app.MapGet("/users/{id}", (int id) =>
{
    return users.TryGetValue(id, out var user) ? Results.Ok(user) : Results.NotFound();
});

app.MapPost("/users", (User user) =>
{
    if (string.IsNullOrEmpty(user.Name) || string.IsNullOrEmpty(user.Email) || !IsValidEmail(user.Email))
    {
        return Results.BadRequest("Invalid user data.");
    }

    user.Id = users.Count > 0 ? users.Keys.Max() + 1 : 1;
    users.Add(user.Id, user);
    return Results.Created($"/users/{user.Id}", user);
});

app.MapPut("/users/{id}", (int id, User updatedUser) =>
{
    if (!users.ContainsKey(id))
    {
        return Results.NotFound("User not found.");
    }
    if (string.IsNullOrEmpty(updatedUser.Name) || string.IsNullOrEmpty(updatedUser.Email) || !IsValidEmail(updatedUser.Email))
    {
        return Results.BadRequest("Invalid user data.");
    }
    var user = users[id];
    user.Name = updatedUser.Name; user.Email = updatedUser.Email;
    return Results.NoContent();
});

app.MapDelete("/users/{id}", (int id) =>
{
    return users.Remove(id) ? Results.NoContent() : Results.NotFound("User not found.");
});

app.Run();


bool ValidateToken(string token)
{
    // For demonstration purposes, we'll assume any token starting with "Bearer " is valid.
    return token.StartsWith("Bearer ");
}

bool IsValidEmail(string email)
{
    try
    {
        var addr = new System.Net.Mail.MailAddress(email);
        return addr.Address == email;
    }
    catch
    {
        return false;
    }
}
public class User
{
    public int Id { get; set; }
    required public string Name { get; set; }
    required public string Email { get; set; }
}
