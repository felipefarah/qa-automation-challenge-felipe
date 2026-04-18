using FluentAssertions;
using MinhasFinancas.Application.DTOs;
using MinhasFinancas.Application.Services;
using MinhasFinancas.Domain.Entities;
using MinhasFinancas.Domain.Interfaces;
using MinhasFinancas.Domain.ValueObjects;
using MinhasFinancas.Unit.Tests.Helpers;
using Moq;
using Xunit;

namespace MinhasFinancas.Unit.Tests.Application;

/// <summary>
/// Testes unitários para PessoaService.
/// </summary>
public class PessoaServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPessoaRepository> _pessoaRepoMock;
    private readonly PessoaService _sut;

    public PessoaServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _pessoaRepoMock = new Mock<IPessoaRepository>();

        _unitOfWorkMock.Setup(u => u.Pessoas).Returns(_pessoaRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _sut = new PessoaService(_unitOfWorkMock.Object);
    }

    // ─── CreateAsync ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "CreateAsync deve criar pessoa e retornar DTO com dados corretos")]
    public async Task CreateAsync_DadosValidos_DeveRetornarDto()
    {
        // Arrange
        var dto = new CreatePessoaDto
        {
            Nome = "Maria Santos",
            DataNascimento = new DateTime(1990, 5, 15)
        };
        _pessoaRepoMock.Setup(r => r.AddAsync(It.IsAny<Pessoa>())).Returns(Task.CompletedTask);

        // Act
        var resultado = await _sut.CreateAsync(dto);

        // Assert
        resultado.Should().NotBeNull();
        resultado.Nome.Should().Be("Maria Santos");
        resultado.DataNascimento.Should().Be(new DateTime(1990, 5, 15));
        resultado.Id.Should().NotBe(Guid.Empty);
    }

    [Fact(DisplayName = "CreateAsync deve lançar ArgumentNullException quando dto é null")]
    public async Task CreateAsync_DtoNulo_DeveLancarArgumentNullException()
    {
        // Act
        var act = async () => await _sut.CreateAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact(DisplayName = "CreateAsync deve chamar SaveChangesAsync")]
    public async Task CreateAsync_DadosValidos_DeveChamarSaveChanges()
    {
        // Arrange
        var dto = new CreatePessoaDto { Nome = "Teste", DataNascimento = DateTime.Today.AddYears(-25) };
        _pessoaRepoMock.Setup(r => r.AddAsync(It.IsAny<Pessoa>())).Returns(Task.CompletedTask);

        // Act
        await _sut.CreateAsync(dto);

        // Assert
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // ─── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "GetByIdAsync deve retornar null quando pessoa não existe")]
    public async Task GetByIdAsync_PessoaInexistente_DeveRetornarNull()
    {
        // Arrange
        _pessoaRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Pessoa?)null);

        // Act
        var resultado = await _sut.GetByIdAsync(Guid.NewGuid());

        // Assert
        resultado.Should().BeNull();
    }

    [Fact(DisplayName = "GetByIdAsync deve retornar DTO quando pessoa existe")]
    public async Task GetByIdAsync_PessoaExistente_DeveRetornarDto()
    {
        // Arrange
        var pessoa = EntityFactory.CriarPessoaAdulta("Carlos");
        _pessoaRepoMock.Setup(r => r.GetByIdAsync(pessoa.Id)).ReturnsAsync(pessoa);

        // Act
        var resultado = await _sut.GetByIdAsync(pessoa.Id);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Id.Should().Be(pessoa.Id);
        resultado.Nome.Should().Be("Carlos");
    }

    // ─── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "UpdateAsync deve lançar KeyNotFoundException quando pessoa não existe")]
    public async Task UpdateAsync_PessoaInexistente_DeveLancarKeyNotFoundException()
    {
        // Arrange
        _pessoaRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Pessoa?)null);
        var dto = new UpdatePessoaDto { Nome = "Novo Nome", DataNascimento = DateTime.Today.AddYears(-25) };

        // Act
        var act = async () => await _sut.UpdateAsync(Guid.NewGuid(), dto);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Pessoa não encontrada.");
    }

    [Fact(DisplayName = "UpdateAsync deve atualizar nome e data de nascimento")]
    public async Task UpdateAsync_DadosValidos_DeveAtualizarPessoa()
    {
        // Arrange
        var pessoa = EntityFactory.CriarPessoaAdulta("Nome Antigo");
        _pessoaRepoMock.Setup(r => r.GetByIdAsync(pessoa.Id)).ReturnsAsync(pessoa);
        _pessoaRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Pessoa>())).Returns(Task.CompletedTask);

        var dto = new UpdatePessoaDto
        {
            Nome = "Nome Novo",
            DataNascimento = new DateTime(1995, 3, 20)
        };

        // Act
        await _sut.UpdateAsync(pessoa.Id, dto);

        // Assert
        pessoa.Nome.Should().Be("Nome Novo");
        pessoa.DataNascimento.Should().Be(new DateTime(1995, 3, 20));
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // ─── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "DeleteAsync deve chamar DeleteAsync no repositório e SaveChangesAsync")]
    public async Task DeleteAsync_IdValido_DeveChamarDeleteEsalvar()
    {
        // Arrange
        var id = Guid.NewGuid();
        _pessoaRepoMock.Setup(r => r.DeleteAsync(id)).Returns(Task.CompletedTask);

        // Act
        await _sut.DeleteAsync(id);

        // Assert
        _pessoaRepoMock.Verify(r => r.DeleteAsync(id), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}
