using WifiProvisioning.Core.Configuration;
using WifiProvisioning.Core.Services;
using WifiProvisioning.Api.Middleware;


var builder = WebApplication.CreateBuilder(args);

builder.Services
    .Configure<SpeedProfileApiOptions>(
        builder.Configuration.GetSection(SpeedProfileApiOptions.SectionName));

builder.Services
    .Configure<ActivationApiOptions>(
        builder.Configuration.GetSection(ActivationApiOptions.SectionName));

// Register HttpClient for the Network Infrastructure API (speed profile catalog)
builder.Services.AddHttpClient<ISpeedProfileClient, SpeedProfileClient>((sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SpeedProfileApiOptions>>().Value;

    if (string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        throw new InvalidOperationException(
            "SpeedProfileApi:BaseUrl is not configured.");
    }

    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

// Register HttpClient for the Network Controller API (activation)
builder.Services.AddHttpClient<IActivationClient, ActivationClient>((sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ActivationApiOptions>>().Value;

    if (string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        throw new InvalidOperationException(
            "ActivationApi:BaseUrl is not configured.");
    }

    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

builder.Services.AddScoped<IProvisioningService, ProvisioningService>();

builder.Services.AddControllers();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapControllers();

app.Run();

public partial class Program { }
