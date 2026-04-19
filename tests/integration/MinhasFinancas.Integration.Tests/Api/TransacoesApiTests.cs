using FluentAssertions;
using MinhasFinancas.Domain.Entities;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace MinhasFinancas.Integration.Tests.Api;

/// <summary>
/// Testes de integração para a API de Transações.
/// Valida os endpoints HTTP end-to-end com banco em memória.
/// </summary>
public class TransacoesApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public TransacoesApiTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ─── GET /api/v1.0/transacoes ─────────────────────────────────────────────

    [Fact(DisplayName = "GET /transacoes deve retornar 200 OK com lista paginada")]
    public async Task GetAll_DeveRetornar200ComListaPaginada()
    {
        var response = await _client.GetAsync("/api/v1.0/transacoes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content).RootElement;
        json.TryGetProperty("items", out _).Should().BeTrue();
        json.TryGetProperty("totalCount", out _).Should().BeTrue();
    }

    // ─── GET /api/v1.0/transacoes/{id} ────────────────────────────────────────

    [Fact(DisplayName = "GET /transacoes/{id} deve retornar 404 para ID inexistente")]
    public async Task GetById_IdInexistente_DeveRetornar404()
    {
        var response = await _client.GetAsync($"/api/v1.0/transacoes/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─── POST /api/v1.0/transacoes ────────────────────────────────────────────

    [Fact(DisplayName = "POST /transacoes deve retornar 400 quando body está vazio")]
    public async Task Create_BodyVazio_DeveRetornar400()
    {
        var response = await _client.PostAsJsonAsync("/api/v1.0/transacoes", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "POST /transacoes deve retornar 400 quando categoria não existe")]
    public async Task Create_CategoriaInexistente_DeveRetornar400()
    {
        var pessoaPayload = new { Nome = "Teste API", DataNascimento = "1990-01-01T00:00:00" };
        var pessoaResponse = await _client.PostAsJsonAsync("/api/v1.0/pessoas", pessoaPayload);
        pessoaResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var pessoaJson = JsonDocument.Parse(await pessoaResponse.Content.ReadAsStringAsync()).RootElement;
        var pessoaId = pessoaJson.GetProperty("id").GetString();

        var payload = new
        {
            Descricao = "Teste",
            Valor = 100.0,
            Tipo = 0, 
            CategoriaId = Guid.NewGuid().ToString(),
            PessoaId = pessoaId,
            Data = DateTime.Today.ToString("o")
        };

        var response = await _client.PostAsJsonAsync("/api/v1.0/transacoes", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "POST /transacoes deve retornar 400 quando pessoa não existe")]
    public async Task Create_PessoaInexistente_DeveRetornar400()
    {
        var categoriaPayload = new { Descricao = "Alimentação API", Finalidade = 0 }; // Despesa
        var categoriaResponse = await _client.PostAsJsonAsync("/api/v1.0/categorias", categoriaPayload);
        categoriaResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var categoriaJson = JsonDocument.Parse(await categoriaResponse.Content.ReadAsStringAsync()).RootElement;
        var categoriaId = categoriaJson.GetProperty("id").GetString();

        var payload = new
        {
            Descricao = "Teste",
            Valor = 100.0,
            Tipo = 0, 
            CategoriaId = categoriaId,
            PessoaId = Guid.NewGuid().ToString(),
            Data = DateTime.Today.ToString("o")
        };

        var response = await _client.PostAsJsonAsync("/api/v1.0/transacoes", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "POST /transacoes deve criar transação válida e retornar 201")]
    public async Task Create_TransacaoValida_DeveRetornar201()
    {
        var pessoaPayload = new { Nome = "Pessoa Transacao", DataNascimento = "1990-06-15T00:00:00" };
        var pessoaResp = await _client.PostAsJsonAsync("/api/v1.0/pessoas", pessoaPayload);
        var pessoaId = JsonDocument.Parse(await pessoaResp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetString();

        var categoriaPayload = new { Descricao = "Despesa Teste", Finalidade = 0 };
        var categoriaResp = await _client.PostAsJsonAsync("/api/v1.0/categorias", categoriaPayload);
        var categoriaId = JsonDocument.Parse(await categoriaResp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetString();

        var payload = new
        {
            Descricao = "Compra no mercado",
            Valor = 150.75,
            Tipo = 0, 
            CategoriaId = categoriaId,
            PessoaId = pessoaId,
            Data = DateTime.Today.ToString("o")
        };

        var response = await _client.PostAsJsonAsync("/api/v1.0/transacoes", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        json.GetProperty("descricao").GetString().Should().Be("Compra no mercado");
        json.GetProperty("valor").GetDecimal().Should().Be(150.75m);
    }
}
