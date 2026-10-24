using System.Text.Json.Serialization;
using TaskManagement.Api.ErrorHandling;
using TaskManagement.Application.Services;
using TaskManagement.Infrastructure;
using TaskManagement.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ---- Services (composition root: the only place that knows every layer) ----

var connectionString = builder.Configuration.GetConnectionString("TaskDb")
    ?? throw new InvalidOperationException("Connection string 'TaskDb' is not configured.");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddInfrastructure(connectionString, builder.Environment.ContentRootPath);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Enums travel as names ("InProgress"); numeric values are rejected.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));

        // Malformed JSON returns a generic message instead of internal type names.
        options.AllowInputFormatterExceptionMessages = false;
    });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// ---- Database ----

if (app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", defaultValue: true))
{
    using var scope = app.Services.CreateScope();
    await DatabaseInitializer.InitializeAsync(
        scope.ServiceProvider.GetRequiredService<AppDbContext>(),
        scope.ServiceProvider.GetRequiredService<TimeProvider>(),
        seedSampleData: app.Configuration.GetValue("Database:SeedSampleData", defaultValue: false));
}

// ---- HTTP pipeline ----

// First in the pipeline so it catches exceptions from everything that follows.
app.UseExceptionHandler();

// Gives empty error responses (e.g. 404 for an unmatched route) a ProblemDetails body.
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Serves the vanilla JS frontend from wwwroot; "/" resolves to index.html.
// Same origin as the API, so no CORS configuration is needed.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposes the implicit Program class to WebApplicationFactory in integration tests.
public partial class Program;
