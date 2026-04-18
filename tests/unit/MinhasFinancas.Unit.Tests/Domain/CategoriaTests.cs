using FluentAssertions;
using MinhasFinancas.Domain.Entities;
using MinhasFinancas.Unit.Tests.Helpers;
using Xunit;

namespace MinhasFinancas.Unit.Tests.Domain;

/// <summary>
/// Testes unitários para a entidade Categoria.
/// Cobre: validação de tipo permitido por finalidade.
/// </summary>
public class CategoriaTests
{
    // ─── Categoria de Despesa ─────────────────────────────────────────────────

    [Fact(DisplayName = "Categoria Despesa deve permitir transação do tipo Despesa")]
    public void PermiteTipo_CategoriaDespesa_ComTipoDespesa_DeveRetornarTrue()
    {
        // Arrange
        var categoria = EntityFactory.CriarCategoriaDespesa();

        // Act
        var resultado = categoria.PermiteTipo(Transacao.ETipo.Despesa);

        // Assert
        resultado.Should().BeTrue();
    }

    [Fact(DisplayName = "Categoria Despesa não deve permitir transação do tipo Receita")]
    public void PermiteTipo_CategoriaDespesa_ComTipoReceita_DeveRetornarFalse()
    {
        // Arrange
        var categoria = EntityFactory.CriarCategoriaDespesa();

        // Act
        var resultado = categoria.PermiteTipo(Transacao.ETipo.Receita);

        // Assert
        resultado.Should().BeFalse();
    }

    // ─── Categoria de Receita ─────────────────────────────────────────────────

    [Fact(DisplayName = "Categoria Receita deve permitir transação do tipo Receita")]
    public void PermiteTipo_CategoriaReceita_ComTipoReceita_DeveRetornarTrue()
    {
        // Arrange
        var categoria = EntityFactory.CriarCategoriaReceita();

        // Act
        var resultado = categoria.PermiteTipo(Transacao.ETipo.Receita);

        // Assert
        resultado.Should().BeTrue();
    }

    [Fact(DisplayName = "Categoria Receita não deve permitir transação do tipo Despesa")]
    public void PermiteTipo_CategoriaReceita_ComTipoDespesa_DeveRetornarFalse()
    {
        // Arrange
        var categoria = EntityFactory.CriarCategoriaReceita();

        // Act
        var resultado = categoria.PermiteTipo(Transacao.ETipo.Despesa);

        // Assert
        resultado.Should().BeFalse();
    }

    // ─── Categoria Ambas ──────────────────────────────────────────────────────

    [Fact(DisplayName = "Categoria Ambas deve permitir transação do tipo Despesa")]
    public void PermiteTipo_CategoriaAmbas_ComTipoDespesa_DeveRetornarTrue()
    {
        // Arrange
        var categoria = EntityFactory.CriarCategoriaAmbas();

        // Act
        var resultado = categoria.PermiteTipo(Transacao.ETipo.Despesa);

        // Assert
        resultado.Should().BeTrue();
    }

    [Fact(DisplayName = "Categoria Ambas deve permitir transação do tipo Receita")]
    public void PermiteTipo_CategoriaAmbas_ComTipoReceita_DeveRetornarTrue()
    {
        // Arrange
        var categoria = EntityFactory.CriarCategoriaAmbas();

        // Act
        var resultado = categoria.PermiteTipo(Transacao.ETipo.Receita);

        // Assert
        resultado.Should().BeTrue();
    }

    // ─── Theory: Matriz completa de permissões ────────────────────────────────

    [Theory(DisplayName = "PermiteTipo deve respeitar a matriz de finalidade x tipo")]
    [InlineData(Categoria.EFinalidade.Despesa, Transacao.ETipo.Despesa, true)]
    [InlineData(Categoria.EFinalidade.Despesa, Transacao.ETipo.Receita, false)]
    [InlineData(Categoria.EFinalidade.Receita, Transacao.ETipo.Receita, true)]
    [InlineData(Categoria.EFinalidade.Receita, Transacao.ETipo.Despesa, false)]
    [InlineData(Categoria.EFinalidade.Ambas, Transacao.ETipo.Despesa, true)]
    [InlineData(Categoria.EFinalidade.Ambas, Transacao.ETipo.Receita, true)]
    public void PermiteTipo_MatrizCompleta_DeveRetornarResultadoEsperado(
        Categoria.EFinalidade finalidade,
        Transacao.ETipo tipo,
        bool esperado)
    {
        // Arrange
        var categoria = new Categoria { Descricao = "Teste", Finalidade = finalidade };

        // Act
        var resultado = categoria.PermiteTipo(tipo);

        // Assert
        resultado.Should().Be(esperado);
    }

    // ─── Propriedades ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "Categoria deve ter ID gerado automaticamente ao ser criada")]
    public void Categoria_AoCriar_DeveGerarIdAutomaticamente()
    {
        // Arrange & Act
        var categoria = EntityFactory.CriarCategoriaDespesa();

        // Assert
        categoria.Id.Should().NotBe(Guid.Empty);
    }
}
