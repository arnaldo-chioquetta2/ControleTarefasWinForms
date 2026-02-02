namespace ControleTarefasWinForms
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.flpTasks = new System.Windows.Forms.FlowLayoutPanel();
            this.btnAddTask = new System.Windows.Forms.Button();
            this.timerGlobal = new System.Windows.Forms.Timer(this.components);
            this.timerMinimize = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // flpTasks
            // 
            this.flpTasks.AutoScroll = true;
            this.flpTasks.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpTasks.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpTasks.Location = new System.Drawing.Point(0, 0);
            this.flpTasks.Name = "flpTasks";
            this.flpTasks.Size = new System.Drawing.Size(800, 560);
            this.flpTasks.TabIndex = 0;
            this.flpTasks.WrapContents = false;
            // 
            // btnAddTask
            // 
            this.btnAddTask.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btnAddTask.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnAddTask.Location = new System.Drawing.Point(0, 560);
            this.btnAddTask.Name = "btnAddTask";
            this.btnAddTask.Size = new System.Drawing.Size(800, 40);
            this.btnAddTask.TabIndex = 2;
            this.btnAddTask.Text = "Adicionar Tarefa";
            this.btnAddTask.UseVisualStyleBackColor = true;
            this.btnAddTask.Click += new System.EventHandler(this.btnAddTask_Click);
            // 
            // timerGlobal
            // 
            this.timerGlobal.Interval = 1000;
            this.timerGlobal.Tick += new System.EventHandler(this.timerGlobal_Tick);
            // 
            // timerMinimize
            // 
            this.timerMinimize.Interval = 2000;
            this.timerMinimize.Tick += new System.EventHandler(this.timerMinimize_Tick);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Controls.Add(this.flpTasks);
            this.Controls.Add(this.btnAddTask);
            this.KeyPreview = true;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Controle de Tarefas";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Shown += new System.EventHandler(this.MainForm_Shown);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flpTasks;
        private System.Windows.Forms.Button btnAddTask;
        private System.Windows.Forms.Timer timerGlobal;
        private System.Windows.Forms.Timer timerMinimize;
    }
}
