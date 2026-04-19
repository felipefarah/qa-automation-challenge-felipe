using FluentAssertions;
using MinhasFinancas.Domain.Entities;
using MinhasFinancas.Unit.Tests.Helpers;
using System.Reflection;
using Xunit;

namespace MinhasFinancas.Unit.Tests.Domain;

/// <summary>
/// Testes unitários para a entidade Transacao.
/// Regras críticas:
///   - Menor de idade não pode ter receita
///   - Categoria deve ser compatível com o tipo da transação
///
/// Nota: Transacao.Categoria e Transacao.Pessoa têm setter internal.
/// Para testar a lógica de validação desses setters a partir de um assembly externo,
/// usamos reflexão — sem modificar o código da aplicação.
/// </summary>
public class TransacaoTests
{
    // ─── Helper local de reflexão ─────────────────────────────────────────────

    /// <summary>
    /// Invoca o setter internal de Categoria ou Pessoa via reflexão,
    /// disparando as validações de negócio contidas no setter.
    /// </summary>
    private static Action SetCategoria(Transacao t, Categoria c) =>
        () => typeof(Transacao)
            .GetProperty("Categoria", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(t, c);

    private static Action SetPessoa(Transacao t, Pessoa p) =>
        () => typeof(Transacao)
            .GetProperty("Pessoa", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(t, p);

    // ─── Regra: Menor de idade não pode ter receita ───────────────────────────

    [Fact(DisplayName = "Deve lançar exceção ao atribuir pessoa menor de idade em transação de receita")]
    public void Pessoa_MenorDeIdade_EmTransacaoReceita_DeveLancarExcecao()
    {
        var menor = EntityFactory.CriarPessoaMenorDeIdade();
        var categoriaReceita = EntityFactory.CriarCategoriaReceita();
        var transacao = new Transacao
        {
            Descricao = "Receita inválida",
            Valor = 500m,
            Tipo = Transacao.ETipo.Receita,
            Data = DateTime.Today
        };
        SetCategoria(transacao, categoriaReceita)();

        var act = SetPessoa(transacao, menor);

        act.Should().Throw<TargetInvocationException>()
            .WithInnerException<InvalidOperationException>()
            .WithMessage("Menores de 18 anos não podem registrar receitas.");
    }

    [Fact(DisplayName = "Deve permitir atribuir pessoa menor de idade em transação de despesa")]
    public void Pessoa_MenorDeIdade_EmTransacaoDespesa_DevePermitir()
    {
        var menor = EntityFactory.CriarPessoaMenorDeIdade();
        var categoriaDespesa = EntityFactory.CriarCategoriaDespesa();
        var transacao = new Transacao
        {
            Descricao = "Despesa válida",
            Valor = 50m,
            Tipo = Transacao.ETipo.Despesa,
            Data = DateTime.Today
        };
        SetCategoria(transacao, categoriaDespesa)();

        var act = SetPessoa(transacao, menor);

        act.Should().NotThrow();

        var pessoaAtribuida = (Pessoa?)typeof(Transacao)
            .GetProperty("Pessoa", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .GetValue(transacao);

        pessoaAtribuida.Should().Be(menor);
    }

    [Fact(DisplayName = "Deve permitir pessoa adulta em transação de receita")]
    public void Pessoa_Adulta_EmTransacaoReceita_DevePermitir()
    {
        var adulto = EntityFactory.CriarPessoaAdulta();
        var categoriaReceita = EntityFactory.CriarCategoriaReceita();
        var transacao = new Transacao
        {
            Descricao = "Salário",
            Valor = 3000m,
            Tipo = Transacao.ETipo.Receita,
            Data = DateTime.Today
        };
        SetCategoria(transacao, categoriaReceita)();

        SetPessoa(transacao, adulto).Should().NotThrow();
    }

    [Fact(DisplayName = "Pessoa com exatamente 18 anos deve poder registrar receita")]
    public void Pessoa_Exatamente18Anos_EmTransacaoReceita_DevePermitir()
    {
        var pessoa18 = EntityFactory.CriarPessoaComExatamente18Anos();
        var categoriaReceita = EntityFactory.CriarCategoriaReceita();
        var transacao = new Transacao
        {
            Descricao = "Primeiro salário",
            Valor = 1500m,
            Tipo = Transacao.ETipo.Receita,
            Data = DateTime.Today
        };
        SetCategoria(transacao, categoriaReceita)();

        SetPessoa(transacao, pessoa18).Should().NotThrow();
    }

    // ─── Regra: Categoria deve ser compatível com o tipo ─────────────────────

    [Fact(DisplayName = "Deve lançar exceção ao registrar despesa em categoria de receita")]
    public void Categoria_Receita_ComTransacaoDespesa_DeveLancarExcecao()
    {
        var categoriaReceita = EntityFactory.CriarCategoriaReceita();
        var transacao = new Transacao
        {
            Descricao = "Despesa inválida",
            Valor = 100m,
            Tipo = Transacao.ETipo.Despesa,
            Data = DateTime.Today
        };

        var act = SetCategoria(transacao, categoriaReceita);

        act.Should().Throw<TargetInvocationException>()
            .WithInnerException<InvalidOperationException>()
            .WithMessage("Não é possível registrar despesa em categoria de receita.");
    }

    [Fact(DisplayName = "Deve lançar exceção ao registrar receita em categoria de despesa")]
    public void Categoria_Despesa_ComTransacaoReceita_DeveLancarExcecao()
    {
        var categoriaDespesa = EntityFactory.CriarCategoriaDespesa();
        var transacao = new Transacao
        {
            Descricao = "Receita inválida",
            Valor = 100m,
            Tipo = Transacao.ETipo.Receita,
            Data = DateTime.Today
        };

        var act = SetCategoria(transacao, categoriaDespesa);

        act.Should().Throw<TargetInvocationException>()
            .WithInnerException<InvalidOperationException>()
            .WithMessage("Não é possível registrar receita em categoria de despesa.");
    }

    [Fact(DisplayName = "Deve permitir despesa em categoria Ambas")]
    public void Categoria_Ambas_ComTransacaoDespesa_DevePermitir()
    {
        var categoriaAmbas = EntityFactory.CriarCategoriaAmbas();
        var transacao = new Transacao
        {
            Descricao = "Investimento saída",
            Valor = 500m,
            Tipo = Transacao.ETipo.Despesa,
            Data = DateTime.Today
        };

        SetCategoria(transacao, categoriaAmbas).Should().NotThrow();
    }

    [Fact(DisplayName = "Deve permitir receita em categoria Ambas")]
    public void Categoria_Ambas_ComTransacaoReceita_DevePermitir()
    {
        var categoriaAmbas = EntityFactory.CriarCategoriaAmbas();
        var transacao = new Transacao
        {
            Descricao = "Investimento entrada",
            Valor = 1000m,
            Tipo = Transacao.ETipo.Receita,
            Data = DateTime.Today
        };

        SetCategoria(transacao, categoriaAmbas).Should().NotThrow();
    }

    [Fact(DisplayName = "Deve permitir despesa em categoria de despesa")]
    public void Categoria_Despesa_ComTransacaoDespesa_DevePermitir()
    {
        var categoriaDespesa = EntityFactory.CriarCategoriaDespesa();
        var transacao = new Transacao
        {
            Descricao = "Compra no mercado",
            Valor = 150m,
            Tipo = Transacao.ETipo.Despesa,
            Data = DateTime.Today
        };

        SetCategoria(transacao, categoriaDespesa).Should().NotThrow();
    }

    // ─── Criação de transação completa válida via EntityFactory ───────────────

    [Fact(DisplayName = "Deve criar transação de despesa completa e válida via factory")]
    public void CriarTransacaoDespesa_Valida_DeveConfigurarTodasAsPropriedades()
    {
        var transacao = EntityFactory.CriarTransacaoDespesa(valor: 250m, descricao: "Conta de luz");

        transacao.Id.Should().NotBe(Guid.Empty);
        transacao.Descricao.Should().Be("Conta de luz");
        transacao.Valor.Should().Be(250m);
        transacao.Tipo.Should().Be(Transacao.ETipo.Despesa);
        transacao.Categoria.Should().NotBeNull();
        transacao.Pessoa.Should().NotBeNull();
        transacao.CategoriaId.Should().Be(transacao.Categoria!.Id);
        transacao.PessoaId.Should().Be(transacao.Pessoa!.Id);
    }

    [Fact(DisplayName = "Deve criar transação de receita completa e válida via factory")]
    public void CriarTransacaoReceita_Valida_DeveConfigurarTodasAsPropriedades()
    {
        var transacao = EntityFactory.CriarTransacaoReceita(valor: 5000m, descricao: "Bônus anual");

        transacao.Id.Should().NotBe(Guid.Empty);
        transacao.Descricao.Should().Be("Bônus anual");
        transacao.Valor.Should().Be(5000m);
        transacao.Tipo.Should().Be(Transacao.ETipo.Receita);
        transacao.Pessoa!.EhMaiorDeIdade().Should().BeTrue();
    }
}
