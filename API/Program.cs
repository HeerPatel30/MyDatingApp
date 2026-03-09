using API.Data;
using API.Extensions;
using API.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Extension methods from API.Extensions
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);

var app = builder.Build();

// --- CONFIGURE THE HTTP REQUEST PIPELINE (Middleware Order is Critical) ---

// 1. Exception handling must be first to catch errors from all other middleware
app.UseMiddleware<ExceptionMiddleware>();

// 2. HTTPS Redirection
app.UseHttpsRedirection();

// 3. CORS MUST be placed before Authentication and Authorization
// Added both http and https for your Angular frontend to prevent "PEM routines" errors
app.UseCors(builder => builder
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithOrigins("http://localhost:4200", "https://localhost:4200"));

// 4. Authentication must come BEFORE Authorization
app.UseAuthentication();
app.UseAuthorization();

// 5. Map the controllers last
app.MapControllers();

// --- DATABASE SEEDING ---
using IServiceScope scope = app.Services.CreateScope();
IServiceProvider services = scope.ServiceProvider;
try
{
    DataContext context = services.GetRequiredService<DataContext>();
    await context.Database.MigrateAsync();
    await Seed.SeedUsers(context);
}
catch (Exception ex)
{
    ILogger<Program> logger = services.GetService<ILogger<Program>>()!;
    logger.LogError(ex, "An error occurred during migration.");
}

app.Run();
