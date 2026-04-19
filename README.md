# Minhas Finanças — Repositório de Testes Automatizados

Repositório de testes automatizados para o sistema **Minhas Finanças** (controle de gastos residenciais).
Nenhum código da aplicação foi modificado.

---

## Pirâmide de Testes Adotada

```
        /\
       /E2E\          ← Playwright (poucos, lentos, alto valor)
      /------\
     /Integração\     ← xUnit + InMemory DB (médio volume)
    /------------\
   / Unitários    \   ← xUnit + Moq (muitos, rápidos, isolados)
  /--------------\
```

| Camada       | Tecnologia                        | Foco                                      |
|--------------|-----------------------------------|-------------------------------------------|
| Unitários    | C# + xUnit + Moq + FluentAssertions | Regras de negócio puras, isoladas         |
| Integração   | C# + xUnit + EF InMemory + WebApplicationFactory | Camadas integradas, persistência, API HTTP |
| E2E          | Playwright + TypeScript           | Fluxos reais do usuário no browser        |

---

## Estrutura do Repositório

```
tests/
├── unit/
│   └── MinhasFinancas.Unit.Tests/
│       ├── Domain/
│       │   ├── PessoaTests.cs          # Cálculo de idade, maioridade
│       │   ├── CategoriaTests.cs       # Matriz de permissões por finalidade
│       │   └── TransacaoTests.cs       # Regras de negócio críticas
│       ├── Application/
│       │   ├── TransacaoServiceTests.cs
│       │   ├── PessoaServiceTests.cs
│       │   ├── CategoriaServiceTests.cs
│       │   └── PessoaValidationTests.cs
│       └── Helpers/
│           └── EntityFactory.cs        # Factory para criação de entidades
│
├── integration/
│   └── MinhasFinancas.Integration.Tests/
│       ├── Fixtures/
│       │   └── IntegrationTestBase.cs  # Base com banco InMemory isolado
│       ├── Services/
│       │   ├── TransacaoServiceIntegrationTests.cs
│       │   └── PessoaServiceIntegrationTests.cs
│       └── Api/
│           ├── ApiWebApplicationFactory.cs
│           ├── TransacoesApiTests.cs
│           └── PessoasApiTests.cs
│
├── e2e/
│   ├── pages/                          # Page Objects
│   │   ├── BasePage.ts
│   │   ├── PessoasPage.ts
│   │   ├── CategoriasPage.ts
│   │   └── TransacoesPage.ts
│   ├── tests/
│   │   ├── pessoas.spec.ts
│   │   ├── categorias.spec.ts
│   │   ├── transacoes.spec.ts
│   │   └── navigation.spec.ts
│   ├── playwright.config.ts
│   └── package.json
│
├── docs/
│   └── bugs.md                         # Bugs encontrados
└── README.md
```

---

## Como Rodar os Testes

### Pré-requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 20+](https://nodejs.org/) ou [Bun](https://bun.sh/)
- Aplicação rodando (apenas para E2E)

---

### Testes Unitários

```bash
cd tests/unit/MinhasFinancas.Unit.Tests
dotnet test
```

Com relatório de cobertura:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

---

### Testes de Integração

```bash
cd tests/integration/MinhasFinancas.Integration.Tests
dotnet test
```

> Os testes de integração usam banco de dados **em memória** — não é necessário ter a aplicação rodando.

---

### Testes E2E (Playwright)

**1. Instalar dependências:**

```bash
cd tests/e2e
npm install
npx playwright install chromium
```

**2. Iniciar a aplicação** (em outro terminal):

```bash
# Com Docker (recomendado)
cd ExameDesenvolvedorDeTestes
docker-compose up --build

# Ou manualmente
# Terminal 1 — API
cd ExameDesenvolvedorDeTestes/api/MinhasFinancas.API
dotnet run

# Terminal 2 — Frontend
cd ExameDesenvolvedorDeTestes/web
bun install && bun run dev
```

**3. Rodar os testes E2E:**

```bash
cd tests/e2e

# Modo headless (CI)
npm test

# Modo com browser visível
npm run test:headed

# Interface visual do Playwright
npm run test:ui

# Ver relatório HTML após execução
npm run test:report
```

**Variável de ambiente para URL customizada:**

```bash
BASE_URL=http://localhost:5173 npm test
```

---

## Justificativa das Escolhas Técnicas

### Backend — xUnit + Moq + FluentAssertions

- **xUnit** é o framework de testes mais adotado no ecossistema .NET moderno, com suporte nativo a `[Fact]` e `[Theory]`
- **Moq** permite criar mocks das interfaces de repositório sem precisar de banco real nos testes unitários
- **FluentAssertions** torna as asserções legíveis em linguagem natural (`resultado.Should().Be(...)`)
- **EF Core InMemory** nos testes de integração garante isolamento total sem dependência de SQLite ou Docker

### Frontend E2E — Playwright

- **Playwright** é mais robusto que Cypress para aplicações modernas: suporte nativo a múltiplos browsers, auto-wait, e melhor suporte a SPAs com React
- **Page Object Model** centraliza seletores e ações, tornando os testes resilientes a mudanças de UI
- Seletores baseados em **roles e texto** (não em classes CSS ou IDs frágeis) seguem as boas práticas de acessibilidade e manutenção

### Padrões Aplicados

| Padrão | Onde | Por quê |
|--------|------|---------|
| AAA (Arrange, Act, Assert) | Todos os testes | Clareza e padronização |
| Factory (EntityFactory) | Testes unitários | Evita duplicação na criação de entidades |
| Page Object Model | E2E | Encapsula seletores, facilita manutenção |
| Banco isolado por teste | Integração | Garante previsibilidade e independência |
| `[Theory] + [InlineData]` | Unitários | Cobre múltiplos cenários com um único teste |

---

## Regras de Negócio Cobertas

| Regra | Unitário | Integração | E2E |
|-------|----------|------------|-----|
| Menor de 18 anos não pode ter receita | ✅ | ✅ | ✅ |
| Categoria Despesa só aceita Despesa | ✅ | ✅ | - |
| Categoria Receita só aceita Receita | ✅ | ✅ | - |
| Categoria Ambas aceita qualquer tipo | ✅ | ✅ | - |
| Pessoa com exatamente 18 anos pode ter receita | ✅ | - | - |
| Cascade delete: pessoa remove transações | - | ✅ | - |
| Data de nascimento não pode ser futura | ✅ | ✅ | - |
| Transação com categoria/pessoa inexistente retorna erro | ✅ | ✅ | - |
| Cadastro de pessoa via UI | - | - | ✅ |
| Cadastro de categoria via UI | - | - | ✅ |
| Criação de transação via UI | - | - | ✅ |
| Aviso de menor de idade na UI | - | - | ✅ |

---

## Bugs Encontrados

Durante a criação dos testes automatizados, foram identificados alguns bugs no sistema. Para uma **documentação completa e detalhada** de cada bug (incluindo passos para reproduzir, trechos de código problemáticos e sugestões de correção), consulte:

📋 **[`docs/bugs.md`](docs/bugs.md)** — Documentação completa de bugs

### Resumo dos Bugs Identificados

| ID | Título | Severidade |
|----|--------|------------|
| BUG-001 | `InvalidOperationException` retorna HTTP 500 em vez de 400 (menor de idade + receita) | Alta |
| BUG-002 | Violação categoria/tipo retorna HTTP 500 em vez de 400 | Alta |
| BUG-003 | Campo `Data` ausente no DTO de resposta ao criar transação | Média |
| BUG-004 | Cascade delete não configurado explicitamente no DbContext | Média |
| BUG-005 | Frontend não valida data de nascimento futura no schema Zod | Baixa |
| BUG-006 | Inconsistência de nomenclatura `totalCount` vs `total` entre API e frontend | Baixa |
