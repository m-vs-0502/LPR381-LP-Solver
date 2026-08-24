namespace LPR_Solver
{
    partial class Main
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
            this.btnClose = new System.Windows.Forms.Button();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.rTBDisplay = new System.Windows.Forms.RichTextBox();
            this.btnSolve = new System.Windows.Forms.Button();
            this.btnEdit = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.cmbAlgorithm = new System.Windows.Forms.ComboBox();
            this.lblAlgorithm = new System.Windows.Forms.Label();
            this.btnNextTable = new System.Windows.Forms.Button();
            this.btnPreviousTable = new System.Windows.Forms.Button();
            this.lblSensitivity = new System.Windows.Forms.Label();
            this.cmbSensitivity = new System.Windows.Forms.ComboBox();
            this.lblSAHelp = new System.Windows.Forms.Label();
            this.txtSAIndex = new System.Windows.Forms.TextBox();
            this.txtSADelta = new System.Windows.Forms.TextBox();
            this.btnSensitivity = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnClose
            // 
            this.btnClose.BackColor = System.Drawing.SystemColors.Control;
            this.btnClose.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClose.ForeColor = System.Drawing.Color.Red;
            this.btnClose.Location = new System.Drawing.Point(865, 12);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(107, 32);
            this.btnClose.TabIndex = 0;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = false;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(12, 12);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnBrowse.TabIndex = 1;
            this.btnBrowse.Text = "Select File";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // rTBDisplay
            // 
            this.rTBDisplay.Enabled = false;
            this.rTBDisplay.Location = new System.Drawing.Point(191, 88);
            this.rTBDisplay.Name = "rTBDisplay";
            this.rTBDisplay.Size = new System.Drawing.Size(558, 322);
            this.rTBDisplay.TabIndex = 2;
            this.rTBDisplay.Text = "Model will show here once selected.";
            // 
            // btnSolve
            // 
            this.btnSolve.Location = new System.Drawing.Point(213, 417);
            this.btnSolve.Name = "btnSolve";
            this.btnSolve.Size = new System.Drawing.Size(75, 23);
            this.btnSolve.TabIndex = 3;
            this.btnSolve.Text = "Solve";
            this.btnSolve.UseVisualStyleBackColor = true;
            this.btnSolve.Click += new System.EventHandler(this.btnSolve_Click);
            // 
            // btnEdit
            // 
            this.btnEdit.Location = new System.Drawing.Point(431, 417);
            this.btnEdit.Name = "btnEdit";
            this.btnEdit.Size = new System.Drawing.Size(75, 23);
            this.btnEdit.TabIndex = 4;
            this.btnEdit.Text = "Edit";
            this.btnEdit.UseVisualStyleBackColor = true;
            this.btnEdit.Click += new System.EventHandler(this.btnEdit_Click);
            // 
            // btnClear
            // 
            this.btnClear.Location = new System.Drawing.Point(665, 416);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(75, 23);
            this.btnClear.TabIndex = 5;
            this.btnClear.Text = "Clear";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // cmbAlgorithm
            // 
            this.cmbAlgorithm.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAlgorithm.FormattingEnabled = true;
            this.cmbAlgorithm.Items.AddRange(new object[] {
            "Primal Simplex",
            "Primal Simplex Revised",
            "Branch & Bound Simplex",
            "Branch & Bound Knapsack",
            "Cutting Plane"});
            this.cmbAlgorithm.Location = new System.Drawing.Point(765, 113);
            this.cmbAlgorithm.Name = "cmbAlgorithm";
            this.cmbAlgorithm.Size = new System.Drawing.Size(189, 25);
            this.cmbAlgorithm.TabIndex = 6;
            // 
            // lblAlgorithm
            // 
            this.lblAlgorithm.AutoSize = true;
            this.lblAlgorithm.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.lblAlgorithm.Location = new System.Drawing.Point(765, 94);
            this.lblAlgorithm.Name = "lblAlgorithm";
            this.lblAlgorithm.Size = new System.Drawing.Size(108, 17);
            this.lblAlgorithm.TabIndex = 7;
            this.lblAlgorithm.Text = "Algorithm to use:";
            // 
            // btnNextTable
            // 
            this.btnNextTable.Location = new System.Drawing.Point(782, 343);
            this.btnNextTable.Name = "btnNextTable";
            this.btnNextTable.Size = new System.Drawing.Size(75, 23);
            this.btnNextTable.TabIndex = 8;
            this.btnNextTable.Text = "Next table iteration";
            this.btnNextTable.UseVisualStyleBackColor = true;
            this.btnNextTable.Visible = false;
            this.btnNextTable.Click += new System.EventHandler(this.btnNextTable_Click);
            // 
            // btnPreviousTable
            // 
            this.btnPreviousTable.Location = new System.Drawing.Point(782, 372);
            this.btnPreviousTable.Name = "btnPreviousTable";
            this.btnPreviousTable.Size = new System.Drawing.Size(75, 23);
            this.btnPreviousTable.TabIndex = 9;
            this.btnPreviousTable.Text = "Previous";
            this.btnPreviousTable.UseVisualStyleBackColor = true;
            this.btnPreviousTable.Visible = false;
            this.btnPreviousTable.Click += new System.EventHandler(this.btnPreviousTable_Click);
            // 
            // lblSensitivity
            // 
            this.lblSensitivity.AutoSize = true;
            this.lblSensitivity.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.lblSensitivity.Location = new System.Drawing.Point(12, 88);
            this.lblSensitivity.Name = "lblSensitivity";
            this.lblSensitivity.Size = new System.Drawing.Size(120, 17);
            this.lblSensitivity.TabIndex = 10;
            this.lblSensitivity.Text = "Sensitivity Analysis:";
            // 
            // cmbSensitivity
            // 
            this.cmbSensitivity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSensitivity.FormattingEnabled = true;
            this.cmbSensitivity.Items.AddRange(new object[] {
            "Range: Non-Basic Variable",
            "Change: Non-Basic Variable",
            "Range: Basic Variable",
            "Change: Basic Variable",
            "Range: Constraint RHS",
            "Change: Constraint RHS",
            "Range: Non-Basic Column Coeff",
            "Change: Non-Basic Column Coeff",
            "Add New Activity",
            "Add New Constraint",
            "Shadow Prices",
            "Duality"});
            this.cmbSensitivity.Location = new System.Drawing.Point(12, 108);
            this.cmbSensitivity.Name = "cmbSensitivity";
            this.cmbSensitivity.Size = new System.Drawing.Size(170, 25);
            this.cmbSensitivity.TabIndex = 11;
            this.cmbSensitivity.SelectedIndexChanged += new System.EventHandler(this.cmbSensitivity_SelectedIndexChanged);
            // 
            // lblSAHelp
            // 
            this.lblSAHelp.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.lblSAHelp.Location = new System.Drawing.Point(12, 140);
            this.lblSAHelp.Name = "lblSAHelp";
            this.lblSAHelp.Size = new System.Drawing.Size(170, 70);
            this.lblSAHelp.TabIndex = 12;
            this.lblSAHelp.Text = "Select an operation.";
            // 
            // txtSAIndex
            // 
            this.txtSAIndex.Location = new System.Drawing.Point(12, 215);
            this.txtSAIndex.Name = "txtSAIndex";
            this.txtSAIndex.Size = new System.Drawing.Size(170, 25);
            this.txtSAIndex.TabIndex = 13;
            // 
            // txtSADelta
            // 
            this.txtSADelta.Location = new System.Drawing.Point(12, 246);
            this.txtSADelta.Name = "txtSADelta";
            this.txtSADelta.Size = new System.Drawing.Size(170, 25);
            this.txtSADelta.TabIndex = 14;
            // 
            // btnSensitivity
            // 
            this.btnSensitivity.Location = new System.Drawing.Point(12, 279);
            this.btnSensitivity.Name = "btnSensitivity";
            this.btnSensitivity.Size = new System.Drawing.Size(170, 28);
            this.btnSensitivity.TabIndex = 15;
            this.btnSensitivity.Text = "Perform Analysis";
            this.btnSensitivity.UseVisualStyleBackColor = true;
            this.btnSensitivity.Click += new System.EventHandler(this.btnSensitivity_Click);
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.GrayText;
            this.ClientSize = new System.Drawing.Size(984, 561);
            this.ControlBox = false;
            this.Controls.Add(this.btnSensitivity);
            this.Controls.Add(this.txtSADelta);
            this.Controls.Add(this.txtSAIndex);
            this.Controls.Add(this.lblSAHelp);
            this.Controls.Add(this.cmbSensitivity);
            this.Controls.Add(this.lblSensitivity);
            this.Controls.Add(this.btnPreviousTable);
            this.Controls.Add(this.btnNextTable);
            this.Controls.Add(this.lblAlgorithm);
            this.Controls.Add(this.cmbAlgorithm);
            this.Controls.Add(this.btnClear);
            this.Controls.Add(this.btnEdit);
            this.Controls.Add(this.btnSolve);
            this.Controls.Add(this.rTBDisplay);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.btnClose);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Main";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "LP & IP Solver";
            this.Load += new System.EventHandler(this.Main_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.RichTextBox rTBDisplay;
        private System.Windows.Forms.Button btnSolve;
        private System.Windows.Forms.Button btnEdit;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.ComboBox cmbAlgorithm;
        private System.Windows.Forms.Label lblAlgorithm;
        private System.Windows.Forms.Button btnNextTable;
        private System.Windows.Forms.Button btnPreviousTable;
        private System.Windows.Forms.Label lblSensitivity;
        private System.Windows.Forms.ComboBox cmbSensitivity;
        private System.Windows.Forms.Label lblSAHelp;
        private System.Windows.Forms.TextBox txtSAIndex;
        private System.Windows.Forms.TextBox txtSADelta;
        private System.Windows.Forms.Button btnSensitivity;
    }
}

