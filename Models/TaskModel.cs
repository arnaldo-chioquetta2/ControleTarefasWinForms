using System;

namespace ControleTarefasWinForms.Models
{
    /// <summary>
    /// Modelo de dados para uma tarefa
    /// </summary>
    public class TaskModel
    {
        /// <summary>
        /// Identificador único da tarefa
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nome da tarefa
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Tempo total acumulado na tarefa
        /// </summary>
        public TimeSpan TotalTime { get; set; }

        /// <summary>
        /// Estado atual da tarefa (Pendente, Ativa, JaClicada)
        /// </summary>
        public TaskState State { get; set; }

        /// <summary>
        /// Momento em que a tarefa começou a contar tempo (quando se tornou ativa)
        /// Null quando a tarefa não está ativa
        /// </summary>
        public DateTime? LastStartTime { get; set; }

        public string FormattedTime
        {
            get
            {
                int days = (int)TotalTime.TotalDays;

                if (days > 0)
                {
                    return string.Format("{0}d {1:D2}:{2:D2}:{3:D2}",
                        days,
                        TotalTime.Hours,
                        TotalTime.Minutes,
                        TotalTime.Seconds);
                }
                else
                {
                    return string.Format("{0:D2}:{1:D2}:{2:D2}",
                        TotalTime.Hours,
                        TotalTime.Minutes,
                        TotalTime.Seconds);
                }
            }
        }

    }
}
