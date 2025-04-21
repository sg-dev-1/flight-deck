using FlightDeck.Services;
using FlightDeck.Hubs;
using Serilog;

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
Log.Information("Starting FlightDeck web application setup...");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        );

    builder.Services.AddCors(options =>
    {
        options.AddPolicy(name: MyAllowSpecificOrigins,
                          policy =>
                          {
                              policy.WithOrigins("http://localhost:5173")
                                    .AllowAnyHeader()
                                    .AllowAnyMethod()
                                    .AllowCredentials();
                          });
    });

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddSingleton<IFlightRepository, InMemoryFlightRepository>();
    builder.Services.AddSignalR();
    builder.Services.AddHostedService<FlightStatusMonitorService>();

    var app = builder.Build();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors(MyAllowSpecificOrigins);
    app.MapControllers();
    app.MapHub<FlightHub>("/flightHub");
    Log.Information("FlightDeck application starting...");
    app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "FlightDeck application terminated unexpectedly during startup");
}
finally
{
    Log.CloseAndFlush();
}