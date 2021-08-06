using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpertroApp
{
    public partial class HoughCircle_Check : Form
    {
        public string path = "";
        public string Radius = "----";
        private Bitmap Goal_Image;
        private Bitmap Goal_Image_After_Dlite;
        public Bitmap Return_Image = null;
        private Form1 f1;
        public HoughCircle_Check(Form1 form)
        {
            InitializeComponent();
            f1 = form;
        }

        private void HoughCircle_Check_Load(object sender, EventArgs e)
        {
            Goal_Image_After_Dlite = f1.Goal_Picture_after_Dlite;
            Goal_Image = f1.Goabal_Picture;
            pictureBox1.Image = Goal_Image;
            textBox1.Text = trackBar1.Value.ToString();
            textBox2.Text = trackBar2.Value.ToString();
            textBox3.Text = trackBar3.Value.ToString();
        }

        private void trackBar1_MouseUp(object sender, MouseEventArgs e)
        {
            // pictureBox1.Image.Dispose();
            if (checkBox1.Checked)
            {
                textBox1.Text = trackBar1.Value.ToString();
                Bitmap a0 = Method.FindCircle_Eddy(Goal_Image_After_Dlite, trackBar1.Value, trackBar2.Value, trackBar3.Value);
                Radius = Method.Radius_of_Circle.ToString();
                label4.Text = Radius;
                pictureBox1.Image = a0;
            }
            else
            {
                textBox1.Text = trackBar1.Value.ToString();
                Bitmap a = Method.FindCircle_Eddy(Goal_Image, trackBar1.Value, trackBar2.Value, trackBar3.Value);
                Radius = Method.Radius_of_Circle.ToString();
                label4.Text = Radius;
                pictureBox1.Image = a;
            }
        }

        private void trackBar2_MouseUp(object sender, MouseEventArgs e)
        {
            //  pictureBox1.Image.Dispose();
            if (checkBox1.Checked)
            {
                textBox2.Text = trackBar2.Value.ToString();
                Bitmap a0 = Method.FindCircle_Eddy(Goal_Image_After_Dlite, trackBar1.Value, trackBar2.Value, trackBar3.Value);
                Radius = Method.Radius_of_Circle.ToString();
                label4.Text = Radius;
                pictureBox1.Image = a0;
            }
            else
            {
                textBox2.Text = trackBar2.Value.ToString();
                Bitmap a = Method.FindCircle_Eddy(Goal_Image, trackBar1.Value, trackBar2.Value, trackBar3.Value);
                Radius = Method.Radius_of_Circle.ToString();
                label4.Text = Radius;
                pictureBox1.Image = a;
            }
        }

        private void trackBar3_MouseUp(object sender, MouseEventArgs e)
        {
            // pictureBox1.Image.Dispose();
            if (checkBox1.Checked)
            {
                textBox3.Text = trackBar3.Value.ToString();
                Bitmap a0 = Method.FindCircle_Eddy(Goal_Image_After_Dlite, trackBar1.Value, trackBar2.Value, trackBar3.Value);
                Radius = Method.Radius_of_Circle.ToString();
                label4.Text = Radius;
                pictureBox1.Image = a0;
            }
            else
            {
                textBox3.Text = trackBar3.Value.ToString();
                Bitmap a = Method.FindCircle_Eddy(Goal_Image, trackBar1.Value, trackBar2.Value, trackBar3.Value);
                Radius = Method.Radius_of_Circle.ToString();
                label4.Text = Radius;
                pictureBox1.Image = a;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void tableLayoutPanel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            path = @"量測結果\" + f1.File_Name + @"\" + "HoughCircle_Image" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm") + ".jpg";
            //Return_Image = (Bitmap)pictureBox1.Image;
            pictureBox1.Image.Save(path);
            MessageBox.Show("存檔完成");
            button1.Enabled = true;
        }
    }
}
