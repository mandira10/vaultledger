using DbUp;
using Npgsql;

namespace VaultLedger.Infrastructure.Migrations;

public static class MigrationRunner
{
    private const string UpResourcePrefix = "VaultLedger.Infrastructure.Migrations.Scripts.up.";
    private const string DownResourcePrefix = "VaultLedger.Infrastructure.Migrations.Scripts.down.";

    public static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: migrate <up|down|status|preview>");
            return 1;
        }

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings__DefaultConnection env var is required.");

        return args[0].ToLowerInvariant() switch
        {
            "up" => RunUp(connectionString),
            "down" => RunDown(connectionString),
            "status" => Status(connectionString),
            "preview" => Preview(connectionString),
            _ => Unknown(args[0]),
        };
    }

    public static int RunUp(string connectionString)
    {
        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                typeof(MigrationRunner).Assembly,
                s => s.StartsWith(UpResourcePrefix) && s.EndsWith(".sql"))
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();
        if (!result.Successful)
        {
            Console.Error.WriteLine($"Migration failed on {result.ErrorScript?.Name}: {result.Error?.Message}");
            return 1;
        }

        Console.WriteLine("Migration succeeded.");
        return 0;
    }

    public static int Status(string connectionString)
    {
        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                typeof(MigrationRunner).Assembly,
                s => s.StartsWith(UpResourcePrefix) && s.EndsWith(".sql"))
            .Build();

        var pending = upgrader.GetScriptsToExecute();
        if (pending.Count == 0)
        {
            Console.WriteLine("Database is up to date.");
            return 0;
        }

        Console.WriteLine($"Pending scripts ({pending.Count}):");
        foreach (var s in pending)
            Console.WriteLine($"  - {s.Name}");
        return 0;
    }

    public static int Preview(string connectionString)
    {
        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                typeof(MigrationRunner).Assembly,
                s => s.StartsWith(UpResourcePrefix) && s.EndsWith(".sql"))
            .Build();

        foreach (var s in upgrader.GetScriptsToExecute())
        {
            Console.WriteLine($"---- {s.Name} ----");
            Console.WriteLine(s.Contents);
            Console.WriteLine();
        }
        return 0;
    }

    // MVP rollback: execute the matching .down.sql for the most recently applied script,
    // then remove its row from schemaversions so the up script can be reapplied.
    public static int RunDown(string connectionString)
    {
        var applied = GetAppliedScripts(connectionString);
        if (applied.Count == 0)
        {
            Console.WriteLine("No migrations applied.");
            return 0;
        }

        var lastApplied = applied[^1];
        var downResource = lastApplied
            .Replace(UpResourcePrefix, DownResourcePrefix)
            .Replace(".sql", ".down.sql");

        var assembly = typeof(MigrationRunner).Assembly;
        using var stream = assembly.GetManifestResourceStream(downResource);
        if (stream is null)
        {
            Console.Error.WriteLine($"No down script found for {lastApplied} (expected {downResource}).");
            return 1;
        }

        using var reader = new StreamReader(stream);
        var sql = reader.ReadToEnd();

        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        using var tx = conn.BeginTransaction();

        using (var cmd = new NpgsqlCommand(sql, conn, tx))
            cmd.ExecuteNonQuery();

        using (var delete = new NpgsqlCommand(
            "DELETE FROM schemaversions WHERE scriptname = @name", conn, tx))
        {
            delete.Parameters.AddWithValue("name", lastApplied);
            delete.ExecuteNonQuery();
        }

        tx.Commit();

        Console.WriteLine($"Rolled back: {lastApplied}");
        return 0;
    }

    private static List<string> GetAppliedScripts(string connectionString)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        using var check = new NpgsqlCommand(
            "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'schemaversions')",
            conn);
        if (!(bool)check.ExecuteScalar()!)
            return new List<string>();

        using var cmd = new NpgsqlCommand(
            "SELECT scriptname FROM schemaversions ORDER BY applied",
            conn);
        using var reader = cmd.ExecuteReader();

        var list = new List<string>();
        while (reader.Read())
            list.Add(reader.GetString(0));
        return list;
    }

    private static int Unknown(string cmd)
    {
        Console.Error.WriteLine($"Unknown command: {cmd}");
        Console.Error.WriteLine("Usage: migrate <up|down|status|preview>");
        return 1;
    }
}
