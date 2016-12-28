using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VideoSafer
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void browseButton_Click(object sender, EventArgs e)
        {
            var dr = folderBrowserDialog1.ShowDialog();
            if (dr == System.Windows.Forms.DialogResult.OK)
            {
                this.baseFolderTextBox.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void convertButton_Click(object sender, EventArgs e)
        {
            string baseFolder = this.baseFolderTextBox.Text;
            if (string.IsNullOrWhiteSpace(baseFolder))
            {
                MessageBox.Show("Please input an base folder.");
                return;
            }
            if (!Directory.Exists(baseFolder))
            {
                MessageBox.Show("Base folder does not exists");
                return;
            }
            if (string.IsNullOrWhiteSpace(includeTextBox.Text))
            {
                includeTextBox.Text = "*";
            }

            convertButton.Enabled = false;
            convertButton.Text = "Converting...";
            backgroundWorker1.RunWorkerAsync();
        }

        /// <summary>
        /// Converts a wildcard to a regex.
        /// </summary>
        /// <param name="pattern">The wildcard pattern to convert.</param>
        /// <returns>A regex equivalent of the given wildcard.</returns>
        private static string WildcardToRegex(string pattern)
        {
            //. 为正则表达式的通配符，表示：与除 \n 之外的任何单个字符匹配。
            //* 为正则表达式的限定符，表示：匹配上一个元素零次或多次
            //? 为正则表达式的限定符，表示：匹配上一个元素零次或一次
            return "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            backgroundWorker1.ReportProgress(0);
            string baseFolder = this.baseFolderTextBox.Text;
            var searchOption = this.recursiveCheckBox.Checked ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var videoFiles = Directory.GetFiles(baseFolder, includeTextBox.Text, searchOption);
            if (!string.IsNullOrWhiteSpace(excludeTextBox.Text))
            {
                var excludePattern = WildcardToRegex(this.excludeTextBox.Text.ToLowerInvariant());
                videoFiles = videoFiles.Where(f => !Regex.IsMatch(f.ToLowerInvariant(), excludePattern)).ToArray();
            }
            for (int i = 0; i < videoFiles.Length; ++i)
            {
                var videoFile = videoFiles[i];
                this.conversionProgressLabel.Text = "Converting " + videoFile + "...";
                backgroundWorker1.ReportProgress((i + 1) * 50 / videoFiles.Length, Path.GetFileName(videoFile));
                if (videoFile.Contains(".safe."))
                {
                    continue;
                }
                var safeVideoFile = Path.Combine(baseFolder, Path.GetFileNameWithoutExtension(videoFile) + ".safe" + Path.GetExtension(videoFile));
                Console.WriteLine("Copy {0} to {1}", videoFile, safeVideoFile);
                File.Copy(videoFile, safeVideoFile, true);
                backgroundWorker1.ReportProgress(i * 100 / videoFiles.Length);
                File.AppendAllText(safeVideoFile, new string('\0', Convert.ToInt32(this.paddingNumBox.Value)));
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            conversionProgressBar.Value = (conversionProgressBar.Maximum - conversionProgressBar.Minimum) * e.ProgressPercentage / 100;
            conversionProgressLabel.Text = "Converting " + e.UserState as string + "...";
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.ToString());
            }
            else
            {
                conversionProgressBar.Value = conversionProgressBar.Maximum;
                conversionProgressLabel.Text = "Conversion done successfully.";
            }
            convertButton.Enabled = true;
            convertButton.Text = "Convert";
        }
    }
}
