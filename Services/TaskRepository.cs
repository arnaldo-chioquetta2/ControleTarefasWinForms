using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using ControleTarefasWinForms.Models;

namespace ControleTarefasWinForms.Services
{
    /// <summary>
    /// Repositório para gerenciar a persistência de tarefas em arquivo INI
    /// </summary>
    public class TaskRepository
    {
        private readonly IniFile _iniFile;
        private readonly string _iniPath;

        public TaskRepository()
        {
            _iniPath = Path.Combine(Application.StartupPath, "tarefas.ini");
            _iniFile = new IniFile(_iniPath);
        }

        /// <summary>
        /// Carrega todas as tarefas do arquivo INI
        /// </summary>
        /// <returns>Lista de tarefas carregadas</returns>
        public List<TaskModel> CarregarTarefas()
        {
            var tasks = new List<TaskModel>();

            try
            {
                if (!_iniFile.FileExists())
                {
                    return tasks; // Retorna lista vazia se arquivo não existe
                }

                // Lê a quantidade de tarefas
                string countStr = _iniFile.Read("Geral", "Count", "0");
                int count = int.Parse(countStr);

                // Carrega cada tarefa
                for (int i = 1; i <= count; i++)
                {
                    string section = $"Tarefa_{i}";

                    var task = new TaskModel
                    {
                        Id = int.Parse(_iniFile.Read(section, "Id", "0")),
                        Name = _iniFile.Read(section, "Nome", ""),
                        TotalTime = TimeSpan.FromSeconds(int.Parse(_iniFile.Read(section, "TotalSegundos", "0"))),
                        State = ParseState(_iniFile.Read(section, "State", "Pendente")),
                        LastStartTime = null // Não continua contando de sessão anterior
                    };

                    tasks.Add(task);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar tarefas do arquivo INI:\n{ex.Message}",
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return tasks;
        }

        /// <summary>
        /// Salva todas as tarefas no arquivo INI
        /// </summary>
        /// <param name="tasks">Lista de tarefas a serem salvas</param>
        public void SalvarTarefas(List<TaskModel> tasks)
        {
            try
            {
                // Limpa o arquivo criando um novo
                if (File.Exists(_iniPath))
                {
                    File.Delete(_iniPath);
                }

                // Salva a quantidade de tarefas
                _iniFile.Write("Geral", "Count", tasks.Count.ToString());

                // Salva cada tarefa
                for (int i = 0; i < tasks.Count; i++)
                {
                    var task = tasks[i];
                    string section = $"Tarefa_{i + 1}";

                    _iniFile.Write(section, "Id", task.Id.ToString());
                    _iniFile.Write(section, "Nome", task.Name);
                    _iniFile.Write(section, "TotalSegundos", ((int)task.TotalTime.TotalSeconds).ToString());
                    _iniFile.Write(section, "State", task.State.ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar tarefas no arquivo INI:\n{ex.Message}",
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Converte string do INI para enum TaskState
        /// </summary>
        private TaskState ParseState(string stateStr)
        {
            switch (stateStr)
            {
                case "Ativa":
                    return TaskState.Ativa;
                case "JaClicada":
                    return TaskState.JaClicada;
                default:
                    return TaskState.Pendente;
            }
        }
    }
}
