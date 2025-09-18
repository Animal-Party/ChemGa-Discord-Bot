using System.Reflection;
using dotenv.net;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Extensions.Logging;

namespace ChemGa.Database;


public sealed class AppDatabase : DbContext
{
    public AppDatabase()
    {
        DotEnv.Load();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .EnableThreadSafetyChecks()
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging(false)
            .UseNpgsql(ConnectionString(), opts =>
            {
                opts.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                opts.CommandTimeout(30);
                opts.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
            });
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
            Log.Information("[{Source}] Registered entity: {entityType}", nameof(AppDatabase), entityType.Name.ToLower());
        }

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName(entity.GetTableName()?.ToLower());
        }
        base.OnModelCreating(modelBuilder);
    }
    private static string ConnectionString() =>
        $"Host={Environment.GetEnvironmentVariable("HOST") ?? "localhost"};Port={Environment.GetEnvironmentVariable("PORT") ?? "5432"};Database={Environment.GetEnvironmentVariable("DB_NAME") ?? "main"};Username={Environment.GetEnvironmentVariable("USER")};Password={Environment.GetEnvironmentVariable("PASSWORD")};Pooling=true;";

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
