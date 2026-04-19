using FluentAssertions;
using MinhasFinancas.Domain.Entities;
using MinhasFinancas.Unit.Tests.Helpers;
using Xunit;

namespace MinhasFinancas.Unit.Tests.Domain;

/// <summary>
/// Testes unitários para a entidade Pessoa.
/// Cobre: cálculo de idade, maioridade e edge cases de data de nascimento.
/// </summary>
public class PessoaTests
{
    // ─── Cálculo de Idade ─────────────────────────────────────────────────────

    [Fact(DisplayName = "Idade deve ser calculada corretamente para adulto")]
    public void Idade_PessoaAdulta_DeveRetornarIdadeCorreta()
    {
        var pessoa = EntityFactory.CriarPessoa("João", DateTime.Today.AddYears(-30));

        var idade = pessoa.Idade;

        idade.Should().Be(30);
    }

    [Fact(DisplayName = "Idade deve ser 0 para pessoa nascida hoje")]
    public void Idade_NascidaHoje_DeveRetornarZero()
    {
        var pessoa = EntityFactory.CriarPessoa("Bebê", DateTime.Today);

        pessoa.Idade.Should().Be(0);
    }

    [Fact(DisplayName = "Idade não deve incrementar antes do aniversário no ano corrente")]
    public void Idade_AniversarioAmanha_NaoDeveIncrementar()
    {
        var dataNascimento = DateTime.Today.AddYears(-18).AddDays(1);
        var pessoa = EntityFactory.CriarPessoa("Quase 18", dataNascimento);

        pessoa.Idade.Should().Be(17);
    }

    [Fact(DisplayName = "Idade deve incrementar no dia do aniversário")]
    public void Idade_AniversarioHoje_DeveIncrementar()
    {
        var pessoa = EntityFactory.CriarPessoaComExatamente18Anos();

        pessoa.Idade.Should().Be(18);
    }

    [Theory(DisplayName = "Idade deve ser calculada corretamente para diferentes idades")]
    [InlineData(1)]
    [InlineData(17)]
    [InlineData(18)]
    [InlineData(65)]
    [InlineData(100)]
    public void Idade_DiferentesIdades_DeveCalcularCorretamente(int anosEsperados)
    {
        var pessoa = EntityFactory.CriarPessoa("Teste", DateTime.Today.AddYears(-anosEsperados));

        pessoa.Idade.Should().Be(anosEsperados);
    }

    // ─── Maioridade ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "EhMaiorDeIdade deve retornar true para adulto de 30 anos")]
    public void EhMaiorDeIdade_PessoaAdulta_DeveRetornarTrue()
    {
        var pessoa = EntityFactory.CriarPessoaAdulta();

        pessoa.EhMaiorDeIdade().Should().BeTrue();
    }

    [Fact(DisplayName = "EhMaiorDeIdade deve retornar true para pessoa com exatamente 18 anos")]
    public void EhMaiorDeIdade_Exatamente18Anos_DeveRetornarTrue()
    {
        var pessoa = EntityFactory.CriarPessoaComExatamente18Anos();

        pessoa.EhMaiorDeIdade().Should().BeTrue();
    }

    [Fact(DisplayName = "EhMaiorDeIdade deve retornar false para menor de 18 anos")]
    public void EhMaiorDeIdade_MenorDeIdade_DeveRetornarFalse()
    {
        var pessoa = EntityFactory.CriarPessoaMenorDeIdade();

        pessoa.EhMaiorDeIdade().Should().BeFalse();
    }

    [Fact(DisplayName = "EhMaiorDeIdade deve retornar false para pessoa com 17 anos e 364 dias")]
    public void EhMaiorDeIdade_QuaseDezoitoAnos_DeveRetornarFalse()
    {
        var dataNascimento = DateTime.Today.AddYears(-18).AddDays(1);
        var pessoa = EntityFactory.CriarPessoa("Quase adulto", dataNascimento);

        pessoa.EhMaiorDeIdade().Should().BeFalse();
    }

    [Theory(DisplayName = "EhMaiorDeIdade deve retornar false para idades menores que 18")]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(17)]
    public void EhMaiorDeIdade_MenorQue18_DeveRetornarFalse(int anos)
    {
        var pessoa = EntityFactory.CriarPessoa("Menor", DateTime.Today.AddYears(-anos));

        pessoa.EhMaiorDeIdade().Should().BeFalse();
    }

    [Theory(DisplayName = "EhMaiorDeIdade deve retornar true para idades maiores ou iguais a 18")]
    [InlineData(18)]
    [InlineData(19)]
    [InlineData(30)]
    [InlineData(65)]
    public void EhMaiorDeIdade_MaiorOuIgual18_DeveRetornarTrue(int anos)
    {
        var pessoa = EntityFactory.CriarPessoa("Adulto", DateTime.Today.AddYears(-anos));

        pessoa.EhMaiorDeIdade().Should().BeTrue();
    }

    // ─── Propriedades ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "Pessoa deve ter ID gerado automaticamente ao ser criada")]
    public void Pessoa_AoCriar_DeveGerarIdAutomaticamente()
    {
        var pessoa = EntityFactory.CriarPessoaAdulta();

        pessoa.Id.Should().NotBe(Guid.Empty);
    }

    [Fact(DisplayName = "Duas pessoas criadas devem ter IDs diferentes")]
    public void Pessoa_DuasCriadas_DevemTerIdsDiferentes()
    {
        var pessoa1 = EntityFactory.CriarPessoaAdulta("Pessoa 1");
        var pessoa2 = EntityFactory.CriarPessoaAdulta("Pessoa 2");

        pessoa1.Id.Should().NotBe(pessoa2.Id);
    }
}
