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
/// Testes unitários para CategoriaService.
/// </summary>
public class CategoriaServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICategoriaRepository> _categoriaRepoMock;
    private readonly CategoriaService _sut;

    public CategoriaServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _categoriaRepoMock = new Mock<ICategoriaRepository>();

        _unitOfWorkMock.Setup(u => u.Categorias).Returns(_categoriaRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _sut = new CategoriaService(_unitOfWorkMock.Object);
    }

    // ─── CreateAsync ──────────────────────────────────────────────────────────

    [Theory(DisplayName = "CreateAsync deve criar categoria com qualquer finalidade válida")]
    [InlineData(Categoria.EFinalidade.Despesa, "Alimentação")]
    [InlineData(Categoria.EFinalidade.Receita, "Salário")]
    [InlineData(Categoria.EFinalidade.Ambas, "Investimentos")]
    public async Task CreateAsync_FinalidadeValida_DeveRetornarDto(
        Categoria.EFinalidade finalidade, string descricao)
    {
        // Arrange
        var dto = new CreateCategoriaDto { Descricao = descricao, Finalidade = finalidade };
        _categoriaRepoMock.Setup(r => r.AddAsync(It.IsAny<Categoria>())).Returns(Task.CompletedTask);

        // Act
        var resultado = await _sut.CreateAsync(dto);

        // Assert
        resultado.Should().NotBeNull();
        resultado.Descricao.Should().Be(descricao);
        resultado.Finalidade.Should().Be(finalidade);
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
        var dto = new CreateCategoriaDto { Descricao = "Teste", Finalidade = Categoria.EFinalidade.Despesa };
        _categoriaRepoMock.Setup(r => r.AddAsync(It.IsAny<Categoria>())).Returns(Task.CompletedTask);

        // Act
        await _sut.CreateAsync(dto);

        // Assert
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // ─── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "GetByIdAsync deve retornar null quando categoria não existe")]
    public async Task GetByIdAsync_CategoriaInexistente_DeveRetornarNull()
    {
        // Arrange
        _categoriaRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Categoria?)null);

        // Act
        var resultado = await _sut.GetByIdAsync(Guid.NewGuid());

        // Assert
        resultado.Should().BeNull();
    }

    [Fact(DisplayName = "GetByIdAsync deve retornar DTO quando categoria existe")]
    public async Task GetByIdAsync_CategoriaExistente_DeveRetornarDto()
    {
        // Arrange
        var categoria = EntityFactory.CriarCategoriaDespesa("Transporte");
        _categoriaRepoMock.Setup(r => r.GetByIdAsync(categoria.Id)).ReturnsAsync(categoria);

        // Act
        var resultado = await _sut.GetByIdAsync(categoria.Id);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Id.Should().Be(categoria.Id);
        resultado.Descricao.Should().Be("Transporte");
        resultado.Finalidade.Should().Be(Categoria.EFinalidade.Despesa);
    }
}
