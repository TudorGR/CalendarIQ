using CalendarIQ.Api.Data;
using CalendarIQ.Api.Options;
using CalendarIQ.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddScoped<IEventValidator, EventValidator>();
builder.Services.Configure<AiOptions>(builder.Configuration.GetSection(AiOptions.SectionName));
builder.Services.AddHttpClient<GroqClient>();
builder.Services.AddHttpClient<GeminiClient>();
builder.Services.AddTransient<LocalEvents>();
builder.Services.AddHttpClient<WeatherService>();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

app.MapGet("/weather", async (
    double? latitude,
    double? longitude,
    WeatherService weatherService) =>
{
    if (latitude is null || longitude is null)
        return Results.BadRequest(new { error = "Latitude and longitude are required" });

    try
    {
        var data = await weatherService.GetWeatherForecastAsync(latitude.Value, longitude.Value);
        return Results.Content(data, "application/json");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching weather data: {ex}");
        return Results.Problem("Failed to fetch weather data");
    }
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");
app.UseAuthorization();
app.MapControllers();

app.Run();
