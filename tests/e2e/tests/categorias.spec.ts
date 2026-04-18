import { test, expect } from "@playwright/test";
import { CategoriasPage } from "../pages/CategoriasPage";

/**
 * Testes E2E para o fluxo de Categorias.
 */
test.describe("Categorias — Fluxos E2E", () => {
  let categoriasPage: CategoriasPage;

  test.beforeEach(async ({ page }) => {
    categoriasPage = new CategoriasPage(page);
    await categoriasPage.goto();
  });

  // ─── Cadastro ─────────────────────────────────────────────────────────────

  test("deve cadastrar categoria de Despesa com sucesso", async () => {
    const descricao = `Alimentação ${Date.now()}`;

    await categoriasPage.cadastrarCategoria(descricao, "Despesa");

    const toast = await categoriasPage.waitForSuccessToast();
    expect(toast).toContain("sucesso");

    await categoriasPage.verificarCategoriaNaTabela(descricao);
  });

  test("deve cadastrar categoria de Receita com sucesso", async () => {
    const descricao = `Salário ${Date.now()}`;

    await categoriasPage.cadastrarCategoria(descricao, "Receita");

    const toast = await categoriasPage.waitForSuccessToast();
    expect(toast).toContain("sucesso");

    await categoriasPage.verificarCategoriaNaTabela(descricao);
  });

  test("deve cadastrar categoria Ambas com sucesso", async () => {
    const descricao = `Investimentos ${Date.now()}`;

    await categoriasPage.cadastrarCategoria(descricao, "Ambas");

    const toast = await categoriasPage.waitForSuccessToast();
    expect(toast).toContain("sucesso");
  });

  test("deve exibir erro de validação quando descrição está vazia", async () => {
    await categoriasPage.abrirFormularioCadastro();
    await categoriasPage.selecionarFinalidade("Despesa");
    await categoriasPage.salvar();

    await categoriasPage.verificarMensagemErro("Descrição é obrigatória");
  });

  // ─── Navegação ────────────────────────────────────────────────────────────

  test("deve exibir a página de categorias com o título correto", async ({ page }) => {
    await expect(page.getByRole("heading", { name: "Categorias" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Adicionar Categoria" })).toBeVisible();
  });

  test("deve exibir as categorias seed na tabela", async ({ page }) => {
    // As categorias seed são criadas automaticamente em desenvolvimento
    // Verificar se há pelo menos algumas categorias na tabela
    await expect(page.getByRole("cell", { name: "Alimentação", exact: true })).toBeVisible();
    
    // Verificar se a tabela tem dados
    const rows = page.locator("tbody tr");
    await expect(rows.first()).toBeVisible();
  });
});
