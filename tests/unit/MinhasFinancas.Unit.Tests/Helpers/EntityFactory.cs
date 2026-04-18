using MinhasFinancas.Domain.Entities;
using System.Reflection;

namespace MinhasFinancas.Unit.Tests.Helpers;

/// <summary>
/// Factory para criação de entidades de domínio nos testes.
/// Como Transacao.Categoria e Transacao.Pessoa têm setter internal (assembly MinhasFinancas.Domain),
/// usamos reflexão para atribuí-los a partir de assemblies externos de teste.
/// </summary>
public static class EntityFactory
{
    // ─── Pessoa ───────────────────────────────────────────────────────────────

    public static Pessoa CriarPessoaAdulta(string nome = "João Silva") =>
        new() { Nome = nome, DataNascimento = DateTime.Today.AddYears(-30) };

    public static Pessoa CriarPessoaComExatamente18Anos() =>
        new() { Nome = "Adulto Limite", DataNascimento = DateTime.Today.AddYears(-18) };

    public static Pessoa CriarPessoaMenorDeIdade(string nome = "Pedro Menor") =>
        new() { Nome = nome, DataNascimento = DateTime.Today.AddYears(-17) };

    public static Pessoa CriarPessoa(string nome, DateTime dataNascimento) =>
        new() { Nome = nome, DataNascimento = dataNascimento };

    // ─── Categoria ────────────────────────────────────────────────────────────

    public static Categoria CriarCategoriaDespesa(string descricao = "Alimentação") =>
        new() { Descricao = descricao, Finalidade = Categoria.EFinalidade.Despesa };

    public static Categoria CriarCategoriaReceita(string descricao = "Salário") =>
        new() { Descricao = descricao, Finalidade = Categoria.EFinalidade.Receita };

    public static Categoria CriarCategoriaAmbas(string descricao = "Investimentos") =>
        new() { Descricao = descricao, Finalidade = Categoria.EFinalidade.Ambas };

    // ─── Transação ────────────────────────────────────────────────────────────

    /// <summary>
    /// Cria uma transação e atribui Categoria e Pessoa via reflexão,
    /// contornando o modificador internal dos setters sem alterar o código da aplicação.
    /// </summary>
    public static Transacao CriarTransacaoDespesa(
        Pessoa? pessoa = null,
        Categoria? categoria = null,
        decimal valor = 100m,
        string descricao = "Compra no mercado")
    {
        var p = pessoa ?? CriarPessoaAdulta();
        var c = categoria ?? CriarCategoriaDespesa();

        var transacao = new Transacao
        {
            Descricao = descricao,
            Valor = valor,
            Tipo = Transacao.ETipo.Despesa,
            Data = DateTime.Today
        };

        SetInternalProperty(transacao, "Categoria", c);
        SetInternalProperty(transacao, "Pessoa", p);

        return transacao;
    }

    public static Transacao CriarTransacaoReceita(
        Pessoa? pessoa = null,
        Categoria? categoria = null,
        decimal valor = 3000m,
        string descricao = "Salário mensal")
    {
        var p = pessoa ?? CriarPessoaAdulta();
        var c = categoria ?? CriarCategoriaReceita();

        var transacao = new Transacao
        {
            Descricao = descricao,
            Valor = valor,
            Tipo = Transacao.ETipo.Receita,
            Data = DateTime.Today
        };

        SetInternalProperty(transacao, "Categoria", c);
        SetInternalProperty(transacao, "Pessoa", p);

        return transacao;
    }

    // ─── Reflexão helper ──────────────────────────────────────────────────────

    /// <summary>
    /// Invoca o setter de uma propriedade com modificador internal via reflexão.
    /// Necessário porque Transacao.Categoria e Transacao.Pessoa têm setter internal.
    /// </summary>
    private static void SetInternalProperty<T>(Transacao transacao, string propertyName, T value)
    {
        var prop = typeof(Transacao).GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        prop?.SetValue(transacao, value);
    }
}
