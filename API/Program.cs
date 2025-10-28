using API.Middlewares;
using API.Utils;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServices(builder.Configuration);

builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ValidationFilter>();
        options.ReturnHttpNotAcceptable = true;
    })
    .AddXmlSerializerFormatters()
    .ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true);
    
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerConfiguration();
builder.Services.AddJWTAuthenticationScheme(builder.Configuration);


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
app.UseHttpsRedirection();
app.MapControllers();
app.Run();