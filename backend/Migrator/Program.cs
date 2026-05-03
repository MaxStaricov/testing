using DbUp;
using System;
using System.Reflection;
using System.Threading;

namespace Migrator;

class Program
{
    static int Main(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ??
                               "Host=localhost;Port=5432;Database=recipes;Username=postgres;Password=postgres";

        Console.WriteLine("Starting Migrator...");
        Console.WriteLine($"Connection string: {connectionString}");

        bool success = false;
        int retries = 10;
        
        while (!success && retries > 0)
        {
            try
            {
                EnsureDatabase.For.PostgresqlDatabase(connectionString);
                success = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to ensure database (retrying in 5s...): {ex.Message}");
                Thread.Sleep(5000);
                retries--;
            }
        }

        if (!success)
        {
            Console.WriteLine("Could not connect to the database to ensure it exists.");
            return -1;
        }

        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsFromFileSystem("Scripts")
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(result.Error);
            Console.ResetColor();
#if DEBUG
            Console.ReadLine();
#endif
            return -1;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Success!");
        Console.ResetColor();
        return 0;
    }
}
