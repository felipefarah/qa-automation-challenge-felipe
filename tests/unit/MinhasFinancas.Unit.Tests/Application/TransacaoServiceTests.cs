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
/// Testes unitários para TransacaoService.
/// Usa mocks para isolar a camada de aplicação do repositório.
/// </summary>
public class TransacaoServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ITransacaoRepository> _transacaoRepoMock;
    private readonly Mock<ICategoriaRepository> _categoriaRepoMock;
    private readonly Mock<IPessoaRepository> _pessoaRepoMock;
    private readonly TransacaoService _sut;

    public TransacaoServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _transacaoRepoMock = new Mock<ITransacaoRepository>();
        _categoriaRepoMock = new Mock<ICategoriaRepository>();
        _pessoaRepoMock = new Mock<IPessoaRepository>();

        _unitOfWorkMock.Setup(u => u.Transacoes).Returns(_transacaoRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Categorias).Returns(_categoriaRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Pessoas).Returns(_pessoaRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _sut = new TransacaoService(_unitOfWorkMock.Object);
    }

    // ─── CreateAsync: Casos de sucesso ────────────────────────────────────────

    [Fact(DisplayName = "CreateAsync deve criar transação de despesa válida")]
    public async Task CreateAsync_TransacaoDespesaValida_DeveRetornarDto()
    {
        // Arrange
        var pessoa = EntityFactory.CriarPessoaAdulta();
        var categoria = EntityFactory.CriarCategoriaDespesa();
        var dto = new CreateTransacaoDto
        {
            Descricao = "Compra no mercado",
            Valor = 150m,
            Tipo = Transacao.ETipo.Despesa,
            CategoriaId = categoria.Id,
            PessoaId = pessoa.Id,
            Data = DateTime.Today
        };

        _categoriaRepoMock.Setup(r => r.GetByIdAsync(categoria.Id)).ReturnsAsync(categoria);
        _pessoaRepoMock.Setup(r => r.GetByIdAsync(pessoa.Id)).ReturnsAsync(pessoa);
        _transacaoRepoMock.Setup(r => r.AddAsync(It.IsAny<Transacao>())).Returns(Task.CompletedTask);

        // Act
        var resultado = await _sut.CreateAsync(dto);

        // Assert
        resultado.Should().NotBeNull();
        resultado.Descricao.Should().Be("Compra no mercado");
        resultado.Valor.Should().Be(150m);
        resultado.Tipo.Should().Be(Transacao.ETipo.Despesa);
        resultado.CategoriaId.Should().Be(categoria.Id);
        resultado.PessoaId.Should().Be(pessoa.Id);
    }

    [Fact(DisplayName = "CreateAsync deve criar transação de receita para adulto")]
    public async Task CreateAsync_TransacaoReceitaAdulto_DeveRetornarDto()
    {
        // Arrange
        var pessoa = EntityFactory.CriarPessoaAdulta();
        var categoria = EntityFactory.CriarCategoriaReceita();
        var dto = new CreateTransacaoDto
        {
            Descricao = "Salário",
            Valor = 3000m,
            Tipo = Transacao.ETipo.Receita,
            CategoriaId = categoria.Id,
            PessoaId = pessoa.Id,
            Data = DateTime.Today
        };

        _categoriaRepoMock.Setup(r => r.GetByIdAsync(categoria.Id)).ReturnsAsync(categoria);
        _pessoaRepoMock.Setup(r => r.GetByIdAsync(pessoa.Id)).ReturnsAsync(pessoa);
        _transacaoRepoMock.Setup(r => r.AddAsync(It.IsAny<Transacao>())).Returns(Task.CompletedTask);

        // Act
        var resultado = await _sut.CreateAsync(dto);

        // Assert
        resultado.Tipo.Should().Be(Transacao.ETipo.Receita);
        resultado.Valor.Should().Be(3000m);
    }

    // ─── CreateAsync: Casos de falha ──────────────────────────────────────────

    [Fact(DisplayName = "CreateAsync deve lançar ArgumentException quando categoria não existe")]
    public async Task CreateAsync_CategoriaInexistente_DeveLancarArgumentException()
    {
        // Arrange
        var dto = new CreateTransacaoDto
        {
            Descricao = "Teste",
            Valor = 100m,
            Tipo = Transacao.ETipo.Despesa,
            CategoriaId = Guid.NewGuid(),
            PessoaId = Guid.NewGuid(),
            Data = DateTime.Today
        };

        _categoriaRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Categoria?)null);

        // Act
        var act = async () => await _sut.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Categoria não encontrada*");
    }

    [Fact(DisplayName = "CreateAsync deve lançar ArgumentException quando pessoa não existe")]
    public async Task CreateAsync_PessoaInexistente_DeveLancarArgumentException()
    {
        // Arrange
        var categoria = EntityFactory.CriarCategoriaDespesa();
        var dto = new CreateTransacaoDto
        {
            Descricao = "Teste",
            Valor = 100m,
            Tipo = Transacao.ETipo.Despesa,
            CategoriaId = categoria.Id,
            PessoaId = Guid.NewGuid(),
            Data = DateTime.Today
        };

        _categoriaRepoMock.Setup(r => r.GetByIdAsync(categoria.Id)).ReturnsAsync(categoria);
        _pessoaRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Pessoa?)null);

        // Act
        var act = async () => await _sut.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Pessoa não encontrada*");
    }

    [Fact(DisplayName = "CreateAsync deve lançar InvalidOperationException para menor de idade com receita")]
    public async Task CreateAsync_MenorDeIdadeComReceita_DeveLancarInvalidOperationException()
    {
        // Arrange
        var menor = EntityFactory.CriarPessoaMenorDeIdade();
        var categoriaReceita = EntityFactory.CriarCategoriaReceita();
        var dto = new CreateTransacaoDto
        {
            Descricao = "Receita inválida",
            Valor = 500m,
            Tipo = Transacao.ETipo.Receita,
            CategoriaId = categoriaReceita.Id,
            PessoaId = menor.Id,
            Data = DateTime.Today
        };

        _categoriaRepoMock.Setup(r => r.GetByIdAsync(categoriaReceita.Id)).ReturnsAsync(categoriaReceita);
        _pessoaRepoMock.Setup(r => r.GetByIdAsync(menor.Id)).ReturnsAsync(menor);

        // Act
        var act = async () => await _sut.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Menores de 18 anos não podem registrar receitas.");
    }

    [Fact(DisplayName = "CreateAsync deve lançar InvalidOperationException para despesa em categoria de receita")]
    public async Task CreateAsync_DespesaEmCategoriaReceita_DeveLancarInvalidOperationException()
    {
        // Arrange
        var adulto = EntityFactory.CriarPessoaAdulta();
        var categoriaReceita = EntityFactory.CriarCategoriaReceita();
        var dto = new CreateTransacaoDto
        {
            Descricao = "Despesa inválida",
            Valor = 100m,
            Tipo = Transacao.ETipo.Despesa,
            CategoriaId = categoriaReceita.Id,
            PessoaId = adulto.Id,
            Data = DateTime.Today
        };

        _categoriaRepoMock.Setup(r => r.GetByIdAsync(categoriaReceita.Id)).ReturnsAsync(categoriaReceita);
        _pessoaRepoMock.Setup(r => r.GetByIdAsync(adulto.Id)).ReturnsAsync(adulto);

        // Act
        var act = async () => await _sut.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Não é possível registrar despesa em categoria de receita.");
    }

    // ─── CreateAsync: Verificações de interação ───────────────────────────────

    [Fact(DisplayName = "CreateAsync deve chamar SaveChangesAsync após criar transação")]
    public async Task CreateAsync_TransacaoValida_DeveChamarSaveChanges()
    {
        // Arrange
        var pessoa = EntityFactory.CriarPessoaAdulta();
        var categoria = EntityFactory.CriarCategoriaDespesa();
        var dto = new CreateTransacaoDto
        {
            Descricao = "Teste",
            Valor = 100m,
            Tipo = Transacao.ETipo.Despesa,
            CategoriaId = categoria.Id,
            PessoaId = pessoa.Id,
            Data = DateTime.Today
        };

        _categoriaRepoMock.Setup(r => r.GetByIdAsync(categoria.Id)).ReturnsAsync(categoria);
        _pessoaRepoMock.Setup(r => r.GetByIdAsync(pessoa.Id)).ReturnsAsync(pessoa);
        _transacaoRepoMock.Setup(r => r.AddAsync(It.IsAny<Transacao>())).Returns(Task.CompletedTask);

        // Act
        await _sut.CreateAsync(dto);

        // Assert
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact(DisplayName = "CreateAsync deve chamar AddAsync no repositório de transações")]
    public async Task CreateAsync_TransacaoValida_DeveChamarAddAsync()
    {
        // Arrange
        var pessoa = EntityFactory.CriarPessoaAdulta();
        var categoria = EntityFactory.CriarCategoriaDespesa();
        var dto = new CreateTransacaoDto
        {
            Descricao = "Teste",
            Valor = 100m,
            Tipo = Transacao.ETipo.Despesa,
            CategoriaId = categoria.Id,
            PessoaId = pessoa.Id,
            Data = DateTime.Today
        };

        _categoriaRepoMock.Setup(r => r.GetByIdAsync(categoria.Id)).ReturnsAsync(categoria);
        _pessoaRepoMock.Setup(r => r.GetByIdAsync(pessoa.Id)).ReturnsAsync(pessoa);
        _transacaoRepoMock.Setup(r => r.AddAsync(It.IsAny<Transacao>())).Returns(Task.CompletedTask);

        // Act
        await _sut.CreateAsync(dto);

        // Assert
        _transacaoRepoMock.Verify(r => r.AddAsync(It.IsAny<Transacao>()), Times.Once);
    }

    // ─── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "GetByIdAsync deve retornar null quando transação não existe")]
    public async Task GetByIdAsync_TransacaoInexistente_DeveRetornarNull()
    {
        // Arrange
        _transacaoRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Transacao?)null);

        // Act
        var resultado = await _sut.GetByIdAsync(Guid.NewGuid());

        // Assert
        resultado.Should().BeNull();
    }

    [Fact(DisplayName = "GetByIdAsync deve retornar DTO quando transação existe")]
    public async Task GetByIdAsync_TransacaoExistente_DeveRetornarDto()
    {
        // Arrange
        var transacao = EntityFactory.CriarTransacaoDespesa();
        _transacaoRepoMock.Setup(r => r.GetByIdAsync(transacao.Id)).ReturnsAsync(transacao);

        // Act
        var resultado = await _sut.GetByIdAsync(transacao.Id);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Id.Should().Be(transacao.Id);
        resultado.Descricao.Should().Be(transacao.Descricao);
        resultado.Valor.Should().Be(transacao.Valor);
    }
}
