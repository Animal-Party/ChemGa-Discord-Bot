using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChemGa.Core.Common.Attributes;
using ChemGa.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace ChemGa.Core.Handler;

[RegisterService(ServiceLifetime.Singleton, serviceType: typeof(ServiceActivator))]
public class ServiceActivator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly List<IService> _services = [];
    private readonly IServiceScopeFactory _scopeFactory;
    private IServiceScope? _scope;
    /// <summary>
    /// Khởi động tất cả các dịch vụ đã đăng ký.
    /// </summary>
    public ServiceActivator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
    }
    /// <summary>
    /// Khởi động tất cả các dịch vụ đã đăng ký.
    /// </summary>
    public async Task StartAllAsync()
    {
        _scope ??= _scopeFactory.CreateScope();
        var resolver = _scope.ServiceProvider;

        var services = resolver.GetServices<IService>()
            .Where(s => !ReferenceEquals(s, this));

        foreach (var service in services)
        {
            Log.Information("Starting service: {service}", service.GetType().Name);
            try
            {
                await service.StartAsync();
                _services.Add(service);
            }
            catch (Exception ex)
            {
                Log.Error("Failed to start service {service}: {ex}", service.GetType().Name, ex.Message);
            }
        }

        if (_services.Count == 0)
        {
            Log.Warning("No services were started. Ensure that services are registered correctly.");
        }
        else
        {
            Log.Information("Started {count} services successfully.", _services.Count);
        }
    }

    /// <summary>
    /// Dừng tất cả các dịch vụ đã khởi động.
    /// </summary>
    /// <summary>
    /// Dừng tất cả các dịch vụ đã khởi động.
    /// </summary>
    public async Task StopAllAsync()
    {
        foreach (var service in _services)
        {
            try
            {
                await service.StopAsync();
            }
            catch (Exception ex)
            {
                Log.Error("Failed to stop service {service}: {message}", service.GetType().Name, ex.Message);
            }
        }
        _services.Clear();

        // Dispose the created scope (if any) after stopping services.
        if (_scope != null)
        {
            _scope.Dispose();
            _scope = null;
        }
    }
}