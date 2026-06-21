using Dapper;
using Domain.Entities;
using Infrastructure.Database;

namespace Infraestructure.DataBase;

/// <summary>
/// ST-08: Seeder que garante a existência do Admin com senha BCrypt no startup.
/// Executado após as migrations DbUp.
/// </summary>
public static class DatabaseSeeder
{
    private static readonly Guid AdminPerfilId = Guid.Parse("A1A1A1A1-A1A1-A1A1-A1A1-A1A1A1A1A1A1");
    private static readonly Guid VendedorPerfilId = Guid.Parse("B2B2B2B2-B2B2-B2B2-B2B2-B2B2B2B2B2B2");
    private static readonly Guid CompradorPerfilId = Guid.Parse("C3C3C3C3-C3C3-C3C3-C3C3-C3C3C3C3C3C3");

    public static void Seed(ConnectionFactory factory)
    {
        using var connection = factory.CreateConnection();

        // Garante os 3 perfis
        const string seedPerfis = @"
            IF NOT EXISTS (SELECT 1 FROM Perfis WHERE Id = @AdminId)
                INSERT INTO Perfis (Id, Nome) VALUES (@AdminId, 'Admin');
            IF NOT EXISTS (SELECT 1 FROM Perfis WHERE Id = @VendedorId)
                INSERT INTO Perfis (Id, Nome) VALUES (@VendedorId, 'Vendedor');
            IF NOT EXISTS (SELECT 1 FROM Perfis WHERE Id = @CompradorId)
                INSERT INTO Perfis (Id, Nome) VALUES (@CompradorId, 'Comprador');";

        connection.Execute(seedPerfis, new
        {
            AdminId = AdminPerfilId,
            VendedorId = VendedorPerfilId,
            CompradorId = CompradorPerfilId
        });

        // Garante o Admin padrão com senha BCrypt
        const string checkAdmin = "SELECT COUNT(1) FROM Usuarios WHERE Email = @Email";
        int adminCount = connection.ExecuteScalar<int>(checkAdmin, new { Email = "admin@soldout.com" });

        if (adminCount == 0)
        {
            string senhaHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");

            var admin = new Usuario("00000000000", "Administrador", "admin@soldout.com", AdminPerfilId, senhaHash);

            const string insertAdmin = @"
                INSERT INTO Usuarios (Cpf, Nome, Email, PerfilId, Senha, Ativo, DataCriacao)
                VALUES (@Cpf, @Nome, @Email, @PerfilId, @Senha, @Ativo, @DataCriacao);";

            connection.Execute(insertAdmin, new
            {
                admin.Cpf,
                admin.Nome,
                admin.Email,
                admin.PerfilId,
                admin.Senha,
                Ativo = true,
                DataCriacao = DateTime.UtcNow
            });

            Console.WriteLine("ST-08: Admin seed criado com BCrypt.");
        }
    }
}
