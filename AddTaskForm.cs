using System;
using System.Windows.Forms;

namespace ControleTarefasWinForms
{
    /// <summary>
    /// Formulário para adicionar uma nova tarefa
    /// </summary>
    public partial class AddTaskForm : Form
    {
        /// <summary>
        /// Nome da tarefa inserido pelo usuário
        /// </summary>
        public string TaskName { get; private set; }

        public AddTaskForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Evento do botão OK
        /// </summary>
        private void btnOk_Click(object sender, EventArgs e)
        {
            // Valida o nome da tarefa
            string name = txtTaskName.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("O nome da tarefa é obrigatório.",
                    "Validação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTaskName.Focus();
                return;
            }

            // Define o nome e fecha o form com sucesso
            TaskName = name;
            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>
        /// Evento do botão Cancelar
        /// </summary>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        /// <summary>
        /// Permite pressionar Enter para confirmar
        /// </summary>
        private void txtTaskName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnOk_Click(sender, e);
            }
        }
    }
}
