using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

// Register MyLibrary and turn on OpenTelemetry integration.
builder.Services.AddMyLibrary().WithOpenTelemetry();

builder.Services.AddOpenTelemetryTracing(builder => builder
    .AddAspNetCoreInstrumentation()
    .AddConsoleExporter());

builder.Services.AddOpenTelemetryMetrics(builder => builder
    .AddAspNetCoreInstrumentation()
    .AddConsoleExporter());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
