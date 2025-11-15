using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add JWT Authentication from Shared library
builder.Services.AddJwtAuthentication(builder.Configuration);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure middleware
app.UseCors();

// Add health check endpoint
app.MapHealthChecks("/health");

// Map reverse proxy routes
app.MapReverseProxy();

app.Run();
