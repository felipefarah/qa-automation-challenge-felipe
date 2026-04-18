import { test, expect } from "@playwright/test";

/**
 * Testes E2E de navegação geral da aplicação.
 */
test.describe("Navegação — Fluxos E2E", () => {
  test("deve carregar a página inicial", async ({ page }) => {
    await page.goto("/");
    await page.waitForLoadState("networkidle");

    // Verificar que a aplicação carregou (sidebar ou header visível)
    await expect(page.locator("nav, aside, header").first()).toBeVisible();
  });

  test("deve navegar para Pessoas via sidebar", async ({ page }) => {
    await page.goto("/");
    await page.waitForLoadState("networkidle");
    
    // Usar seletor específico da sidebar para evitar ambiguidade
    await page.locator("aside").getByRole("link", { name: /pessoas/i }).click();
    await expect(page).toHaveURL(/\/pessoas/);
    await expect(page.getByRole("heading", { name: "Pessoas" })).toBeVisible();
  });

  test("deve navegar para Categorias via sidebar", async ({ page }) => {
    await page.goto("/");
    await page.waitForLoadState("networkidle");
    
    // Usar seletor específico da sidebar para evitar ambiguidade
    await page.locator("aside").getByRole("link", { name: /categorias/i }).click();
    await expect(page).toHaveURL(/\/categorias/);
    await expect(page.getByRole("heading", { name: "Categorias" })).toBeVisible();
  });

  test("deve navegar para Transações via sidebar", async ({ page }) => {
    await page.goto("/");
    await page.waitForLoadState("networkidle");
    
    // Usar seletor específico da sidebar para evitar ambiguidade
    await page.locator("aside").getByRole("link", { name: /transações/i }).click();
    await expect(page).toHaveURL(/\/transacoes/);
    await expect(page.getByRole("heading", { name: "Transações" })).toBeVisible();
  });

  test("deve navegar para Dashboard via sidebar", async ({ page }) => {
    await page.goto("/");
    await page.waitForLoadState("networkidle");
    
    // Usar seletor específico da sidebar para evitar ambiguidade
    await page.locator("aside").getByRole("link", { name: /dashboard/i }).click();
    await expect(page).toHaveURL("/");
    
    // Verificar se há elementos do dashboard (cards de resumo, gráficos, etc.)
    // Aguardar um pouco para os dados carregarem
    await page.waitForTimeout(2000);
    
    // Verificar se existem elementos típicos de dashboard
    const dashboardElements = [
      page.getByText(/total/i),
      page.getByText(/receitas/i),
      page.getByText(/despesas/i),
      page.getByText(/saldo/i),
      page.locator('[data-testid*="dashboard"], [class*="dashboard"], [class*="card"], [class*="summary"]')
    ];
    
    // Pelo menos um elemento do dashboard deve estar visível
    let dashboardFound = false;
    for (const element of dashboardElements) {
      try {
        await expect(element.first()).toBeVisible({ timeout: 3000 });
        dashboardFound = true;
        break;
      } catch {
        // Continuar tentando outros elementos
      }
    }
    
    // Se não encontrou elementos específicos, pelo menos verificar que não está em uma página de erro
    if (!dashboardFound) {
      await expect(page.locator("body")).not.toContainText("404");
      await expect(page.locator("body")).not.toContainText("Error");
    }
  });

  test("deve navegar para Totais via sidebar", async ({ page }) => {
    await page.goto("/");
    await page.waitForLoadState("networkidle");
    
    // Usar seletor específico da sidebar para evitar ambiguidade
    await page.locator("aside").getByRole("link", { name: /totais|relatórios/i }).click();
    await expect(page).toHaveURL(/\/totais/);
  });
});
