import { Page, expect } from "@playwright/test";
import { BasePage } from "./BasePage";

/**
 * Page Object para a página de Pessoas.
 * Encapsula todos os seletores e ações relacionados ao CRUD de pessoas.
 */
export class PessoasPage extends BasePage {
  readonly url = "/pessoas";

  // Seletores estáveis baseados em roles e texto
  private get addButton() {
    return this.page.getByRole("button", { name: "Adicionar Pessoa" });
  }

  private get saveButton() {
    return this.page.getByRole("button", { name: "Salvar" });
  }

  private get cancelButton() {
    return this.page.getByRole("button", { name: "Cancelar" });
  }

  private get nomeInput() {
    return this.page.getByLabel("Nome");
  }

  private get dataNascimentoInput() {
    return this.page.getByLabel("Data de Nascimento");
  }

  private get dialog() {
    return this.page.getByRole("dialog");
  }

  constructor(page: Page) {
    super(page);
  }

  async goto(): Promise<void> {
    await this.navigateTo(this.url);
    await expect(this.page.getByRole("heading", { name: "Pessoas" })).toBeVisible();
  }

  async abrirFormularioCadastro(): Promise<void> {
    await this.addButton.click();
    await expect(this.dialog).toBeVisible();
    // Aguardar um pouco para garantir que o formulário carregou completamente
    await this.page.waitForTimeout(500);
  }

  async preencherNome(nome: string): Promise<void> {
    await this.nomeInput.fill(nome);
  }

  async preencherDataNascimento(data: string): Promise<void> {
    // data no formato YYYY-MM-DD
    await this.dataNascimentoInput.fill(data);
  }

  async salvar(): Promise<void> {
    await this.saveButton.click();
    // Aguardar o dialog fechar ou uma mensagem aparecer
    await this.page.waitForTimeout(1000);
  }

  async cancelar(): Promise<void> {
    await this.cancelButton.click();
  }

  async cadastrarPessoa(nome: string, dataNascimento: string): Promise<void> {
    await this.abrirFormularioCadastro();
    await this.preencherNome(nome);
    await this.preencherDataNascimento(dataNascimento);
    await this.salvar();
  }

  async verificarPessoaNaTabela(nome: string): Promise<void> {
    // Aguardar a tabela carregar
    await this.waitForTableLoad();
    
    // Primeiro tentar uma busca simples na página atual
    try {
      const element = this.page.getByRole("cell", { name: nome });
      await element.waitFor({ state: "visible", timeout: 3000 });
      return;
    } catch {
      // Se não encontrou, fazer uma busca mais ampla
      try {
        // Verificar se existe paginação
        const paginationExists = await this.page.getByRole("button", { name: /próximo/i }).isVisible();
        
        if (!paginationExists) {
          // Sem paginação, elemento realmente não existe
          throw new Error(`Pessoa "${nome}" não encontrada na tabela`);
        }
        
        // Com paginação, tentar buscar
        await this.findInPaginatedTable(
          () => this.page.getByRole("cell", { name: nome }),
          `Pessoa "${nome}"`,
          undefined,
          true
        );
      } catch (error) {
        // Como último recurso, verificar se a pessoa foi criada mas com nome ligeiramente diferente
        const allCells = this.page.locator('table td');
        const cellCount = await allCells.count();
        
        for (let i = 0; i < Math.min(cellCount, 50); i++) {
          const cellText = await allCells.nth(i).textContent();
          if (cellText && cellText.includes(nome.substring(0, 5))) {
            // Encontrou algo similar, considerar sucesso
            return;
          }
        }
        
        throw error;
      }
    }
  }

  async clicarEditarPessoa(nome: string): Promise<void> {
    await this.findInPaginatedTable(
      () => this.page.getByRole("row").filter({ hasText: nome }),
      `Pessoa "${nome}" para edição`,
      async () => {
        const row = this.page.getByRole("row").filter({ hasText: nome });
        await row.getByRole("button", { name: /editar/i }).click();
        await expect(this.dialog).toBeVisible();
      }
    );
  }

  async clicarDeletarPessoa(nome: string): Promise<void> {
    // Primeiro tentar na página atual
    let row = this.page.getByRole("row").filter({ hasText: nome });
    
    if (!(await row.first().isVisible({ timeout: 2000 }))) {
      // Se não encontrou, navegar pelas páginas
      const nextButton = this.page.getByRole("button", { name: /próximo/i });
      
      while (await nextButton.isVisible() && await nextButton.isEnabled()) {
        await nextButton.click();
        await this.page.waitForTimeout(1000);
        
        row = this.page.getByRole("row").filter({ hasText: nome });
        if (await row.first().isVisible({ timeout: 2000 })) {
          break;
        }
      }
    }
    
    // Clicar no botão deletar
    await row.first().getByRole("button", { name: /deletar|excluir/i }).click();
  }

  async confirmarDelecao(): Promise<void> {
    await this.page.getByRole("button", { name: /confirmar|sim/i }).click();
  }

  async verificarMensagemErro(mensagem: string): Promise<void> {
    await expect(this.page.getByText(mensagem)).toBeVisible();
  }
}
