namespace ControleTarefasWinForms
{
    partial class TaskNoteForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TextBox txtNote;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.txtNote = new System.Windows.Forms.TextBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();

            // txtNote
            this.txtNote.Multiline = true;
            this.txtNote.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtNote.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.txtNote.Location = new System.Drawing.Point(12, 12);
            this.txtNote.Size = new System.Drawing.Size(460, 220);
            this.txtNote.Anchor = ((System.Windows.Forms.AnchorStyles)
                ((((System.Windows.Forms.AnchorStyles.Top |
                    System.Windows.Forms.AnchorStyles.Bottom) |
                    System.Windows.Forms.AnchorStyles.Left) |
                    System.Windows.Forms.AnchorStyles.Right)));

            // btnOK
            this.btnOK.Text = "OK";
            this.btnOK.Location = new System.Drawing.Point(316, 245);
            this.btnOK.Size = new System.Drawing.Size(75, 32);
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)
                (System.Windows.Forms.AnchorStyles.Bottom |
                 System.Windows.Forms.AnchorStyles.Right));
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);

            // btnCancel
            this.btnCancel.Text = "Cancelar";
            this.btnCancel.Location = new System.Drawing.Point(397, 245);
            this.btnCancel.Size = new System.Drawing.Size(75, 32);
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)
                (System.Windows.Forms.AnchorStyles.Bottom |
                 System.Windows.Forms.AnchorStyles.Right));
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

            // TaskNoteForm
            this.ClientSize = new System.Drawing.Size(484, 291);
            this.Controls.Add(this.txtNote);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Name = "TaskNoteForm";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}