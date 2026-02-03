using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ControleTarefasWinForms.Models;
using ControleTarefasWinForms.Services;

namespace ControleTarefasWinForms
{
    /// <summary>
    /// Formulário principal do aplicativo de controle de tarefas
    /// </summary>
    public partial class MainForm : Form
    {
        // Lista de tarefas em memória
        private List<TaskModel> _tasks;
        private Button _dragButton;
        private Point _dragStartPoint;
        private bool _dragArmed;

        // Tarefa atualmente ativa (contando tempo)
        private TaskModel _activeTask;

        // Repositório para persistência
        private readonly TaskRepository _repository;

        // Próximo ID disponível para nova tarefa
        private int _nextId = 1;

        // Flag para controlar se houve clique recente em tarefa
        private bool _tarefaClicadaRecentemente = false;

        private bool _desabilitadosExpandidos = false;

        private Button _botaoSelecionado;
        private TextBox _textBoxEdicao;
        private string _nomeOriginal;
        private bool _finalizandoEdicao;

        #region Inicialização

        public MainForm()
        {
            InitializeComponent();
            _repository = new TaskRepository();
        }

        /// <summary>
        /// Evento de carregamento do formulário
        /// </summary>
        private void MainForm_Load(object sender, EventArgs e)
        {
            flpTasks.AllowDrop = true;
            flpTasks.DragOver += FlpTasks_DragOver;
            flpTasks.DragDrop += FlpTasks_DragDrop;

            // Adiciona evento MouseLeave ao formulário e controles
            this.MouseLeave += MainForm_MouseLeave;
            flpTasks.MouseLeave += MainForm_MouseLeave;
            btnAddTask.MouseLeave += MainForm_MouseLeave;

            _tasks = _repository.CarregarTarefas();

            // Define o próximo ID
            if (_tasks.Count > 0)
            {
                _nextId = _tasks.Max(t => t.Id) + 1;
            }

            foreach (var task in _tasks)
            {
                Console.WriteLine(
                        $"[MainForm_Load - ANTES] Id={task.Id} | Nome={task.Name} | State={task.State}"
                    );
                task.LastStartTime = null;

                if (task.State == TaskState.Ativa)
                    task.State = TaskState.Pausada;
                Console.WriteLine(
                        $"[MainForm_Load - DEPOIS] Id={task.Id} | Nome={task.Name} | State={task.State}"
                    );
            }

            RecriarListaVisual();

            // Cria os botões e atualiza a interface
            // CriarBotoesTarefas();

            // Define o título com versão
            this.Text = "Controle de Tarefas";

            // Inicia o timer global
            timerGlobal.Start();
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            BeginInvoke((Action)(() =>
            {
                AjustarAlturaInicial();
            }));
        }

        private void AjustarAlturaInicial()
        {
            if (flpTasks.Controls.Count == 0)
                return;

            AjustarAlturaJanelaSeNecessario();
        }

        private void CriarBotoesTarefas()
        {
            flpTasks.Controls.Clear();

            foreach (var task in _tasks)
            {
                var button = CriarBotaoTarefa(task);
                button.MouseDown += BotaoTarefa_MouseDown;
                button.MouseMove += BotaoTarefa_MouseMove;
                flpTasks.Controls.Add(button);
            }
        }

        private Button CriarBotaoTarefa(TaskModel task)
        {
            var button = new Button
            {
                Tag = task,
                Width = flpTasks.Width - 25,
                Height = 60,
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };

            AtualizarBotao(button, task);

            // 🔹 Clique: seleciona o botão + executa lógica normal
            button.Click += (s, e) =>
            {
                _botaoSelecionado = button;   // marca como selecionado
                BotaoTarefa_Click(s, e);      // mantém comportamento existente
            };

            // 🔹 Botão direito (menu / desabilitar / excluir)
            button.MouseUp += BotaoTarefa_MouseUp;

            // 🔹 Mouse saiu da área (minimização automática)
            button.MouseLeave += MainForm_MouseLeave;

            return button;
        }

        #endregion

        #region Evento de Mouse

        private void MainForm_MouseLeave(object sender, EventArgs e)
        {
            // Verifica se o mouse realmente saiu da área do formulário
            if (!this.ClientRectangle.Contains(this.PointToClient(Cursor.Position)))
            {
                // Só minimiza se alguma tarefa foi clicada recentemente
                if (_tarefaClicadaRecentemente)
                {
                    timerMinimize.Stop();
                    timerMinimize.Start();
                }
            }
        }

        private void BotaoTarefa_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            _dragButton = sender as Button;
            _dragStartPoint = e.Location;
            _dragArmed = true;
        }

        private void BotaoTarefa_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragArmed || _dragButton == null)
                return;

            Rectangle dragRect = new Rectangle(
                _dragStartPoint.X - SystemInformation.DragSize.Width / 2,
                _dragStartPoint.Y - SystemInformation.DragSize.Height / 2,
                SystemInformation.DragSize.Width,
                SystemInformation.DragSize.Height
            );

            if (!dragRect.Contains(e.Location))
            {
                _dragArmed = false;
                DoDragDrop(_dragButton, DragDropEffects.Move);
            }
        }

        #endregion

        private void BotaoTarefa_MouseUp(object sender, MouseEventArgs e)
        {
            _dragArmed = false;

            if (e.Button != MouseButtons.Right)
                return;

            var button = sender as Button;
            var task = button.Tag as TaskModel;

            if (task == null)
                return;

            var result = MessageBox.Show(
                $"O que deseja fazer com a tarefa \"{task.Name}\"?\n\n" +
                "Sim = Desabilitar / Reabilitar\n" +
                "Não = Excluir\n" +
                "Cancelar = Nada",
                "Opções da tarefa",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                if (_activeTask != null && _activeTask.Id == task.Id)
                {
                    if (_activeTask.LastStartTime.HasValue)
                    {
                        var elapsed = DateTime.Now - _activeTask.LastStartTime.Value;
                        _activeTask.TotalTime = _activeTask.TotalTime.Add(elapsed);
                    }

                    _activeTask.LastStartTime = null;
                    _activeTask = null;
                }
                task.State = TaskState.Desabilitada;

                // garante que desabilitadas vão para baixo
                ReordenarTarefasDesabilitadasParaBaixo();

                // recria UI na nova ordem
                RecriarListaVisual();

                _repository.SalvarTarefas(_tasks);
                return;
            }
            else if (result == DialogResult.No)
            {
                ApagarTarefa(task);
            }
        }

        private void ReordenarTarefasDesabilitadasParaBaixo()
        {
            var ativas = _tasks
                .Where(t => t.State != TaskState.Desabilitada)
                .ToList();

            var desabilitadas = _tasks
                .Where(t => t.State == TaskState.Desabilitada)
                .ToList();

            _tasks.Clear();
            _tasks.AddRange(ativas);
            _tasks.AddRange(desabilitadas);
        }

        private void RecriarListaVisual()
        {
            flpTasks.SuspendLayout();
            flpTasks.Controls.Clear();

            var tarefasAtivas = _tasks
                .Where(t => t.State != TaskState.Desabilitada)
                .ToList();

            var tarefasDesabilitadas = _tasks
                .Where(t => t.State == TaskState.Desabilitada)
                .ToList();

            // 1. Adiciona tarefas normais
            foreach (var task in tarefasAtivas)
            {
                var button = CriarBotaoTarefa(task);
                flpTasks.Controls.Add(button);
            }

            // 2. Se houver desabilitadas, adiciona o botão agrupador
            if (tarefasDesabilitadas.Any())
            {
                var btnGrupo = CriarBotaoAgrupadorDesabilitados();
                flpTasks.Controls.Add(btnGrupo);

                // 3. Se estiver expandido, mostra as desabilitadas
                if (_desabilitadosExpandidos)
                {
                    foreach (var task in tarefasDesabilitadas)
                    {
                        var button = CriarBotaoTarefa(task);
                        flpTasks.Controls.Add(button);
                    }
                }
            }

            flpTasks.ResumeLayout();
        }

        private Button CriarBotaoAgrupadorDesabilitados()
        {
            var btn = new Button
            {
                Height = 30, // metade dos botões normais (60)
                Width = flpTasks.Width - 25,
                Text = _desabilitadosExpandidos
                    ? "Encolher Desabilitados"
                    : "Desabilitados",
                BackColor = Color.Gainsboro,
                ForeColor = Color.DimGray,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btn.FlatAppearance.BorderSize = 0;

            btn.Click += (s, e) =>
            {
                bool expandindo = !_desabilitadosExpandidos;

                _desabilitadosExpandidos = expandindo;
                RecriarListaVisual();

                // 🔥 Só ajusta altura ao EXPANDIR
                if (expandindo)
                {
                    BeginInvoke((Action)(() =>
                    {
                        AjustarAlturaJanelaSeNecessario();
                    }));
                }
            };

            return btn;
        }

        private void MoverTarefaParaBlocoDesabilitadas(TaskModel task)
        {
            if (task == null)
                return;

            // Remove da posição atual
            _tasks.Remove(task);

            // Procura a primeira desabilitada existente
            int indexPrimeiraDesabilitada =
                _tasks.FindIndex(t => t.State == TaskState.Desabilitada);

            if (indexPrimeiraDesabilitada == -1)
            {
                // Nenhuma desabilitada ainda → vai para o final
                _tasks.Add(task);
            }
            else
            {
                // Entra como a PRIMEIRA das desabilitadas
                _tasks.Insert(indexPrimeiraDesabilitada, task);
            }
        }

        private void ApagarTarefa(TaskModel task)
        {
            if (task == null) return;

            if (_activeTask != null && _activeTask.Id == task.Id)
            {
                _activeTask = null;
            }

            _tasks.Remove(task);

            Button buttonToRemove = null;

            foreach (Control control in flpTasks.Controls)
            {
                if (control is Button button &&
                    button.Tag is TaskModel t &&
                    t.Id == task.Id)
                {
                    buttonToRemove = button;
                    break;
                }
            }

            if (buttonToRemove != null)
            {
                flpTasks.Controls.Remove(buttonToRemove);
                buttonToRemove.Dispose();
            }

            _repository.SalvarTarefas(_tasks);
        }

        /// <summary>
        /// Atualiza a aparência de um botão conforme o estado da tarefa
        /// </summary>
        private void AtualizarBotao(Button button, TaskModel task)
        {
            switch (task.State)
            {
                case TaskState.Pendente:
                    // "Tarefa a ser feita" -> laranja forte (chama atenção)
                    button.BackColor = Color.OrangeRed;          // forte
                    button.ForeColor = Color.White;
                    break;

                case TaskState.Ativa:
                    // Mantém como estava
                    button.BackColor = Color.LightGreen;
                    button.ForeColor = Color.Black;
                    break;

                case TaskState.JaClicada:
                    // "Já feita nessa rodada" -> laranja mais suave
                    button.BackColor = Color.PeachPuff;          // laranja claro
                    button.ForeColor = Color.Black;
                    break;

                case TaskState.Pausada:
                    // Pausada -> amarelo com leve ajuste de tom
                    button.BackColor = Color.Goldenrod;          // amarelo mais “quente”/escuro que Khaki
                    button.ForeColor = Color.Black;
                    break;

                case TaskState.Desabilitada:
                    button.BackColor = Color.Gainsboro;
                    button.ForeColor = Color.DimGray;

                    // ❌ Remove qualquer efeito de hover
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderSize = 0;
                    button.FlatAppearance.MouseOverBackColor = button.BackColor;
                    button.FlatAppearance.MouseDownBackColor = button.BackColor;

                    // ❌ Cursor neutro
                    button.Cursor = Cursors.Default;

                    break;

                    //case TaskState.Desabilitada:
                    //    // Desabilitada -> cinza (menos chamativa)
                    //    button.BackColor = Color.Gainsboro;
                    //    button.ForeColor = Color.DimGray;
                    //    break;
            }

            button.Text = $"{task.Name} - {task.FormattedTime}";
        }
        

        private void BotaoTarefa_Click(object sender, EventArgs e)
        {
            var button = sender as Button;
            var clickedTask = button?.Tag as TaskModel;

            if (clickedTask == null)
                return;

            // Marca que uma tarefa foi clicada
            _tarefaClicadaRecentemente = true;

            // REGRA 1: Clicar novamente na tarefa ATIVA → pausa
            if (_activeTask != null && _activeTask.Id == clickedTask.Id)
            {
                if (_activeTask.LastStartTime.HasValue)
                {
                    var elapsed = DateTime.Now - _activeTask.LastStartTime.Value;
                    _activeTask.TotalTime = _activeTask.TotalTime.Add(elapsed);
                }

                _activeTask.State = TaskState.Pausada;
                _activeTask.LastStartTime = null;
                _activeTask = null;

                AtualizarInterfaceTarefas();
                _repository.SalvarTarefas(_tasks);
                return;
            }

            // REGRA 2: Clicar em uma tarefa PAUSADA → volta a ser ATIVA
            if (clickedTask.State == TaskState.Pausada)
            {
                if (_activeTask != null && _activeTask.LastStartTime.HasValue)
                {
                    var elapsed = DateTime.Now - _activeTask.LastStartTime.Value;
                    _activeTask.TotalTime = _activeTask.TotalTime.Add(elapsed);
                    _activeTask.LastStartTime = null;
                    _activeTask.State = TaskState.JaClicada;
                }

                _activeTask = clickedTask;
                _activeTask.State = TaskState.Ativa;
                _activeTask.LastStartTime = DateTime.Now;

                AtualizarInterfaceTarefas();
                _repository.SalvarTarefas(_tasks);
                return;
            }

            // LÓGICA NORMAL: Clique em OUTRA tarefa

            // 1. Finaliza contagem da tarefa ativa (se houver)
            if (_activeTask != null && _activeTask.LastStartTime.HasValue)
            {
                var elapsed = DateTime.Now - _activeTask.LastStartTime.Value;
                _activeTask.TotalTime = _activeTask.TotalTime.Add(elapsed);
                _activeTask.LastStartTime = null;
            }

            // 2. Se existir alguma tarefa PAUSADA (diferente da clicada), ela vira "JáClicada"
            var tarefaPausada = _tasks
                .FirstOrDefault(t => t.State == TaskState.Pausada && t.Id != clickedTask.Id);

            if (tarefaPausada != null)
            {
                tarefaPausada.State = TaskState.JaClicada;
            }

            // 3. Obter lista de tarefas válidas (ignora desabilitadas)
            var tarefasValidas = _tasks
                .Where(t => t.State != TaskState.Desabilitada)
                .ToList();

            // 4. Verificar se precisa resetar o ciclo
            bool resetarCiclo = false;

            if (_activeTask != null)
            {
                var ultimaTarefaValida = tarefasValidas.LastOrDefault();

                if (ultimaTarefaValida != null &&
                    _activeTask.Id == ultimaTarefaValida.Id &&
                    clickedTask.Id != ultimaTarefaValida.Id)
                {
                    resetarCiclo = true;
                }
            }

            if (resetarCiclo)
            {
                foreach (var task in _tasks)
                {
                    if (task.State != TaskState.Desabilitada)
                        task.State = TaskState.Pendente;
                }
            }
            else
            {
                if (_activeTask != null)
                    _activeTask.State = TaskState.JaClicada;
            }

            // 5. Ativa a nova tarefa
            _activeTask = clickedTask;
            _activeTask.State = TaskState.Ativa;
            _activeTask.LastStartTime = DateTime.Now;

            // 6. Atualiza UI e salva
            AtualizarInterfaceTarefas();
            _repository.SalvarTarefas(_tasks);
        }

        /// <summary>
        /// Atualiza todos os botões
        /// </summary>
        private void AtualizarInterfaceTarefas()
        {
            foreach (Control control in flpTasks.Controls)
            {
                if (control is Button button && button.Tag is TaskModel task)
                {
                    AtualizarBotao(button, task);
                }
            }
        }

        /// <summary>
        /// Timer global que atualiza o tempo da tarefa ativa a cada segundo
        /// </summary>
        private void timerGlobal_Tick(object sender, EventArgs e)
        {
            if (_activeTask != null && _activeTask.LastStartTime.HasValue)
            {
                _activeTask.TotalTime = _activeTask.TotalTime.Add(TimeSpan.FromSeconds(1));
                _activeTask.LastStartTime = DateTime.Now;
                AtualizarInterfaceTarefas();
            }
        }

        /// <summary>
        /// Timer de minimização (dispara após o mouse sair da janela)
        /// </summary>
        private void timerMinimize_Tick(object sender, EventArgs e)
        {
            timerMinimize.Stop();
            this.WindowState = FormWindowState.Minimized;

            // Reseta a flag após minimizar
            _tarefaClicadaRecentemente = false;
        }

        /// <summary>
        /// Evento do botão Adicionar Tarefa
        /// </summary>
        private void btnAddTask_Click(object sender, EventArgs e)
        {
            using (var form = new AddTaskForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    var newTask = new TaskModel
                    {
                        Id = _nextId++,
                        Name = form.TaskName,
                        TotalTime = TimeSpan.Zero,
                        State = TaskState.Pendente,
                        LastStartTime = null
                    };

                    _tasks.Add(newTask);

                    var button = CriarBotaoTarefa(newTask);
                    flpTasks.Controls.Add(button);

                    _repository.SalvarTarefas(_tasks);

                    AjustarAlturaJanelaSeNecessario();
                }
            }
        }


        private void AjustarAlturaJanelaSeNecessario()
    {
        Debug.WriteLine("==== AjustarAlturaJanelaSeNecessario (REAL) ====");

        if (flpTasks.Controls.Count == 0)
        {
            Debug.WriteLine("Nenhuma tarefa no painel.");
            return;
        }

        flpTasks.PerformLayout();

        Control ultimo = flpTasks.Controls[flpTasks.Controls.Count - 1];

        int bottomUltimoControle = ultimo.Bottom + flpTasks.Padding.Bottom;
        int alturaVisivel = flpTasks.ClientSize.Height;

        Debug.WriteLine($"Bottom do último controle: {ultimo.Bottom}");
        Debug.WriteLine($"Altura visível do flpTasks: {alturaVisivel}");

        if (bottomUltimoControle <= alturaVisivel)
        {
            Debug.WriteLine("❌ Último controle ainda está visível. Não vai redimensionar.");
            return;
        }

        int diferenca = bottomUltimoControle - alturaVisivel;
        Debug.WriteLine($"Diferença necessária: {diferenca}");

        Rectangle areaTrabalho = Screen.FromControl(this).WorkingArea;

        int alturaMaximaForm = areaTrabalho.Height;
        int alturaAtual = this.Height;
        int novaAltura = alturaAtual + diferenca;

        Debug.WriteLine($"Altura atual do Form: {alturaAtual}");
        Debug.WriteLine($"Altura desejada do Form: {novaAltura}");
        Debug.WriteLine($"Altura máxima permitida: {alturaMaximaForm}");

        int alturaFinal = Math.Min(novaAltura, alturaMaximaForm);

        this.Height = alturaFinal;

        Debug.WriteLine($"Altura final aplicada: {alturaFinal}");
        Debug.WriteLine("=============================================");
    }


    /// <summary>
    /// Evento de fechamento do formulário (salva antes de fechar)
    /// </summary>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_activeTask != null && _activeTask.LastStartTime.HasValue)
            {
                var elapsed = DateTime.Now - _activeTask.LastStartTime.Value;
                _activeTask.TotalTime = _activeTask.TotalTime.Add(elapsed);
                _activeTask.LastStartTime = null;
            }

            _repository.SalvarTarefas(_tasks);
        }

        /// <summary>
        /// Redimensiona os botões quando o form é redimensionado
        /// </summary>
        private void MainForm_Resize(object sender, EventArgs e)
        {
            foreach (Control control in flpTasks.Controls)
            {
                if (control is Button button)
                {
                    button.Width = flpTasks.Width - 25;
                }
            }
        }

        private void FlpTasks_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Button)))
                e.Effect = DragDropEffects.Move;
        }

        private void FlpTasks_DragDrop(object sender, DragEventArgs e)
        {
            var draggedButton = (Button)e.Data.GetData(typeof(Button));

            Point point = flpTasks.PointToClient(new Point(e.X, e.Y));
            Control target = flpTasks.GetChildAtPoint(point);

            if (target == null || target == draggedButton)
                return;

            int oldIndex = flpTasks.Controls.IndexOf(draggedButton);
            int newIndex = flpTasks.Controls.IndexOf(target);

            if (oldIndex == newIndex)
                return;

            flpTasks.Controls.SetChildIndex(draggedButton, newIndex);
            flpTasks.Invalidate();

            var draggedTask = draggedButton.Tag as TaskModel;
            _tasks.Remove(draggedTask);
            _tasks.Insert(newIndex, draggedTask);

            _repository.SalvarTarefas(_tasks);
        }

        #region F2

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F2 && _botaoSelecionado != null)
            {
                IniciarEdicaoTarefa(_botaoSelecionado);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void IniciarEdicaoTarefa(Button button)
        {
            if (button == null || _textBoxEdicao != null)
                return;

            var task = button.Tag as TaskModel;
            if (task == null)
                return;

            // (Opcional) bloquear renomear desabilitadas
            if (task.State == TaskState.Desabilitada)
                return;

            _nomeOriginal = task.Name;

            _textBoxEdicao = new TextBox
            {
                Text = task.Name,
                Font = button.Font,
                Bounds = button.Bounds,
                Parent = flpTasks
            };

            _textBoxEdicao.KeyDown += TextBoxEdicao_KeyDown;
            _textBoxEdicao.LostFocus += TextBoxEdicao_LostFocus;

            _textBoxEdicao.SelectAll();
            _textBoxEdicao.Focus();
        }

        private void TextBoxEdicao_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                FinalizarEdicaoTarefa(confirmar: true);
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                FinalizarEdicaoTarefa(confirmar: false);
            }
        }

        private void TextBoxEdicao_LostFocus(object sender, EventArgs e)
        {
            FinalizarEdicaoTarefa(confirmar: true);
        }

        private void FinalizarEdicaoTarefa(bool confirmar)
        {
            if (_finalizandoEdicao)
                return;

            if (_textBoxEdicao == null || _botaoSelecionado == null)
                return;

            _finalizandoEdicao = true;

            try
            {
                // Captura local e zera o campo logo no início (evita null no meio)
                var tb = _textBoxEdicao;
                _textBoxEdicao = null;

                // Desconecta eventos (evita reentrância por LostFocus/KeyDown)
                tb.KeyDown -= TextBoxEdicao_KeyDown;
                tb.LostFocus -= TextBoxEdicao_LostFocus;

                var task = _botaoSelecionado.Tag as TaskModel;

                string novoNome = tb.Text.Trim();

                // Remove/Dispose do TextBox com segurança
                if (tb.Parent != null)
                    tb.Parent.Controls.Remove(tb);

                tb.Dispose();

                // Se cancelou, ou não tem task, ou nome inválido/igual, só restaura UI e sai
                if (!confirmar || task == null || string.IsNullOrWhiteSpace(novoNome) || novoNome == _nomeOriginal)
                {
                    if (task != null)
                        AtualizarBotao(_botaoSelecionado, task);
                    return;
                }

                var result = MessageBox.Show(
                    $"Deseja renomear a tarefa \"{_nomeOriginal}\" para \"{novoNome}\"?\n\n" +
                    "Sim = Renomeia e zera o tempo\n" +
                    "Não = Renomeia mantendo o tempo\n" +
                    "Cancelar = Não renomeia",
                    "Renomear tarefa",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Cancel)
                {
                    AtualizarBotao(_botaoSelecionado, task);
                    return;
                }

                // Renomeia
                task.Name = novoNome;

                if (result == DialogResult.Yes)
                {
                    bool eraAtiva = (_activeTask != null && _activeTask.Id == task.Id);

                    // Finaliza corretamente a contagem atual
                    if (eraAtiva && task.LastStartTime.HasValue)
                    {
                        var elapsed = DateTime.Now - task.LastStartTime.Value;
                        task.TotalTime = task.TotalTime.Add(elapsed);
                    }

                    // Zera o tempo
                    task.TotalTime = TimeSpan.Zero;

                    if (eraAtiva)
                    {
                        // 🔥 continua ativa e reinicia contagem
                        task.LastStartTime = DateTime.Now;
                        _activeTask = task;
                        task.State = TaskState.Ativa;
                    }
                    else
                    {
                        task.LastStartTime = null;
                    }
                }


                AtualizarBotao(_botaoSelecionado, task);
                _repository.SalvarTarefas(_tasks);
            }
            finally
            {
                _finalizandoEdicao = false;
            }
        }

        #endregion

    }
}