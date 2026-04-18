import { test, expect } from "@playwright/test";
import { PessoasPage } from "../pages/PessoasPage";

/**
 * Testes E2E para o fluxo de Pessoas.
 * Pré-requisito: aplicação rodando em http://localhost:5173
 */
test.describe("Pessoas — Fluxos E2E", () => {
  let pessoasPage: PessoasPage;

  test.beforeEach(async ({ page }) => {
    pessoasPage = new PessoasPage(page);
    await pessoasPage.goto();
  });

  // ─── Cadastro ─────────────────────────────────────────────────────────────

  test("deve cadastrar uma pessoa adulta com sucesso", async () => {
    const nome = `João Teste ${Date.now()}`;

    await pessoasPage.cadastrarPessoa(nome, "1990-05-15");

    // Verificar toast de sucesso
    const toast = await pessoasPage.waitForSuccessToast();
    expect(toast).toContain("sucesso");

    // Verificar na tabela
    await pessoasPage.verificarPessoaNaTabela(nome);
  });

  test("deve exibir erro de validação quando nome está vazio", async () => {
    await pessoasPage.abrirFormularioCadastro();
    await pessoasPage.preencherDataNascimento("1990-01-01");
    await pessoasPage.salvar();

    await pessoasPage.verificarMensagemErro("Nome é obrigatório");
  });

  test("deve exibir erro de validação quando data de nascimento não é preenchida", async ({ page }) => {
    await pessoasPage.abrirFormularioCadastro();
    await pessoasPage.preencherNome("Pessoa Sem Data");
    await pessoasPage.salvar();

    // Aguardar um pouco para ver se há validação
    await page.waitForTimeout(1000);

    // Verificar se há alguma mensagem de erro relacionada à data
    const errorMessages = [
      "Data de nascimento é obrigatória",
      "Data é obrigatória", 
      "Campo obrigatório",
      "Required",
      "Invalid input",
      "obrigatório"
    ];
    
    let errorFound = false;
    for (const message of errorMessages) {
      try {
        await expect(page.getByText(message, { exact: false })).toBeVisible({ timeout: 1000 });
        errorFound = true;
        break;
      } catch {
        // Continuar tentando outras mensagens
      }
    }
    
    // Se não encontrou mensagem de erro, verificar se o formulário ainda está aberto
    if (!errorFound) {
      try {
        await expect(page.getByRole("dialog")).toBeVisible({ timeout: 1000 });
        errorFound = true; // Formulário aberto indica erro de validação
      } catch {
        // Se o formulário fechou, pode ser que a validação seja no backend
        // Vamos verificar se a pessoa foi realmente criada (não deveria ter sido)
        const pessoaSemData = page.getByRole("cell", { name: "Pessoa Sem Data" });
        await expect(pessoaSemData).not.toBeVisible({ timeout: 2000 });
        errorFound = true; // Se não foi criada, a validação funcionou
      }
    }
    
    // Se chegou aqui e não encontrou nenhuma evidência de validação, falhar o teste
    if (!errorFound) {
      throw new Error("Nenhuma validação de data de nascimento foi detectada");
    }
  });

  test("deve fechar o formulário ao clicar em Cancelar", async ({ page }) => {
    await pessoasPage.abrirFormularioCadastro();
    await pessoasPage.cancelar();

    await expect(page.getByRole("dialog")).not.toBeVisible();
  });

  // ─── Edição ───────────────────────────────────────────────────────────────

  test("deve editar uma pessoa existente com sucesso", async ({ page }) => {
    const nomeOriginal = `Editar Teste ${Date.now()}`;
    const nomeAtualizado = `Editado ${Date.now()}`;

    // Cadastrar primeiro
    await pessoasPage.cadastrarPessoa(nomeOriginal, "1985-03-20");
    await pessoasPage.waitForSuccessToast();
    
    // Aguardar um pouco para garantir que a pessoa foi salva e está na tabela
    await page.waitForTimeout(2000);

    // Editar
    await pessoasPage.clicarEditarPessoa(nomeOriginal);
    await pessoasPage.preencherNome(nomeAtualizado);
    await pessoasPage.salvar();

    const toast = await pessoasPage.waitForSuccessToast();
    expect(toast).toContain("sucesso");
  });

  // ─── Exclusão ─────────────────────────────────────────────────────────────

  test("deve deletar uma pessoa após confirmação", async ({ page }) => {
    const nome = `Deletar Teste ${Date.now()}`;

    // Cadastrar pessoa
    await pessoasPage.cadastrarPessoa(nome, "1992-07-10");
    await pessoasPage.waitForSuccessToast();
    
    // Aguardar brevemente
    await page.waitForTimeout(2000);

    // Tentar deletar diretamente sem verificações complexas
    await pessoasPage.clicarDeletarPessoa(nome);
    await pessoasPage.confirmarDelecao();

    // Verificar que foi deletada com timeout curto
    await expect(page.getByRole("cell", { name: nome })).not.toBeVisible({ timeout: 5000 });
  });

  // ─── Navegação ────────────────────────────────────────────────────────────

  test("deve exibir a página de pessoas com o título correto", async ({ page }) => {
    await expect(page.getByRole("heading", { name: "Pessoas" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Adicionar Pessoa" })).toBeVisible();
  });
});
