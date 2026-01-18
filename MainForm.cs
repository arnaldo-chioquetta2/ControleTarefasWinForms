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

            // Cria os botões e atualiza a interface
            CriarBotoesTarefas();

            // Define o título com versão
            this.Text = "Controle de Tarefas - v1.0";

            // Inicia o timer global
            timerGlobal.Start();
        }
        //private void MainForm_Load(object sender, EventArgs e)
        //{
        //    flpTasks.AllowDrop = true;
        //    flpTasks.DragOver += FlpTasks_DragOver;
        //    flpTasks.DragDrop += FlpTasks_DragDrop;

        //    // Adiciona evento MouseLeave ao formulário e controles
        //    this.MouseLeave += MainForm_MouseLeave;
        //    flpTasks.MouseLeave += MainForm_MouseLeave;
        //    btnAddTask.MouseLeave += MainForm_MouseLeave;

        //    _tasks = _repository.CarregarTarefas();

        //    // Define o próximo ID
        //    if (_tasks.Count > 0)
        //    {
        //        _nextId = _tasks.Max(t => t.Id) + 1;
        //    }

        //    foreach (var task in _tasks)
        //    {
        //        Console.WriteLine(
        //                $"[MainForm_Load - ANTES] Id={task.Id} | Nome={task.Name} | State={task.State}"
        //            );
        //        task.LastStartTime = null;

        //        if (task.State == TaskState.Ativa)
        //            task.State = TaskState.Pausada;
        //        Console.WriteLine(
        //                $"[MainForm_Load - DEPOIS] Id={task.Id} | Nome={task.Name} | State={task.State}"
        //            );
        //    }

        //    // Cria os botões e atualiza a interface
        //    CriarBotoesTarefas();

        //    // Inicia o timer global
        //    timerGlobal.Start();
        //}

        /// <summary>
        /// Evento disparado quando o mouse sai da janela
        /// </summary>
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
                Width = flpTasks.Width - 25,
                Height = 60,
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };

            AtualizarBotao(button, task);
            button.Click += BotaoTarefa_Click;
            button.MouseUp += BotaoTarefa_MouseUp;
            button.MouseLeave += MainForm_MouseLeave; // Adiciona evento ao botão

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
                AtualizarInterfaceTarefas();
                _repository.SalvarTarefas(_tasks);
                return;
            }
            else if (result == DialogResult.No)
            {
                ApagarTarefa(task);
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
                    button.BackColor = Color.Khaki;
                    button.ForeColor = Color.Black;
                    break;

                case TaskState.Desabilitada:
                    button.BackColor = Color.White;
                    button.ForeColor = Color.Black;
                    break;
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
                }
            }
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
    }
}