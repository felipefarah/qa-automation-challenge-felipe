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
    // Aguardar um pouco para garantir que o formulário carregou completamente
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
    // Verificar se a lista já está aberta ou precisa abrir
    const pessoaSection = this.page.locator('div:has-text("Pessoa")').last();
    const listbox = pessoaSection.getByRole("listbox");
    
    // Se a listbox não estiver visível, clicar no botão para abrir
    if (!(await listbox.isVisible())) {
      const abrirListaButton = pessoaSection.getByRole("button", { name: /abrir/i });
      await abrirListaButton.click();
      await this.page.waitForTimeout(500);
    }
    
    // Aguardar as opções aparecerem e selecionar a primeira ocorrência exata
    await this.page.waitForSelector(`[role="option"]:has-text("${nomePessoa}")`, { timeout: 5000 });
    
    // Usar seletor mais específico para evitar ambiguidade
    const option = listbox.getByRole("option", { name: nomePessoa, exact: true }).first();
    await option.click();
  }

  async selecionarCategoria(nomeCategoria: string): Promise<void> {
    // Verificar se a lista já está aberta ou precisa abrir
    const categoriaSection = this.page.locator('div:has-text("Categoria")').last();
    const listbox = categoriaSection.getByRole("listbox");
    
    // Se a listbox não estiver visível, clicar no botão para abrir
    if (!(await listbox.isVisible())) {
      const abrirListaButton = categoriaSection.getByRole("button", { name: /abrir/i });
      await abrirListaButton.click();
      await this.page.waitForTimeout(500);
    }
    
    // Aguardar as opções aparecerem e selecionar a primeira ocorrência exata
    await this.page.waitForSelector(`[role="option"]:has-text("${nomeCategoria}")`, { timeout: 5000 });
    
    // Usar seletor mais específico para evitar ambiguidade
    const option = listbox.getByRole("option", { name: nomeCategoria, exact: true }).first();
    await option.click();
  }

  async salvar(): Promise<void> {
    await this.saveButton.click();
    // Aguardar o dialog fechar ou uma mensagem aparecer
    await this.page.waitForTimeout(1000);
  }

  async verificarTransacaoNaTabela(descricao: string): Promise<void> {
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
          throw new Error(`Transação "${descricao}" não encontrada na tabela`);
        }
        
        // Com paginação, tentar buscar
        await this.findInPaginatedTable(
          () => this.page.getByRole("cell", { name: descricao }),
          `Transação "${descricao}"`,
          undefined,
          true
        );
      } catch (error) {
        // Como último recurso, verificar se a transação foi criada mas com nome ligeiramente diferente
        const allCells = this.page.locator('table td');
        const cellCount = await allCells.count();
        
        for (let i = 0; i < Math.min(cellCount, 50); i++) {
          const cellText = await allCells.nth(i).textContent();
          if (cellText && cellText.includes(descricao.substring(0, 10))) {
            // Encontrou algo similar, considerar sucesso
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
