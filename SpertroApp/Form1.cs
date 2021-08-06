using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitHub.secile.Video;
using System.Runtime.InteropServices;
using System.IO;
using System.Drawing.Imaging;
using System.Threading;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;


namespace SpertroApp
{

    public partial class Form1 : Form
    {
        #region 全域參數
        //===============================================================
        public int DilteNumber = 3;
        public bool isBlankMode = false;
        public Bitmap Blank;
        public bool isGetBlank = false;
        public bool isAutoScalingEnd = false;
        public bool isROI_Revision = false;
        public int ROI_X_Save = 0;
        private bool isROIok = false;
        private bool isReadyToCaculateBeamIntensity = false;
        private bool isGetBeamIntensity = false;
        public List<double> Beam_Intensity = new List<double>();
        public bool isMeasureEnd = true;
        public string Image_path = "";
        public string Radius = "--";
        public string File_Name = "";
        private bool isHoughCorrect = false;
        private Point Center_of_Counter;
        public Bitmap Goal_Picture_after_Dlite;
        public Bitmap New_ROI_Picture;
        public Bitmap Goabal_Picture;
        public Bitmap FullImage;
        public Bitmap RoiImage;
        public  Bitmap HoughCircle_Image;
        public Bitmap Center_Image;
        #region AutoScaling
        public int Scaling_Times = 0;
        public int back_number = 0;
        public bool isGetROI = false;
        delegate void Dg_Update(int dg);
        delegate void Gamma_Update(int gamma);
        delegate void Back_Update(int back);
        delegate void set_camera_prop_dele(string item, int prop_num);
       // List<double> result_buffer = new List<double>();
        List<double> dg_set = new List<double>();
        List<double> Max_Intensity = new List<double>();
        public string final_dg = "";
        public string final_gamma = "";
        public string final_back = "";
        public double Index_of_Max = 0;
        public double dark_avg = 0;
        #endregion
        #region Beam_wast
        List<Point> Goabal = new List<Point>();
        private bool isGetBeamPoint = false;
        #endregion
        #region 比例參數
        private static int scaleX = 1;
        private static int scaleY = 1;
        #endregion
        #region Roi
        private static int ROI_Xx = 0;
        private static int ROI_Yx = 0;
        private static int ROI_Wx = 1280;
        private static int ROI_Hx = 20;
        private static int ROI_Xy = 0;
        private static int ROI_Yy = 0;
        private static int ROI_Wy = 20;
        private static int ROI_Hy = 960;
        public static int ROI_3_X = 0;
        public static int ROI_3_Y = 0;
        #endregion
        //===============================================================
        #region Intensity
        public List<double> RealTime_Original_Intensity = new List<double>();
        public List<double> Dark_Intensity = new List<double>();
        private bool isGetDarkIntensity = false;
        #endregion
        //===============================================================
        //步驟控制 iTask
        private static int iTask = 999999;
        //Camera
        UsbCamera camera;
        //Live Stop
        public bool bCameraLive = false;
        //ROI
        Point ROI_Point;
        const int ROI_Edge = 10;
        private object BufferLock = new object();
        #endregion
        private Bitmap CopyBitmap(byte[] Buffer,int width, int height)
        {
            
            var result = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            if (Buffer == null) return result;

            var bmp_data = result.LockBits(new Rectangle(Point.Empty, result.Size), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            lock (BufferLock)
            {
                // copy from last row.
                for (int y = 0; y < height; y++)
                {
                    int stride = width* 3;

                    var src_idx = Buffer.Length - (stride * (y + 1));
                    var dst = IntPtr.Add(bmp_data.Scan0, stride * y);
                    
                    Marshal.Copy(Buffer, src_idx, dst, stride);

                }
            }
            result.UnlockBits(bmp_data);

            return result;
        }

        //convert image to bytearray
        public static byte[] ImageToBuffer(Image Image, System.Drawing.Imaging.ImageFormat imageFormat)
        {
            if (Image == null) { return null; }
            byte[] data = null;
            using (MemoryStream oMemoryStream = new MemoryStream())
            {
                //建立副本
                using (Bitmap oBitmap = new Bitmap(Image))
                {
                    //儲存圖片到 MemoryStream 物件，並且指定儲存影像之格式
                    oBitmap.Save(oMemoryStream, imageFormat);
                    //設定資料流位置
                    oMemoryStream.Position = 0;
                    //設定 buffer 長度
                    data = new byte[oMemoryStream.Length];
                    //將資料寫入 buffer
                    oMemoryStream.Read(data, 0, Convert.ToInt32(oMemoryStream.Length));
                    //將所有緩衝區的資料寫入資料流
                    oMemoryStream.Flush();
                }
            }
            return data;
        }
        //======================刷新控件(EX:TextBox的文字等等)======================
        delegate void FormUpdata(int ROI_Yx, List<Point> Goabal,string Radius,List<double> Beam_Intensity);
        delegate void FormUpdata2(Bitmap p0, Bitmap p1, Bitmap p2);
        delegate void FormUpdata3(Bitmap p0, Bitmap p1, Bitmap Goal_Image, Bitmap Center_Image, Bitmap Hough_Image, List<Point> Goabal);
        delegate void FormUpdata4(Bitmap p1,string Radius);
        void FormUpdataMethod(int ROI_Yx, List<Point> Goabal,string Radius,List<double> Beam_Intensity)
        {
            DrawCanvas.Top = ROI_Yx;
            label18.Text = Radius;
            if (isAutoScalingEnd)
            {
                List<string> Parameter = new List<string>();
                Parameter.Add("DG: "+label_DG.Text);
                Parameter.Add("AG(gamma): " + label_Gamma.Text);
                Parameter.Add("EXP(Back_Light): " + label_Back_Light.Text);
                File.WriteAllLines(@"量測結果\" + File_Name + @"\" + "Parameter_" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm") + ".txt", Parameter.ToArray());
                isAutoScalingEnd = false;
            }
            if (isROI_Revision)
            {
                button2.Enabled = true;
                button4.Enabled = true;
                button5.Enabled = true;
            }
            if (isReadyToCaculateBeamIntensity)
            {
                textBox2.Text = (((int)Convert.ToDouble(label18.Text) + 1) * 2).ToString();
            }
            if (isGetBeamIntensity)
            {
                label19.Text = Math.Round(Beam_Intensity[2],4).ToString();
                label21.Text = Math.Round((Beam_Intensity[2]/(Convert.ToDouble(textBox13.Text)* Convert.ToDouble(textBox13.Text))),4).ToString();
                label23.Text = Beam_Intensity[0].ToString();
                label28.Text = Math.Round(Math.Pow(Beam_Intensity[0],2),2).ToString();
            }
            if (isMeasureEnd)
            {
                textBox1.Enabled = true;
                textBox5.Enabled = true;
                textBox4.Enabled = true;
                textBox13.Enabled = true;
                textBox3.Enabled = true;
            }
            else 
            {
                textBox1.Enabled = false;
                textBox5.Enabled = false;
                textBox4.Enabled = false;
                textBox13.Enabled = false;
                textBox3.Enabled = false;
            }

            if (isGetBeamPoint)
            {
                int W = Convert.ToInt32(textBox2.Text);
                int H = Convert.ToInt32(textBox2.Text);
                System.Windows.Forms.DataVisualization.Charting.Series seriesP = new System.Windows.Forms.DataVisualization.Charting.Series("BeamWAST", 2000);
                System.Windows.Forms.DataVisualization.Charting.Series seriesMu = new System.Windows.Forms.DataVisualization.Charting.Series("BeamWAST1", 2000);
                //設定座標大小
                this.chart1.ChartAreas[0].AxisY.Minimum = 0;
                this.chart1.ChartAreas[0].AxisY.Maximum = H;
                this.chart1.ChartAreas[0].AxisX.Minimum = 0;
                this.chart1.ChartAreas[0].AxisX.Maximum = W;
                this.chart2.ChartAreas[0].AxisY.Minimum = 0;
                this.chart2.ChartAreas[0].AxisY.Maximum = H * (int)Convert.ToDouble(textBox13.Text);
                this.chart2.ChartAreas[0].AxisX.Minimum = 0;
                this.chart2.ChartAreas[0].AxisX.Maximum = W * (int)Convert.ToDouble(textBox13.Text);
                //==========去格線=========================
                //this.chart1.ChartAreas[0].AxisX.MajorGrid.LineWidth = 0;
                //this.chart1.ChartAreas[0].AxisY.MajorGrid.LineWidth = 0;
                //this.chart2.ChartAreas[0].AxisX.MajorGrid.LineWidth = 0;
                //this.chart2.ChartAreas[0].AxisY.MajorGrid.LineWidth = 0;
                //設定標題
                this.chart1.Titles.Clear();
                this.chart2.Titles.Clear();
                /*
                this.chart1.Titles.Add("S01");
                this.chart1.Titles[0].Text = "Point_of_I/e^2";
                this.chart1.Titles[0].ForeColor = Color.Black;
                this.chart1.Titles[0].Font = new System.Drawing.Font("標楷體", 16F);
                */
                //設定顏色
                seriesP.Color = Color.Blue;
                seriesMu.Color = Color.Blue;

                //設定樣式
                seriesP.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Point;
                seriesMu.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Point;
                //迴圈二
                for (int i = 0; i < Goabal.Count; i++)
                {
                    //給入數據畫圖
                    seriesP.Points.AddXY(Goabal[i].X , Goabal[i].Y);
                    this.chart1.Series.Clear();
                    this.chart1.Series.Add(seriesP);
                    seriesMu.Points.AddXY(Goabal[i].X *(int)Convert.ToDouble(textBox13.Text), Goabal[i].Y*(int)Convert.ToDouble(textBox13.Text));
                    this.chart2.Series.Clear();
                    this.chart2.Series.Add(seriesMu);
                }
                isGetBeamPoint = false;
            }
        }
        void PictureBoxUpdate(Bitmap p0,Bitmap p1,Bitmap p2)
        {
            /*
            if (pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose();
            }
            if (pictureBox2.Image != null)
            {
                pictureBox2.Image.Dispose();
            }
            if (pictureBox3.Image != null)
            {
                pictureBox3.Image.Dispose();
            }
            */
            if (p0 == null)
            {
                pictureBox1.Image = null;
            } 
            else
            {
                pictureBox1.Image = new Bitmap(p0);
                //p0.Dispose();
            }
            if (p1 == null)
            {
                pictureBox2.Image = null;
            }
            else
            {
                pictureBox2.Image = new Bitmap(p1); // 设置本次图片
                //p1.Dispose();
            }
            if (p2 == null)
            {
                pictureBox3.Image = null;
            }
            else
            {
                pictureBox3.Image = new Bitmap(p2); // 设置本次图片
                //p2.Dispose();
            }
        }
        void CircleImageUpdate(Bitmap p1,string Radius)
        {
            if (p1 == null)
            {
                pictureBox2.Image = null;
            }
            else
            {
                pictureBox2.Image = new Bitmap(p1); // 设置本次图片
            }
            label18.Text = Radius;
        }

        void ImageSave(Bitmap FullImage, Bitmap Roi_Image,Bitmap Goal_Image, Bitmap Center_Image,Bitmap Hough_Image, List<Point> Goabal)
        {
            List<string> Save = new List<string>();
            for (int i= 0;i < Goabal.Count; i++)
            {
                Save.Add(Goabal[i].X.ToString() + "," + Goabal[i].Y.ToString());
            }
            File.WriteAllLines(@"量測結果\" + File_Name + @"\" + "Point_" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm")+".txt",Save.ToArray());
            FullImage.Save(@"量測結果\" + File_Name + @"\" + "FullImage"+ DateTime.Now.ToString("yyyy-MM-dd-HH-mm") + ".jpg");
            Roi_Image.Save(@"量測結果\" + File_Name + @"\" + "RoiImage"+ DateTime.Now.ToString("yyyy-MM-dd-HH-mm") + ".jpg");
            Goal_Image.Save(@"量測結果\" + File_Name + @"\" + "BeamWaist_Image" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm") + ".jpg");
            Center_Image.Save(@"量測結果\" + File_Name + @"\" + "Center_Image" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm") + ".jpg");
                if (isHoughCorrect)
            {
                Hough_Image.Save(@"量測結果\" + File_Name + @"\" + "HoughCircle_Image" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm") + ".jpg");
            }
            chart1.SaveImage(@"量測結果\" + File_Name + @"\" + "(Pixel)Result_Image" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm") + ".jpg",ImageFormat.Bmp);
            chart2.SaveImage(@"量測結果\" + File_Name + @"\" + "(Mu)Result_Image" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm") + ".jpg", ImageFormat.Bmp);
        }
        private async Task<bool> SpertroImageProcess(byte[] FULLimgbuffer)
        {
            await Task.Run(() =>
            {
                FormUpdata formcontrl = new FormUpdata(FormUpdataMethod);
                FormUpdata2 formcontrl2 = new FormUpdata2(PictureBoxUpdate);
                FormUpdata3 formcontrl3 = new FormUpdata3(ImageSave);
                FormUpdata4 formcontrl4 = new FormUpdata4(CircleImageUpdate);
                //=========================程式的主要區域(每個步驟)===========================
                #region 主要區域
                switch (iTask)
                {
                    case 0:
                        MessageBox.Show("測量結束");
                        isMeasureEnd = true;
                        iTask = 99999999;
                        break;
                    case 1:
                        if (isBlankMode == false)
                        {
                            iTask = 3;
                        }
                        else
                        {
                            DialogResult re0 = MessageBox.Show("請確認關閉燈光後，按下'Yes'擷取Blank", "擷取Blank", MessageBoxButtons.YesNo);
                            if (re0 == DialogResult.Yes)
                            {
                                iTask = 2;
                            }
                        }
                        break;
                   case 2:
                        if (isGetBlank == false)
                        {
                            Blank = Method.BufferToBitmap(FULLimgbuffer);
                            isGetBlank = true;
                        }
                        DialogResult re = MessageBox.Show("請確認開啟燈光後，按下'Yes'繼續量測","開啟光源",MessageBoxButtons.YesNo);
                        if (re == DialogResult.Yes)
                        {
                            iTask = 3;
                        }
                        break;
                    case 3:
                        Set_Dg(150);
                        Set_Gamma(100);
                        Set_Back(2);
                        iTask = 5;
                        Thread.Sleep(500);
                        break;

                    case 5:
                        if (isGetDarkIntensity == false)
                        {
                            Dark_Intensity = RealTime_Original_Intensity;
                            dark_avg = Dark_Intensity.Average();
                            isGetDarkIntensity = true;
                        }
                        iTask = 50;
                        break;

                    case 50:
                        Bitmap Image_0 = Method.BufferToBitmap(FULLimgbuffer);
                        IDictionary<string, int> ROI_x = Method.RoiScan_X(Image_0);
                        ROI_Xx = ROI_x["x"]; ROI_Yx = ROI_x["y"]; ROI_Wx = ROI_x["w"]; ROI_Hx = ROI_x["h"];
                        IDictionary<string, int> ROI_y = Method.RoiScan_Y(Image_0);
                        ROI_Xy = ROI_y["x"]; ROI_Yy = ROI_y["y"]; ROI_Wy = ROI_y["w"]; ROI_Hy = ROI_y["h"];
                        isGetROI = true;
                        ROI_X_Save = ROI_Xy;
                        iTask = 100;
                        break;

                    case 100:
                        Set_Dg(220);
                        Set_Gamma(100);
                        if (back_number > 3)
                        {
                            MessageBox.Show("請更換減光片");
                            iTask = 0;

                        }
                        else
                        {
                            Set_Back(back_number);
                            iTask = 150;
                        }                     
                        Thread.Sleep(1000);
                        break;

                    case 150:
                        Thread.Sleep(1500);
                        dg_set.Clear();
                        Max_Intensity.Clear();
                        //Step 0 找到最大值的位置 紀錄此時的"dg,max",index
                        List<double> result_buffer = Smart_Calibrate_DG_Intensity(RealTime_Original_Intensity, Index_of_Max); //輸出一個dg值
                        if (result_buffer[1] >= 250)// - dark_avg)
                        {
                            iTask = 150;
                            Index_of_Max = 0;
                            Scaling_Times++;
                            if (result_buffer[0] == 32 && Scaling_Times >=2)
                            {
                                iTask = 0;
                                MessageBox.Show("DG以達最低");
                            }
                        }
                        else
                        {
                            dg_set.Add(result_buffer[0]);
                            Max_Intensity.Add(result_buffer[1]);
                            Index_of_Max = result_buffer[2];
                            //  Set_Dg_half();//-------------
                            iTask = 200;
                        }
                        Thread.Sleep(3000);
                        break;

                    case 200:
                        double max_ = Convert.ToDouble(textBox1.Text);
                        int Goal_intensity = Convert.ToInt32(256 * (max_ / 100));
                        //Step 1 根據最大值的位置 找到dg/2後的dg,"max",index
                        List<double> result_buffer2 = Smart_Calibrate_DG_Intensity(RealTime_Original_Intensity, Index_of_Max); //輸出一個dg值
                        dg_set.Add(result_buffer2[0]);
                        Max_Intensity.Add(result_buffer2[1]);

                        List<double> Auto_Scaling_Coef = Method.Polynomial_Fitting(Max_Intensity, dg_set, 1);

                        int show_new_dg = (int)Math.Round((Auto_Scaling_Coef[0] + Auto_Scaling_Coef[1] * Goal_intensity),2);
                        if (show_new_dg >= 254)// - dark_avg)
                        {
                            back_number++;
                            Index_of_Max = 0;
                            iTask = 100;
                        }
                        else
                        {
                            if (show_new_dg < 32)
                            {
                                Set_Dg(32);
                            }
                            else
                            { Set_Dg(show_new_dg); }
                            isAutoScalingEnd = true;
                            iTask = 230;
                        }
                        break;

                    case 215:
                        MessageBox.Show("請調整ROI位置，完成後請按繼續運算");
                        isROI_Revision = true;
                        iTask = 9999999;
                        break;

                    case 230:
                        Bitmap Image_ = Method.BufferToBitmap(FULLimgbuffer);
                        New_ROI_Picture = Method.Get_new_Picture(Image_,ROI_Xy);
                        iTask = 250;
                        ROI_Xy = ROI_X_Save;
                        break;

                    case 250:
                         Bitmap Image_1 = Method.BufferToBitmap(FULLimgbuffer);
                         Rectangle cloneRect = new Rectangle(ROI_3_X, ROI_3_Y, Convert.ToInt32(textBox2.Text), Convert.ToInt32(textBox2.Text));
                         System.Drawing.Imaging.PixelFormat format = Image_1.PixelFormat;
                         Bitmap cloneBitmap = Method.crop(Image_1, cloneRect);
                      /*  Rectangle cloneRect = new Rectangle(ROI_3_X, ROI_3_Y, Convert.ToInt32(textBox2.Text), Convert.ToInt32(textBox2.Text));
                        System.Drawing.Imaging.PixelFormat format = New_ROI_Picture.PixelFormat;
                        Bitmap cloneBitmap = Method.crop(New_ROI_Picture, cloneRect);*/
                        //======================================================================
                        Goabal = Method.Get_Goabal_Pixel(cloneBitmap,Convert.ToDouble(textBox4.Text), dark_avg);
                        Goabal_Picture = Method.Get_Goabal_Picture(cloneBitmap, Convert.ToDouble(textBox4.Text), dark_avg);
                        isGetBeamPoint = true;
                        iTask = 300;
                        break;

                    case 300:
                        Bitmap for_FindCenter = Method.Dlite(Goabal_Picture, DilteNumber);
                        Goal_Picture_after_Dlite = new Bitmap(for_FindCenter);
                        HoughCircle_Image = Method.FindCircle_Eddy(Goabal_Picture, 1, 1, 10);
                        Center_Image = Method.Find_Center_Point(for_FindCenter);
                        if(Method.isGetCenter)
                        { 
                        ROI_Yx = ROI_3_Y + Method.Center_of_Counter.Y;
                        ROI_Xy = ROI_3_X + Method.Center_of_Counter.X;
                        }
                        Radius = Method.Radius_of_Circle.ToString();
                        iTask = 310;
                        break;

                    case 310:
                        this.Invoke(formcontrl2, Goabal_Picture, HoughCircle_Image, Center_Image);
                        iTask = 330;
                        break;

                    case 330:
                        DialogResult isCheckCirle = MessageBox.Show("是否找出正確的圓" + "\r\n" + "若沒有，請點擊" + @"""NO""" + "\r\n" + "若有找出正確的圓，請點擊" + @"""YES""", "是否手動找圓", MessageBoxButtons.YesNo);
                        if (isCheckCirle.ToString() == "No")
                        {
                            HoughCircle_Check Hc = new HoughCircle_Check(this);
                            Hc.ShowDialog();
                            if (Hc.button1.DialogResult.ToString() == "OK")
                            {
                                Radius = Hc.label4.Text;
                                Image_path = Hc.path;
                            }
                        }
                        else { isHoughCorrect = true; }
                        iTask = 340;
                        break;

                    case 340:
                        HoughCircle_Image = new Bitmap(Image_path);
                        this.Invoke(formcontrl4, HoughCircle_Image, Radius);
                        iTask = 350;
                        break;

                    case 350:
                        FullImage = Method.BufferToBitmap(FULLimgbuffer);
                        Rectangle cloneRect2 = new Rectangle(ROI_3_X, ROI_3_Y, Convert.ToInt32(textBox2.Text), Convert.ToInt32(textBox2.Text));
                        System.Drawing.Imaging.PixelFormat format2 = FullImage.PixelFormat;
                        RoiImage = Method.crop(FullImage, cloneRect2);
                        this.Invoke(formcontrl3, FullImage, RoiImage, Goabal_Picture, Center_Image, HoughCircle_Image, Goabal);
                        FullImage.Dispose();
                        RoiImage.Dispose();
                        isReadyToCaculateBeamIntensity = true;
                        iTask = 400;
                        break;

                    case 400:
                        Bitmap full_Image = Method.BufferToBitmap(FULLimgbuffer);
                        Rectangle cloneRect3 = new Rectangle(ROI_3_X, ROI_3_Y, Convert.ToInt32(textBox2.Text), Convert.ToInt32(textBox2.Text));
                        System.Drawing.Imaging.PixelFormat format3 = full_Image.PixelFormat;
                        Bitmap cloneBitmap2 = Method.crop(full_Image, cloneRect3);
                        Beam_Intensity = Method.Get_BeamNumber(cloneBitmap2, dark_avg);
                        isGetBeamIntensity = true;
                        iTask = 0;
                        break;

                }
                #endregion
                //============================================================================
                this.Invoke(formcontrl, Convert.ToInt32(ROI_Yx / scaleY), Goabal, Radius, Beam_Intensity);
            });

            return true;
        }

        public Form1()
        {
            InitializeComponent();
        }
        
        private void btnStart_Click(object sender, EventArgs e)
        {
          

            if (bCameraLive == false)
            {
                set_camera_prop("exp", -2);
                set_camera_prop("bright", 500);
                set_camera_prop("con", 0);

                set_camera_prop("hue", -2000);
                set_camera_prop("sat", 0);
                set_camera_prop("sharp", 1);
                //gamma
                set_camera_prop("white", 2800);
                //back
                //gain
                set_camera_prop("gain", 150);

                set_camera_prop("gamma", 100);
                set_camera_prop("back", 2);

                load_Matlab();

                bCameraLive = true;
                camera.Start();
                
                //var bmp = camera.GetBitmap();
                btnStart.Text = "Stop";
                // show image in PictureBox.
                timer1.Start();

            }
            else
            {
                bCameraLive = false;
                btnStart.Text = "Start";
                timer1.Stop();
                camera.Stop();
            }
        }
        private static int inTimer = 0;
        
        private async void timer1_Tick(object sender, EventArgs e)
        {
            //========================Timer1用來執行:定時取像(刷影像)，畫圖============================
            
            #region 每N秒取像，畫圖
            Bitmap myBitmap = camera.GetBitmap();
            myBitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);//影像轉向 才能符合正確光譜圖形
            if (isGetBlank)
            {
                myBitmap = Method.Remove_Blank(myBitmap,Blank);
            }
            scaleX = camera.Size.Width / CCDImage.Width;
            scaleY = camera.Size.Height / CCDImage.Height;
            int x = DrawCanvas.Left * scaleX;
            int y = DrawCanvas.Top * scaleY;
            int w = DrawCanvas.Width * scaleX;
            int h = DrawCanvas.Height * scaleY;
            Rectangle cloneRect = new Rectangle(x, y, w, h);
            System.Drawing.Imaging.PixelFormat format = myBitmap.PixelFormat;
            Bitmap cloneBitmap = Method.crop(myBitmap, cloneRect);
            //Bitmap cloneBitmap = myBitmap.Clone(cloneRect, format);
            //Bitmap cloneBitmap_For_Pbox = myBitmap.Clone(cloneRect, format);
            //CCDImage.Image = myBitmap;
            //ROIImage.Image = cloneBitmap;          
            ROIImage.Left = DrawCanvas.Left;
            ROIImage.Top = DrawCanvas.Top;
            ROIImage.Height = DrawCanvas.Height;
            ROIImage.Width = DrawCanvas.Width;
            byte[] FULLBuffer = ImageToBuffer(myBitmap, System.Drawing.Imaging.ImageFormat.Bmp);
            byte[] ROIBuffer  = ImageToBuffer(cloneBitmap, System.Drawing.Imaging.ImageFormat.Bmp);
            displayOriginal(ROIBuffer, 0);

            #region 畫ROI矩形
            if (isGetROI)
            {
                int R = 0;
                /* m_PictureBox.Image = bmp; //<====影像
                 Bitmap bmp_for_draw = new Bitmap(m_PictureBox.Image);*/
                Graphics g = Graphics.FromImage(myBitmap);
                Rectangle rect1 = new Rectangle(ROI_Xx, ROI_Yx, ROI_Wx, ROI_Hx);
                Rectangle rect2 = new Rectangle(ROI_Xy, ROI_Yy, ROI_Wy, ROI_Hy);
                try
                {
                    R = Convert.ToInt32(textBox2.Text) / 2;//設定矩形3半徑
                }
                catch{ }
                ROI_3_X = (ROI_Xy + ROI_Wy / 2) - R; //(ROI_Xy+ ROI_Wy/2 )用意為將x移至正中心
                ROI_3_Y = (ROI_Yx + ROI_Hx / 2) - R;
                int new_width = R * 2;//矩形3的寬度
                Rectangle rect3 = new Rectangle(ROI_3_X, ROI_3_Y, new_width, new_width);
                g.DrawRectangle(new Pen(Color.White, 2), rect1);
                g.DrawRectangle(new Pen(Color.White, 2), rect2);
                g.DrawRectangle(new Pen(Color.Yellow, 2), rect3);
            }
            #endregion
            CCDImage.Image = myBitmap;
            ROIImage.Image = cloneBitmap;

            if (Interlocked.Exchange(ref inTimer, 1) == 1)
                return;

            if (ROIBuffer == null)
            {
                Interlocked.Exchange(ref inTimer, 0);
                return;
            }
            Text = "Process...";
            //Image Process
            var processTask1 = SpertroImageProcess(FULLBuffer);
            Task processFinishTask1 = await Task.WhenAny(processTask1);
            Interlocked.Exchange(ref inTimer, 0);
            #endregion
           
        }
        private void Form1_Shown(object sender, EventArgs e)
        {
            int cameraIndex = 0;
            // check format.
            string[] devices = UsbCamera.FindDevices();
            if (devices.Length == 0) return; // no camera.

            UsbCamera.VideoFormat[] formats = UsbCamera.GetVideoFormat(cameraIndex);
            //for (int i = 0; i < formats.Length; i++) Console.WriteLine("{0}:{1}", i, formats[i]);

            // create usb camera and start.
            camera = new UsbCamera(cameraIndex, formats[11]); //1280*960
            Console.WriteLine(formats[8] + "\n" + formats[9] + "\n" + formats[10] + "\n" + formats[11]);
        }

        int iDirectionLock = -1;
        private void DrawCanvas_MouseDown(object sender, MouseEventArgs e)
        {

            PictureBox pDrawROI = (PictureBox)sender;
            

            if (e.Button.Equals(MouseButtons.Left))
            {
                /*
                if (e.X < ROI_Edge)
                {
                    iDirectionLock = 1;
                }
                else */if (e.X > pDrawROI.Width - ROI_Edge)
                {
                    iDirectionLock = 2;
                }
                /*
                else if (e.Y < ROI_Edge)
                {
                    iDirectionLock = 3;
                }
                */
                else if (e.Y > pDrawROI.Height - ROI_Edge)
                {
                    iDirectionLock = 4;
                }
                else 
                {
                    //移動 Top Left

                    iDirectionLock = 0;
                }
                ROI_Point = e.Location;
            }
        }


        private void DrawCanvas_MouseMove(object sender, MouseEventArgs e)
        {

            PictureBox pDrawROI = (PictureBox)sender;
            //bool bEdge = false;
            /*
            if (e.X < ROI_Edge)
            {
                pDrawROI.Cursor = Cursors.PanWest;
            }
            else */if (e.X > pDrawROI.Width - ROI_Edge)
            {
                pDrawROI.Cursor = Cursors.PanEast;
            }
            /*
            else if (e.Y < ROI_Edge)
            {
                pDrawROI.Cursor = Cursors.PanNorth;
            }
            */
            else if (e.Y > pDrawROI.Height - ROI_Edge)
            {
                pDrawROI.Cursor = Cursors.PanSouth;
            }
            else
            {
                pDrawROI.Cursor = Cursors.Cross;
            }
            int X = pDrawROI.Left;
            int Y = pDrawROI.Top;
            int W = pDrawROI.Width;
            int H = pDrawROI.Height; 

            if (e.Button.Equals(MouseButtons.Left))
            {
                if (iDirectionLock == 1)
                {

                    X -= ROI_Point.X - e.Location.X;
                    W += ROI_Point.X - e.Location.X;

                }
                else if (iDirectionLock == 2)
                {
                    W += e.Location.X - ROI_Point.X;
                }

                else if (iDirectionLock == 3)
                {
                    X -= ROI_Point.Y - e.Location.Y;
                    H += ROI_Point.Y - e.Location.Y;

                }

                else if (iDirectionLock == 4)
                {
                    H += e.Location.Y - ROI_Point.Y;

                }
                
                if (iDirectionLock == 0)
                {
                    X += e.Location.X - ROI_Point.X;
                    Y += e.Location.Y - ROI_Point.Y;

                }
                else
                {
                    if (iDirectionLock == 1)
                    {
                        
                        ROI_Point.Y = e.Location.Y;
                    }
                    else if (iDirectionLock == 2)
                    {
                        ROI_Point.X = e.Location.X;
                        ROI_Point.Y = e.Location.Y;
                    }
                    else if (iDirectionLock == 3)
                    {
                        ROI_Point.X = e.Location.X;
                       
                    }
                    else if (iDirectionLock == 4)
                    {
                        ROI_Point.X = e.Location.X;
                        ROI_Point.Y = e.Location.Y;
                    }     
                }

                if (X >= 0 && X+W <= CCDImage.Width)
                    pDrawROI.Left = X;
                if (Y >= 0 && Y+H <= CCDImage.Height)
                    pDrawROI.Top = Y;
                if (X + W >= 0 && X + W <= CCDImage.Width)
                    pDrawROI.Width = W;
                if (Y + H >= 0 && Y + H <= CCDImage.Height)
                    pDrawROI.Height = H;
            }
        }

        private void DrawCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            iDirectionLock = -1;
        }

        private void displayOriginal(byte[] input_image_buffer, int start_pixel)//育代
        {
            Bitmap input_image = Method.BufferToImage(input_image_buffer);
            int W;
            int H;
            W = input_image.Width;
            H = input_image.Height;
        
            int Pixel_x = 0;//正在被掃描的點
            int Pixel_y = 0;

            System.Windows.Forms.DataVisualization.Charting.Series seriesGray = new System.Windows.Forms.DataVisualization.Charting.Series("灰階", 2000);
            RealTime_Original_Intensity = Method.get_Original_Intensity(input_image);
           /* if (isGetDarkIntensity)
            {
                RealTime_Original_Intensity = Method.Remove_BaseLine(RealTime_Original_Intensity,Dark_Intensity);
            }*/
            //設定座標大小
            this.chart_original.ChartAreas[0].AxisY.Minimum = 0;
            this.chart_original.ChartAreas[0].AxisY.Maximum = 300;
            this.chart_original.ChartAreas[0].AxisX.Minimum = start_pixel;
            this.chart_original.ChartAreas[0].AxisX.Maximum = start_pixel + W;

            input_image.Dispose();

            //設定標題
            this.chart_original.Titles.Clear();
            this.chart_original.Titles.Add("S01");
            this.chart_original.Titles[0].Text = "原始光譜";
            this.chart_original.Titles[0].ForeColor = Color.Black;
            this.chart_original.Titles[0].Font = new System.Drawing.Font("標楷體", 16F);
            //設定顏色
            seriesGray.Color = Color.Blue;

            //設定樣式
            seriesGray.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            //迴圈二
            for (Pixel_x = 0; Pixel_x < W; Pixel_x++)
            {
                  //給入數據畫圖
                seriesGray.Points.AddXY(Pixel_x + start_pixel, RealTime_Original_Intensity[Pixel_x]);
                this.chart_original.Series.Clear();
                this.chart_original.Series.Add(seriesGray);      
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DilteNumber = Convert.ToInt32(textBox7.Text);
            textBox2.Text = "300";
            label18.Text = "----";
            isROI_Revision = false;
            isReadyToCaculateBeamIntensity = false;
            isGetBeamIntensity = false;
            isMeasureEnd = false;
            File_Name = textBox5.Text;
            isHoughCorrect = false;
            Scaling_Times = 0;
            isGetROI = false;
            Goabal.Clear();
            back_number = 0;
            Index_of_Max = 0;
            iTask = 1;
            Directory.CreateDirectory(@"量測結果\"+ File_Name + @"\");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            DrawCanvas.Visible = false;
            DrawCanvas.Parent = CCDImage;
            ROIImage.Parent = panel1;
            //初始化 ROI位置
            ROI_Point.X = 0;
            ROI_Point.Y = 0;
            DrawCanvas.Left = ROI_Point.X;
            DrawCanvas.Top = ROI_Point.Y;

            CCDImage.Width = 320;
            CCDImage.Height = 240;
            panel1.Width = 320;
            panel1.Height = 240;
            ROIImage.Width = 320;
            ROIImage.Height = 240;

            DrawCanvas.Width = CCDImage.Width;
            DrawCanvas.Height = 5;
        }

     #region 相機參數調整_DG_GAMMA_BACK
        private void Set_Dg(int new_dg)
        {
            Dg_Update dg_Update = new Dg_Update(Update_Dg);
            this.Invoke(dg_Update, new_dg);
            int def_dg = 100;
            int max_dg = 255;
            int min_dg = 32;

            UsbCamera.PropertyItems.Property prop;
            set_camera_prop_dele camera_prop = new set_camera_prop_dele(set_camera_prop);
            prop = camera.Properties[DirectShow.VideoProcAmpProperty.Gain];
            if (prop.Available)
            {
                max_dg = prop.Max;
                min_dg = prop.Min;
                def_dg = prop.Default;

            }
            if (new_dg >= 255)
            {
                this.Invoke(camera_prop, "gain", 255);
                // MessageBox.Show("其他參數調太暗了!已無法更亮");
            }
            else if (new_dg <= 32)
            {
                this.Invoke(camera_prop, "gain", 32);
                //MessageBox.Show("其他參數調太亮了!已無法更暗");

            }
            else
            {
                this.Invoke(camera_prop, "gain", new_dg);
            }

        }
        private void Update_Dg(int dg)
        {
            label_DG.Text = dg.ToString();
        }
        private void Update_Gamma(int gamma)
        {
            label_Gamma.Text = gamma.ToString();
        }
        private void Update_Back(int back)
        {
            label_Back_Light.Text = back.ToString();
        }

        private List<int> exp_recorder = new List<int>();
        void set_camera_prop(string item, int prop_num)
        {
            UsbCamera.PropertyItems.Property prop;
            switch (item)
            {
                case "exp":
                    prop = camera.Properties[DirectShow.CameraControlProperty.Exposure];
                    if (prop.Available && prop.CanAuto)
                    {
                        prop.SetValue(DirectShow.CameraControlFlags.Auto, 0);
                    }
                    break;

                case "bright":
                    prop = camera.Properties[DirectShow.VideoProcAmpProperty.Brightness];
                    break;
                case "con":
                    prop = camera.Properties[DirectShow.VideoProcAmpProperty.Contrast];
                    break;
                case "hue":
                    prop = camera.Properties[DirectShow.VideoProcAmpProperty.Hue];
                    break;
                case "sat":
                    prop = camera.Properties[DirectShow.VideoProcAmpProperty.Saturation];
                    break;
                case "sharp":
                    prop = camera.Properties[DirectShow.VideoProcAmpProperty.Sharpness];
                    break;
                case "gamma":
                    prop = camera.Properties[DirectShow.VideoProcAmpProperty.Gamma];
                    break;
                case "white":
                    prop = camera.Properties[DirectShow.VideoProcAmpProperty.WhiteBalance];
                    break;
                case "back":
                    prop = camera.Properties[DirectShow.VideoProcAmpProperty.BacklightCompensation];
                    break;
                case "gain":
                    prop = camera.Properties[DirectShow.VideoProcAmpProperty.Gain];
                    break;
                default:
                    prop = camera.Properties[DirectShow.VideoProcAmpProperty.Gain];
                    break;


            }

            if (prop.Available)
            {
                var min = prop.Min;
                var max = prop.Max;
                var def = prop.Default;
                var step = prop.Step;


                if (prop_num <= min)
                {
                    prop.Default = min;
                }
                else if (prop_num >= max)
                {

                    prop.Default = max;
                }
                else
                {
                    prop.Default = prop_num;
                }
                exp_recorder.Add(prop.Default);
                Console.WriteLine(exp_recorder);
                prop.SetValue(DirectShow.CameraControlFlags.Manual, prop.Default);




            }

            var q = from p in exp_recorder
                    group p by p.ToString() into g
                    where g.Count() > 2//出現1次以上的數字
                    select new
                    {
                        g.Key,
                        NumProducts = g.Count()
                    };
            foreach (var x in q)
            {
                Console.WriteLine(x.Key);//陣列中 每個數字出現的數量
            }
            Console.ReadLine();
        }
        

        private void Set_Gamma(int new_gamma)
        {
            Gamma_Update gamma_Update = new Gamma_Update(Update_Gamma);
            this.Invoke(gamma_Update, new_gamma);
            int def_gamma = 100;
            int max_gamma = 100;
            int min_gamma = 300;

            UsbCamera.PropertyItems.Property prop;
            set_camera_prop_dele camera_prop = new set_camera_prop_dele(set_camera_prop);
            prop = camera.Properties[DirectShow.VideoProcAmpProperty.Gamma];
            if (prop.Available)
            {
                max_gamma = prop.Max;
                min_gamma = prop.Min;
                def_gamma = prop.Default;

            }
            if (new_gamma >= 300)
            {
                this.Invoke(camera_prop, "gamma", 300);
                //MessageBox.Show("其他參數調太暗了!已無法更亮");
            }
            else if (new_gamma <= 100)
            {
                this.Invoke(camera_prop, "gamma", 100);
                //MessageBox.Show("其他參數調太亮了!已無法更暗");

            }
            else
            {
                this.Invoke(camera_prop, "gamma", new_gamma);
            }

        }
        private void Set_Back(int new_back)
        {
            Gamma_Update back_Update = new Gamma_Update(Update_Back);
            this.Invoke(back_Update, new_back);
            int def_back = 1;
            int max_back = 3;
            int min_back = 0;

            UsbCamera.PropertyItems.Property prop;
            set_camera_prop_dele camera_prop = new set_camera_prop_dele(set_camera_prop);
            prop = camera.Properties[DirectShow.VideoProcAmpProperty.BacklightCompensation];
            if (prop.Available)
            {
                max_back = prop.Max;
                min_back = prop.Min;
                def_back = prop.Default;

            }
            if (new_back >= 3)
            {
                this.Invoke(camera_prop, "back", 3);
                //MessageBox.Show("其他參數調太暗了!已無法更亮");
            }
            else if (new_back <= 0)
            {
                this.Invoke(camera_prop, "back", 0);
                //MessageBox.Show("其他參數調太亮了!已無法更暗");

            }
            else
            {
                this.Invoke(camera_prop, "back", new_back);
            }

        }
        private List<double> Smart_Calibrate_DG_Intensity(List<double> Input_List, double index) //.
        {




            int Max = 1;
            int def_dg = 100;
            int max_dg = 255;
            int min_dg = 32;
            List<double> result = new List<double>();



            UsbCamera.PropertyItems.Property prop;
            set_camera_prop_dele camera_prop = new set_camera_prop_dele(set_camera_prop);
            prop = camera.Properties[DirectShow.VideoProcAmpProperty.Gain];
            if (prop.Available)
            {
                max_dg = prop.Max;
                min_dg = prop.Min;
                def_dg = prop.Default;

            }


            //  this.Invoke(camera_prop, "gain", def_dg);
            if (index == 0)
            {
                double Max_Intensity = Input_List.Max();
                int Index_of_Max = Input_List.IndexOf(Max_Intensity);
                result.Add(def_dg);//[0] 是否過曝  0:
                result.Add(Max_Intensity);//[0] 此時的值
                result.Add(Index_of_Max);//[1] 發生的位置
                this.Invoke(camera_prop, "gain", Convert.ToInt32(def_dg / 2));
            }
            else
            {
                double New_Max_Intensity = Input_List[Convert.ToInt32(index)];
                result.Add(def_dg);//[0] 是否過曝  0:
                result.Add(New_Max_Intensity);//[0] 此時的值
                result.Add(Convert.ToInt32(index));//[1] 發生的位置
            }



            return result;


        }
        #endregion
        void load_Matlab()
        {
            List<double> forTest = new List<double>() {2,4};
            List<double> forTest2 = new List<double>(){6,8};
            List<double> Loading = Method.Polynomial_Fitting(forTest,forTest2,1);
            MessageBox.Show("載入完成");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            iTask = 230;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Set_Dg(Convert.ToInt32(textBox3.Text));
            //Bitmap a = Method.FindCircle_Eddy((Bitmap)CCDImage.Image);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            isROIok = true;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            ROI_Xy = ROI_Xy - 1;
            textBox6.Text = ROI_Xy.ToString();
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            ROI_Xy = ROI_Xy + 1;
            textBox6.Text = ROI_Xy.ToString();
        }

        private void textBox6_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (textBox6.Text != null)
                {
                    ROI_Xy = Convert.ToInt32(textBox6.Text);
                }
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                isBlankMode = true;
            }
            else
            {
                isBlankMode = false;
            }
        }
    }
}


