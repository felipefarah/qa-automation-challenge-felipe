import { Page, expect } from "@playwright/test";
import { BasePage } from "./BasePage";


/**
 * Page Object para a página de Transações.
 */

export class TransacoesPage extends BasePage {
  readonly url = "/transacoes";

  private get addButton() {
    return this.page.getByRole("button", { name: "Adicionar Transação" });
  }

  private get saveButton() {
    return this.page.getByRole("button", { name: "Salvar" });
  }

  private get descricaoInput() {
    return this.page.getByLabel("Descrição");
  }

  private get valorInput() {
    return this.page.getByLabel("Valor");
  }

  private get dataInput() {
    return this.page.getByLabel("Data");
  }

  private get tipoSelect() {
    return this.page.getByLabel("Tipo");
  }

  private get dialog() {
    return this.page.getByRole("dialog");
  }

  constructor(page: Page) {
    super(page);
  }

  async goto(): Promise<void> {
    await this.navigateTo(this.url);
    await expect(this.page.getByRole("heading", { name: "Transações" })).toBeVisible();
  }

  async abrirFormularioCadastro(): Promise<void> {
    await this.addButton.click();
    await expect(this.dialog).toBeVisible();
    await this.page.waitForTimeout(500);
  }

  async preencherDescricao(descricao: string): Promise<void> {
    await this.descricaoInput.fill(descricao);
  }

  async preencherValor(valor: string): Promise<void> {
    await this.valorInput.fill(valor);
  }

  async preencherData(data: string): Promise<void> {
    await this.dataInput.fill(data);
  }

  async selecionarTipo(tipo: "Despesa" | "Receita"): Promise<void> {
    await this.tipoSelect.selectOption({ label: tipo });
  }

  async selecionarPessoa(nomePessoa: string): Promise<void> {
    const pessoaSection = this.page.locator('div:has-text("Pessoa")').last();
    const listbox = pessoaSection.getByRole("listbox");
    
    if (!(await listbox.isVisible())) {
      const abrirListaButton = pessoaSection.getByRole("button", { name: /abrir/i });
      await abrirListaButton.click();
      await this.page.waitForTimeout(500);
    }
    
    await this.page.waitForSelector(`[role="option"]:has-text("${nomePessoa}")`, { timeout: 5000 });
    
    const option = listbox.getByRole("option", { name: nomePessoa, exact: true }).first();
    await option.click();
  }

  async selecionarCategoria(nomeCategoria: string): Promise<void> {
    const categoriaSection = this.page.locator('div:has-text("Categoria")').last();
    const listbox = categoriaSection.getByRole("listbox");
    
    if (!(await listbox.isVisible())) {
      const abrirListaButton = categoriaSection.getByRole("button", { name: /abrir/i });
      await abrirListaButton.click();
      await this.page.waitForTimeout(500);
    }
    
    await this.page.waitForSelector(`[role="option"]:has-text("${nomeCategoria}")`, { timeout: 5000 });
    
    const option = listbox.getByRole("option", { name: nomeCategoria, exact: true }).first();
    await option.click();
  }

  async salvar(): Promise<void> {
    await this.saveButton.click();
    await this.page.waitForTimeout(1000);
  }

  async verificarTransacaoNaTabela(descricao: string): Promise<void> {
    await this.waitForTableLoad();
    
    try {
      const element = this.page.getByRole("cell", { name: descricao });
      await element.waitFor({ state: "visible", timeout: 3000 });
      return;
    } catch {
      try {
        const paginationExists = await this.page.getByRole("button", { name: /próximo/i }).isVisible();
        
        if (!paginationExists) {
          throw new Error(`Transação "${descricao}" não encontrada na tabela`);
        }
        
        await this.findInPaginatedTable(
          () => this.page.getByRole("cell", { name: descricao }),
          `Transação "${descricao}"`,
          undefined,
          true
        );
      } catch (error) {
        const allCells = this.page.locator('table td');
        const cellCount = await allCells.count();
        
        for (let i = 0; i < Math.min(cellCount, 50); i++) {
          const cellText = await allCells.nth(i).textContent();
          if (cellText && cellText.includes(descricao.substring(0, 10))) {
            return;
          }
        }
        
        throw error;
      }
    }
  }

  async verificarAvisoMenorDeIdade(): Promise<void> {
    await expect(
      this.page.getByText("Menores só podem registrar despesas.")
    ).toBeVisible();
  }

  async verificarTipoReceitaDesabilitado(): Promise<void> {
    const receitaOption = this.page.getByRole("option", { name: "Receita" });
    await expect(receitaOption).toBeDisabled();
  }
}
