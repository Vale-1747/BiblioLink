namespace BiblioLink.Services;

public class CuentaSeederHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;

    public CuentaSeederHostedService(IServiceProvider services, IConfiguration configuration)
    {
        _services = services;
        _configuration = configuration;
    }

    public Task StartAsync(CancellationToken cancellationToken) =>
        CuentaSeeder.CrearAdminInicialAsync(_services, _configuration);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
