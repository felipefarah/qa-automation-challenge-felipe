using FluentAssertions;
using MinhasFinancas.Application.DTOs;
using MinhasFinancas.Application.Services;
using MinhasFinancas.Domain.Entities;
using MinhasFinancas.Integration.Tests.Fixtures;
using Xunit;

namespace MinhasFinancas.Integration.Tests.Services;

/// <summary>
/// Testes de integração para TransacaoService.
/// Valida a integração entre serviço, repositório e banco de dados em memória.
/// </summary>
public class TransacaoServiceIntegrationTests : IntegrationTestBase
{
    private readonly TransacaoService _sut;

    public TransacaoServiceIntegrationTests()
    {
        _sut = new TransacaoService(UnitOfWork);
    }

    // ─── Criação de transações válidas ────────────────────────────────────────

    [Fact(DisplayName = "Deve criar transação de despesa e persistir no banco")]
    public async Task CreateAsync_DespesaValida_DevePersistitrNoBanco()
    {
        // Arrange
        var pessoa = await SeedPessoaAdultaAsync();
        var categoria = await SeedCategoriaDespesaAsync();

        var dto = new CreateTransacaoDto
        {
            Descricao = "Compra no supermercado",
            Valor = 250.50m,
            Tipo = Transacao.ETipo.Despesa,
            CategoriaId = categoria.Id,
            PessoaId = pessoa.Id,
            Data = DateTime.Today
        };

        // Act
        var resultado = await _sut.CreateAsync(dto);

        // Assert
        resultado.Should().NotBeNull();
        resultado.Id.Should().NotBe(Guid.Empty);

        // Verificar persistência real no banco
        var transacaoNoBanco = await UnitOfWork.Transacoes.GetByIdAsync(resultado.Id);
        transacaoNoBanco.Should().NotBeNull();
        transacaoNoBanco!.Descricao.Should().Be("Compra no supermercado");
        transacaoNoBanco.Valor.Should().Be(250.50m);
        transacaoNoBanco.Tipo.Should().Be(Transacao.ETipo.Despesa);
    }

    [Fact(DisplayName = "Deve criar transação de receita para adulto e persistir no banco")]
    public async Task CreateAsync_ReceitaAdulto_DevePersistitrNoBanco()
    {
        // Arrange
        var pessoa = await SeedPessoaAdultaAsync("Maria Santos");
        var categoria = await SeedCategoriaReceitaAsync("Salário");

        var dto = new CreateTransacaoDto
        {
            Descricao = "Salário mensal",
            Valor = 5000m,
            Tipo = Transacao.ETipo.Receita,
            CategoriaId = categoria.Id,
            PessoaId = pessoa.Id,
            Data = DateTime.Today
        };

        // Act
        var resultado = await _sut.CreateAsync(dto);

        // Assert
        resultado.Tipo.Should().Be(Transacao.ETipo.Receita);
        resultado.Valor.Should().Be(5000m);

        var transacaoNoBanco = await UnitOfWork.Transacoes.GetByIdAsync(resultado.Id);
        transacaoNoBanco.Should().NotBeNull();
    }

    [Fact(DisplayName = "Deve criar transação em categoria Ambas para despesa")]
    public async Task CreateAsync_DespesaEmCategoriaAmbas_DevePersistitr()
    {
        // Arrange
        var pessoa = await SeedPessoaAdultaAsync();
        var categoria = await SeedCategoriaAmbasAsync("Investimentos");

        var dto = new CreateTransacaoDto
        {
            Descricao = "Aporte em fundo",
            Valor = 1000m,
            Tipo = Transacao.ETipo.Despesa,
            CategoriaId = categoria.Id,
            PessoaId = pessoa.Id,
            Data = DateTime.Today
        };

        // Act
        var resultado = await _sut.CreateAsync(dto);

        // Assert
        resultado.Should().NotBeNull();
        resultado.CategoriaId.Should().Be(categoria.Id);
    }

    // ─── Criação de transações inválidas ──────────────────────────────────────

    [Fact(DisplayName = "Deve rejeitar receita para menor de idade")]
    public async Task CreateAsync_ReceitaMenorDeIdade_DeveLancarExcecao()
    {
        // Arrange
        var menor = await SeedPessoaMenorAsync();
        var categoria = await SeedCategoriaReceitaAsync();

        var dto = new CreateTransacaoDto
        {
            Descricao = "Receita inválida",
            Valor = 500m,
            Tipo = Transacao.ETipo.Receita,
            CategoriaId = categoria.Id,
            PessoaId = menor.Id,
            Data = DateTime.Today
        };

        // Act
        var act = async () => await _sut.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Menores de 18 anos não podem registrar receitas.");
    }

    [Fact(DisplayName = "Deve rejeitar despesa em categoria de receita")]
    public async Task CreateAsync_DespesaEmCategoriaReceita_DeveLancarExcecao()
    {
        // Arrange
        var pessoa = await SeedPessoaAdultaAsync();
        var categoriaReceita = await SeedCategoriaReceitaAsync();

        var dto = new CreateTransacaoDto
        {
            Descricao = "Despesa inválida",
            Valor = 100m,
            Tipo = Transacao.ETipo.Despesa,
            CategoriaId = categoriaReceita.Id,
            PessoaId = pessoa.Id,
            Data = DateTime.Today
        };

        // Act
        var act = async () => await _sut.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Não é possível registrar despesa em categoria de receita.");
    }

    [Fact(DisplayName = "Deve rejeitar receita em categoria de despesa")]
    public async Task CreateAsync_ReceitaEmCategoriaDespesa_DeveLancarExcecao()
    {
        // Arrange
        var pessoa = await SeedPessoaAdultaAsync();
        var categoriaDespesa = await SeedCategoriaDespesaAsync();

        var dto = new CreateTransacaoDto
        {
            Descricao = "Receita inválida",
            Valor = 100m,
            Tipo = Transacao.ETipo.Receita,
            CategoriaId = categoriaDespesa.Id,
            PessoaId = pessoa.Id,
            Data = DateTime.Today
        };

        // Act
        var act = async () => await _sut.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Não é possível registrar receita em categoria de despesa.");
    }

    [Fact(DisplayName = "Deve rejeitar transação com categoria inexistente")]
    public async Task CreateAsync_CategoriaInexistente_DeveLancarArgumentException()
    {
        // Arrange
        var pessoa = await SeedPessoaAdultaAsync();

        var dto = new CreateTransacaoDto
        {
            Descricao = "Teste",
            Valor = 100m,
            Tipo = Transacao.ETipo.Despesa,
            CategoriaId = Guid.NewGuid(), // não existe
            PessoaId = pessoa.Id,
            Data = DateTime.Today
        };

        // Act
        var act = async () => await _sut.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Categoria não encontrada*");
    }

    [Fact(DisplayName = "Deve rejeitar transação com pessoa inexistente")]
    public async Task CreateAsync_PessoaInexistente_DeveLancarArgumentException()
    {
        // Arrange
        var categoria = await SeedCategoriaDespesaAsync();

        var dto = new CreateTransacaoDto
        {
            Descricao = "Teste",
            Valor = 100m,
            Tipo = Transacao.ETipo.Despesa,
            CategoriaId = categoria.Id,
            PessoaId = Guid.NewGuid(), // não existe
            Data = DateTime.Today
        };

        // Act
        var act = async () => await _sut.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Pessoa não encontrada*");
    }

    // ─── GetAllAsync com paginação ────────────────────────────────────────────

    [Fact(DisplayName = "GetAllAsync deve retornar transações paginadas")]
    public async Task GetAllAsync_ComTransacoes_DeveRetornarPaginado()
    {
        // Arrange
        var pessoa = await SeedPessoaAdultaAsync();
        var categoria = await SeedCategoriaDespesaAsync();

        for (int i = 1; i <= 5; i++)
        {
            await _sut.CreateAsync(new CreateTransacaoDto
            {
                Descricao = $"Transação {i}",
                Valor = i * 10m,
                Tipo = Transacao.ETipo.Despesa,
                CategoriaId = categoria.Id,
                PessoaId = pessoa.Id,
                Data = DateTime.Today
            });
        }

        // Act
        var resultado = await _sut.GetAllAsync(new MinhasFinancas.Domain.ValueObjects.PagedRequest
        {
            Page = 1,
            PageSize = 3
        });

        // Assert
        resultado.Should().NotBeNull();
        resultado.Items.Should().HaveCount(3);
        resultado.TotalCount.Should().Be(5);
        resultado.TotalPages.Should().Be(2);
    }
}
