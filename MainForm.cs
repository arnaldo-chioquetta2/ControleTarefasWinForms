using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
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
        private const string TituloBase = "Controle de Tarefas";

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
        private Font _fonteOriginalBotoes;

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

            // Guarda a fonte original dos botões de tarefa
            foreach (Control control in flpTasks.Controls)
            {
                var btn = control as Button;
                if (btn != null && btn.Tag is TaskModel)
                {
                    _fonteOriginalBotoes = btn.Font;
                    break;
                }
            }

            AtualizarTituloFormulario();

            // Se já abrir maximizado, aplica a escala imediatamente
            MainForm_Resize(this, EventArgs.Empty);

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

            AjustarAlturaJanelaAoConteudo();
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

            // 🔹 Clique normal (seleção + lógica)
            button.Click += (s, e) =>
            {
                BotaoTarefa_Click(s, e);
            };

            // 🔥 EVENTOS DE DRAG (ESTAVAM FALTANDO)
            button.MouseDown += BotaoTarefa_MouseDown;
            button.MouseMove += BotaoTarefa_MouseMove;

            // 🔹 Menu / desabilitar / excluir
            button.MouseUp += BotaoTarefa_MouseUp;

            // 🔹 Controle de mouse fora
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
            if (sender is Button btn && btn.Tag is TaskModel task)
            {
                _dragArmed = true;
                _dragStartPoint = e.Location;

                Debug.WriteLine($"[Drag] MouseDown → Task Id={task.Id}, Name={task.Name}");
            }
        }

        private void BotaoTarefa_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragArmed)
                return;

            var btn = sender as Button;
            if (btn == null)
                return;

            var task = btn.Tag as TaskModel;
            if (task == null)
                return;

            if (Math.Abs(e.X - _dragStartPoint.X) < SystemInformation.DragSize.Width &&
                Math.Abs(e.Y - _dragStartPoint.Y) < SystemInformation.DragSize.Height)
                return;

            Debug.WriteLine($"[Drag] DoDragDrop START → Task Id={task.Id}, Name={task.Name}");

            _dragArmed = false;
            btn.DoDragDrop(btn, DragDropEffects.Move);
        }

        #endregion

        private void BotaoTarefa_MouseUp(object sender, MouseEventArgs e)
        {
            _dragArmed = false;

            if (e.Button != MouseButtons.Right)
                return;

            var button = sender as Button;
            if (button == null)
                return;

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

            // ─────────────────────────────
            // DESABILITAR / REABILITAR
            // ─────────────────────────────
            if (result == DialogResult.Yes)
            {
                bool estavaDesabilitada = task.State == TaskState.Desabilitada;

                // Se a tarefa estava ativa, finalize corretamente
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

                if (estavaDesabilitada)
                {
                    // 🔼 REABILITAR
                    task.State = TaskState.Pendente;

                    // Move para cima do separador, como última ativa
                    MoverTarefaReabilitadaParaUltimaAtiva(task);
                }
                else
                {
                    // 🔽 DESABILITAR
                    task.State = TaskState.Desabilitada;

                    // Garante que vá para o bloco das desabilitadas
                    ReordenarTarefasDesabilitadasParaBaixo();
                }

                // Recria UI (ordem + agrupador)
                RecriarListaVisual();

                BeginInvoke((Action)(() =>
                {
                    AjustarAlturaJanelaAoConteudo();
                }));

                _repository.SalvarTarefas(_tasks);
                return;
            }

            // ─────────────────────────────
            // EXCLUIR
            // ─────────────────────────────
            if (result == DialogResult.No)
            {
                ApagarTarefa(task);
                return;
            }

            // Cancelar → não faz nada
        }

        private void MoverTarefaReabilitadaParaUltimaAtiva(TaskModel task)
        {
            if (task == null)
                return;

            // Remove da posição atual
            _tasks.Remove(task);

            // Encontra a primeira desabilitada
            int indexPrimeiraDesabilitada =
                _tasks.FindIndex(t => t.State == TaskState.Desabilitada);

            if (indexPrimeiraDesabilitada == -1)
            {
                // Não há desabilitadas → adiciona no final
                _tasks.Add(task);
            }
            else
            {
                // Insere imediatamente ANTES do bloco de desabilitadas
                _tasks.Insert(indexPrimeiraDesabilitada, task);
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
                //var button = CriarBotaoTarefa(task);
                //flpTasks.Controls.Add(button);
                var item = CriarItemTarefa(task);
                flpTasks.Controls.Add(item);
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
                        //var button = CriarBotaoTarefa(task);
                        //flpTasks.Controls.Add(button);
                        var item = CriarItemTarefa(task);
                        flpTasks.Controls.Add(item);
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
                _desabilitadosExpandidos = !_desabilitadosExpandidos;

                RecriarListaVisual();

                BeginInvoke((Action)(() =>
                {
                    AjustarAlturaJanelaAoConteudo();
                }));
            };  

            //btn.Click += (s, e) =>
            //{
            //    bool expandindo = !_desabilitadosExpandidos;

            //    _desabilitadosExpandidos = expandindo;
            //    RecriarListaVisual();

            //    // 🔥 Só ajusta altura ao EXPANDIR
            //    if (expandindo)
            //    {
            //        BeginInvoke((Action)(() =>
            //        {
            //            AjustarAlturaJanelaAoConteudo();
            //        }));
            //    }
            //};

            return btn;
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

            BeginInvoke((Action)(() =>
            {
                AjustarAlturaJanelaAoConteudo();
            }));

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

            }

            var texto = $"{task.Name} - {task.FormattedTime}";

            if (!string.IsNullOrWhiteSpace(task.Note))
            {
                texto += $" | {CortarTexto(task.Note, 60)}";
            }

            button.Text = texto;

        }

        private void BotaoTarefa_Click(object sender, EventArgs e)
        {
            _dragArmed = false;
            var clickedTask = ObterTarefaClicada(sender);
            if (clickedTask == null)
                return;
            _tarefaClicadaRecentemente = true;
            if (TratarTarefaDesabilitada(clickedTask))
                return;
            _botaoSelecionado = sender as Button;
            if (TratarCliqueNaTarefaAtiva(clickedTask))
                return;
            if (TratarCliqueNaTarefaPausada(clickedTask))
                return;
            TratarFluxoNormal(clickedTask);
        }

        private TaskModel ObterTarefaClicada(object sender)
        {
            var button = sender as Button;
            var task = button?.Tag as TaskModel;
            if (task == null)
                Debug.WriteLine("[Click] clickedTask é null");
            else
                Debug.WriteLine($"[Click] DISPAROU → Id={task.Id}, Name={task.Name}, State={task.State}");
            return task;
        }

        private bool TratarTarefaDesabilitada(TaskModel clickedTask)
        {
            if (clickedTask.State != TaskState.Desabilitada)
                return false;
            clickedTask.State = TaskState.Pendente;
            MoverTarefaReabilitadaParaUltimaAtiva(clickedTask);
            RecriarListaVisual();
            _repository.SalvarTarefas(_tasks);
            return true;
        }

        private bool TratarCliqueNaTarefaAtiva(TaskModel clickedTask)
        {
            if (_activeTask == null || _activeTask.Id != clickedTask.Id)
                return false;
            if (_activeTask.LastStartTime.HasValue)
                AcumularTempo(_activeTask);
            _activeTask.State = TaskState.Pausada;
            _activeTask.LastStartTime = null;
            _activeTask = null;
            AtualizarInterfaceTarefas();
            _repository.SalvarTarefas(_tasks);
            return true;
        }

        private bool TratarCliqueNaTarefaPausada(TaskModel clickedTask)
        {
            if (clickedTask.State != TaskState.Pausada)
                return false;
            FinalizarTarefaAtivaSeNecessario();
            _activeTask = clickedTask;
            _activeTask.State = TaskState.Ativa;
            _activeTask.LastStartTime = DateTime.Now;
            AtualizarInterfaceTarefas();
            _repository.SalvarTarefas(_tasks);
            return true;
        }

        private void TratarFluxoNormal(TaskModel clickedTask)
        {
            FinalizarTarefaAtivaSeNecessario();

            if (DeveResetarCiclo(clickedTask))
            {
                foreach (var task in _tasks)
                {
                    // 🔒 Mantém tarefas pausadas e desabilitadas intactas
                    if (task.State != TaskState.Desabilitada &&
                        task.State != TaskState.Pausada)
                    {
                        task.State = TaskState.Pendente;
                    }
                }
            }
            else if (_activeTask != null)
            {
                _activeTask.State = TaskState.JaClicada;
            }

            _activeTask = clickedTask;
            _activeTask.State = TaskState.Ativa;
            _activeTask.LastStartTime = DateTime.Now;

            AtualizarInterfaceTarefas();
            _repository.SalvarTarefas(_tasks);
        }

        private void AtualizarTituloFormulario()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";
            Text = $"{TituloBase} v{version}";
        }

        private void FinalizarTarefaAtivaSeNecessario()
        {
            if (_activeTask == null)
                return;
            if (_activeTask.LastStartTime.HasValue)
                AcumularTempo(_activeTask);
            _activeTask.LastStartTime = null;
            _activeTask.State = TaskState.JaClicada;
        }

        private void AcumularTempo(TaskModel task)
        {
            var elapsed = DateTime.Now - task.LastStartTime.Value;
            task.TotalTime = task.TotalTime.Add(elapsed);
        }

        private bool DeveResetarCiclo(TaskModel clickedTask)
        {
            if (_activeTask == null)
                return false;
            var tarefasValidas = _tasks.Where(t => t.State != TaskState.Desabilitada).ToList();
            var ultimaTarefaValida = tarefasValidas.LastOrDefault();
            if (ultimaTarefaValida == null)
                return false;
            return _activeTask.Id == ultimaTarefaValida.Id && clickedTask.Id != ultimaTarefaValida.Id;
        }


        /// <summary>
        /// Atualiza todos os botões
        /// </summary>
        private void AtualizarInterfaceTarefas()
        {
            foreach (Control control in flpTasks.Controls)
            {
                // Botão agrupador "Desabilitados"
                if (control is Button botaoAgrupador)
                {
                    continue;
                }

                // Item de tarefa agora é Panel
                if (control is Panel panel && panel.Tag is TaskModel task)
                {
                    foreach (Control child in panel.Controls)
                    {
                        if (child is Button button && button.Tag is TaskModel)
                        {
                            // Botão principal da tarefa
                            if (button.Name == "btnTarefa")
                            {
                                AtualizarBotao(button, task);
                            }

                            // Botão de anotação
                            if (button.Name == "btnNota")
                            {
                                button.Text = string.IsNullOrWhiteSpace(task.Note) ? "N" : "N!";
                                button.BackColor = string.IsNullOrWhiteSpace(task.Note)
                                    ? Color.Gainsboro
                                    : Color.Gold;
                            }
                        }
                    }
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

        private void btnAddTask_Click(object sender, EventArgs e)
        {
            using (var form = new AddTaskForm())
            {
                if (form.ShowDialog() != DialogResult.OK)
                    return;

                var newTask = new TaskModel
                {
                    Id = _nextId++,
                    Name = form.TaskName,
                    TotalTime = TimeSpan.Zero,
                    State = TaskState.Pendente,
                    LastStartTime = null
                };

                // 🔥 INSERE COMO ÚLTIMA TAREFA HABILITADA (ACIMA DO SEPARADOR)
                MoverTarefaReabilitadaParaUltimaAtiva(newTask);

                // 🔄 Recria a UI para respeitar a nova ordem
                RecriarListaVisual();

                // 💾 Persiste no repositório
                _repository.SalvarTarefas(_tasks);

                // 📐 Ajusta altura da janela se necessário
                AjustarAlturaJanelaAoConteudo();
            }
        }

        private void AjustarAlturaJanelaAoConteudo()
        {
            if (flpTasks.Controls.Count == 0)
                return;

            flpTasks.PerformLayout();

            int bottomMax = 0;

            foreach (Control c in flpTasks.Controls)
            {
                if (!c.Visible)
                    continue;

                if (c.Bottom > bottomMax)
                    bottomMax = c.Bottom;
            }

            int alturaConteudo = bottomMax + flpTasks.Padding.Bottom;

            int alturaAreaVisivel = flpTasks.Height;

            int diferenca = alturaConteudo - alturaAreaVisivel;

            int novaAltura = this.Height + diferenca;

            Rectangle areaTrabalho = Screen.FromControl(this).WorkingArea;

            int alturaMaxima = areaTrabalho.Height;

            if (novaAltura > alturaMaxima)
                novaAltura = alturaMaxima;

            if (novaAltura < 200)
                novaAltura = 200;

            this.Height = novaAltura;
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
            bool maximizado = this.WindowState == FormWindowState.Maximized;

            int alturaTarefa = maximizado ? 90 : 60;
            int alturaAgrupador = maximizado ? 45 : 30;
            int larguraTotal = flpTasks.Width - 25;
            int larguraBotaoNota = maximizado ? 60 : 40;

            foreach (Control control in flpTasks.Controls)
            {
                // Botão agrupador "Desabilitados"
                if (control is Button botaoAgrupador && !(botaoAgrupador.Tag is TaskModel))
                {
                    botaoAgrupador.Width = larguraTotal;
                    botaoAgrupador.Height = alturaAgrupador;

                    if (_fonteOriginalBotoes != null)
                    {
                        float tamanhoFonteAgrupador = maximizado
                            ? _fonteOriginalBotoes.Size * 1.6f
                            : 9f;

                        botaoAgrupador.Font = new Font(
                            _fonteOriginalBotoes.FontFamily,
                            tamanhoFonteAgrupador,
                            FontStyle.Bold
                        );
                    }

                    continue;
                }

                // Item de tarefa
                if (control is Panel panel && panel.Tag is TaskModel task)
                {
                    panel.Width = larguraTotal;
                    panel.Height = alturaTarefa;

                    foreach (Control child in panel.Controls)
                    {
                        var button = child as Button;
                        if (button == null)
                            continue;

                        if (button.Name == "btnTarefa")
                        {
                            button.Location = new Point(0, 0);
                            button.Width = panel.Width - larguraBotaoNota;
                            button.Height = panel.Height;

                            if (_fonteOriginalBotoes != null)
                            {
                                float tamanhoFonte = maximizado
                                    ? _fonteOriginalBotoes.Size * 2.2f
                                    : _fonteOriginalBotoes.Size;

                                button.Font = new Font(
                                    _fonteOriginalBotoes.FontFamily,
                                    tamanhoFonte,
                                    _fonteOriginalBotoes.Style
                                );
                            }
                        }
                        else if (button.Name == "btnNota")
                        {
                            button.Location = new Point(panel.Width - larguraBotaoNota, 0);
                            button.Width = larguraBotaoNota;
                            button.Height = panel.Height;

                            if (_fonteOriginalBotoes != null)
                            {
                                float tamanhoFonteNota = maximizado
                                    ? _fonteOriginalBotoes.Size * 1.6f
                                    : 9f;

                                button.Font = new Font(
                                    _fonteOriginalBotoes.FontFamily,
                                    tamanhoFonteNota,
                                    FontStyle.Bold
                                );
                            }
                        }
                    }
                }
            }
        }

        private TaskModel ObterTaskDoControl(Control control)
        {
            if (control == null)
                return null;

            var task = control.Tag as TaskModel;
            if (task != null)
                return task;

            if (control.Parent != null)
            {
                var parentTask = control.Parent.Tag as TaskModel;
                if (parentTask != null)
                    return parentTask;
            }

            return null;
        }

        private bool EhItemDeTarefa(Control control)
        {
            return ObterTaskDoControl(control) != null;
        }

        #region DragInDrop

        private void FlpTasks_DragOver(object sender, DragEventArgs e)
        {
            Debug.WriteLine("[DragOver] chamado");

            if (!e.Data.GetDataPresent(typeof(Button)))
            {
                Debug.WriteLine("[DragOver] Data não é Button");
                e.Effect = DragDropEffects.None;
                return;
            }

            var draggedButton = e.Data.GetData(typeof(Button)) as Button;
            Debug.WriteLine($"[DragOver] DraggedButton ok? {draggedButton != null}");

            if (!EhBotaoDeTarefa(draggedButton))
            {
                Debug.WriteLine("[DragOver] DraggedButton NÃO é tarefa");
                e.Effect = DragDropEffects.None;
                return;
            }

            Point point = flpTasks.PointToClient(new Point(e.X, e.Y));
            Control target = flpTasks.GetChildAtPoint(point);

            Debug.WriteLine($"[DragOver] Target = {target?.GetType().Name ?? "null"}");

            if (!EhBotaoDeTarefa(target))
            {
                Debug.WriteLine("[DragOver] Target NÃO é tarefa");
                e.Effect = DragDropEffects.None;
                return;
            }

            Debug.WriteLine("[DragOver] OK → Move");
            e.Effect = DragDropEffects.Move;
        }


        private bool EhBotaoDeTarefa(Control control)
        {
            return control is Button btn && btn.Tag is TaskModel;
        }

        private void FlpTasks_DragDrop(object sender, DragEventArgs e)
        {
            Debug.WriteLine("[DragDrop] chamado");

            var draggedButton = e.Data.GetData(typeof(Button)) as Button;
            Debug.WriteLine($"[DragDrop] DraggedButton null? {draggedButton == null}");

            if (!EhBotaoDeTarefa(draggedButton))
            {
                Debug.WriteLine("[DragDrop] DraggedButton NÃO é tarefa");
                return;
            }

            var draggedTask = draggedButton.Tag as TaskModel;
            Debug.WriteLine($"[DragDrop] DraggedTask Id={draggedTask?.Id}");

            Point point = flpTasks.PointToClient(new Point(e.X, e.Y));
            Control target = flpTasks.GetChildAtPoint(point);

            Debug.WriteLine($"[DragDrop] Target = {target?.GetType().Name ?? "null"}");

            if (!EhBotaoDeTarefa(target))
            {
                Debug.WriteLine("[DragDrop] Target NÃO é tarefa");
                return;
            }

            var targetTask = (target as Button)?.Tag as TaskModel;
            Debug.WriteLine($"[DragDrop] TargetTask Id={targetTask?.Id}");

            bool draggedIsDisabled = draggedTask.State == TaskState.Desabilitada;
            bool targetIsDisabled = targetTask.State == TaskState.Desabilitada;

            Debug.WriteLine($"[DragDrop] draggedIsDisabled={draggedIsDisabled}, targetIsDisabled={targetIsDisabled}");

            if (draggedIsDisabled != targetIsDisabled)
            {
                Debug.WriteLine("[DragDrop] BLOQUEADO por bloco diferente");
                return;
            }

            int oldIndex = _tasks.IndexOf(draggedTask);
            int newIndex = _tasks.IndexOf(targetTask);

            Debug.WriteLine($"[DragDrop] oldIndex={oldIndex}, newIndex={newIndex}");

            if (oldIndex < 0 || newIndex < 0 || oldIndex == newIndex)
            {
                Debug.WriteLine("[DragDrop] Índices inválidos");
                return;
            }

            _tasks.RemoveAt(oldIndex);
            _tasks.Insert(newIndex, draggedTask);

            Debug.WriteLine("[DragDrop] Reordenado com sucesso");

            RecriarListaVisual();
            _repository.SalvarTarefas(_tasks);
        }

        #endregion

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

        #region Notas das tarefas

        private string CortarTexto(string texto, int max)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return "";

            texto = texto.Replace(Environment.NewLine, " ");

            if (texto.Length <= max)
                return texto;

            return texto.Substring(0, max) + "...";
        }

        private Panel CriarItemTarefa(TaskModel task)
        {
            var panel = new Panel
            {
                Tag = task,
                Width = flpTasks.Width - 25,
                Height = 60,
                Margin = new Padding(3)
            };

            var btnTarefa = CriarBotaoTarefa(task);
            btnTarefa.Name = "btnTarefa";
            btnTarefa.Width = panel.Width - 45;
            btnTarefa.Height = panel.Height;
            btnTarefa.Location = new Point(0, 0);

            var btnNota = new Button
            {
                Name = "btnNota",
                Tag = task,
                Width = 40,
                Height = panel.Height,
                Location = new Point(panel.Width - 40, 0),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            AtualizarBotaoNota(btnNota, task);

            btnNota.Click += (s, e) =>
            {
                AbrirAnotacao(task);
            };

            panel.Controls.Add(btnTarefa);
            panel.Controls.Add(btnNota);

            return panel;
        }

        private void AbrirAnotacao(TaskModel task)
        {
            if (task == null)
                return;

            using (var form = new TaskNoteForm(task.Name, task.Note))
            {
                if (form.ShowDialog(this) != DialogResult.OK)
                    return;

                task.Note = form.NoteText ?? "";

                RecriarListaVisual();
                _repository.SalvarTarefas(_tasks);
            }
        }

        private void AtualizarBotaoNota(Button btnNota, TaskModel task)
        {
            bool temNota = !string.IsNullOrWhiteSpace(task.Note);

            btnNota.Text = temNota ? "N!" : "N";
            btnNota.BackColor = temNota ? Color.Gold : Color.Gainsboro;
            btnNota.ForeColor = temNota ? Color.Black : Color.DimGray;
            btnNota.FlatStyle = FlatStyle.Flat;
            btnNota.FlatAppearance.BorderSize = 0;
            btnNota.Cursor = Cursors.Hand;
        }



        #endregion

    }
}
