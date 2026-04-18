using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MinhasFinancas.Infrastructure.Data;

namespace MinhasFinancas.Integration.Tests.Api;

/// <summary>
/// Factory para criar a aplicação web em memória para testes de API.
/// 
/// O Program.cs chama IncludeXmlComments no AddSwaggerGen, que tenta carregar
/// o arquivo XML de documentação. Esse arquivo existe no output da API mas não
/// no output do projeto de testes. Para contornar isso sem modificar a aplicação,
/// copiamos o arquivo XML para o diretório de output dos testes via MSBuild (ver .csproj).
/// </summary>
public class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"TestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove all EF Core related services to avoid conflicts
            var descriptorsToRemove = services
                .Where(d => d.ServiceType.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") == true ||
                           d.ServiceType == typeof(MinhasFinancasDbContext) ||
                           d.ImplementationType == typeof(MinhasFinancasDbContext))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Add fresh DbContext with InMemory database - use same database name for all requests in this factory instance
            services.AddDbContext<MinhasFinancasDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });
        });
    }
}
