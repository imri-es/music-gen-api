using System.Text;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerUI;

var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
if (File.Exists(envPath))
{
    DotNetEnv.Env.Load(envPath);
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<MusicGen.Core.MusicDataGenerator>();

builder.Services.AddCors(options =>
{
    var allowedOriginsStr = Environment.GetEnvironmentVariable("AllowedOrigins");
    var allowedOrigins = !string.IsNullOrEmpty(allowedOriginsStr)
        ? allowedOriginsStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
        : builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? Array.Empty<string>();
    options.AddPolicy(
        "AllowFrontend",
        policy =>
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        }
    );
});

var app = builder.Build();

// дев.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseStaticFiles();

app.MapControllers();

app.Run();
