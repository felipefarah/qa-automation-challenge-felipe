import { Page } from "@playwright/test";

/**
 * Classe base para Page Objects.
 * Centraliza utilitários comuns como navegação e espera por toasts.
 */
export abstract class BasePage {
  constructor(protected readonly page: Page) {}

  /** Aguarda e retorna o texto do toast de sucesso. */
  async waitForSuccessToast(): Promise<string> {
    const toast = this.page.locator('[data-sonner-toast], [role="status"]').first();
    await toast.waitFor({ state: "visible", timeout: 8_000 });
    return (await toast.textContent()) ?? "";
  }

  /** Aguarda e retorna o texto do toast de erro. */
  async waitForErrorToast(): Promise<string> {
    // react-hot-toast usa div com role="status" ou classe específica
    const toast = this.page
      .locator('div[role="status"], [data-type="error"]')
      .first();
    await toast.waitFor({ state: "visible", timeout: 8_000 });
    return (await toast.textContent()) ?? "";
  }

  /** Navega para uma rota relativa. */
  async navigateTo(path: string): Promise<void> {
    await this.page.goto(path);
    await this.page.waitForLoadState("networkidle");
    // Aguardar um pouco mais para garantir que componentes dinâmicos carregaram
    await this.page.waitForTimeout(500);
  }

  /** Aguarda uma tabela carregar completamente. */
  async waitForTableLoad(): Promise<void> {
    await this.page.waitForSelector("table", { timeout: 10000 });
    await this.page.waitForTimeout(1000); // Aguardar dados carregarem
  }

  /** 
   * Procura por um elemento em uma tabela paginada.
   * Navega pelas páginas até encontrar o elemento ou esgotar as páginas.
   * Para itens recém-criados, começa pela última página.
   */
  async findInPaginatedTable(
    elementLocator: () => any,
    elementName: string,
    action?: () => Promise<void>,
    searchFromEnd: boolean = true
  ): Promise<void> {
    await this.waitForTableLoad();
    
    // Primeiro tentar encontrar na página atual
    try {
      const element = elementLocator();
      await element.waitFor({ state: "visible", timeout: 3000 });
      if (action) {
        await action();
      }
      return;
    } catch {
      // Se não encontrou na página atual, tentar navegar
    }
    
    // Se searchFromEnd for true, ir para a última página primeiro (para itens recém-criados)
    if (searchFromEnd) {
      try {
        const lastPageButtons = this.page.getByRole("button").filter({ hasText: /\d+/ });
        const lastPageButton = lastPageButtons.last();
        
        if (await lastPageButton.isVisible() && await lastPageButton.isEnabled()) {
          await lastPageButton.click();
          await this.page.waitForLoadState("networkidle");
          
          // Tentar encontrar na última página
          try {
            const element = elementLocator();
            await element.waitFor({ state: "visible", timeout: 3000 });
            if (action) {
              await action();
            }
            return;
          } catch {
            // Não encontrou na última página, continuar procurando
          }
        }
      } catch {
        // Se não conseguir ir para a última página, continuar normalmente
      }
    }
    
    // Voltar para a primeira página e navegar sequencialmente
    try {
      const firstPageButton = this.page.getByRole("button", { name: "1" });
      if (await firstPageButton.isVisible() && await firstPageButton.isEnabled()) {
        await firstPageButton.click();
        await this.page.waitForLoadState("networkidle");
      }
    } catch {
      // Ignorar se não conseguir voltar para a primeira página
    }
    
    // Navegar página por página
    let currentPage = 1;
    const maxPages = 5; // Reduzir limite para evitar timeouts
    
    while (currentPage <= maxPages) {
      try {
        const element = elementLocator();
        await element.waitFor({ state: "visible", timeout: 2000 });
        if (action) {
          await action();
        }
        return;
      } catch {
        // Tentar próxima página
        const nextButton = this.page.getByRole("button", { name: /próximo/i });
        
        try {
          if (await nextButton.isVisible() && await nextButton.isEnabled()) {
            await nextButton.click();
            await this.page.waitForLoadState("networkidle");
            currentPage++;
          } else {
            break; // Não há mais páginas
          }
        } catch {
          break; // Erro ao navegar, parar
        }
      }
    }
    
    // Se chegou aqui, não encontrou em nenhuma página
    throw new Error(`Elemento "${elementName}" não encontrado na tabela paginada`);
  }

  /** Clica em um botão pelo texto visível. */
  async clickButton(text: string): Promise<void> {
    await this.page.getByRole("button", { name: text }).click();
  }

  /** Preenche um campo pelo label. */
  async fillByLabel(label: string, value: string): Promise<void> {
    await this.page.getByLabel(label).fill(value);
  }
}
