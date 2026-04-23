using System;
using System.Drawing; 
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp; 

namespace ColorBot
{
    class Program
    {
        // === WINDOWS API ===
        [DllImport("user32.dll")]
        static extern bool SetProcessDPIAware(); 

        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out WinPoint lpPoint);

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);

        const int SM_CXSCREEN = 0;
        const int SM_CYSCREEN = 1;
        const int VK_X = 0x58; // Клавиша X для старта/стопа

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;

        [StructLayout(LayoutKind.Sequential)]
        public struct WinPoint
        {
            public int X;
            public int Y;
        }

        static volatile bool botEnabled = false;

        static void Main(string[] args)
        {
            SetProcessDPIAware(); // Обязательно для точного наведения!

            Console.WriteLine("========================================");
            Console.WriteLine(" BOT V3 (FIXED MOVEMENT & THRESHOLD)");
            Console.WriteLine(" Нажми [X] для запуска");
            Console.WriteLine("========================================");

            // Поток управления клавишей
            Task.Run(() => 
            {
                while (true)
                {
                    if ((GetAsyncKeyState(VK_X) & 0x8000) != 0)
                    {
                        botEnabled = !botEnabled; 
                        Console.WriteLine(botEnabled ? ">>> РАБОТАЮ (ВКЛ) <<<" : ">>> ПАУЗА (ВЫКЛ) <<<");
                        Thread.Sleep(300); // Защита от двойного нажатия
                    }
                    Thread.Sleep(10); 
                }
            });

            // Определяем центр экрана
            int width = GetSystemMetrics(SM_CXSCREEN);
            int height = GetSystemMetrics(SM_CYSCREEN);
            
            int cropSize = 500; 
            int cropX = (width / 2) - (cropSize / 2);
            // Чуть выше центра, GUI обычно смещен вверх
            int cropY = (height / 2) - (cropSize / 2) - 40; 
            
            if (cropX < 0) cropX = 0;
            if (cropY < 0) cropY = 0;

            System.Drawing.Rectangle roiRect = new System.Drawing.Rectangle(cropX, cropY, cropSize, cropSize);

            while (true)
            {
                try
                {
                    if (botEnabled)
                    {
                        // 1. Делаем скриншот ТОЛЬКО области GUI
                        using (Bitmap bmp = new Bitmap(roiRect.Width, roiRect.Height))
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            g.CopyFromScreen(roiRect.X, roiRect.Y, 0, 0, roiRect.Size);

                            BitmapData bmpData = bmp.LockBits(
                                new Rectangle(0, 0, bmp.Width, bmp.Height),
                                ImageLockMode.ReadOnly,
                                PixelFormat.Format24bppRgb);

                            // 2. Ищем цель на картинке
                            using (Mat mat = Mat.FromPixelData(bmp.Height, bmp.Width, MatType.CV_8UC3, bmpData.Scan0, bmpData.Stride))
                            {
                                DetectAndMove(mat, roiRect.X, roiRect.Y);
                            }

                            bmp.UnlockBits(bmpData);
                        }
                    }
                    else
                    {
                        // Экономим ресурсы в простое
                        Thread.Sleep(50);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Err: {ex.Message}");
                    GC.Collect(); // Чистим мусор при ошибке
                    Thread.Sleep(1000);
                }
            }
        }

        static void DetectAndMove(Mat src, int offsetX, int offsetY)
        {
            // HSV преобразование
            using (Mat hsv = new Mat())
            {
                Cv2.CvtColor(src, hsv, ColorConversionCodes.BGR2HSV);

                // === ФИЛЬТР ЦВЕТА ===
                // Яркость > 185 (Светящиеся блоки обычно > 200, обычные < 180)
                // Слегка опустил порог (с 200 до 185), чтобы бот видел наверняка,
                // но при этом всё еще игнорировал обычное стекло.
                
                Scalar lowerGlow = new Scalar(35, 60, 185); 
                Scalar upperGlow = new Scalar(110, 255, 255);

                // Белое стекло (особый случай: низкая насыщенность)
                Scalar lowerWhite = new Scalar(0, 0, 205); 
                Scalar upperWhite = new Scalar(180, 50, 255);

                using (Mat maskColor = new Mat())
                using (Mat maskWhite = new Mat())
                using (Mat maskCombined = new Mat())
                {
                    Cv2.InRange(hsv, lowerGlow, upperGlow, maskColor);
                    Cv2.InRange(hsv, lowerWhite, upperWhite, maskWhite);
                    
                    Cv2.BitwiseOr(maskColor, maskWhite, maskCombined);

                    // Убираем шумы
                    Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
                    Cv2.MorphologyEx(maskCombined, maskCombined, MorphTypes.Open, kernel);
                    Cv2.Dilate(maskCombined, maskCombined, kernel);

                    // Ищем контуры
                    OpenCvSharp.Point[][] contours;
                    HierarchyIndex[] hierarchy;
                    
                    Cv2.FindContours(maskCombined, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                    double maxArea = 0;
                    Rect bestRect = new Rect();
                    bool found = false;

                    foreach (var contour in contours)
                    {
                        double area = Cv2.ContourArea(contour);
                        
                        // Ищем объект размером со слот (примерно 400+ px)
                        if (area > 350)
                        {
                            Rect rect = Cv2.BoundingRect(contour);
                            double aspect = (double)rect.Width / rect.Height;

                            // Квадратный слот
                            if (aspect > 0.8 && aspect < 1.25) 
                            {
                                if (area > maxArea)
                                {
                                    maxArea = area;
                                    bestRect = rect;
                                    found = true;
                                }
                            }
                        }
                    }

                    if (found && botEnabled)
                    {
                        // Вычисляем координаты центра (в абсолютных координатах экрана)
                        int cX = bestRect.X + (bestRect.Width / 2) + offsetX;
                        int cY = bestRect.Y + (bestRect.Height / 2) + offsetY;

                        // Двигаем мышку и кликаем
                        PerformAction(cX, cY);
                    }
                }
            }
        }

        static void PerformAction(int targetX, int targetY)
        {
            GetCursorPos(out WinPoint start);
            double dist = Math.Sqrt(Math.Pow(targetX - start.X, 2) + Math.Pow(targetY - start.Y, 2));

            // Если мышка уже очень близко (< 10 пикс), считаем, что мы навелись
            // Если далеко - двигаем.
            if (dist > 10)
            {
                int steps = dist > 200 ? 5 : 8; // Если далеко - быстрее (5 шагов), близко - плавнее
                
                for (int i = 1; i <= steps; i++)
                {
                    if (!botEnabled) return;
                    float t = (float)i / steps;
                    
                    int newX = (int)(start.X + (targetX - start.X) * t);
                    int newY = (int)(start.Y + (targetY - start.Y) * t);
                    
                    SetCursorPos(newX, newY);
                    Thread.Sleep(1);
                }
                
                // Финальная коррекция в центр (очень важно для клика)
                if (botEnabled) SetCursorPos(targetX, targetY);
            }

            if (!botEnabled) return;

            // Клик
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            Thread.Sleep(new Random().Next(25, 45)); // Имитация человека
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
            
            // Пауза, чтобы не кликать повторно в тот же светящийся блок
            Thread.Sleep(250);
        }
    }
}