using System.Diagnostics;

namespace WebpCompression
{
    public partial class Form1 : Form
    {
        private string _processPath;

        public Form1()
        {
            this.InitializeComponent();

            var binaryDirectory = "bin";

            if (!Directory.Exists(binaryDirectory))
            {
                Directory.CreateDirectory(binaryDirectory);
            }

            this._processPath = Path.Combine(binaryDirectory, "cwebp.exe");
            if (!File.Exists(this._processPath))
            {
                MessageBox.Show("Cannot found cwebp.exe, please download and copy into bin directory", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonSelectFiles_Click(object sender, EventArgs e)
        {
            var dialogResult = this.openFileDialog1.ShowDialog();
            if (dialogResult != DialogResult.OK)
            {
                return;
            }

            var images = this.openFileDialog1.FileNames;

            this.dataGridView1.DataSource = images.Select(o => new ImageInfo { Name = o }).ToArray();
        }

        private void buttonOptimize_Click(object sender, EventArgs e)
        {
            var imageSizes = new int[] { 2560, 1920, 1080 };

            var imageInfos = this.dataGridView1.DataSource as ImageInfo[];
            if (imageInfos == null)
            {
                return;
            }

            foreach (var imageInfo in imageInfos)
            {
                foreach (var imageSize in imageSizes)
                {

                    var fileInfo = new FileInfo(imageInfo.Name);
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(imageInfo.Name);
                    var newFileName = $"{fileNameWithoutExtension}-{imageSize}.webp";

                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = this._processPath,
                        Arguments = $"\"{imageInfo.Name}\" -resize {imageSize} 0 -o \"{newFileName}\"",
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
                    process.WaitForExit(30000);

                    if (process.HasExited)
                    {
                        var exitCode = process.ExitCode;

                        var originalSize = fileInfo.Length;
                        var newFileInfo = new FileInfo($"{newFileName}");

                        if (!newFileInfo.Exists)
                        {
                            return;
                        }

                        var compression = Math.Round(100 - (100.0 * newFileInfo.Length / originalSize));
                        imageInfo.CompressionInfo = $"-{compression}%";
                    }
                    else
                    {
                        MessageBox.Show("cwebp.exe timeout");
                    }

                    process.ErrorDataReceived -= this.Process_OutputDataReceived;
                    process.OutputDataReceived -= this.Process_OutputDataReceived;
                }
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

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            var paths = new List<string>();

            foreach (string path in (string[])e.Data.GetData(DataFormats.FileDrop))
            {
                paths.Add(path);
            }

            this.dataGridView1.DataSource = paths.Select(o => new ImageInfo { Name = o }).ToArray();
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }
    }
}