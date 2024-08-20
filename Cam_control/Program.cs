using System;
using System.IO;
using System.Collections.Generic;
using DALSA.SaperaLT.SapClassBasic;
using OpenCvSharp;

namespace Cam_control
{
    public class Cam_params
    {
        // 全域設定
        
        private static string serverName = "Linea_M8192-7um_1"; // 裝置 serverName 在這裡設定，換相機才需要改
        // private static string ffcFile = @"C:\Users\9288\Desktop\Cam_control\Linea_M8192-7um_FlatFieldCoefficients0.tif"; // 指定 FFC 的 tif 檔
        private string imageSavedPath = @"C:\Users\9288\Desktop\Cam_image\"; // 指定影像儲存位置
        
        //private static double lineRate = 100; // Line Rate 設定，單位 Hz
        //private double exposureTime = 4; // 曝光時間在這裡設定，單位us
        //private double gain = 1; // Gain 在這邊設定，範圍是 1.0~10.0
        
        private static double lineRate;
        private static double exposureTime;
        private static double gain;
        
        private static string pixelFormat = "Mono12"; // 設定影像深度
        private static int height = 7997; // 設定影像高度
        private static string configFilePath = @"C:\Program Files\Teledyne DALSA\Sapera\CamFiles\User\T_Linea_M8192-7um_Default_Default.ccf"; //指定.ccf檔，ccf檔建議用CamExpert產生
        //private static double wait = (1 + (height / lineRate)); // 計算ImageTimout，為拍攝時間+1秒

        private double[] exposureTimeCycle = new double[] {4, 5, 10, 20, 40, 60, 80, 100, 120, 140, 160, 180, 200, 220, 240, 260, 280, 300, 320, 340, 360, 380, 400, 420, 440, 460, 480, 500, 520, 540, 560, 580, 600, 620, 640, 660, 680, 700, 720, 740, 760, 780, 800, 820, 840, 860, 880, 900, 920, 940, 960, 980, 1000, 1020, 1040, 1060, 1080, 1100, 1120, 1140, 1160, 1180, 1200, 1220, 1240, 1260, 1280, 1300, 1320, 1340, 1360, 1380, 1400, 1420, 1440, 1460, 1480, 1500, 1520, 1540, 1560, 1580, 1600, 1620, 1640, 1660, 1680, 1700, 1720, 1740, 1760, 1780, 1800, 1820, 1840, 1860, 1880, 1900, 1920, 1940, 1960, 1980, 2000, 2020, 2040, 2060, 2080, 2100, 2120, 2140, 2160, 2180, 2200, 2220, 2240, 2260, 2280, 2300, 2320, 2340, 2360, 2380, 2400, 2420, 2440, 2460, 2480, 2500, 2520, 2540, 2560, 2580, 2600, 2620, 2640, 2660, 2680, 2700, 2720, 2740, 2760, 2780, 2800, 2820, 2840, 2860, 2880, 2900, 2920, 2940, 2960, 2980, 3000
}; // Exposure 的排程設定
        private double[] gainCycle = new double[] {1.0, 3.0, 5.0, 7.0, 10.0}; // Gain 的排程設定

        private static SapAcqDevice acqDevice;
        private static SapBuffer buffer;
        private static SapFlatField ffc;
        private static SapAcqDeviceToBuf transfer;
        private static SapLocation location = new SapLocation(serverName, 0);

        double wait;
        
        public static void Main(string[] args)
        {
            Cam_params cam = new Cam_params(); // 這邊選擇相機拍攝模式，也可以多個執行
            cam.Connect();
            cam.Scan(); // 實際整合後使用的功能（基本上是讀python主程式輸出的config版本的Snap）
            //cam.Snap();  // 用來拍單張或測試，要改Line Rate建議從這邊
            //cam.Cycle(); // 用來排程拍多張
            //cam.Read(); // 用來讀取影像數
            
        }

        public void Connect()
        {
            DestroysObjects();
            string configPath = "C:/Users/9288/Desktop/Cam_control/config.txt";
            string[] configs = File.ReadAllLines(configPath);
            lineRate = double.Parse(configs[0]);
            exposureTime = double.Parse(configs[1]);
            gain = double.Parse(configs[2]);

            wait = (2 + (height / lineRate));

            Console.WriteLine("Sapera 8k Line Scan Camera control, TASA project.");

            // Create camera object，並連線（可以同時參考Sapera Status Dialog確認相機狀態）
            acqDevice = new SapAcqDevice(location, configFilePath);

            acqDevice = new SapAcqDevice(location);
            if (!acqDevice.Create())
            {
                Console.WriteLine("Error during SapAcqDevice creation.");
                return;
            }

            Console.WriteLine("Successfully connected to the camera.");

            //設定各種相機參數
            acqDevice.SetFeatureValue("AcquisitionLineRate", lineRate); //LineRate如果要用ccf檔的話只能在ccf先設好
            acqDevice.SetFeatureValue("ExposureTime", exposureTime);
            acqDevice.SetFeatureValue("Gain", gain);
            acqDevice.SetFeatureValue("PixelFormat", pixelFormat);
            acqDevice.SetFeatureValue("Height", height);
            acqDevice.SetFeatureValue("ImageTimeout", wait);
            acqDevice.SetFeatureValue("BinningHorizontal", 2);

            //並顯示
            double m_lineRate, m_exposureTime, m_gain, m_height, m_imageTimeout; string m_pixelFormat;
            acqDevice.GetFeatureValue("AcquisitionLineRate", out m_lineRate);
            acqDevice.GetFeatureValue("ExposureTime", out m_exposureTime);
            acqDevice.GetFeatureValue("Gain", out m_gain);
            acqDevice.GetFeatureValue("PixelFormat", out m_pixelFormat);
            acqDevice.GetFeatureValue("Height", out m_height);
            acqDevice.GetFeatureValue("ImageTimeout", out m_imageTimeout);

            Console.WriteLine(
                "\nLine Rate = " + m_lineRate + "Hz" +
                "\nExposure Time = " + m_exposureTime + "us" +
                "\nGain = " + m_gain +
                "\nPixel Format = " + m_pixelFormat +
                "\nImageTimeout = " + m_imageTimeout + "s"
                );


            buffer = new SapBuffer(3, acqDevice, SapBuffer.MemoryType.ScatterGather);
            if (!buffer.Create())
            {
                Console.WriteLine("Error during Buffer creation.");
                return;
            }

            ffc = new SapFlatField(buffer);
            if (!ffc.Create())
            {
                Console.WriteLine("Error during FFC creation.");
                return;
            }
            //ffc.Load(ffcFile);
            ffc.Enable(true, false);

            transfer = new SapAcqDeviceToBuf(acqDevice, buffer);
            if (!transfer.Create())
            {
                Console.WriteLine("Error during Tranfer creation.");
                return;
            }

        }
        public void Scan()
        {
            Console.WriteLine("Capturing...");
            transfer.Snap();
            transfer.Wait(1000 * (int)wait);

            if (ffc.Enabled && ffc.SoftwareCorrection)
            {
                //ffc.Execute(buffer);
            }
            else
            {
                Console.WriteLine("No FFC.");
            }
            
            buffer.Save(imageSavedPath + lineRate + "Hz_" + exposureTime + "us_Gain" + gain + "_PST.tif", "-format tiff");

            Console.WriteLine("\nCapture completed");
            DestroysObjects();
        }

        
        public void Snap()
        {
            Console.WriteLine("Sapera 8k Line Scan Camera control, TASA project.");

            lineRate = 100;
            gain = 1;
            exposureTime = 4;

            // Create camera object，並連線（可以同時參考Sapera Status Dialog確認相機狀態）
            acqDevice = new SapAcqDevice(location, configFilePath);

            acqDevice = new SapAcqDevice(location);
            if (!acqDevice.Create())
            {
                Console.WriteLine("Error during SapAcqDevice creation.");
                return;
            }

            Console.WriteLine("Successfully connected to the camera.");

            //顯示所有可用的Features，可關閉
            
            string[] featureName = acqDevice.FeatureNames;
            Console.WriteLine("\nAvailable Features: " + string.Join(", ", featureName) + "\n");
            

            //顯示最長可用曝光，供設定曝光時間參考
            double maxExposureTime = GetMaxValue("ExposureTime");
            Console.WriteLine("Max Exposure Time: " + maxExposureTime + "us");

            //顯示一個frame最多可用line數
            int maxHeight;
            acqDevice.GetFeatureValue("HeightMax", out maxHeight);
            Console.WriteLine("Max Height: " + maxHeight);

            Console.WriteLine("\nPress any key to start setting.");
            Console.ReadKey();


            wait = (2 + (height / lineRate));
            //設定各種相機參數
            acqDevice.SetFeatureValue("AcquisitionLineRate", lineRate); //LineRate如果要用ccf檔的話只能在ccf先設好
            acqDevice.SetFeatureValue("ExposureTime", exposureTime);
            acqDevice.SetFeatureValue("Gain", gain);
            acqDevice.SetFeatureValue("PixelFormat", pixelFormat);
            acqDevice.SetFeatureValue("Height", height);
            acqDevice.SetFeatureValue("ImageTimeout", wait);
            acqDevice.SetFeatureValue("BinningHorizontal", 2);

            //並顯示
            double m_lineRate, m_exposureTime, m_gain, m_height, m_imageTimeout; string m_pixelFormat;
            acqDevice.GetFeatureValue("AcquisitionLineRate", out m_lineRate);
            acqDevice.GetFeatureValue("ExposureTime", out m_exposureTime);
            acqDevice.GetFeatureValue("Gain", out m_gain);
            acqDevice.GetFeatureValue("PixelFormat", out m_pixelFormat);
            acqDevice.GetFeatureValue("Height", out m_height);
            acqDevice.GetFeatureValue("ImageTimeout", out m_imageTimeout);

            Console.WriteLine(
                "\nLine Rate = " + m_lineRate + "Hz" +
                "\nExposure Time = " + m_exposureTime + "us" +
                "\nGain = " + m_gain +
                "\nPixel Format = " + m_pixelFormat + 
                "\nImageTimeout = " + m_imageTimeout + "s"
                );

            Console.WriteLine("\nPress any key to trigger.");
            Console.ReadKey();
            Console.WriteLine("Capturing...");

            buffer = new SapBuffer(2, acqDevice, SapBuffer.MemoryType.ScatterGather);
            if (!buffer.Create())
            {
                Console.WriteLine("Error during Buffer creation.");
                return;
            } 

            /*
            ffc = new SapFlatField(buffer);
            if (!ffc.Create())
            {
                Console.WriteLine("Error during FFC creation.");
                return;
            }
            ffc.Load(ffcFile);
            ffc.Execute(buffer);
            */

            transfer = new SapAcqDeviceToBuf(acqDevice, buffer);
            if (!transfer.Create())
            {
                Console.WriteLine("Error during Tranfer creation.");
                return;
            }

            transfer.Snap();
            transfer.Wait(1000*(int)wait);
            buffer.Save(imageSavedPath + lineRate + "Hz_" + exposureTime + "us_Gain" + gain + "_Snap.tif", "-format tiff");

            Console.WriteLine("\nCapture completed");
            Console.WriteLine("Press any key to terminate.");
            Console.ReadKey();
            DestroysObjects();
        }
        public void Cycle()
        {
            Console.WriteLine("Sapera 8k Line Scan Camera control, TASA project.");

            lineRate = 100;
            gain = 1;
            exposureTime = 4;

            // 創建相機對象並連接
            acqDevice = new SapAcqDevice(location);
            if (!acqDevice.Create())
            {
                Console.WriteLine("Error during SapAcqDevice creation.");
                return;
            }

            Console.WriteLine("Successfully connected to the camera.");

            // 顯示最長可用曝光時間
            double maxExposureTime = GetMaxValue("ExposureTime");
            Console.WriteLine("Max Exposure Time: " + maxExposureTime + "us");

            // 顯示最大可用高度
            int maxHeight;
            acqDevice.GetFeatureValue("HeightMax", out maxHeight);
            Console.WriteLine("Max Height: " + maxHeight);

            Console.WriteLine("\nPress any key to trigger.");
            Console.ReadKey();
            Console.WriteLine("Capturing...");

            wait = (2 + (height / lineRate));
            // 設定不變的相機參數
            acqDevice.SetFeatureValue("AcquisitionLineRate", lineRate);
            acqDevice.SetFeatureValue("PixelFormat", pixelFormat);
            acqDevice.SetFeatureValue("Height", height);
            acqDevice.SetFeatureValue("ImageTimeout", wait);
            acqDevice.SetFeatureValue("BinningHorizontal", 2);

            // 創建buffer
            buffer = new SapBuffer(2, acqDevice, SapBuffer.MemoryType.ScatterGather);
            if (!buffer.Create())
            {
                Console.WriteLine("Error during Buffer creation.");
                return;
            }

            // 創建ffc 
            /*
            ffc = new SapFlatField(buffer);
            if (!ffc.Create())
            {
                Console.WriteLine("Error during FFC creation.");
                return;
            }
            ffc.Load(ffcFile);
            ffc.Execute(buffer);
            */

            // 創建transfer
            transfer = new SapAcqDeviceToBuf(acqDevice, buffer);
            if (!transfer.Create())
            {
                Console.WriteLine("Error during Transfer creation.");
                return;
            }


            // 循環處理每個曝光時間和gain
            for (int gainCount = 1; gainCount <= gainCycle.Length; gainCount++)
            {
                for (int exposureTimeCount = 1; exposureTimeCount <= exposureTimeCycle.Length; exposureTimeCount++)
                {

                    // 設定曝光時間和gain
                    acqDevice.SetFeatureValue("ExposureTime", exposureTimeCycle[exposureTimeCount - 1]);
                    acqDevice.SetFeatureValue("Gain", gainCycle[gainCount - 1]);

                    // 顯示設定的參數
                    double m_exposureTime, m_gain;
                    acqDevice.GetFeatureValue("ExposureTime", out m_exposureTime);
                    acqDevice.GetFeatureValue("Gain", out m_gain);

                    Console.WriteLine(
                        "\nExposure Time = " + m_exposureTime + "us" +
                        "\nGain = " + m_gain
                    );

                    // 抓取影像並保存
                    transfer.Snap();
                    transfer.Wait(1000*(int)wait);

                    int count = (gainCount - 1) * exposureTimeCycle.Length + exposureTimeCount;

                    string fileName = Path.Combine(imageSavedPath, $"{lineRate}Hz_{m_exposureTime}us_Gain{m_gain}_Cycle_{count:D2}.tif");
                    if (!buffer.Save(fileName, "-format tiff"))
                    {
                        Console.WriteLine($"Error saving image {fileName}");
                    }

                }
            }

            Console.WriteLine("\nCapture completed.");
            // Console.WriteLine("Press any key to terminate.");
            // Console.ReadKey();
            DestroysObjects();
        }


        public void Read()
        {
            Console.WriteLine("Reading images...");
            string[] files = Directory.GetFiles(imageSavedPath, "*.tif");
            using (StreamWriter sw = new StreamWriter(Path.Combine(imageSavedPath, "image_statistics.txt")))
            {
                sw.WriteLine("FileName, Mean, Max, Min, Median");
                foreach (string file in files)
                {
                    Mat image = Cv2.ImRead(file, ImreadModes.Unchanged);
                    if (image.Empty())
                    {
                        Console.WriteLine($"Error reading image {file}");
                        continue;
                    }

                    double sum = 0;
                    int pixelCount = 0;
                    int maxPixel = int.MinValue;
                    int minPixel = int.MaxValue;
                    List<ushort> pixelValues = new List<ushort>();

                    for (int y = 0; y < image.Rows; y++)
                    {
                        for (int x = 0; x < image.Cols; x++)
                        {
                            ushort pixelValue = image.Get<ushort>(y, x);
                            pixelValues.Add(pixelValue);

                            sum += pixelValue;
                            pixelCount++;

                            if (pixelValue > maxPixel) maxPixel = pixelValue;
                            if (pixelValue < minPixel) minPixel = pixelValue;
                        }
                    }

                    double meanPixel = sum / pixelCount;

                    pixelValues.Sort();
                    double medianPixel;
                    if (pixelValues.Count % 2 == 0)
                    {
                        // Even number of elements, take the average of the two middle values
                        medianPixel = (pixelValues[pixelValues.Count / 2 - 1] + pixelValues[pixelValues.Count / 2]) / 2.0;
                    }
                    else
                    {
                        // Odd number of elements, take the middle value
                        medianPixel = pixelValues[pixelValues.Count / 2];
                    }

                    string line = $"{Path.GetFileName(file)}, {meanPixel}, {maxPixel}, {minPixel}, {medianPixel}";
                    sw.WriteLine(line);
                }
            }
            Console.WriteLine("\nImage statistics have been written to image_statistics.txt.");
            Console.WriteLine("Press any key to terminate.");
            Console.ReadKey();
        }

        

        private double GetMaxValue(string featureName) // 用來取得Features的最大值
        {
            SapFeature feature = new SapFeature(location);
            if (!feature.Create()) return -1;
            if (!acqDevice.GetFeatureInfo(featureName, feature)) return -1;
            double maxValue;
            if (!feature.GetValueMax(out maxValue)) return -1;
            return maxValue;
        }

        public void DestroysObjects() // 用來消除RAM裡全部的東西
        {
            if (transfer != null)
            {
                transfer.Destroy();
                transfer.Dispose();
                transfer = null;
            }

            if (acqDevice != null)
            {
                acqDevice.Destroy();
                acqDevice.Dispose();
                acqDevice = null;
            }

            if (buffer != null)
            {
                buffer.Destroy();
                buffer.Dispose();
                buffer = null;
            }

            if (ffc != null)
            {
                ffc.Destroy();
                ffc.Dispose();
                ffc = null;
            }
        }
        
    }
}