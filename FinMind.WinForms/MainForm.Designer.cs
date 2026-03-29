using System.Drawing;

namespace FinMind.WinForms;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    private Label lblTitle;
    private Label lblSymbols;
    private TextBox txtSymbols;
    private Label lblStart;
    private DateTimePicker dtpStart;
    private Label lblEnd;
    private DateTimePicker dtpEnd;
    private Label lblPython;
    private TextBox txtPythonExe;
    private Button btnRun;
    private Button btnOpenOutputFolder;
    private Button btnOpenLatestCsv;
    private TextBox txtOutput;
    private Label lblHint;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        lblTitle = new Label();
        lblSymbols = new Label();
        txtSymbols = new TextBox();
        lblStart = new Label();
        dtpStart = new DateTimePicker();
        lblEnd = new Label();
        dtpEnd = new DateTimePicker();
        lblPython = new Label();
        txtPythonExe = new TextBox();
        btnRun = new Button();
        btnOpenOutputFolder = new Button();
        btnOpenLatestCsv = new Button();
        txtOutput = new TextBox();
        lblHint = new Label();
        SuspendLayout();
        //
        // lblTitle
        //
        lblTitle.AutoSize = true;
        lblTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        lblTitle.Location = new Point(16, 12);
        lblTitle.Size = new Size(280, 21);
        lblTitle.Text = "FinMind K-Bar (newapp.py)";
        //
        // lblSymbols
        //
        lblSymbols.AutoSize = true;
        lblSymbols.Location = new Point(16, 44);
        lblSymbols.Text = "Symbols (one per line or comma-separated)";
        //
        // txtSymbols
        //
        txtSymbols.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtSymbols.Location = new Point(16, 64);
        txtSymbols.Multiline = true;
        txtSymbols.ScrollBars = ScrollBars.Vertical;
        txtSymbols.Size = new Size(592, 72);
        txtSymbols.TabIndex = 0;
        txtSymbols.PlaceholderText = "2330\r\n2317";
        //
        // lblStart
        //
        lblStart.AutoSize = true;
        lblStart.Location = new Point(16, 148);
        lblStart.Text = "Start date";
        //
        // dtpStart
        //
        dtpStart.Format = DateTimePickerFormat.Short;
        dtpStart.Location = new Point(16, 168);
        dtpStart.Size = new Size(120, 23);
        dtpStart.TabIndex = 1;
        //
        // lblEnd
        //
        lblEnd.AutoSize = true;
        lblEnd.Location = new Point(152, 148);
        lblEnd.Text = "End date";
        //
        // dtpEnd
        //
        dtpEnd.Format = DateTimePickerFormat.Short;
        dtpEnd.Location = new Point(152, 168);
        dtpEnd.Size = new Size(120, 23);
        dtpEnd.TabIndex = 2;
        //
        // lblPython
        //
        lblPython.AutoSize = true;
        lblPython.Location = new Point(288, 148);
        lblPython.Text = "Python executable";
        //
        // txtPythonExe
        //
        txtPythonExe.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtPythonExe.Location = new Point(288, 168);
        txtPythonExe.Size = new Size(320, 23);
        txtPythonExe.TabIndex = 3;
        txtPythonExe.Text = "python";
        //
        // btnRun
        //
        btnRun.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnRun.Location = new Point(433, 204);
        btnRun.Size = new Size(175, 32);
        btnRun.TabIndex = 4;
        btnRun.Text = "Run Python";
        btnRun.UseVisualStyleBackColor = true;
        btnRun.Click += BtnRun_Click;
        //
        // btnOpenOutputFolder
        //
        btnOpenOutputFolder.Location = new Point(16, 204);
        btnOpenOutputFolder.Size = new Size(160, 32);
        btnOpenOutputFolder.TabIndex = 5;
        btnOpenOutputFolder.Text = "Open output folder";
        btnOpenOutputFolder.UseVisualStyleBackColor = true;
        btnOpenOutputFolder.Click += BtnOpenOutputFolder_Click;
        //
        // btnOpenLatestCsv
        //
        btnOpenLatestCsv.Location = new Point(184, 204);
        btnOpenLatestCsv.Size = new Size(160, 32);
        btnOpenLatestCsv.TabIndex = 6;
        btnOpenLatestCsv.Text = "Open latest CSV";
        btnOpenLatestCsv.UseVisualStyleBackColor = true;
        btnOpenLatestCsv.Click += BtnOpenLatestCsv_Click;
        //
        // lblHint
        //
        lblHint.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        lblHint.AutoEllipsis = true;
        lblHint.ForeColor = SystemColors.GrayText;
        lblHint.Location = new Point(16, 488);
        lblHint.Size = new Size(592, 20);
        lblHint.Text = "Set FINMIND_TOKEN in your environment. CSV files are written next to newapp.py.";
        //
        // txtOutput
        //
        txtOutput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        txtOutput.Font = new Font("Consolas", 9F);
        txtOutput.Location = new Point(16, 248);
        txtOutput.Multiline = true;
        txtOutput.ReadOnly = true;
        txtOutput.ScrollBars = ScrollBars.Both;
        txtOutput.Size = new Size(592, 232);
        txtOutput.TabIndex = 7;
        txtOutput.WordWrap = false;
        //
        // MainForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(624, 521);
        Controls.Add(lblHint);
        Controls.Add(txtOutput);
        Controls.Add(btnOpenLatestCsv);
        Controls.Add(btnOpenOutputFolder);
        Controls.Add(btnRun);
        Controls.Add(txtPythonExe);
        Controls.Add(lblPython);
        Controls.Add(dtpEnd);
        Controls.Add(lblEnd);
        Controls.Add(dtpStart);
        Controls.Add(lblStart);
        Controls.Add(txtSymbols);
        Controls.Add(lblSymbols);
        Controls.Add(lblTitle);
        MinimumSize = new Size(500, 400);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "FinMind K-Bar Fetch";
        ResumeLayout(false);
        PerformLayout();
    }
}
