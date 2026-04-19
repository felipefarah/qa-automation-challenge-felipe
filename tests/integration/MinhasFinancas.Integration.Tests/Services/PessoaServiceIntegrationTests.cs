using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MinhasFinancas.Application.DTOs;
using MinhasFinancas.Application.Services;
using MinhasFinancas.Domain.Entities;
using MinhasFinancas.Integration.Tests.Fixtures;
using Xunit;

namespace MinhasFinancas.Integration.Tests.Services;

/// <summary>
/// Testes de integração para PessoaService.
/// Valida CRUD completo e comportamento de cascade delete.
/// </summary>
public class PessoaServiceIntegrationTests : IntegrationTestBase
{
    private readonly PessoaService _sut;

    public PessoaServiceIntegrationTests()
    {
        _sut = new PessoaService(UnitOfWork);
    }

    // ─── CreateAsync ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "Deve criar pessoa e persistir no banco")]
    public async Task CreateAsync_DadosValidos_DevePersistitrNoBanco()
    {
        var dto = new CreatePessoaDto
        {
            Nome = "Ana Oliveira",
            DataNascimento = new DateTime(1992, 8, 20)
        };

        var resultado = await _sut.CreateAsync(dto);

        resultado.Id.Should().NotBe(Guid.Empty);

        var pessoaNoBanco = await UnitOfWork.Pessoas.GetByIdAsync(resultado.Id);
        pessoaNoBanco.Should().NotBeNull();
        pessoaNoBanco!.Nome.Should().Be("Ana Oliveira");
        pessoaNoBanco.DataNascimento.Should().Be(new DateTime(1992, 8, 20));
    }

    // ─── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "Deve atualizar pessoa e persistir mudanças no banco")]
    public async Task UpdateAsync_DadosValidos_DeveAtualizarNoBanco()
    {
        var pessoa = await SeedPessoaAdultaAsync("Nome Original");
        var dto = new UpdatePessoaDto
        {
            Nome = "Nome Atualizado",
            DataNascimento = new DateTime(1988, 3, 10)
        };

        await _sut.UpdateAsync(pessoa.Id, dto);

        var pessoaAtualizada = await UnitOfWork.Pessoas.GetByIdAsync(pessoa.Id);
        pessoaAtualizada!.Nome.Should().Be("Nome Atualizado");
        pessoaAtualizada.DataNascimento.Should().Be(new DateTime(1988, 3, 10));
    }

    [Fact(DisplayName = "UpdateAsync deve lançar KeyNotFoundException para ID inexistente")]
    public async Task UpdateAsync_IdInexistente_DeveLancarKeyNotFoundException()
    {
        var dto = new UpdatePessoaDto { Nome = "Teste", DataNascimento = DateTime.Today.AddYears(-25) };

        var act = async () => await _sut.UpdateAsync(Guid.NewGuid(), dto);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ─── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "Deve deletar pessoa do banco")]
    public async Task DeleteAsync_PessoaExistente_DeveRemoverDoBanco()
    {
        var pessoa = await SeedPessoaAdultaAsync("Para Deletar");

        await _sut.DeleteAsync(pessoa.Id);

        var pessoaNoBanco = await UnitOfWork.Pessoas.GetByIdAsync(pessoa.Id);
        pessoaNoBanco.Should().BeNull();
    }

    [Fact(DisplayName = "Deve deletar pessoa e suas transações (cascade delete)")]
    public async Task DeleteAsync_PessoaComTransacoes_DeveRemoverTransacoesEmCascata()
    {
        
        var pessoa = await SeedPessoaAdultaAsync("Pessoa Com Transações");
        var categoria = await SeedCategoriaDespesaAsync();

        var transacaoService = new TransacaoService(UnitOfWork);
        var t1 = await transacaoService.CreateAsync(new CreateTransacaoDto
        {
            Descricao = "Transação 1",
            Valor = 100m,
            Tipo = Transacao.ETipo.Despesa,
            CategoriaId = categoria.Id,
            PessoaId = pessoa.Id,
            Data = DateTime.Today
        });
        var t2 = await transacaoService.CreateAsync(new CreateTransacaoDto
        {
            Descricao = "Transação 2",
            Valor = 200m,
            Tipo = Transacao.ETipo.Despesa,
            CategoriaId = categoria.Id,
            PessoaId = pessoa.Id,
            Data = DateTime.Today
        });

        (await UnitOfWork.Transacoes.GetByIdAsync(t1.Id)).Should().NotBeNull();
        (await UnitOfWork.Transacoes.GetByIdAsync(t2.Id)).Should().NotBeNull();

        await _sut.DeleteAsync(pessoa.Id);

        var pessoaNoBanco = await UnitOfWork.Pessoas.GetByIdAsync(pessoa.Id);
        pessoaNoBanco.Should().BeNull();

        var transacoesRestantes = await UnitOfWork.Transacoes.FindAsync(t => t.PessoaId == pessoa.Id);
        transacoesRestantes.Should().BeEmpty();
    }

    // ─── GetAllAsync com busca ────────────────────────────────────────────────

    [Fact(DisplayName = "GetAllAsync deve retornar todas as pessoas")]
    public async Task GetAllAsync_ComPessoas_DeveRetornarTodasAsPessoas()
    {
        await SeedPessoaAdultaAsync("Alice");
        await SeedPessoaAdultaAsync("Bob");
        await SeedPessoaAdultaAsync("Carlos");

        var resultado = await _sut.GetAllAsync();

        resultado.Items.Should().HaveCount(3);
        resultado.TotalCount.Should().Be(3);
    }

    [Fact(DisplayName = "GetAllAsync deve filtrar por nome quando search é fornecido")]
    public async Task GetAllAsync_ComSearch_DeveRetornarApenasCorrespondentes()
    {
        await SeedPessoaAdultaAsync("Alice Souza");
        await SeedPessoaAdultaAsync("Bob Silva");
        await SeedPessoaAdultaAsync("Alice Ferreira");

        var resultado = await _sut.GetAllAsync(new MinhasFinancas.Domain.ValueObjects.PagedRequest
        {
            Search = "Alice"
        });

        
        resultado.Items.Should().HaveCount(2);
        resultado.Items.Should().OnlyContain(p => p.Nome.Contains("Alice"));
    }
}
