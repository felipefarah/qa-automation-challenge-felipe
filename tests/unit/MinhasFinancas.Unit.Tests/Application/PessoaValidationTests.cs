using FluentAssertions;
using MinhasFinancas.Application.DTOs;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace MinhasFinancas.Unit.Tests.Application;

/// <summary>
/// Testes unitários para as validações de DTO de Pessoa.
/// Cobre: data de nascimento no futuro, campos obrigatórios, tamanho máximo.
/// </summary>
public class PessoaValidationTests
{
    // ─── ValidarDataNascimento ────────────────────────────────────────────────

    [Fact(DisplayName = "ValidarDataNascimento deve retornar sucesso para data no passado")]
    public void ValidarDataNascimento_DataNoPassado_DeveRetornarSucesso()
    {
        // Arrange
        var data = DateTime.Today.AddYears(-25);
        var context = new ValidationContext(new object());

        // Act
        var resultado = PessoaValidation.ValidarDataNascimento(data, context);

        // Assert
        resultado.Should().Be(ValidationResult.Success);
    }

    [Fact(DisplayName = "ValidarDataNascimento deve retornar erro para data no futuro")]
    public void ValidarDataNascimento_DataNoFuturo_DeveRetornarErro()
    {
        // Arrange
        var data = DateTime.Today.AddDays(1);
        var context = new ValidationContext(new object());

        // Act
        var resultado = PessoaValidation.ValidarDataNascimento(data, context);

        // Assert
        resultado.Should().NotBe(ValidationResult.Success);
        resultado!.ErrorMessage.Should().Be("Data de nascimento não pode ser no futuro.");
    }

    [Fact(DisplayName = "ValidarDataNascimento deve retornar sucesso para data de hoje")]
    public void ValidarDataNascimento_DataHoje_DeveRetornarSucesso()
    {
        // Arrange
        var data = DateTime.Today;
        var context = new ValidationContext(new object());

        // Act
        var resultado = PessoaValidation.ValidarDataNascimento(data, context);

        // Assert
        resultado.Should().Be(ValidationResult.Success);
    }

    // ─── Validação de DTO via DataAnnotations ─────────────────────────────────

    [Fact(DisplayName = "CreatePessoaDto deve ser inválido quando nome está vazio")]
    public void CreatePessoaDto_NomeVazio_DeveSerInvalido()
    {
        // Arrange
        var dto = new CreatePessoaDto { Nome = "", DataNascimento = DateTime.Today.AddYears(-25) };
        var resultados = new List<ValidationResult>();
        var context = new ValidationContext(dto);

        // Act
        var valido = Validator.TryValidateObject(dto, context, resultados, true);

        // Assert
        valido.Should().BeFalse();
        resultados.Should().Contain(r => r.ErrorMessage!.Contains("Nome é obrigatório"));
    }

    [Fact(DisplayName = "CreatePessoaDto deve ser inválido quando nome excede 200 caracteres")]
    public void CreatePessoaDto_NomeMuitoLongo_DeveSerInvalido()
    {
        // Arrange
        var dto = new CreatePessoaDto
        {
            Nome = new string('A', 201),
            DataNascimento = DateTime.Today.AddYears(-25)
        };
        var resultados = new List<ValidationResult>();
        var context = new ValidationContext(dto);

        // Act
        var valido = Validator.TryValidateObject(dto, context, resultados, true);

        // Assert
        valido.Should().BeFalse();
        resultados.Should().Contain(r => r.ErrorMessage!.Contains("200 caracteres"));
    }

    [Fact(DisplayName = "CreatePessoaDto deve ser válido com dados corretos")]
    public void CreatePessoaDto_DadosValidos_DeveSerValido()
    {
        // Arrange
        var dto = new CreatePessoaDto
        {
            Nome = "João Silva",
            DataNascimento = new DateTime(1990, 1, 1)
        };
        var resultados = new List<ValidationResult>();
        var context = new ValidationContext(dto);

        // Act
        var valido = Validator.TryValidateObject(dto, context, resultados, true);

        // Assert
        valido.Should().BeTrue();
        resultados.Should().BeEmpty();
    }
}
