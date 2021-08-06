using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathWorks.MATLAB.NET.Arrays;
using PolyFit_NPlot;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using System.Drawing.Imaging;

namespace SpertroApp
{
    class Method
    {
        public static bool isGetCenter = false;
        public static Point Center_of_Counter;
        public static double Radius_of_Circle;
        public static Bitmap Remove_Blank(Bitmap input, Bitmap Blank)
        {
            int W = input.Width;
            int H = input.Height;
            for (int Pixel_x = 0; Pixel_x < W; Pixel_x++)
            {
                for (int Pixel_y = 0; Pixel_y < H; Pixel_y++)
                {
                    //先把圖變灰階
                    Color p0 = input.GetPixel(Pixel_x, Pixel_y);
                    Color pb = Blank.GetPixel(Pixel_x, Pixel_y);
                    int R = p0.R, G = p0.G, B = p0.B;
                    int Rb = pb.R, Gb = pb.G, Bb = pb.B;
                    int newR = 0; int newG = 0; int newB = 0;
                    if (R - Rb < 0)
                    {
                        newR = 0;
                    }
                    else
                    {
                        newR = R - Rb;
                    }
                    if (G - Gb < 0)
                    {
                        newG = 0;
                    }
                    else
                    {
                        newG = G - Gb;
                    }
                    if (B - Bb < 0)
                    {
                        newB = 0;
                    }
                    else
                    {
                        newB = B - Bb;
                    }
                    input.SetPixel(Pixel_x, Pixel_y, Color.FromArgb(newR,newG,newB));
                }
            }
            return input;
        }
        public static Bitmap Dlite(Bitmap input,int DilteNumber)
        {
            Mat src = new Image<Bgr, byte>(input).Mat;
            Mat dst = new Mat();
            Mat element = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Cross,
                    new Size(DilteNumber, DilteNumber), new Point(-1, -1));
            CvInvoke.Dilate(src, dst, element, new Point(-1, -1), 3,
                    Emgu.CV.CvEnum.BorderType.Default, new MCvScalar(0, 0, 0));
            Image<Bgr, Byte> outImg = dst.ToImage<Bgr, Byte>();
            Bitmap result = new Bitmap(outImg.Bitmap);
            return result; ;
        }
        public static Bitmap Get_new_Picture(Bitmap input_image,int pixelX)
        {
            
            int W = input_image.Width;
            int H = input_image.Height;
            List<double> I_of_every_pixel = new List<double>();
            double Max_I = 0;
            double gobal_value;
            List<Point> Gobal_Point = new List<Point>();
            for (int Pixel_x = 0; Pixel_x < W; Pixel_x++)
            {
                for (int Pixel_y = 0; Pixel_y < H; Pixel_y++)
                {
                    if (Pixel_x< (pixelX-5))
                    {
                        input_image.SetPixel(Pixel_x, Pixel_y, Color.FromArgb(0,0,0));
                    }
                }
            }
            return input_image;
        }
        public static Bitmap Get_Goabal_Picture(Bitmap input_image, double thresold, double dark_avg)
        {
            int W = input_image.Width;
            int H = input_image.Height;
            List<double> I_of_every_pixel = new List<double>();
            double Max_I = 0;
            double gobal_value;
            List<Point> Gobal_Point = new List<Point>();
            for (int Pixel_x = 0; Pixel_x < W; Pixel_x++)
            {
                for (int Pixel_y = 0; Pixel_y < H; Pixel_y++)
                {
                    //先把圖變灰階
                    Color p0 = input_image.GetPixel(Pixel_x, Pixel_y);//太快會閃退，全世界都在用image_roi
                    int R = p0.R, G = p0.G, B = p0.B;
                    int gray = (R * 313524 + G * 615514 + B * 119538) >> 20;
                    I_of_every_pixel.Add(gray);
                }
                Max_I = I_of_every_pixel.Max();
            }
            //gobal_value = (Max_I - dark_avg) /3;
             gobal_value = (Max_I - dark_avg) * (1 / Math.Exp(2));
            for (int Pixel_x = 0; Pixel_x < W; Pixel_x++)
            {
                for (int Pixel_y = 0; Pixel_y < H; Pixel_y++)
                {
                    Color p0 = input_image.GetPixel(Pixel_x, Pixel_y);//太快會閃退，全世界都在用image_roi
                    int R = p0.R, G = p0.G, B = p0.B;
                    double gray = (R * 313524 + G * 615514 + B * 119538) >> 20;
                    gray = gray - dark_avg;
                    //Color p1 = Color.FromArgb(gray, gray, gray);
                    if (Math.Abs(gray - gobal_value) <= thresold)
                    {
                        input_image.SetPixel(Pixel_x, Pixel_y, Color.FromArgb(255, 255, 255));
                    }
                    else { input_image.SetPixel(Pixel_x, Pixel_y, Color.FromArgb(0, 0, 0)); }
                }
            }
            return input_image;
        }
        public static Bitmap Find_Line(Bitmap input)
        {
            Image<Bgr, byte> src = new Image<Bgr, byte>(input);
            Image<Gray, Byte> cannyGray = new Image<Gray, Byte>(input);
            //调用HoughLinesBinary检测直线，返回一个LineSegment2D[]的数组
            LineSegment2D[] lines = cannyGray.HoughLinesBinary(600, Math.PI / 45, 600, 5, 1000)[0];
            //画线
            Image<Bgr, Byte> imageLines = new Image<Bgr, Byte>(cannyGray.Width, cannyGray.Height);
            foreach (LineSegment2D line in lines)
            {

                //在imageLines上将直线画出
                src.Draw(line, new Bgr(Color.DeepSkyBlue), 2);
                
            }
            //显示结果
            return src.ToBitmap();

        }
        public static Bitmap Canny(Bitmap input, int LowThreshold1,int x)
        {
            Mat src = new Image<Bgr, byte>(input).Mat;
            Mat dst = new Mat();
            //轉為灰度圖並進行影象平滑
            CvInvoke.CvtColor(src, dst, ColorConversion.Rgb2Gray);
            CvInvoke.GaussianBlur(dst, dst , new Size(9, 9), 0, 0);
            CvInvoke.Canny(dst, dst, LowThreshold1, LowThreshold1*x);
            return dst.Bitmap;
        }
        public static Bitmap Find_Center_Point(Bitmap input)
        {           
                Mat src = new Image<Bgr, byte>(input).Mat;
                Mat dst = new Mat();
                CvInvoke.CvtColor(src, dst, ColorConversion.Rgb2Gray);
                CvInvoke.GaussianBlur(dst, dst, new Size(3, 3), 0, 0);
                CvInvoke.Canny(dst, dst, 10, 20);
                int MaxArea = src.Width * src.Height;
                VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();

                CvInvoke.FindContours(dst, contours, null, Emgu.CV.CvEnum.RetrType.External,
                    Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxNone);

                //筛选出面积不为0的轮廓并画出
                VectorOfVectorOfPoint use_contours = new VectorOfVectorOfPoint();
            VectorOfVectorOfPoint use_contours2 = new VectorOfVectorOfPoint();

            for (int i = 0; i < contours.Size; i++)
                {
                    //获取独立的连通轮廓
                    //VectorOfPoint contour = contours[i];
                //计算连通轮廓的面积
                double area = Emgu.CV.CvInvoke.ContourArea(contours[i]);
                //进行面积筛选
                    if (area > MaxArea/100)
                    {
                          //添加筛选后的连通轮廓
                          use_contours.Push(contours[i]);
                          use_contours2.Push(contours[i]);
                          isGetCenter = true;
                    }
                }
               
                CvInvoke.DrawContours(src, use_contours, -1, new MCvScalar(0, 0, 255));
                //计算轮廓中心并画出
                int ksize = use_contours.Size;
            
                double[] m00 = new double[ksize];
                double[] m01 = new double[ksize];
                double[] m10 = new double[ksize];
                Point[] gravity = new Point[ksize];//用于存储轮廓中心点坐标
                MCvMoments[] moments = new MCvMoments[ksize];
            
                for (int i = 0; i < ksize; i++)
                {
                    //VectorOfPoint contour = use_contours[i];
                    //计算当前轮廓的矩
                    moments[i] = Emgu.CV.CvInvoke.Moments(use_contours2[i], false);

                    m00[i] = moments[i].M00;
                    m01[i] = moments[i].M01;
                    m10[i] = moments[i].M10;
                    int x = Convert.ToInt32(m10[i] / m00[i]);//计算当前轮廓中心点坐标
                    int y = Convert.ToInt32(m01[i] / m00[i]);
                    gravity[i] = new Point(x, y);
                }
                
                //画出中心点位置
                foreach (Point cent in gravity)
                {
                    CvInvoke.Circle(src, cent, 2, new MCvScalar(0, 0, 255), 2);
                    Center_of_Counter = cent;
                }
                
            Image<Bgr,byte> outImg = src.ToImage<Bgr,byte>();
            //outImg.ToBitmap();
            Bitmap result = new Bitmap(outImg.Bitmap);
            return result;
            
        }
        public static Bitmap FindCircle_Eddy(Bitmap input,int canny_threshold_Low, int p, int minR)
        {
            Mat src = new Image<Bgr, byte>(input).Mat;
            Mat dst = new Mat();
            //1:因为霍夫圆检测对噪声比较敏感，所以首先对图像做一个中值滤波或高斯滤波(噪声如果没有可以不做)
            Mat m1 = new Mat();
            CvInvoke.MedianBlur(src, m1, 11); //  ksize必须大于1且是奇数
                                              // m1 = DrawCross(m1);
                                              //2：转为灰度图像

            //Mat m2 = new Mat(new Size(m1.Height, m1.Width), CvInvoke.MakeType.CV_8UC1);//CV_8UC1//CV_32SC1


            CvInvoke.CvtColor(m1, m1, ColorConversion.Bgr2Gray);

            //3：霍夫圆检测：使用霍夫变换查找灰度图像中的圆。
            /*
             * 参数：_
             * 
             *      1：输入参数： 8位、单通道、灰度输入图像
             *      2：实现方法：目前，唯一的实现方法是HoughCirclesMethod.Gradient
             *      3: dp      :累加器分辨率与图像分辨率的反比。默认=1
             *      4：minDist: 检测到的圆的中心之间的最小距离。(最短距离-可以分辨是两个圆的，否则认为是同心圆-                            src_gray.rows/8)
             *      5:param1:   第一个方法特定的参数。[默认值是100] canny边缘检测阈值低
             *      6:param2:   第二个方法特定于参数。[默认值是100] 中心点累加器阈值 – 候选圆心
             *      7:minRadius: 最小半径
             *      8:maxRadius: 最大半径
             * 
             */
            CircleF[] cs = CvInvoke.HoughCircles(m1, HoughType.Gradient, 1, m1.Width, canny_threshold_Low, p,minR, m1.Width);//72
            //100,30,20,150

            src.CopyTo(dst);

            // Vec3d vec = new Vec3d();
            int Rang = 55;

            int i = 0;

          
                for (i = 0; i < cs.Length; i++)
                {
                //画圆
                CvInvoke.Circle(dst, new Point((int)cs[i].Center.X , (int)cs[i].Center.Y), (int)cs[i].Radius, new MCvScalar(0, 0, 255), 2, LineType.AntiAlias);
                //加强圆心显示
                CvInvoke.Circle(dst, new Point((int)cs[i].Center.X , (int)cs[i].Center.Y), 3, new MCvScalar(0, 0, 255), 2, LineType.AntiAlias);

                #region 寫字
                //寫字在旁邊
                //CvInvoke.PutText(dst, "123", new Point(((((int)cs[i].Center.X + (int)cs[i].Radius + 100) < src.Width) ? ((int)cs[i].Center.X + (int)cs[i].Radius + 10) : ((int)cs[i].Center.X - (int)cs[i].Radius - 20)), (int)cs[i].Center.Y),
                //           FontFace.HersheyComplex, 0.5, new MCvScalar(255, 255, 255), 2, LineType.AntiAlias);
                 CvInvoke.PutText(dst,"D:"+ Math.Round((2 * cs[i].Radius),4).ToString() + " pixels", new Point(((((int)cs[i].Center.X + (int)cs[i].Radius + 100) < src.Width) ? ((int)cs[i].Center.X + (int)cs[i].Radius + 10) : ((int)cs[i].Center.X - (int)cs[i].Radius - 20)), (int)cs[i].Center.Y + Rang),
                          FontFace.HersheyComplex,0.5, new MCvScalar(255, 0, 0), 1, LineType.AntiAlias);
                //CvInvoke.PutText(dst, cs[i].Radius * 0.0096 + " mm", new Point(((((int)cs[i].Center.X+ (int)cs[i].Radius + 100) < src.Width) ? ((int)cs[i].Center.X + (int)cs[i].Radius + 10) : ((int)cs[i].Center.X  - (int)cs[i].Radius - 20)), (int)cs[i].Center.Y + Rang * 2),
                //        FontFace.HersheyComplex,0.5, new MCvScalar(255, 255, 255), 2, LineType.AntiAlias);
                //CvInvoke.PutText(dst, ((int)cs[i].Center.X).ToString()+","+ ((int)cs[i].Center.Y).ToString(), new Point(((((int)cs[i].Center.X + (int)cs[i].Radius + 100) < src.Width) ? ((int)cs[i].Center.X + (int)cs[i].Radius + 10) : ((int)cs[i].Center.X - (int)cs[i].Radius - 20)), (int)cs[i].Center.Y + Rang * 3),
                //        FontFace.HersheyComplex, 0.5, new MCvScalar(255, 255, 255), 2, LineType.AntiAlias);
                //Cv2.PutText(dst, cs[i].Radius.ToString(), new OpenCvSharp.Point(0,0), HersheyFonts.HersheyPlain, 12, new Scalar(255, 255, 255), 5, LineTypes.AntiAlias);
                #endregion
            }
            foreach (var rad in cs)
            {
                Radius_of_Circle = rad.Radius;
            }
            Image<Bgr, Byte> outImg = dst.ToImage<Bgr, Byte>();
            Bitmap result = new Bitmap(outImg.Bitmap);
            return result;
           
        }
        public static Bitmap FindCircle(Bitmap input)
        {
            Image<Bgr, Byte> CvImage = new Image<Bgr, Byte>(input);
            Mat srcImage = CvImage.Mat;
            //定義臨時變數和目標圖
            Mat tempImage = new Mat(), dstImage = new Mat();
            //轉為灰度圖並進行影象平滑
            CvInvoke.CvtColor(srcImage, tempImage, ColorConversion.Rgb2Gray);
            CvInvoke.GaussianBlur(tempImage, tempImage, new Size(9, 9), 0, 0);
            //tempImage.ConvertTo(tempImage,DepthType.Cv8U);
            //進行霍夫圓變換
            VectorOfPoint3D32F circles = new VectorOfPoint3D32F(); //定義vect儲存圓引數（中心點x、y座標和半徑）
            CvInvoke.HoughCircles(tempImage, circles, HoughType.Gradient, 1, 20, 200, 200, 0, 0);

            //依次在圖中繪製出圓
            for (int i = 0; i < circles.Size; i++)
            {
                //引數定義
                Point center = new Point((int)Math.Round(circles[i].X), (int)Math.Round(circles[i].Y)); //定義圓心
                int radius = (int)Math.Round(circles[i].Z);
                //繪製圓心(圓的thickness設定為-1）
                CvInvoke.Circle(srcImage, center, 3, new MCvScalar(0, 255, 0), -1);
                //繪製圓輪廓
                CvInvoke.Circle(srcImage, center, radius, new MCvScalar(155, 50, 255), 3);
            }
            
            return srcImage.Bitmap;

        }

        
    
        public static Bitmap RemoveBeamWast(Bitmap input)
        {
            int W = input.Width;
            int H = input.Height;
            Bitmap im1 = new Bitmap(W, H);//讀出原圖X軸 pixel
            List<double> I_of_every_pixel = new List<double>();
            double AGray = 0;
            double Max_I = 0;
            double gobal_value = 0;
            List<Point> Gobal_Point = new List<Point>();
            for (int Pixel_x = 0; Pixel_x < W; Pixel_x++)
            {
                for (int Pixel_y = 0; Pixel_y < H; Pixel_y++)
                {
                    //先把圖變灰階
                    Color p0 = input.GetPixel(Pixel_x, Pixel_y);//太快會閃退，全世界都在用image_roi
                    int R = p0.R, G = p0.G, B = p0.B;
                    int gray = (R * 313524 + G * 615514 + B * 119538) >> 20;
                    I_of_every_pixel.Add(gray);
                }
                Max_I = I_of_every_pixel.Max();
            }
            gobal_value = Max_I * (1 / Math.Exp(2));
            for (int Pixel_x = 0; Pixel_x < W; Pixel_x++)
            {
                for (int Pixel_y = 0; Pixel_y < H; Pixel_y++)
                {
                    Color p0 = input.GetPixel(Pixel_x, Pixel_y);//太快會閃退，全世界都在用image_roi
                    int R = p0.R, G = p0.G, B = p0.B;
                    int gray = (R * 313524 + G * 615514 + B * 119538) >> 20;
                    gray = gray - Convert.ToInt32(gobal_value);
                    if (gray < 0)
                    { gray = 0; }
                    Color p1 = Color.FromArgb(gray, gray, gray);
                    input.SetPixel(Pixel_x,Pixel_y,p1);
                }
            }
            return input;
        }
       
        public static Bitmap BufferToImage(byte[] Buffer) //改
        {
            if (Buffer == null || Buffer.Length == 0) { return null; }
            byte[] data = null;
            Image oImage = null;
            Bitmap oBitmap = null;
            //建立副本
            data = (byte[])Buffer.Clone();
            try
            {
                MemoryStream oMemoryStream = new MemoryStream(Buffer);
                //設定資料流位置
                oMemoryStream.Position = 0;
                oImage = System.Drawing.Image.FromStream(oMemoryStream);
                //建立副本
                oBitmap = new Bitmap(oImage);
            }
            catch
            {
                throw;
            }
            //return oImage;
            return oBitmap;
        }
        public static List<double> Remove_BaseLine(List<double> Original_Intensity, List<double> Dark_Intensity)
        {
            int Data_Length = Original_Intensity.Count;
            List<double> Pure_Intensity = new List<double>(Data_Length);

            for (int i = 0; i < Data_Length; i++) Pure_Intensity.Add(Original_Intensity[i] - Dark_Intensity[i]);

            return Pure_Intensity;
        }
        public static List<double> Get_BeamNumber(Bitmap input_image, double dark_avg)
        {
            List<double> output = new List<double>();
            double Intensity = 0;
            int W = input_image.Width;
            int H = input_image.Height;
            double Area = 0;
            Bitmap im1 = new Bitmap(W, H);//讀出原圖X軸 pixel
            List<double> I_of_every_pixel = new List<double>();
            int sum_of_gray = 0;
            int gray_of_Area = 0;
            double AGray = 0;
            double Max_I = 0;
            double gobal_value = 0;
            List<Point> Gobal_Point = new List<Point>();
            for (int Pixel_x = 0; Pixel_x < W; Pixel_x++)
            {
                for (int Pixel_y = 0; Pixel_y < H; Pixel_y++)
                {
                    //先把圖變灰階

                    Color p0 = input_image.GetPixel(Pixel_x, Pixel_y);//太快會閃退，全世界都在用image_roi
                    int R = p0.R, G = p0.G, B = p0.B;
                    int gray = (R * 313524 + G * 615514 + B * 119538) >> 20;
                    Color p1 = Color.FromArgb(gray, gray, gray);
                    sum_of_gray += gray; 
                    I_of_every_pixel.Add(gray);
                }
                Max_I = I_of_every_pixel.Max();
            }
            gobal_value = (Max_I - dark_avg) * (1 / Math.Exp(2));
            for (int Pixel_x = 0; Pixel_x < W; Pixel_x++)
            {
                for (int Pixel_y = 0; Pixel_y < H; Pixel_y++)
                {
                    Color p0 = input_image.GetPixel(Pixel_x, Pixel_y);//太快會閃退，全世界都在用image_roi
                    int R = p0.R, G = p0.G, B = p0.B;
                    int gray = (R * 313524 + G * 615514 + B * 119538) >> 20;
                    double gray2 = gray - dark_avg;
                    //Color p1 = Color.FromArgb(gray, gray, gray);
                    if (gray2 - gobal_value > 0)
                    {
                        gray_of_Area += gray;
                        Area++;
                    }
                }
            }
            output.Add(Area);
            output.Add(gray_of_Area);
            output.Add(gray_of_Area / Area);
            return output;
        }
        public static List<Point> Get_Goabal_Pixel(Bitmap input_image ,double thresold,double dark_avg)
        {
            int W = input_image.Width;
            int H = input_image.Height;
            Bitmap im1 = new Bitmap(W, H);//讀出原圖X軸 pixel
            List<double> I_of_every_pixel = new List<double>();
            double AGray = 0;
            double Max_I = 0;
            double gobal_value = 0;
            List<Point> Gobal_Point = new List<Point>();
            for (int Pixel_x = 0; Pixel_x < W; Pixel_x++)
            {
                for (int Pixel_y = 0; Pixel_y < H; Pixel_y++)
                {
                    //先把圖變灰階

                    Color p0 = input_image.GetPixel(Pixel_x, Pixel_y);//太快會閃退，全世界都在用image_roi
                    int R = p0.R, G = p0.G, B = p0.B;
                    int gray = (R * 313524 + G * 615514 + B * 119538) >> 20;
                    Color p1 = Color.FromArgb(gray, gray, gray);
                    I_of_every_pixel.Add(gray);
                }
                Max_I = I_of_every_pixel.Max();
            }
            //gobal_value = (Max_I - dark_avg) /2;
            gobal_value = (Max_I- dark_avg )* (1 / Math.Exp(2));
            for (int Pixel_x = 0; Pixel_x < W; Pixel_x++)
            {
                for (int Pixel_y = 0; Pixel_y < H; Pixel_y++)
                {
                    Color p0 = input_image.GetPixel(Pixel_x, Pixel_y);//太快會閃退，全世界都在用image_roi
                    int R = p0.R, G = p0.G, B = p0.B;
                    double gray = (R * 313524 + G * 615514 + B * 119538) >> 20;
                    gray = gray - dark_avg;
                    //Color p1 = Color.FromArgb(gray, gray, gray);
                    if (Math.Abs(gray - gobal_value) <= thresold)
                    {
                        Gobal_Point.Add(new Point(Pixel_x, Pixel_y));
                    }
                }
            }
            return Gobal_Point;
        }
        public static List<double> get_Original_Intensity(Bitmap input_image)
        {
            int W;
            int H;
            W = input_image.Width;
            H = input_image.Height;
            Bitmap im1 = new Bitmap(W, H);//讀出原圖X軸 pixel
            int Pixel_x = 0;//正在被掃描的點
            int Pixel_y = 0;
            double[] ARed = new double[W];
            double[] AGreen = new double[W];
            double[] ABlue = new double[W];
            double[] AGray = new double[W];
            double[] IntensityRed = new double[W];
            double[] IntensityGreen = new double[W];
            double[] IntensityBlue = new double[W];
            double[] IntensityGray = new double[W];
            for (Pixel_x = 0; Pixel_x < W; Pixel_x++)
            {
                for (Pixel_y = 0; Pixel_y < H; Pixel_y++)
                {
                    //先把圖變灰階

                    Color p0 = input_image.GetPixel(Pixel_x, Pixel_y);//太快會閃退，全世界都在用image_roi
                    int R = p0.R, G = p0.G, B = p0.B;
                    int gray = (R * 313524 + G * 615514 + B * 119538) >> 20;
                    Color p1 = Color.FromArgb(gray, gray, gray);
                   
                    ARed[Pixel_x] = ARed[Pixel_x] + R;
                    AGreen[Pixel_x] = AGreen[Pixel_x] + G;
                    ABlue[Pixel_x] = ABlue[Pixel_x] + B;
                    AGray[Pixel_x] = AGray[Pixel_x] + gray;
                }
                IntensityRed[Pixel_x] = ARed[Pixel_x] / H;//平均
                IntensityGreen[Pixel_x] = AGreen[Pixel_x] / H;//平均
                IntensityBlue[Pixel_x] = ABlue[Pixel_x] / H;//平均
                IntensityGray[Pixel_x] = AGray[Pixel_x] / H;//平均
            }
            return IntensityGray.ToList();
        }

        /// <summary>
        /// X方向ROI掃描,輸入bitmap格式影像
        /// </summary>
        /// <param name="input_image0"></param>
        /// <returns></returns>
        public static IDictionary<string, int> RoiScan_X(Bitmap input_image0)
        {
            Bitmap input_image = new Bitmap(input_image0.Width, input_image0.Height);
            input_image = input_image0;
            IDictionary<string, int> ROI = new Dictionary<string, int>();
            int progressBar = 0;
            int now_y = 0;
            int roi_fixHeight = 20; //掃描矩形的寬度
            int Pixel_x = 0;//正在被掃描的點
            int Pixel_y = 0;
            int sum_gray = 0;
            int clip = 0; //掃描起點，跳過起始較暗區域，進而增快速度
            List<int> sum4eachROI = new List<int>();
            now_y = clip;

            while (now_y < (input_image0.Height - roi_fixHeight))
            {
                for (Pixel_x = 0; Pixel_x < input_image0.Width; Pixel_x++)
                {
                    for (Pixel_y = now_y; Pixel_y < (now_y + roi_fixHeight); Pixel_y++)//初始值為now_y掃到now_y+20
                    {
                        Color p0 = input_image.GetPixel(Pixel_x, Pixel_y);
                        int R = p0.R, G = p0.G, B = p0.B;
                        int gray = (R * 313524 + G * 615514 + B * 119538) >> 20;

                        sum_gray = sum_gray + gray;
                    }

                }
                // Add parts to the list.
                sum4eachROI.Add(sum_gray);
                sum_gray = 0;
                now_y++;
                progressBar++;

            }
            int max = sum4eachROI.Max();
            var MAX_y = sum4eachROI.IndexOf(max) + clip;

            ROI.Add("x", 0);
            ROI.Add("y", Convert.ToInt32(MAX_y));
            ROI.Add("w", input_image0.Width);
            ROI.Add("h", roi_fixHeight);
            Console.WriteLine(progressBar);
            return ROI;
        }

        /// <summary>
        /// Y方向ROI掃描,輸入bitmap格式影像
        /// </summary>
        /// <param name="input_image0"></param>
        /// <returns></returns>
        public static IDictionary<string, int> RoiScan_Y(Bitmap input_image0)
        {
            Bitmap input_image = new Bitmap(input_image0.Width, input_image0.Height);
            input_image = input_image0;
            IDictionary<string, int> ROI = new Dictionary<string, int>();
            int progressBar = 0;
            int now_x = 0;
            int roi_fixWidth = 20;//之後可設為外部設定
            int Pixel_x = 0;//正在被掃描的點
            int Pixel_y = 0;
            int sum_gray = 0;
            int clip = 0;
            List<int> sum4eachROI = new List<int>();

            now_x = clip;

            while (now_x < (input_image0.Width - roi_fixWidth))
            {
                for (Pixel_y = 0; Pixel_y < input_image0.Height; Pixel_y++)
                {
                    for (Pixel_x = now_x; Pixel_x < (now_x + roi_fixWidth); Pixel_x++)//初始值為now_y掃到now_y+20
                    {
                        Color p0 = input_image.GetPixel(Pixel_x, Pixel_y);
                        int R = p0.R, G = p0.G, B = p0.B;
                        int gray = (R * 313524 + G * 615514 + B * 119538) >> 20;

                        sum_gray = sum_gray + gray;
                    }

                }
                sum4eachROI.Add(sum_gray);
                sum_gray = 0;
                now_x++;
                progressBar++;

            }
            int max = sum4eachROI.Max();
            var MAX_x = sum4eachROI.IndexOf(max) + clip;

            ROI.Add("x", Convert.ToInt32(MAX_x));
            ROI.Add("y", 0);
            ROI.Add("w", roi_fixWidth);
            ROI.Add("h", input_image0.Height);
            Console.WriteLine(progressBar);
            return ROI;
        }

        public static Bitmap BufferToBitmap(byte[] Buffer) //改
        {
            if (Buffer == null || Buffer.Length == 0) { return null; }
            byte[] data = null;
            Image oImage = null;
            Bitmap oBitmap = null;
            //建立副本
            data = (byte[])Buffer.Clone();
            try
            {
                MemoryStream oMemoryStream = new MemoryStream(Buffer);
                //設定資料流位置
                oMemoryStream.Position = 0;
                oImage = System.Drawing.Image.FromStream(oMemoryStream);
                //建立副本
                oBitmap = new Bitmap(oImage);
            }
            catch
            {
                throw;
            }
            //return oImage;
            return oBitmap;
        }
        public static Bitmap crop(Bitmap src, Rectangle cropRect)
        {
            // Rectangle cropRect = new Rectangle(0, 0, 400, 400);
            Bitmap target = new Bitmap(cropRect.Width, cropRect.Height);

            using (Graphics g = Graphics.FromImage(target))
            {
                g.DrawImage(src, new Rectangle(0, 0, target.Width, target.Height),
                      cropRect,
                      GraphicsUnit.Pixel);
            }
            return target;
        }

        public static List<double> Polynomial_Fitting(List<double> X_Pixles, List<double> Y_Wavelength, int order)
        {

            MWArray order_M = (MWNumericArray)order;
            MWArray X_Pixles_M = (MWNumericArray)X_Pixles.ToArray();
            MWArray Y_Wavelength_M = (MWNumericArray)Y_Wavelength.ToArray();



            List<double> Coef_List = new List<double>();
            PF_NP pf_np = new PF_NP();

            MWArray Fit = pf_np.PolyFit_NPlot(X_Pixles_M, Y_Wavelength_M, order_M);


            return MWArray2Array(Fit).ToList();

        }
        private static double[] MWArray2Array(MWArray Array_M)
        {


            double[,] dd;
            dd = (double[,])((MWNumericArray)Array_M).ToArray();
            double[] d = new double[5];//預設大小是5，用來放POLY擬和後的參數
            for (int i = 0; i < dd.Length; i++) d[i] = dd[0, dd.Length - i - 1];

            return d;
        }
    }
}
