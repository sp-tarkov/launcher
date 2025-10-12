namespace SPT.Launcher.PatchGen
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            textBoxOriginal = new TextBox();
            textBoxPatched = new TextBox();
            textBoxDelta = new TextBox();
            labelOriginal = new Label();
            labelPatched = new Label();
            labelOutput = new Label();
            groupBoxDiff = new GroupBox();
            buttonCreate = new Button();
            buttonDelta = new Button();
            buttonPatched = new Button();
            buttonOriginal = new Button();
            openFileDialog = new OpenFileDialog();
            saveFileDialog = new SaveFileDialog();
            groupBoxDiff.SuspendLayout();
            SuspendLayout();
            // 
            // textBoxOriginal
            // 
            textBoxOriginal.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBoxOriginal.Location = new Point(111, 22);
            textBoxOriginal.Name = "textBoxOriginal";
            textBoxOriginal.Size = new Size(318, 23);
            textBoxOriginal.TabIndex = 0;
            // 
            // textBoxPatched
            // 
            textBoxPatched.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBoxPatched.Location = new Point(111, 51);
            textBoxPatched.Name = "textBoxPatched";
            textBoxPatched.Size = new Size(318, 23);
            textBoxPatched.TabIndex = 1;
            // 
            // textBoxDelta
            // 
            textBoxDelta.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBoxDelta.Location = new Point(111, 80);
            textBoxDelta.Name = "textBoxDelta";
            textBoxDelta.Size = new Size(318, 23);
            textBoxDelta.TabIndex = 2;
            // 
            // labelOriginal
            // 
            labelOriginal.AutoSize = true;
            labelOriginal.Location = new Point(6, 25);
            labelOriginal.Name = "labelOriginal";
            labelOriginal.Size = new Size(71, 15);
            labelOriginal.TabIndex = 3;
            labelOriginal.Text = "Original file:";
            // 
            // labelPatched
            // 
            labelPatched.AutoSize = true;
            labelPatched.Location = new Point(6, 54);
            labelPatched.Name = "labelPatched";
            labelPatched.Size = new Size(72, 15);
            labelPatched.TabIndex = 4;
            labelPatched.Text = "Patched file:";
            // 
            // labelOutput
            // 
            labelOutput.AutoSize = true;
            labelOutput.Location = new Point(6, 83);
            labelOutput.Name = "labelOutput";
            labelOutput.Size = new Size(99, 15);
            labelOutput.TabIndex = 5;
            labelOutput.Text = "Delta File Output:";
            // 
            // groupBoxDiff
            // 
            groupBoxDiff.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBoxDiff.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            groupBoxDiff.Controls.Add(buttonCreate);
            groupBoxDiff.Controls.Add(buttonDelta);
            groupBoxDiff.Controls.Add(buttonPatched);
            groupBoxDiff.Controls.Add(buttonOriginal);
            groupBoxDiff.Controls.Add(labelOriginal);
            groupBoxDiff.Controls.Add(labelOutput);
            groupBoxDiff.Controls.Add(textBoxOriginal);
            groupBoxDiff.Controls.Add(labelPatched);
            groupBoxDiff.Controls.Add(textBoxPatched);
            groupBoxDiff.Controls.Add(textBoxDelta);
            groupBoxDiff.Location = new Point(12, 12);
            groupBoxDiff.Name = "groupBoxDiff";
            groupBoxDiff.Size = new Size(480, 157);
            groupBoxDiff.TabIndex = 6;
            groupBoxDiff.TabStop = false;
            groupBoxDiff.Text = "Create Patch";
            // 
            // buttonCreate
            // 
            buttonCreate.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCreate.Location = new Point(367, 123);
            buttonCreate.Name = "buttonCreate";
            buttonCreate.Size = new Size(98, 23);
            buttonCreate.TabIndex = 9;
            buttonCreate.Text = "Create Delta";
            buttonCreate.UseVisualStyleBackColor = true;
            buttonCreate.Click += buttonCreate_Click;
            // 
            // buttonDelta
            // 
            buttonDelta.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonDelta.Location = new Point(435, 80);
            buttonDelta.Name = "buttonDelta";
            buttonDelta.Size = new Size(30, 23);
            buttonDelta.TabIndex = 8;
            buttonDelta.Text = "...";
            buttonDelta.UseVisualStyleBackColor = true;
            buttonDelta.Click += buttonDelta_Click;
            // 
            // buttonPatched
            // 
            buttonPatched.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonPatched.Location = new Point(435, 51);
            buttonPatched.Name = "buttonPatched";
            buttonPatched.Size = new Size(30, 23);
            buttonPatched.TabIndex = 7;
            buttonPatched.Text = "...";
            buttonPatched.UseVisualStyleBackColor = true;
            buttonPatched.Click += buttonPatched_Click;
            // 
            // buttonOriginal
            // 
            buttonOriginal.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonOriginal.Location = new Point(435, 22);
            buttonOriginal.Name = "buttonOriginal";
            buttonOriginal.Size = new Size(30, 23);
            buttonOriginal.TabIndex = 6;
            buttonOriginal.Text = "...";
            buttonOriginal.UseVisualStyleBackColor = true;
            buttonOriginal.Click += buttonOriginal_Click;
            // 
            // openFileDialog
            // 
            openFileDialog.FileName = "openFileDialog";
            openFileDialog.Filter = "Dynamic Link Library (*.dll;*.dll.spt-bak)|*.dll;*.dll.spt-bak|All Files (*.*)|*.*";
            // 
            // saveFileDialog
            // 
            saveFileDialog.DefaultExt = "delta";
            saveFileDialog.Filter = "Delta file (*.delta)|*.delta";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(504, 181);
            Controls.Add(groupBoxDiff);
            Name = "Form1";
            Text = "PatchGen";
            FormClosing += Form1_FormClosing;
            groupBoxDiff.ResumeLayout(false);
            groupBoxDiff.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private TextBox textBoxOriginal;
        private TextBox textBoxPatched;
        private TextBox textBoxDelta;
        private Label labelOriginal;
        private Label labelPatched;
        private Label labelOutput;
        private GroupBox groupBoxDiff;
        private Button buttonPatched;
        private Button buttonOriginal;
        private Button buttonDelta;
        private Button buttonCreate;
        private OpenFileDialog openFileDialog;
        private SaveFileDialog saveFileDialog;
    }
}
