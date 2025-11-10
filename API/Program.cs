using API.Middlewares;
using API.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.FileProviders;
using Services.Dtos;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for longer timeouts and large file uploads
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(10);
    options.Limits.MaxRequestBodySize = 600 * 1024 * 1024; // 600 MB
    options.Limits.MinRequestBodyDataRate = null; // Disable minimum data rate for file uploads
    options.Limits.MinResponseDataRate = null;
});

builder.Services.AddServices(builder.Configuration);

builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ValidationFilter>();
        options.ReturnHttpNotAcceptable = true;
    })
    .AddXmlSerializerFormatters()
    .AddOData(opt => opt.Select().Filter().OrderBy().Expand().Count().SetMaxTop(1000))
    .ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true);
    
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerConfiguration();
builder.Services.AddJWTAuthenticationScheme(builder.Configuration);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});


var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandler>();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Repositories.Data.AppDbContext>();
    await db.Database.MigrateAsync();
    await Repositories.Data.DbSeeder.SeedDefaultsAsync(db);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
// Only redirect HTTPS for non-upload endpoints (large file uploads may fail with HTTPS redirection)
// app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowReactApp");

// Serve storage as static files at /files
var storageOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<StorageOptions>>().Value;
var storageRoot = Path.Combine(AppContext.BaseDirectory, storageOptions.Root);
Directory.CreateDirectory(storageRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(storageRoot),
    RequestPath = "/files"
});
app.MapControllers();
app.Run();