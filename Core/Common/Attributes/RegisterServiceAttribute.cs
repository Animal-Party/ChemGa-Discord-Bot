using Microsoft.Extensions.DependencyInjection;

namespace ChemGa.Core.Common.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class RegisterServiceAttribute(ServiceLifetime lifetime = ServiceLifetime.Transient, Type? serviceType = null) : Attribute
{
    public Type? ServiceType { get; } = serviceType;
    public ServiceLifetime Lifetime { get; } = lifetime;
}
