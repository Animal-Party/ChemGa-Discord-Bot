using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace ChemGa.Database;


internal sealed class AppDatabase : DbContext
{
    public AppDatabase()
    {
        Database.EnsureCreated();
        Database.Migrate();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .EnableThreadSafetyChecks()
            .UseNpgsql(ConnectionString());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var assemblies = new[] { Assembly.GetExecutingAssembly() };

        if (Assembly.GetEntryAssembly() is Assembly entry && !assemblies.Contains(entry))
            assemblies = [.. assemblies, entry];

        var entityTypes = assemblies.SelectMany(a => a.GetTypes())
            .Where(t => t.GetCustomAttribute<DbSetAttribute>() != null && t.IsClass)
            .Distinct();

        foreach (var entityType in entityTypes)
        {
            modelBuilder.Entity(entityType);
            Console.WriteLine($"[AppDatabase] Registered entity: {entityType.FullName}");
        }

        base.OnModelCreating(modelBuilder);
    }
    private static string ConnectionString() => $"Host={Environment.GetEnvironmentVariable("HOST")};Port={Environment.GetEnvironmentVariable("PORT")};Database={Environment.GetEnvironmentVariable("DB_NAME")};Username=${Environment.GetEnvironmentVariable("USER")};Password={Environment.GetEnvironmentVariable("PASSWORD")};Pooling=true;";

    // Ví dụ: Định nghĩa các DbSet với Attribute ở file khác
    // [DbSet]
    // public DbSet<MyEntity> MyEntities { get; set; } = default!;

    /// <summary>
    /// Trả về DbSet tương ứng với kiểu T nếu T có Attribute DbSetAttribute ở class
    /// </summary>
    public DbSet<T> GetModel<T>() where T : class
    {
        var entityType = typeof(T);
        if (entityType.GetCustomAttribute<DbSetAttribute>() == null)
            throw new InvalidOperationException($"Class {entityType.Name} is not assign [DbSet] attribute.");
        return Set<T>();
    }
}
