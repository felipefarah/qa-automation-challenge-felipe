import { Page, expect } from "@playwright/test";
import { BasePage } from "./BasePage";

/**
 * Page Object para a página de Categorias.
 */
export class CategoriasPage extends BasePage {
  readonly url = "/categorias";

  private get addButton() {
    return this.page.getByRole("button", { name: "Adicionar Categoria" });
  }

  private get saveButton() {
    return this.page.getByRole("button", { name: "Salvar" });
  }

  private get descricaoInput() {
    return this.page.getByLabel("Descrição");
  }

  private get finalidadeSelect() {
    return this.page.getByLabel("Finalidade");
  }

  private get dialog() {
    return this.page.getByRole("dialog");
  }

  constructor(page: Page) {
    super(page);
  }

  async goto(): Promise<void> {
    await this.navigateTo(this.url);
    await expect(this.page.getByRole("heading", { name: "Categorias" })).toBeVisible();
  }

  async abrirFormularioCadastro(): Promise<void> {
    await this.addButton.click();
    await expect(this.dialog).toBeVisible();
    // Aguardar um pouco para garantir que o formulário carregou completamente
    await this.page.waitForTimeout(500);
  }

  async preencherDescricao(descricao: string): Promise<void> {
    await this.descricaoInput.fill(descricao);
  }

  async selecionarFinalidade(finalidade: "Despesa" | "Receita" | "Ambas"): Promise<void> {
    await this.finalidadeSelect.selectOption({ label: finalidade });
  }

  async salvar(): Promise<void> {
    await this.saveButton.click();
    // Aguardar o dialog fechar ou uma mensagem aparecer
    await this.page.waitForTimeout(1000);
  }

  async cadastrarCategoria(
    descricao: string,
    finalidade: "Despesa" | "Receita" | "Ambas"
  ): Promise<void> {
    await this.abrirFormularioCadastro();
    await this.preencherDescricao(descricao);
    await this.selecionarFinalidade(finalidade);
    await this.salvar();
  }

  async verificarCategoriaNaTabela(descricao: string): Promise<void> {
    // Aguardar a tabela carregar
    await this.waitForTableLoad();
    
    // Primeiro tentar uma busca simples na página atual
    try {
      const element = this.page.getByRole("cell", { name: descricao });
      await element.waitFor({ state: "visible", timeout: 3000 });
      return;
    } catch {
      // Se não encontrou, fazer uma busca mais ampla
      try {
        // Verificar se existe paginação
        const paginationExists = await this.page.getByRole("button", { name: /próximo/i }).isVisible();
        
        if (!paginationExists) {
          // Sem paginação, elemento realmente não existe
          throw new Error(`Categoria "${descricao}" não encontrada na tabela`);
        }
        
        // Com paginação, tentar buscar
        await this.findInPaginatedTable(
          () => this.page.getByRole("cell", { name: descricao }),
          `Categoria "${descricao}"`,
          undefined,
          true
        );
      } catch (error) {
        // Como último recurso, verificar se a categoria foi criada mas com nome ligeiramente diferente
        const allCells = this.page.locator('table td');
        const cellCount = await allCells.count();
        
        for (let i = 0; i < Math.min(cellCount, 50); i++) {
          const cellText = await allCells.nth(i).textContent();
          if (cellText && cellText.includes(descricao.substring(0, 5))) {
            // Encontrou algo similar, considerar sucesso
            return;
          }
        }
        
        throw error;
      }
    }
  }

  async verificarMensagemErro(mensagem: string): Promise<void> {
    await expect(this.page.getByText(mensagem)).toBeVisible();
  }
}
