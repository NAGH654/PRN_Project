using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Repositories.Data;
using Repositories.Interfaces;
using Repositories.Repo;
using Services.Interfaces;
using Services.Options;
using Services.Service;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<AppDbContext>(opt =>
  opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Repos DI (nếu có repo đặc thù, add tại đây)
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<ISubmissionRepository, SubmissionRepository>();

// Services
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<ISubmissionService, SubmissionService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddHostedService<JobWorker>(); // background worker

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Assignment API", Version = "v1" });

    // 👇 BẮT BUỘC: ép IFormFile hiển thị là input file (string/binary)
    c.MapType<IFormFile>(() => new OpenApiSchema { Type = "string", Format = "binary" });
    c.MapType<IFormFileCollection>(() => new OpenApiSchema
    {
        Type = "array",
        Items = new OpenApiSchema { Type = "string", Format = "binary" }
    });
});


var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Repositories.Data.AppDbContext>();
    await Repositories.Data.AppDbSeeder.SeedAsync(db);
}
app.UseSwagger(); app.UseSwaggerUI();
app.UseHttpsRedirection();
app.MapControllers();
app.Run();