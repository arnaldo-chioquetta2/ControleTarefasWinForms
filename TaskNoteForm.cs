using System;
using System.Windows.Forms;

namespace ControleTarefasWinForms
{
    public partial class TaskNoteForm : Form
    {
        public string NoteText
        {
            get { return txtNote.Text; }
        }

        public TaskNoteForm(string taskName, string currentNote)
        {
            InitializeComponent();

            this.Text = "Anotação - " + taskName;
            txtNote.Text = currentNote ?? "";
            txtNote.SelectAll();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}