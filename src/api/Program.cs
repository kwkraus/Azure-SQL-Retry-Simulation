using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using SimpleTodo.Api;

var builder = WebApplication.CreateBuilder(args);
var credential = new DefaultAzureCredential();

// configure Key Vault if not in development
if (!builder.Environment.IsDevelopment())
{
    builder.Configuration.AddAzureKeyVault(new Uri(builder.Configuration["AZURE_KEY_VAULT_ENDPOINT"]), credential);
}

builder.Services.AddScoped<ListsRepository>();
builder.Services.AddDbContext<TodoDb>(options =>
{
    var connectionString = builder.Configuration["AZURE_SQL_CONNECTION_STRING_KEY"];

    // enable detailed errors and sensitive data logging for development
    if (builder.Environment.IsDevelopment())
    {
        options
            .UseSqlServer(connectionString)
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging();
    }
    else // enable retry on failure for production
    {
        options
            .UseSqlServer(
                connectionString,
                sqlOptions => sqlOptions.EnableRetryOnFailure(
                                            maxRetryCount: 6,
                                            maxRetryDelay: TimeSpan.FromSeconds(5),
                                            errorNumbersToAdd: null));
    }
});

builder.Services.AddControllers();
builder.Services.AddApplicationInsightsTelemetry(builder.Configuration);

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TodoDb>();
    await db.Database.EnsureCreatedAsync();
}

app.UseCors(policy =>
{
    policy.AllowAnyOrigin();
    policy.AllowAnyHeader();
    policy.AllowAnyMethod();
});

// Swagger UI
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("./openapi.yaml", "v1");
    options.RoutePrefix = "";
});

app.UseStaticFiles(new StaticFileOptions
{
    // Serve openapi.yaml file
    ServeUnknownFileTypes = true,
});

app.MapControllers();
app.Run();