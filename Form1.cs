using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tesseract;

namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            EXTENSIONS = new string[]{ ".jpeg", ".jpg", ".JPEG", ".png", ".PNG",".bmp" };
            LoadPaths();
            TessEngine = new TesseractEngine(@"./tessdata", "kaz", EngineMode.Default);
        }

        public DirectoryInfo inputDir; 
        public string[] EXTENSIONS;
        public string currentImagePath;
        public Bitmap procImage;
        public TesseractEngine TessEngine;

        public void LoadPaths() //загрузка путей из Properties 
        {
            textBox1.Text = Properties.Settings.Default.inputPath;
            textBox2.Text = Properties.Settings.Default.outputPath;
            if (!string.IsNullOrEmpty(textBox1.Text)) {
                inputDir = new DirectoryInfo(textBox1.Text);
                LoadFilesFromDirectory();
            }
        }

        public void LoadFilesFromDirectory()    //отображение списка файлов из input папки
        {
            treeView1.Nodes.Clear();
            DirectoryInfo dir = new DirectoryInfo(Properties.Settings.Default.inputPath);
            foreach (FileInfo file in dir.GetFiles())
            {   
                if (EXTENSIONS.Contains(file.Extension))
                {
                    treeView1.Nodes.Add(file.Name);
                }
            }
        }

        private void Button2_Click(object sender, EventArgs e)  //кнопка input папки
        {
            DialogResult res = folderBrowserDialog1.ShowDialog();
            if(res == DialogResult.OK)
            {
                textBox1.Text = folderBrowserDialog1.SelectedPath;
                Properties.Settings.Default.inputPath = folderBrowserDialog1.SelectedPath;
                Properties.Settings.Default.Save();
                LoadFilesFromDirectory();
            }
        }

        private void Button3_Click(object sender, EventArgs e)  //кнопка output папки
        {
            DialogResult res = folderBrowserDialog1.ShowDialog();
            if (res == DialogResult.OK)
            {
                textBox2.Text = folderBrowserDialog1.SelectedPath;
                Properties.Settings.Default.outputPath = folderBrowserDialog1.SelectedPath;
                Properties.Settings.Default.Save();
            }
        }

        //смена картинки
        private void TreeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            currentImagePath = inputDir.FullName + "\\" + e.Node.Text;
            pictureBox1.Image = Bitmap.FromFile(currentImagePath);
            pictureBox2.Image = process(currentImagePath);
        }

        public Bitmap process(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                Image<Bgr, Byte> img = new Image<Bgr, Byte>(path);
                Image<Gray, Byte> grayImage = img.Convert<Gray, Byte>();
                Image<Gray, Byte> result = new Image<Gray, Byte>(img.Width, img.Height);
                double CannyAccThresh = 0;
                if (checkBox1.Checked)
                {
                    CannyAccThresh = CvInvoke.Threshold(grayImage, result, this.trackBar1.Value, 255, Emgu.CV.CvEnum.ThresholdType.Binary);
                }
                if (checkBox3.Checked)
                    CannyAccThresh = CvInvoke.Threshold(grayImage, result, 100, 255, Emgu.CV.CvEnum.ThresholdType.Otsu);
                if (checkBox2.Checked)
                {
                    Image<Gray, Byte> im1 = result.SmoothGaussian(trackBar2.Value);
                    double CannyThresh = 0.1 * CannyAccThresh;
                    result = im1.Canny(CannyThresh, CannyAccThresh);
                }
                procImage = result.Bitmap;
                return result.Bitmap;
            }
            else return null;
        }

        private void TrackBar1_ValueChanged(object sender, EventArgs e)
        {
            pictureBox2.Image = process(currentImagePath);
            numericUpDown1.Value = trackBar1.Value;
        }

        private void NumericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            trackBar1.Value = (int)numericUpDown1.Value;
        }

        private void TrackBar2_ValueChanged(object sender, EventArgs e)
        {
            numericUpDown2.Value = trackBar2.Value;
            pictureBox2.Image = process(currentImagePath);
        }

        private void NumericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            trackBar2.Value = (int)numericUpDown2.Value;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            foreach (TreeNode node in treeView1.Nodes)
            {
                string imgPath = inputDir.FullName + "\\" + node.Text;
                Bitmap img = process(imgPath);
                img.Save(Properties.Settings.Default.outputPath + "\\" + node.Text);
            }
        }

        private void Label2_Click(object sender, EventArgs e)
        {
            Process.Start("explorer", Properties.Settings.Default.outputPath);
        }

        private void Label1_Click(object sender, EventArgs e)
        {
            Process.Start("explorer", Properties.Settings.Default.inputPath);
        }

        public Tuple<double, string> GetText(Bitmap imgsource)
        {   
            string ocrtext = string.Empty;
            double conf = 0;
            Pix img = PixConverter.ToPix(imgsource);
            Page page = TessEngine.Process(img);
            ocrtext = page.GetText();
            conf = page.GetMeanConfidence();
            page.Dispose();
            
            return new Tuple<double,string> (conf, ocrtext);
        }

        private void Button5_Click(object sender, EventArgs e)
        {
            Thread run = new Thread(PROCESS);
            run.Start();
        }

        public void PROCESS()
        {
            richTextBox1.Invoke((MethodInvoker)delegate {
                this.Cursor = Cursors.WaitCursor;
            });
            Tuple<double, string> data = GetText(procImage);
            richTextBox1.Invoke((MethodInvoker)delegate {
                richTextBox1.Text = data.Item2;
                label5.Text = data.Item1.ToString();
                this.Cursor = Cursors.Arrow;
            });
        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                trackBar1.Enabled = true;
                numericUpDown1.Enabled = true;
                checkBox3.Checked = false;
            }
            else
            {
                trackBar1.Enabled = false;
                numericUpDown1.Enabled = false;
                checkBox2.Checked = false;
            }
        }

        private void CheckBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                trackBar2.Enabled = true;
                numericUpDown2.Enabled = true;
                checkBox1.Checked = true;
                numericUpDown1.Value = 85;
            }
            else
            {
                trackBar2.Enabled = false;
                numericUpDown2.Enabled = false;
                checkBox1.Checked =false;
            }
        }

        private void PictureBox1_DoubleClick(object sender, EventArgs e)
        {
            Process.Start(currentImagePath);
        }

        private void CheckBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3.Checked)
            {
                checkBox1.Checked = false;
                pictureBox2.Image = process(currentImagePath);
            }
        }
    }
}
