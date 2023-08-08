using System.Diagnostics;

namespace WebpCompression
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            this.InitializeComponent();
        }

        private void buttonSelectFiles_Click(object sender, EventArgs e)
        {
            this.openFileDialog1.ShowDialog();

            var images = this.openFileDialog1.FileNames;

            this.dataGridView1.DataSource = images.Select(o => new ImageInfo { Name = o }).ToArray();
        }

        private void buttonOptimize_Click(object sender, EventArgs e)
        {
            var imageInfos = this.dataGridView1.DataSource as ImageInfo[];
            foreach (var imageInfo in imageInfos)
            {
                var fileInfo = new FileInfo(imageInfo.Name);
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(imageInfo.Name);
                var newFileName = $"{fileNameWithoutExtension}.webp";

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = @"bin\cwebp.exe",
                    Arguments = $"\"{imageInfo.Name}\" -o {newFileName}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new Process();
                process.StartInfo = processStartInfo;
                process.OutputDataReceived += this.Process_OutputDataReceived;
                process.ErrorDataReceived += this.Process_OutputDataReceived;

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit(2000);
                var exitCode = process.ExitCode;

                var originalSize = fileInfo.Length;
                var newFileInfo = new FileInfo($"{newFileName}");

                if (!newFileInfo.Exists)
                {
                    return;
                }

                var compression = Math.Round(100 - (100.0 * newFileInfo.Length / originalSize));
                imageInfo.CompressionInfo = $"-{compression}%";

                process.ErrorDataReceived -= this.Process_OutputDataReceived;
                process.OutputDataReceived -= this.Process_OutputDataReceived;
            }

            //this.dataGridView1.DataSource = null;
            this.dataGridView1.DataSource = imageInfos;
            this.dataGridView1.Refresh();
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            this.textBoxLog.Invoke((MethodInvoker)delegate
            {
                if (e.Data == null)
                {
                    return;
                }

                this.textBoxLog.Text += $"{e.Data.Trim()}\r\n";
            });
        }
    }
}