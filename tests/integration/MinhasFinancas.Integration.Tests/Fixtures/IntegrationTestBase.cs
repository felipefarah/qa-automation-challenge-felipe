using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MinhasFinancas.Domain.Entities;
using MinhasFinancas.Domain.Interfaces;
using MinhasFinancas.Infrastructure;
using MinhasFinancas.Infrastructure.Data;
using MinhasFinancas.Infrastructure.Queries;

namespace MinhasFinancas.Integration.Tests.Fixtures;

/// <summary>
/// Base para testes de integração.
/// Cria um banco SQLite em memória isolado por teste, garantindo previsibilidade.
/// </summary>
public abstract class IntegrationTestBase : IDisposable
{
    protected readonly MinhasFinancasDbContext DbContext;
    protected readonly IUnitOfWork UnitOfWork;
    protected readonly IMemoryCache MemoryCache;

    protected IntegrationTestBase()
    {
        var options = new DbContextOptionsBuilder<MinhasFinancasDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // banco único por teste
            .Options;

        DbContext = new MinhasFinancasDbContext(options);
        DbContext.Database.EnsureCreated();

        UnitOfWork = new UnitOfWork(DbContext);
        MemoryCache = new MemoryCache(new MemoryCacheOptions());
    }

    // ─── Helpers de seed ──────────────────────────────────────────────────────

    protected async Task<Pessoa> SeedPessoaAdultaAsync(string nome = "João Silva")
    {
        var pessoa = new Pessoa { Nome = nome, DataNascimento = DateTime.Today.AddYears(-30) };
        await UnitOfWork.Pessoas.AddAsync(pessoa);
        await UnitOfWork.SaveChangesAsync();
        return pessoa;
    }

    protected async Task<Pessoa> SeedPessoaMenorAsync(string nome = "Pedro Menor")
    {
        var pessoa = new Pessoa { Nome = nome, DataNascimento = DateTime.Today.AddYears(-15) };
        await UnitOfWork.Pessoas.AddAsync(pessoa);
        await UnitOfWork.SaveChangesAsync();
        return pessoa;
    }

    protected async Task<Categoria> SeedCategoriaDespesaAsync(string descricao = "Alimentação")
    {
        var categoria = new Categoria { Descricao = descricao, Finalidade = Categoria.EFinalidade.Despesa };
        await UnitOfWork.Categorias.AddAsync(categoria);
        await UnitOfWork.SaveChangesAsync();
        return categoria;
    }

    protected async Task<Categoria> SeedCategoriaReceitaAsync(string descricao = "Salário")
    {
        var categoria = new Categoria { Descricao = descricao, Finalidade = Categoria.EFinalidade.Receita };
        await UnitOfWork.Categorias.AddAsync(categoria);
        await UnitOfWork.SaveChangesAsync();
        return categoria;
    }

    protected async Task<Categoria> SeedCategoriaAmbasAsync(string descricao = "Investimentos")
    {
        var categoria = new Categoria { Descricao = descricao, Finalidade = Categoria.EFinalidade.Ambas };
        await UnitOfWork.Categorias.AddAsync(categoria);
        await UnitOfWork.SaveChangesAsync();
        return categoria;
    }

    public void Dispose()
    {
        DbContext.Dispose();
        MemoryCache.Dispose();
        GC.SuppressFinalize(this);
    }
}
