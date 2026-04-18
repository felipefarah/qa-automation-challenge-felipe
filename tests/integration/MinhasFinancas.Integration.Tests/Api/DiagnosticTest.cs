using Xunit;
using Xunit.Abstractions;

namespace MinhasFinancas.Integration.Tests.Api;

/// <summary>
/// Teste de diagnóstico para capturar o erro real do WebApplicationFactory.
/// Será removido após resolver o problema.
/// </summary>
public class DiagnosticTest
{
    private readonly ITestOutputHelper _output;

    public DiagnosticTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void DiagnosticFactory_DeveCapturarErroReal()
    {
        try
        {
            using var factory = new ApiWebApplicationFactory();
            var client = factory.CreateClient();
            _output.WriteLine("Factory criada com sucesso!");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Tipo: {ex.GetType().FullName}");
            _output.WriteLine($"Mensagem: {ex.Message}");
            var inner = ex.InnerException;
            while (inner != null)
            {
                _output.WriteLine($"  Inner: {inner.GetType().FullName}: {inner.Message}");
                inner = inner.InnerException;
            }
            throw;
        }
    }
}
