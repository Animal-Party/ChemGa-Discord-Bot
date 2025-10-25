namespace ChemGa.Core.Common.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class CronJobAttribute(string expression) : Attribute
{
    public string Expression { get; } = expression ?? throw new ArgumentNullException(nameof(expression));
}
