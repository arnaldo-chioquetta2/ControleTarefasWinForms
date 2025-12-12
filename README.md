# Controle de Tarefas - Aplicativo Desktop Windows

## Descrição

Aplicativo desktop desenvolvido em **C# WinForms** com **.NET Framework 4.7.2** para controle e gerenciamento de tarefas sequenciais com contagem de tempo acumulado.

## Funcionalidades Principais

### 1. Gerenciamento de Tarefas
- **Adicionar tarefas dinamicamente** através de interface intuitiva
- **Ativar tarefas** clicando nos botões coloridos
- **Visualização em tempo real** do tempo acumulado em cada tarefa
- **Exclusão de tarefas** usando a tecla Delete (com confirmação)

### 2. Sistema de Estados
Cada tarefa possui três estados possíveis:

- **Pendente** (Cinza claro): Tarefa nunca foi clicada ou foi resetada no ciclo atual
- **Ativa** (Verde claro): Tarefa atualmente selecionada, com contagem de tempo ativa
- **Já Clicada** (Azul claro): Tarefa já foi executada neste ciclo, mas não é a ativa

### 3. Ciclo de Tarefas
O aplicativo implementa um sistema de ciclo inteligente:

- As tarefas podem ser clicadas em **qualquer ordem**
- Quando a **última tarefa** da lista é clicada e depois qualquer outra tarefa é selecionada, um **novo ciclo** é iniciado
- No início de um novo ciclo, todas as tarefas retornam ao estado **Pendente**
- O **tempo acumulado é preservado** entre ciclos

### 4. Contagem de Tempo
- Timer global atualiza a cada **1 segundo**
- Apenas a tarefa **Ativa** tem seu tempo incrementado
- Tempo é exibido no formato **HH:MM:SS**
- Tempo total é **acumulativo** e não zera entre ciclos

### 5. Persistência de Dados
- Todas as tarefas são salvas em arquivo **INI** (`tarefas.ini`)
- Dados persistidos:
  - Nome da tarefa
  - Tempo total acumulado (em segundos)
  - Estado atual
  - ID único
- Carregamento automático ao iniciar o aplicativo
- Salvamento automático após cada alteração

### 6. Interface Responsiva
- **FlowLayoutPanel** com scroll automático para lista de tarefas
- **DataGridView** com visão tabular das tarefas
- Botões se ajustam automaticamente ao redimensionar a janela
- **Minimização automática** 2 segundos após clicar em uma tarefa

## Estrutura do Projeto

```
ControleTarefasWinForms/
│
├── Models/
│   ├── TaskModel.cs          # Modelo de dados da tarefa
│   └── TaskState.cs           # Enum com estados possíveis
│
├── Services/
│   ├── IniFile.cs             # Helper para manipulação de arquivos INI
│   └── TaskRepository.cs      # Repositório de persistência
│
├── Properties/
│   ├── AssemblyInfo.cs
│   ├── Resources.resx
│   ├── Resources.Designer.cs
│   ├── Settings.settings
│   └── Settings.Designer.cs
│
├── Program.cs                 # Ponto de entrada do aplicativo
├── MainForm.cs                # Formulário principal
├── MainForm.Designer.cs       # Designer do formulário principal
├── MainForm.resx              # Recursos do formulário principal
├── AddTaskForm.cs             # Formulário de adição de tarefas
├── AddTaskForm.Designer.cs    # Designer do formulário de adição
├── AddTaskForm.resx           # Recursos do formulário de adição
├── App.config                 # Configuração do aplicativo
├── ControleTarefasWinForms.csproj  # Arquivo de projeto
└── ControleTarefasWinForms.sln     # Solution do Visual Studio
```

## Requisitos

- **Sistema Operacional**: Windows 7 ou superior
- **Framework**: .NET Framework 4.7.2
- **IDE recomendada**: Visual Studio 2017 ou superior

## Como Compilar

### Usando Visual Studio

1. Abra o arquivo `ControleTarefasWinForms.sln` no Visual Studio
2. No menu superior, selecione **Build** → **Build Solution** (ou pressione `Ctrl+Shift+B`)
3. O executável será gerado em `bin/Debug/ControleTarefasWinForms.exe` ou `bin/Release/ControleTarefasWinForms.exe`

### Usando MSBuild (linha de comando)

```bash
# Para compilação em modo Debug
msbuild ControleTarefasWinForms.sln /p:Configuration=Debug

# Para compilação em modo Release
msbuild ControleTarefasWinForms.sln /p:Configuration=Release
```

## Como Usar

### Iniciando o Aplicativo

1. Execute o arquivo `ControleTarefasWinForms.exe`
2. Na primeira execução, o aplicativo inicia sem tarefas cadastradas

### Adicionando Tarefas

1. Clique no botão **"Adicionar Tarefa"** na parte inferior da janela
2. Digite o nome da tarefa no campo de texto
3. Clique em **"OK"** para confirmar ou **"Cancelar"** para desistir

### Ativando Tarefas

1. Clique em qualquer botão de tarefa para ativá-la
2. A tarefa ficará **verde** (Ativa) e começará a contar tempo
3. A janela será **minimizada automaticamente** após 2 segundos

### Excluindo Tarefas

1. Clique na tarefa que deseja excluir para torná-la ativa
2. Pressione a tecla **Delete**
3. Confirme a exclusão na janela de diálogo

### Visualizando Dados

- **Botões coloridos**: Visualização rápida com cores indicando estados
- **DataGridView**: Tabela detalhada com nome, tempo e status de cada tarefa

## Estrutura do Arquivo INI

O arquivo `tarefas.ini` é criado automaticamente na mesma pasta do executável:

```ini
[Geral]
Count=3

[Tarefa_1]
Id=1
Nome=Primeira tarefa
TotalSegundos=125
State=Pendente

[Tarefa_2]
Id=2
Nome=Segunda tarefa
TotalSegundos=300
State=JaClicada

[Tarefa_3]
Id=3
Nome=Terceira tarefa
TotalSegundos=45
State=Ativa
```

## Atalhos de Teclado

- **Delete**: Exclui a tarefa atualmente ativa (com confirmação)
- **Enter**: Confirma adição de nova tarefa (quando na janela de adição)

## Características Técnicas

### Design Patterns Utilizados
- **Repository Pattern**: Separação da lógica de persistência
- **Model-View Pattern**: Separação entre dados e interface

### Boas Práticas Implementadas
- Código comentado e documentado
- Tratamento de erros com try/catch
- Validação de entrada de dados
- Uso de eventos do Windows Forms
- Gerenciamento adequado de recursos (Dispose)

## Observações Importantes

1. **Persistência de Estado**: Ao fechar o aplicativo, o tempo da tarefa ativa é salvo, mas ela não continua contando automaticamente na próxima execução
2. **Tempo Acumulativo**: O tempo total de cada tarefa é sempre acumulado, nunca é zerado automaticamente
3. **Ciclo Automático**: O reset de estados só ocorre quando a última tarefa é clicada e depois outra tarefa é selecionada
4. **Minimização**: A janela minimiza automaticamente 2 segundos após clicar em qualquer tarefa

## Solução de Problemas

### O arquivo INI não está sendo criado
- Verifique se o aplicativo tem permissão de escrita na pasta onde está sendo executado
- Execute o aplicativo como administrador se necessário

### As tarefas não estão sendo salvas
- Certifique-se de que o arquivo `tarefas.ini` não está marcado como somente leitura
- Verifique se há espaço disponível no disco

### O timer não está funcionando
- Certifique-se de que o `timerGlobal` está iniciado no evento `MainForm_Load`
- Verifique se há alguma tarefa ativa

## Licença

Este projeto foi desenvolvido como exemplo educacional e pode ser utilizado livremente.

## Autor

Desenvolvido seguindo as especificações técnicas fornecidas para aplicativo de controle de tarefas em C# WinForms.

---

**Versão**: 1.0.0  
**Data**: Dezembro 2025  
**Framework**: .NET Framework 4.7.2
