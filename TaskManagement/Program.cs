using Microsoft.EntityFrameworkCore;
using TaskManagement.Configuration;
using TaskManagement.Data;
using TaskManagement.Endpoints;
using TaskManagement.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? throw new InvalidOperationException("Database connection string is not configured.");

builder.Services.AddDbContext<TaskDbContext>(options => options.UseNpgsql(connectionString));

builder.Services
    .AddOptions<RabbitMqOptions>()
    .BindConfiguration(RabbitMqOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddScoped<TaskService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<TaskEventQueue>();

builder.Services.AddHostedService<RabbitMqBackgroundService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapTaskEndpoints();

app.Run();