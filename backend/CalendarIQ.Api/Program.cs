using CalendarIQ.Api.Data;
using CalendarIQ.Api.Options;
using CalendarIQ.Api.Services;
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
builder.Services.AddHttpClient<TravelService>();
builder.Services.AddSingleton<IConversationMemoryService, ConversationMemoryService>();
builder.Services.AddScoped<ITimeService, TimeService>();
builder.Services.AddScoped<ISchedulingService, SchedulingService>();
builder.Services.AddScoped<IChatIntentService, ChatIntentService>();
builder.Services.AddScoped<IChatActionService, ChatActionService>();
builder.Services.AddScoped<ISuggestionsService, SuggestionsService>();

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
