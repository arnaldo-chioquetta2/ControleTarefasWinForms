namespace ControleTarefasWinForms.Models
{
    /// <summary>
    /// Representa os possíveis estados de uma tarefa
    /// </summary>
    public enum TaskState
    {
        /// <summary>
        /// Tarefa nunca foi clicada ou foi resetada no ciclo atual
        /// </summary>
        Pendente,

        /// <summary>
        /// Tarefa atualmente selecionada (contando tempo)
        /// </summary>
        Ativa,

        /// <summary>
        /// Tarefa já foi clicada neste ciclo, mas não é a ativa
        /// </summary>
        JaClicada,
        Pausada,
        Desabilitada
    }
}
