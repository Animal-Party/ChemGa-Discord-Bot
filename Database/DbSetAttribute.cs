namespace ChemGa.Database;

[AttributeUsage(AttributeTargets.Class)]
public class DbSetAttribute : Attribute
{
    public DbSetAttribute()
    { 
        Console.WriteLine("DbSetAttribute created for class");
    }
}
