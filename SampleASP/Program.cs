using FluentValidation;
using SampleASP.Services;
using SampleASP.Validators;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "SampleASP API", Version = "v1", Description = "Authored by Melnikov GV" });
});

// Register services
builder.Services.AddScoped<ProcessingService>();

builder.Services.AddValidatorsFromAssemblyContaining<ApiRequestValidator>();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseSwagger(c =>
{
    c.RouteTemplate = "api/swagger/{documentName}/swagger.json";
});
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/api/swagger/v1/swagger.json", "SampleASP API v1");
    c.RoutePrefix = "api/swagger";
});

app.MapControllers();

app.Run();