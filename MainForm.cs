using System;
using System.Collections.Generic;
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
        //private bool _isDragging = false;
        //private Button _dragButton = null;
        private Button _dragButton;
        private Point _dragStartPoint;
        private bool _dragArmed;

        // Tarefa atualmente ativa (contando tempo)
        private TaskModel _activeTask;

        // Repositório para persistência
        private readonly TaskRepository _repository;

        // Próximo ID disponível para nova tarefa
        private int _nextId = 1;

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

            _tasks = _repository.CarregarTarefas();

            // Define o próximo ID
            if (_tasks.Count > 0)
            {
                _nextId = _tasks.Max(t => t.Id) + 1;
            }

            foreach (var task in _tasks)
            {
                task.LastStartTime = null;

                if (task.State == TaskState.Ativa)
                    task.State = TaskState.Pausada;
            }

            // Cria os botões e atualiza a interface
            CriarBotoesTarefas();
            AtualizarDataGridView();

            // Inicia o timer global
            timerGlobal.Start();
        }

        /// <summary>
        /// Cria os botões de tarefa no FlowLayoutPanel
        /// </summary>
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


        /// <summary>
        /// Cria um botão individual para uma tarefa
        /// </summary>
        private Button CriarBotaoTarefa(TaskModel task)
        {
            var button = new Button
            {
                Tag = task,
                Width = flpTasks.Width - 25, // Largura do painel menos margem para scroll
                Height = 60,
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };

            // Define cor e texto do botão conforme estado
            AtualizarBotao(button, task);

            // Evento de clique
            button.Click += BotaoTarefa_Click;
            button.MouseUp += BotaoTarefa_MouseUp;

            return button;
        }

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
                // Alternar desabilitado
                if (task.State == TaskState.Desabilitada)
                {
                    task.State = TaskState.Pendente;
                }
                else
                {
                    // Se estava ativa, encerra
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
                }

                AtualizarInterfaceTarefas();
                _repository.SalvarTarefas(_tasks);
            }
            else if (result == DialogResult.No)
            {
                ApagarTarefa(task);
            }
        }


        private void ApagarTarefa(TaskModel task)
        {
            if (task == null) return;

            // Se a tarefa excluída era a ativa, limpa referência
            if (_activeTask != null && _activeTask.Id == task.Id)
            {
                _activeTask = null;
            }

            _tasks.Remove(task);

            // Remover botão correspondente
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

            AtualizarDataGridView();
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
                    button.BackColor = Color.LightGray;
                    button.ForeColor = Color.Black;
                    break;

                case TaskState.Ativa:
                    button.BackColor = Color.LightGreen;
                    button.ForeColor = Color.Black;
                    break;

                case TaskState.JaClicada:
                    button.BackColor = Color.LightBlue;
                    button.ForeColor = Color.Black;
                    break;

                case TaskState.Pausada:
                    // cor de pausa – escolha a que achar melhor
                    button.BackColor = Color.Khaki;      // ou LightYellow, Orange, etc.
                    button.ForeColor = Color.Black;
                    break;
                case TaskState.Desabilitada:
                    button.BackColor = Color.White;
                    button.ForeColor = Color.Black;
                    break;
            }

            button.Text = $"{task.Name} - {task.FormattedTime}";
        }
        

        /// <summary>
        /// Evento de clique em um botão de tarefa
        /// </summary>
        private void BotaoTarefa_Click(object sender, EventArgs e)
        {
            var button = sender as Button;
            var clickedTask = button.Tag as TaskModel;

            if (clickedTask == null) return;

            //
            // >>> NOVA REGRA 1 <<<
            // Se clicar novamente na tarefa ATIVA:
            // - Para contagem
            // - Vira PAUSADA
            // - Nenhuma tarefa fica ativa
            //
            if (_activeTask != null && _activeTask.Id == clickedTask.Id)
            {
                // Finaliza a contagem
                if (_activeTask.LastStartTime.HasValue)
                {
                    var elapsed = DateTime.Now - _activeTask.LastStartTime.Value;
                    _activeTask.TotalTime = _activeTask.TotalTime.Add(elapsed);
                }

                _activeTask.State = TaskState.Pausada;
                _activeTask.LastStartTime = null;

                // Fica sem tarefa ativa
                _activeTask = null;

                AtualizarInterfaceTarefas();
                _repository.SalvarTarefas(_tasks);
                return;
            }

            //
            // >>> NOVA REGRA 2 <<<
            // Se a tarefa clicada estava PAUSADA:
            // Ela volta a ficar ATIVA
            //
            if (clickedTask.State == TaskState.Pausada)
            {
                // Nenhuma outra tarefa ativa
                if (_activeTask != null && _activeTask.LastStartTime.HasValue)
                {
                    var elapsed = DateTime.Now - _activeTask.LastStartTime.Value;
                    _activeTask.TotalTime = _activeTask.TotalTime.Add(elapsed);
                    _activeTask.LastStartTime = null;
                    _activeTask.State = TaskState.JaClicada;
                }

                // Agora esta tarefa volta para ativa
                _activeTask = clickedTask;
                _activeTask.State = TaskState.Ativa;
                _activeTask.LastStartTime = DateTime.Now;

                AtualizarInterfaceTarefas();
                _repository.SalvarTarefas(_tasks);

                // Timer de minimizar
                timerMinimize.Stop();
                timerMinimize.Start();
                return;
            }


            //
            // >>> LÓGICA NORMAL (clique em outra tarefa diferente da ativa) <<<
            //

            // 1. Finaliza contagem da tarefa anteriormente ativa
            if (_activeTask != null && _activeTask.LastStartTime.HasValue)
            {
                var elapsed = DateTime.Now - _activeTask.LastStartTime.Value;
                _activeTask.TotalTime = _activeTask.TotalTime.Add(elapsed);
                _activeTask.LastStartTime = null;
            }

            // 2. Verificar se precisa resetar o ciclo
            bool resetarCiclo = false;

            if (_activeTask != null)
            {
                var ultimaTarefa = _tasks.LastOrDefault();
                if (ultimaTarefa != null &&
                    _activeTask.Id == ultimaTarefa.Id &&
                    clickedTask.Id != ultimaTarefa.Id)
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

            // 3. Define a nova tarefa ativa
            _activeTask = clickedTask;
            _activeTask.State = TaskState.Ativa;
            _activeTask.LastStartTime = DateTime.Now;

            // 4. Atualiza a interface
            AtualizarInterfaceTarefas();

            // 5. Salvar
            _repository.SalvarTarefas(_tasks);

            // 6. Timer de minimizar
            timerMinimize.Stop();
            timerMinimize.Start();
        }        


        /// <summary>
        /// Atualiza todos os botões e o DataGridView
        /// </summary>
        private void AtualizarInterfaceTarefas()
        {
            // Atualiza os botões
            foreach (Control control in flpTasks.Controls)
            {
                if (control is Button button && button.Tag is TaskModel task)
                {
                    AtualizarBotao(button, task);
                }
            }

            // Atualiza o DataGridView
            AtualizarDataGridView();
        }

        /// <summary>
        /// Atualiza o DataGridView com os dados das tarefas
        /// </summary>
        private void AtualizarDataGridView()
        {
            dgvTasks.Rows.Clear();

            foreach (var task in _tasks)
            {
                string status = "";
                switch (task.State)
                {
                    case TaskState.Pendente:
                        status = "Pendente";
                        break;
                    case TaskState.Ativa:
                        status = "Ativa";
                        break;
                    case TaskState.JaClicada:
                        status = "Já clicada";
                        break;
                }

                dgvTasks.Rows.Add(task.Name, task.FormattedTime, status);
            }
        }

        /// <summary>
        /// Timer global que atualiza o tempo da tarefa ativa a cada segundo
        /// </summary>
        private void timerGlobal_Tick(object sender, EventArgs e)
        {
            if (_activeTask != null && _activeTask.LastStartTime.HasValue)
            {
                // Incrementa 1 segundo no tempo total
                _activeTask.TotalTime = _activeTask.TotalTime.Add(TimeSpan.FromSeconds(1));

                // Atualiza LastStartTime para o momento atual
                _activeTask.LastStartTime = DateTime.Now;

                // Atualiza a interface
                AtualizarInterfaceTarefas();
            }
        }

        /// <summary>
        /// Timer de minimização (dispara 2 segundos após clique)
        /// </summary>
        private void timerMinimize_Tick(object sender, EventArgs e)
        {
            timerMinimize.Stop();
            this.WindowState = FormWindowState.Minimized;
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
                    // Cria nova tarefa
                    var newTask = new TaskModel
                    {
                        Id = _nextId++,
                        Name = form.TaskName,
                        TotalTime = TimeSpan.Zero,
                        State = TaskState.Pendente,
                        LastStartTime = null
                    };

                    // Adiciona à lista
                    _tasks.Add(newTask);

                    // Cria o botão
                    var button = CriarBotaoTarefa(newTask);
                    flpTasks.Controls.Add(button);

                    // Atualiza DataGridView
                    AtualizarDataGridView();

                    // Salva no INI
                    _repository.SalvarTarefas(_tasks);
                }
            }
        }

        /// <summary>
        /// Apaga a tarefa atualmente ativa
        /// </summary>
        private void ApagarTarefaAtiva()
        {
            if (_activeTask == null) return;

            // Remove da lista
            _tasks.Remove(_activeTask);

            // Remove o botão correspondente
            Button buttonToRemove = null;
            foreach (Control control in flpTasks.Controls)
            {
                if (control is Button button && button.Tag is TaskModel task && task.Id == _activeTask.Id)
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

            // Limpa a tarefa ativa
            _activeTask = null;

            // Atualiza DataGridView
            AtualizarDataGridView();

            // Salva no INI
            _repository.SalvarTarefas(_tasks);
        }

        /// <summary>
        /// Evento de fechamento do formulário (salva antes de fechar)
        /// </summary>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Atualiza tempo da tarefa ativa antes de fechar
            if (_activeTask != null && _activeTask.LastStartTime.HasValue)
            {
                var elapsed = DateTime.Now - _activeTask.LastStartTime.Value;
                _activeTask.TotalTime = _activeTask.TotalTime.Add(elapsed);
                _activeTask.LastStartTime = null;
            }

            // Salva todas as tarefas
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

            // Converte coordenada do mouse para posição dentro do painel
            Point point = flpTasks.PointToClient(new Point(e.X, e.Y));

            // Descobrir qual controle está na posição de drop
            Control target = flpTasks.GetChildAtPoint(point);

            if (target == null || target == draggedButton)
                return;

            int oldIndex = flpTasks.Controls.IndexOf(draggedButton);
            int newIndex = flpTasks.Controls.IndexOf(target);

            if (oldIndex == newIndex)
                return;

            // Remove e insere na nova posição
            flpTasks.Controls.SetChildIndex(draggedButton, newIndex);
            flpTasks.Invalidate();

            //
            // >>> Atualiza ordem na lista interna _tasks <<<
            //
            var draggedTask = draggedButton.Tag as TaskModel;

            _tasks.Remove(draggedTask);
            _tasks.Insert(newIndex, draggedTask);

            // Salvar nova ordem no INI
            _repository.SalvarTarefas(_tasks);
        }


    }
}
