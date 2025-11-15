using Microsoft.EntityFrameworkCore;
using StorageService.Data;
using StorageService.Repositories;
using StorageService.Services;
using Shared.Middleware;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<StorageDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null
        );
    });
});

// JWT Authentication (validation only, not issuing tokens)
builder.Services.AddJwtAuthentication(builder.Configuration);

// Repositories
builder.Services.AddScoped<ISubmissionRepository, SubmissionRepository>();
builder.Services.AddScoped<IFileRepository, FileRepository>();

// Services
builder.Services.AddScoped<ISubmissionService, SubmissionService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<INestedZipService, NestedZipService>();
builder.Services.AddScoped<ITextExtractionService, TextExtractionService>();

// Configuration
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<StorageDbContext>("database");

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<StorageDbContext>();
    try
    {
        context.Database.Migrate();
        app.Logger.LogInformation("Database migration completed successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while migrating the database");
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<JwtMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
