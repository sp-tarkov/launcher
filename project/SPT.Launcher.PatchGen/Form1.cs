using System.Diagnostics;

namespace SPT.Launcher.PatchGen
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            PopulateDefaults();
            ResourceManager.ExtractResourcesToTempDir();
        }

        private void PopulateDefaults()
        {
            textBoxOriginal.Text = Settings.Read("originalFilePath");
            textBoxPatched.Text = Settings.Read("patchedFilePath");
            textBoxDelta.Text = Settings.Read("deltaFilePath");
        }

        private void buttonOriginal_Click(object sender, EventArgs e)
        {
            openFileDialog.Title = "Select Original File";
            openFileDialog.InitialDirectory = textBoxOriginal.Text;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                textBoxOriginal.Text = openFileDialog.FileName;
                Settings.AddUpdateAppSettings("originalFilePath", openFileDialog.FileName);
            }
        }

        private void buttonPatched_Click(object sender, EventArgs e)
        {
            openFileDialog.Title = "Select Patched File";
            openFileDialog.InitialDirectory = textBoxPatched.Text;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                textBoxPatched.Text = openFileDialog.FileName;
                Settings.AddUpdateAppSettings("patchedFilePath", openFileDialog.FileName);
            }
        }

        private void buttonDelta_Click(object sender, EventArgs e)
        {
            saveFileDialog.Title = "Select Output File";
            saveFileDialog.InitialDirectory = textBoxDelta.Text;
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                textBoxDelta.Text = saveFileDialog.FileName;
                Settings.AddUpdateAppSettings("deltaFilePath", saveFileDialog.FileName);
            }
        }

        private void buttonCreate_Click(object sender, EventArgs e)
        {
            string originalFile = textBoxOriginal.Text;
            string patchedFile = textBoxPatched.Text;
            string deltaFile = textBoxDelta.Text;
            string outputDir = Path.GetDirectoryName(deltaFile) ?? "";

            if (originalFile.Length == 0 || patchedFile.Length == 0 || deltaFile.Length == 0)
            {
                MessageBox.Show("Missing required field", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!File.Exists(originalFile))
            {
                MessageBox.Show("Original file not found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!File.Exists(patchedFile))
            {
                MessageBox.Show("Patched file not found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            if (!Directory.Exists(outputDir) && outputDir.Length > 0)
            {
                Directory.CreateDirectory(outputDir);
            }
            if (File.Exists(deltaFile))
            {
                File.Delete(deltaFile);
            }

            // The parameters we use here are important to allow patch application using the SharpHDiffPatch.Core library, please don't change them
            Cursor = Cursors.WaitCursor;
            Process.Start(new ProcessStartInfo
            {
                FileName = ResourceManager.HDiffPath,
                Arguments = $"-s-64 -c-zstd-21-24 -d \"{originalFile}\" \"{patchedFile}\" \"{deltaFile}\"",
                CreateNoWindow = true
            })?.WaitForExit();

            if (!File.Exists(deltaFile))
            {
                Cursor = Cursors.Arrow;
                MessageBox.Show($"File Create failed [DELTA]: {deltaFile}");
                return;
            }

            MessageBox.Show($"File created [DELTA]: {deltaFile}");
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            ResourceManager.CleanupTempDir();
        }
    }
}
