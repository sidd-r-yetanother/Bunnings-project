using Bunnings.Application;
using Bunnings.Application.Implementation;
using Bunnings.Application.Interface;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddScoped<IJsonFileProcessor, JsonFileProcessor>();
builder.Services.AddScoped<IHotProductCalculator, HotProductCalculator>();
builder.Services.AddScoped<IOrderProcessor, OrderProcessor>();

// Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger middleware (usually only in Development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();