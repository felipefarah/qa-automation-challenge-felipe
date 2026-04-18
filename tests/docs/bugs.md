# Bugs Encontrados — Minhas Finanças

Documentação de bugs identificados durante a análise do código e criação dos testes automatizados.

---

## BUG-001 — Transação de receita para menor de idade lança 500 em vez de 400

**Título:** InvalidOperationException não tratada no controller de Transações retorna HTTP 500

**Descrição:**
Ao tentar criar uma transação de receita para uma pessoa menor de 18 anos, o domínio lança corretamente uma `InvalidOperationException`. Porém, o `TransacoesController` captura apenas `ArgumentException` no bloco `catch`. A `InvalidOperationException` escapa para o `ExceptionMiddleware`, que retorna HTTP 500 (Internal Server Error) em vez de HTTP 400 (Bad Request).

**Passos para reproduzir:**
1. Criar uma pessoa com data de nascimento que resulte em menos de 18 anos
2. Criar uma categoria com finalidade `Receita`
3. Enviar `POST /api/v1.0/transacoes` com `Tipo = 1` (Receita), usando os IDs da pessoa menor e da categoria de receita
4. Observar a resposta HTTP

**Resultado esperado:**
`HTTP 400 Bad Request` com mensagem: `"Menores de 18 anos não podem registrar receitas."`

**Resultado atual:**
`HTTP 500 Internal Server Error` com mensagem genérica do middleware de exceção.

**Trecho do código com o problema:**
```csharp
// TransacoesController.cs — linha ~55
catch (ArgumentException ex)
{
    return BadRequest(ex.Message);
}
// InvalidOperationException NÃO é capturada aqui
```

**Severidade:** Alta — regra de negócio crítica retorna status HTTP incorreto.

---

## BUG-002 — Transação com tipo incompatível com categoria lança 500 em vez de 400

**Título:** Violação de compatibilidade categoria/tipo retorna HTTP 500

**Descrição:**
Assim como o BUG-001, ao tentar criar uma despesa em categoria de receita (ou vice-versa), o domínio lança `InvalidOperationException`. O controller não captura esse tipo de exceção, resultando em HTTP 500.

**Passos para reproduzir:**
1. Criar uma categoria com finalidade `Receita`
2. Enviar `POST /api/v1.0/transacoes` com `Tipo = 0` (Despesa) e o ID dessa categoria
3. Observar a resposta HTTP

**Resultado esperado:**
`HTTP 400 Bad Request` com mensagem: `"Não é possível registrar despesa em categoria de receita."`

**Resultado atual:**
`HTTP 500 Internal Server Error`

**Severidade:** Alta — regra de negócio crítica retorna status HTTP incorreto.

**Sugestão de correção:**
```csharp
// TransacoesController.cs
catch (ArgumentException ex)
{
    return BadRequest(ex.Message);
}
catch (InvalidOperationException ex) // <-- adicionar este bloco
{
    return BadRequest(ex.Message);
}
```

---

## BUG-003 — TransacaoDto não inclui campo `Data` na resposta de criação

**Título:** Campo `Data` ausente no DTO de resposta ao criar transação

**Descrição:**
No método `CreateAsync` do `TransacaoService`, o `TransacaoDto` retornado não inclui o campo `Data`. O campo existe no DTO e é persistido corretamente no banco, mas não é mapeado no retorno da criação.

**Passos para reproduzir:**
1. Enviar `POST /api/v1.0/transacoes` com uma data específica
2. Observar o JSON de resposta (HTTP 201)

**Resultado esperado:**
O campo `data` deve estar presente no JSON de resposta com o valor enviado.

**Resultado atual:**
O campo `data` retorna `0001-01-01T00:00:00` (valor padrão de `DateTime`) na resposta de criação.

**Trecho do código com o problema:**
```csharp
// TransacaoService.cs — método CreateAsync, retorno final
return new TransacaoDto
{
    Id = transacao.Id,
    Descricao = transacao.Descricao,
    Valor = transacao.Valor,
    Tipo = transacao.Tipo,
    CategoriaId = transacao.CategoriaId,
    CategoriaDescricao = transacao.Categoria?.Descricao ?? string.Empty,
    PessoaId = transacao.PessoaId,
    PessoaNome = transacao.Pessoa?.Nome ?? string.Empty
    // Data = transacao.Data  <-- CAMPO AUSENTE
};
```

**Severidade:** Média — o dado é persistido corretamente, mas a resposta da API está incompleta.

---

## BUG-004 — Exclusão de pessoa não remove transações (ausência de cascade delete configurado)

**Título:** Cascade delete não configurado no DbContext para Transações

**Descrição:**
O `MinhasFinancasDbContext` não configura explicitamente o comportamento de cascade delete para o relacionamento `Pessoa → Transacoes`. O EF Core pode usar o comportamento padrão (`ClientSetNull` para relacionamentos opcionais), o que pode causar erro de constraint ao deletar uma pessoa que possui transações, dependendo do banco de dados utilizado.

**Passos para reproduzir:**
1. Criar uma pessoa
2. Criar uma ou mais transações vinculadas a essa pessoa
3. Deletar a pessoa via `DELETE /api/v1.0/pessoas/{id}`
4. Observar o comportamento

**Resultado esperado:**
A pessoa e todas as suas transações devem ser removidas (cascade delete).

**Resultado atual:**
Comportamento inconsistente: com SQLite pode funcionar silenciosamente, mas com outros bancos pode lançar erro de foreign key constraint.

**Sugestão de correção:**
```csharp
// MinhasFinancasDbContext.cs — OnModelCreating
modelBuilder.Entity<Transacao>()
    .HasOne(t => t.Pessoa)
    .WithMany(p => p.Transacoes)
    .HasForeignKey(t => t.PessoaId)
    .OnDelete(DeleteBehavior.Cascade); // <-- adicionar
```

**Severidade:** Média — comportamento dependente do banco de dados utilizado.

---

## BUG-005 — Validação de data de nascimento no futuro não é aplicada no frontend

**Título:** Frontend permite submeter data de nascimento futura sem validação no schema Zod

**Descrição:**
O schema Zod de `pessoaSchema` em `schemas.ts` não valida se a data de nascimento é no futuro. A validação existe no backend (`PessoaValidation.ValidarDataNascimento`), mas o frontend não a replica, permitindo que o formulário seja submetido com uma data futura. O erro só é retornado pelo backend, sem feedback imediato ao usuário.

**Passos para reproduzir:**
1. Acessar a página de Pessoas
2. Clicar em "Adicionar Pessoa"
3. Preencher o nome
4. Inserir uma data de nascimento no futuro (ex: amanhã)
5. Clicar em "Salvar"

**Resultado esperado:**
Mensagem de erro imediata no formulário: "Data de nascimento não pode ser no futuro."

**Resultado atual:**
O formulário é submetido, a requisição vai ao backend, e o erro retorna como toast genérico "Erro ao salvar pessoa."

**Sugestão de correção:**
```typescript
// schemas.ts
export const pessoaSchema = z.object({
  nome: z.string().min(1, "Nome é obrigatório").max(200),
  dataNascimento: z.date()
    .max(new Date(), "Data de nascimento não pode ser no futuro"), // <-- adicionar
});
```

**Severidade:** Baixa — UX degradada, mas a regra é aplicada no backend.

---

## BUG-006 — Paginação usa `TotalCount` no backend mas `total` no frontend

**Título:** Inconsistência de nomenclatura entre API e frontend para campo de total

**Descrição:**
O `PagedResult<T>` do backend retorna o campo como `TotalCount` (PascalCase serializado como `totalCount`). O frontend em `apiUtils.ts` normaliza para `total`. Porém, o `PagedResult` do frontend usa `total` enquanto o backend usa `totalCount`, o que pode causar problemas se a normalização falhar ou for removida.

**Passos para reproduzir:**
1. Fazer `GET /api/v1.0/pessoas`
2. Observar o JSON de resposta

**Resultado esperado:**
Campo consistente entre API e frontend.

**Resultado atual:**
API retorna `totalCount`, frontend espera `total` após normalização em `apiUtils.ts`.

**Severidade:** Baixa — atualmente mitigado pela normalização, mas frágil.
