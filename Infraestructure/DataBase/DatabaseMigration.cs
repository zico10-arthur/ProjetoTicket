using System.Reflection;
using DbUp;

namespace Infrastructure.Database;

public static class DatabaseMigration
{
    public static void Initialize(string connectionString)
    {
        EnsureDatabase.For.SqlDatabase(connectionString);

        // 2. Configura o DbUp para procurar os scripts embutidos e rodar
        var upgrader = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
            .LogToConsole()
            .Build();

        // 3. Executa a atualização
        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(result.Error);
            Console.ResetColor();
            throw new Exception("Erro ao rodar os scripts do banco de dados", result.Error);
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Banco de dados atualizado com sucesso!");
        Console.ResetColor();
    }
}