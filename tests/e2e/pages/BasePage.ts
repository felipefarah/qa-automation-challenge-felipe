import { Page } from "@playwright/test";


/**
 * Classe base para Page Objects.
 * Centraliza utilitários comuns como navegação e espera por toasts.
 */

export abstract class BasePage {
  constructor(protected readonly page: Page) {}

 
  async waitForSuccessToast(): Promise<string> {
    const toast = this.page.locator('[data-sonner-toast], [role="status"]').first();
    await toast.waitFor({ state: "visible", timeout: 8_000 });
    return (await toast.textContent()) ?? "";
  }

  async waitForErrorToast(): Promise<string> {
   
    const toast = this.page
      .locator('div[role="status"], [data-type="error"]')
      .first();
    await toast.waitFor({ state: "visible", timeout: 8_000 });
    return (await toast.textContent()) ?? "";
  }

  async navigateTo(path: string): Promise<void> {
    await this.page.goto(path);
    await this.page.waitForLoadState("networkidle");
    await this.page.waitForTimeout(500);
  }


  async waitForTableLoad(): Promise<void> {
    await this.page.waitForSelector("table", { timeout: 10000 });
    await this.page.waitForTimeout(1000); // Aguardar dados carregarem
  }


  async findInPaginatedTable(
    elementLocator: () => any,
    elementName: string,
    action?: () => Promise<void>,
    searchFromEnd: boolean = true
  ): Promise<void> {
    await this.waitForTableLoad();
    

    if (searchFromEnd) {
     
      const lastPageButtons = this.page.getByRole("button").filter({ hasText: /\d+/ });
      const lastPageButton = lastPageButtons.last();
      
      try {
        if (await lastPageButton.isVisible() && await lastPageButton.isEnabled()) {
          await lastPageButton.click();
          await this.page.waitForTimeout(1000);
        }
      } catch {
        
      }
    }
    
  
    try {
      const element = elementLocator();
      await element.waitFor({ state: "visible", timeout: 5000 });
      if (action) {
        await action();
      }
      return;
    } catch {
  
      const firstPageButton = this.page.getByRole("button", { name: "1" });
      try {
        if (await firstPageButton.isVisible() && await firstPageButton.isEnabled()) {
          await firstPageButton.click();
          await this.page.waitForTimeout(1000);
        }
      } catch {
    
      }
      
      
      let currentPage = 1;
      const maxPages = 10; 
      
      while (currentPage <= maxPages) {
        try {
          const element = elementLocator();
          await element.waitFor({ state: "visible", timeout: 3000 });
          if (action) {
            await action();
          }
          return;
        } catch {
         
          const nextButton = this.page.getByRole("button", { name: /próximo/i });
          
          if (await nextButton.isVisible() && await nextButton.isEnabled()) {
            await nextButton.click();
            await this.page.waitForTimeout(1000);
            currentPage++;
          } else {
            break; 
          }
        }
      }
      
      
      throw new Error(`Elemento "${elementName}" não encontrado na tabela paginada`);
    }
  }


  async clickButton(text: string): Promise<void> {
    await this.page.getByRole("button", { name: text }).click();
  }

  async fillByLabel(label: string, value: string): Promise<void> {
    await this.page.getByLabel(label).fill(value);
  }
}
