using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace MinhasFinancas.Integration.Tests.Api;

/// <summary>
/// Testes de integração para a API de Pessoas.
/// </summary>
public class PessoasApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PessoasApiTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ─── GET ──────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "GET /pessoas deve retornar 200 OK")]
    public async Task GetAll_DeveRetornar200()
    {
        var response = await _client.GetAsync("/api/v1.0/pessoas");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "GET /pessoas/{id} deve retornar 404 para ID inexistente")]
    public async Task GetById_IdInexistente_DeveRetornar404()
    {
        var response = await _client.GetAsync($"/api/v1.0/pessoas/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─── POST ─────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "POST /pessoas deve criar pessoa e retornar 201")]
    public async Task Create_DadosValidos_DeveRetornar201()
    {
        var payload = new { Nome = "Carlos Teste", DataNascimento = "1985-03-20T00:00:00" };

        var response = await _client.PostAsJsonAsync("/api/v1.0/pessoas", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        json.GetProperty("nome").GetString().Should().Be("Carlos Teste");
        json.TryGetProperty("id", out var idProp).Should().BeTrue();
        idProp.GetString().Should().NotBeNullOrEmpty();
    }

    [Fact(DisplayName = "POST /pessoas deve retornar 400 quando nome está vazio")]
    public async Task Create_NomeVazio_DeveRetornar400()
    {
        var payload = new { Nome = "", DataNascimento = "1990-01-01T00:00:00" };

        var response = await _client.PostAsJsonAsync("/api/v1.0/pessoas", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "POST /pessoas deve retornar 400 quando data de nascimento é no futuro")]
    public async Task Create_DataNascimentoFutura_DeveRetornar400()
    {
        var payload = new
        {
            Nome = "Pessoa Futura",
            DataNascimento = DateTime.Today.AddDays(1).ToString("o")
        };

        var response = await _client.PostAsJsonAsync("/api/v1.0/pessoas", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ─── PUT ──────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "PUT /pessoas/{id} deve atualizar pessoa e retornar 204")]
    public async Task Update_DadosValidos_DeveRetornar204()
    {
        var createPayload = new { Nome = "Nome Original", DataNascimento = "1990-01-01T00:00:00" };
        var createResp = await _client.PostAsJsonAsync("/api/v1.0/pessoas", createPayload);
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetString();

        var updatePayload = new { Nome = "Nome Atualizado", DataNascimento = "1990-01-01T00:00:00" };

        var response = await _client.PutAsJsonAsync($"/api/v1.0/pessoas/{id}", updatePayload);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact(DisplayName = "PUT /pessoas/{id} deve retornar 404 para ID inexistente")]
    public async Task Update_IdInexistente_DeveRetornar404()
    {
        var payload = new { Nome = "Teste", DataNascimento = "1990-01-01T00:00:00" };

        
        var response = await _client.PutAsJsonAsync($"/api/v1.0/pessoas/{Guid.NewGuid()}", payload);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─── DELETE ───────────────────────────────────────────────────────────────

    [Fact(DisplayName = "DELETE /pessoas/{id} deve remover pessoa e retornar 204")]
    public async Task Delete_PessoaExistente_DeveRetornar204()
    {
        var createPayload = new { Nome = "Para Deletar", DataNascimento = "1990-01-01T00:00:00" };
        var createResp = await _client.PostAsJsonAsync("/api/v1.0/pessoas", createPayload);
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetString();

        var response = await _client.DeleteAsync($"/api/v1.0/pessoas/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/v1.0/pessoas/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
