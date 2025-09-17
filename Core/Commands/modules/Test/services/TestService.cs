using ChemGa.Core.Common.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace ChemGa.Core.Commands.modules.Test.services;

[RegisterService(ServiceLifetime.Singleton)]
public class TestService
{
    public string GetTestMessage()
    {
        return "This is a test message from TestService.";
    }
}