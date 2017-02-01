using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Timers;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Drawing.Printing;
using Nikon;
using ImageProcessing.Properties;
namespace ImageProcessing
{
    public partial class TakePhoto : Form
    {
        System.Timers.Timer timer;
        ImageCollection Image;
        Counter counter;
        GreenBox test;
        PrintDocument printDocument1;
        private NikonDevice device;
        System.Timers.Timer closingTimer;
        Bitmap printImage;
        Boolean ClosingTimerStarted = false;
        CoinSelector coinSelector;
        System.Timers.Timer coinWaitTimer;
        CopyCount copyCount;
        public TakePhoto(ImageCollection image, NikonDevice device,CoinSelector selector)
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.Cursor = new Cursor(GetType(), "hand128.cur");
            Cursor.Hide();
            CenterComponents();

            coinSelector = selector;
            coinSelector.OnCoinDetected += coinSelector_OnCoinDetected;
            coinSelector.PollingEnabled = false;
            // Hook up device capture events
            this.device = device;
            device.ImageReady += new ImageReadyDelegate(device_ImageReady);
            device.CaptureComplete += new CaptureCompleteDelegate(device_CaptureComplete);

            counter = new Counter();
            Image = image;
            this.BackgroundImage = new Bitmap(Image.BackgroundImage);
            printImage = new Bitmap(Image.BackgroundImage);
            timer = new System.Timers.Timer(1000);
            timer.Elapsed += new ElapsedEventHandler(CountDown);
            timer.Enabled = false;

            copyCount = new CopyCount(Settings.Instance.CopyCount);

            coinWaitTimer = new System.Timers.Timer();
            coinWaitTimer.Interval = 60000;
            coinWaitTimer.Enabled = false;
            coinWaitTimer.AutoReset = false;
            coinWaitTimer.Elapsed += coinWaitTimer_Elapsed;

            closingTimer = new System.Timers.Timer(6000);

            CounterItem cItem = counter.CountDown();
            pictureBox1.Image = cItem.Number;
            pictureBox2.Image = cItem.Smile;
            timer.Start();

            pictureBox5.Image = generateADImage(copyCount.Count);
            pictureBox8.Image = generateTotalCoinImage(copyCount.Count * Settings.Instance.COST);
            pictureBox9.Image = generateCoinImage(coinSelector.RemainedCoin);
        }

        void coinSelector_OnCoinDetected(object eventObject, CoinDetectedEventArgs args)
        {
            coinWaitTimer.Enabled = false;
            pictureBox9.Invoke((MethodInvoker)delegate
            {
                pictureBox9.Image = generateCoinImage(args.RemainedCoinValue);
            });
            double remainingValue = Settings.Instance.COST * copyCount.Count - args.RemainedCoinValue;
            if (args.RemainedCoinValue >= Settings.Instance.COST * copyCount.Count)
            {
                if (coinSelector.PollingEnabled)
                    coinSelector.PollingEnabled = false;
                coinWaitTimer.Enabled = false;
                pictureBox10.Invoke((MethodInvoker)delegate
                {
                    pictureBox10.Visible = true;
                });
            }
            else
            {
                coinWaitTimer.Enabled = true; 
                pictureBox10.Invoke((MethodInvoker)delegate
                {
                    pictureBox10.Visible = false;
                });
            }
            double[] enabledValues = new double[3];
            if (remainingValue >= CoinValues.TRY025)
                enabledValues[0] = CoinValues.TRY025;
            if (remainingValue >= CoinValues.TRY050)
                enabledValues[1] = CoinValues.TRY050;
            if (remainingValue >= CoinValues.TRY100)
                enabledValues[2] = CoinValues.TRY100;
            coinSelector.SetEnabledCoins(enabledValues);
            
        }
        void coinWaitTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (coinSelector.PollingEnabled)
                coinSelector.PollingEnabled = false;
            coinWaitTimer.Enabled = false;
        }
        void CenterComponents()
        {
            pictureBox1.Height = Height / 4;
            pictureBox2.Height = Height / 8;
            pictureBox3.Height = Height / 8;
            pictureBox1.Width = Width / 2;
            pictureBox2.Width = Width / 2;
            pictureBox3.Width = Width / 2;
            pictureBox3.Top = Height/15;
            pictureBox1.Top = pictureBox3.Top+pictureBox3.Height +Height/6;
            pictureBox2.Top = pictureBox1.Top + pictureBox1.Height;
            pictureBox1.Left = (ClientSize.Width - pictureBox1.Width) / 2;
            pictureBox2.Left = (ClientSize.Width - pictureBox1.Width) / 2;
            pictureBox3.Left = (ClientSize.Width - pictureBox1.Width) / 2;
        }
        void device_ImageReady(NikonDevice sender, NikonImage image)
        {
            if (ClosingTimerStarted)
            {
                closingTimer.Elapsed -= new ElapsedEventHandler(CloseForm);
                closingTimer.Stop();
                ClosingTimerStarted = false;
            }
            closingTimer.Elapsed += new ElapsedEventHandler(CloseForm);
            closingTimer.Start();
            ClosingTimerStarted = true;
            pictureBox1.Invoke((MethodInvoker)delegate
            {
                pictureBox1.Image = ImageProcessing.Properties.Resources.bekleme;
                pictureBox2.Image = null;
            });
            Image.PersonImage = new Bitmap(new MemoryStream(image.Buffer));

        }
        void device_CaptureComplete(NikonDevice sender, int data)
        {
            if (ClosingTimerStarted)
            {
                closingTimer.Elapsed -= new ElapsedEventHandler(CloseForm);
                closingTimer.Stop();
                ClosingTimerStarted = false;
            }
            test = new GreenBox(Image, Position.BottomLeft);
            test.Hue = 2;
            printImage = test.CImage;
            this.BackgroundImage = new Bitmap(printImage);
            pictureBox1.Image = null;
            Cursor.Show();
            if (coinSelector.RemainedCoin < copyCount.Count * Settings.Instance.COST)
            {
                pictureBox10.Visible = false;
                coinSelector.PollingEnabled = true;
            }
            else
            {
                pictureBox10.Visible = true;
                coinSelector.PollingEnabled = false;
            }
            double remainingValue = Settings.Instance.COST * copyCount.Count - coinSelector.RemainedCoin;
            double[] enabledValues = new double[3];
            if (remainingValue >= CoinValues.TRY025)
                enabledValues[0] = CoinValues.TRY025;
            if (remainingValue >= CoinValues.TRY050)
                enabledValues[1] = CoinValues.TRY050;
            if (remainingValue >= CoinValues.TRY100)
                enabledValues[2] = CoinValues.TRY100;
            coinSelector.SetEnabledCoins(enabledValues);
            setPictureCountSelectVisible(true);

            //Print(printImage);
        }
        void setPictureCountSelectVisible(bool value)
        {
            pictureBox4.Visible = value;
            pictureBox5.Visible = value;
            pictureBox6.Visible = value;
            pictureBox6.Visible = value;
            pictureBox7.Visible = value;
            pictureBox8.Visible = value;
            pictureBox9.Visible = value;
            pictureBox11.Visible = value;
        }
        void CloseForm(object o, ElapsedEventArgs a)
        {

            closingTimer.Stop();
            closingTimer.Elapsed -= new ElapsedEventHandler(CloseForm);
            coinSelector.OnCoinDetected -= new CoinSelector.CoinDetectedEventHandler(coinSelector_OnCoinDetected);
            coinSelector.PollingEnabled = false;
            this.Invoke((MethodInvoker)delegate
            {
                this.Close();
            });
        }
        void CountDown(object o, ElapsedEventArgs a)
        {
            CounterItem cItem = counter.CountDown();
            if (cItem.Count == 0)
            {
                timer.Stop(); 
                
                if (device == null)
                {

                    return;
                }

                try
                {
                    pictureBox1.Image = ImageProcessing.Properties.Resources.smile;
                    pictureBox2.Image = null;
                    if (ClosingTimerStarted)
                    {
                        closingTimer.Elapsed -= new ElapsedEventHandler(CloseForm);
                        closingTimer.Stop();
                        ClosingTimerStarted = false;
                    }
                    closingTimer.Elapsed += new ElapsedEventHandler(CloseForm);
                    closingTimer.Start();
                    ClosingTimerStarted = true;
                    device.Capture();

                }
                catch (NikonException ex)
                {
                    if (ex.ErrorCode == eNkMAIDResult.kNkMAIDResult_OutOfFocus)
                    {
                        pictureBox3.Image = null;
                        pictureBox1.Image = ImageProcessing.Properties.Resources.focuserror;
                        pictureBox2.Image = null;
                    }
                    else if (ex.ErrorCode == eNkMAIDResult.kNkMAIDResult_BatteryDontWork)
                    {
                        pictureBox3.Image = null;
                        pictureBox1.Image = ImageProcessing.Properties.Resources.lowbattery;
                        pictureBox2.Image = null;
                    }
                    else if (ex.ErrorCode == eNkMAIDResult.kNkMAIDResult_UnexpectedError)
                    {
                        pictureBox3.Image = null;
                        pictureBox1.Image = ImageProcessing.Properties.Resources.unknownerror;
                        pictureBox2.Image = null;
                    }
                    /*
                    closingTimer.Elapsed += new ElapsedEventHandler(CloseForm);
                    closingTimer.Start();*/
                    counter = new Counter();
                    timer.Enabled = true;
                    timer.Start();
                }
                return;
            }
            pictureBox1.Image = cItem.Number;
            pictureBox2.Image = cItem.Smile;

        }
        void Print(Bitmap Image,int printCount)
        {   
            printDocument1 = new System.Drawing.Printing.PrintDocument();
            printDocument1.PrintPage += new PrintPageEventHandler(this.printDocument1_PrintPage);
            printDocument1.EndPrint += new PrintEventHandler(this.printDocument1_EndPrint);
            System.Drawing.Printing.PrintController printController = new System.Drawing.Printing.StandardPrintController();
            printDocument1.PrintController = printController;
            if (!File.Exists("data.bin"))
            {
                PrintDialog printDialog1 = new PrintDialog();
                DialogResult dr = printDialog1.ShowDialog();
                using (Stream stream = System.IO.File.Open("data.bin", FileMode.Create))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    bin.Serialize(stream, printDialog1.PrinterSettings);
                }
            }
            using (Stream stream = File.Open("data.bin", FileMode.Open))
            {
                BinaryFormatter bin = new BinaryFormatter();

                System.Drawing.Printing.PrinterSettings setting = (System.Drawing.Printing.PrinterSettings)bin.Deserialize(stream);
                printDocument1.PrinterSettings = setting;
            }
            //Image.Save("Image.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
            for(int i=0;i<printCount;i++)
                printDocument1.Print();
        }
        private void printDocument1_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            //double cmToUnits = 100 / 2.54;
            float oran = (float)e.PageSettings.PaperSize.Width / (float)printImage.Width;//(float)(17.8 * cmToUnits) / printImage.Width;
            float width = e.PageSettings.PaperSize.Width;//(float)(17.8 * cmToUnits);
            float height = printImage.Height * oran;
            //(float)(12.7 * cmToUnits)
            e.Graphics.DrawImage(printImage, (e.PageSettings.PaperSize.Width - width) / 2.0f, (e.PageSettings.PaperSize.Height - height) / 2.0f, width, height);
            //e.Graphics.DrawImage(printImage, 0, 0, e.PageSettings.PaperSize.Width, e.PageSettings.PaperSize.Height);
            
        }
        private void printDocument1_EndPrint(object sender,PrintEventArgs e)
        {
            coinSelector.UsedCoin = Settings.Instance.COST;
        }

        private void TakePhoto_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyData)
            {
                case Keys.Escape:
                    Close();
                    break;
            }
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void TakePhoto_FormClosing(object sender, FormClosingEventArgs e)
        {
            Cursor.Show();
            timer.Elapsed -=  new ElapsedEventHandler(CountDown);
            timer.Stop();
            // Hook up device capture events
            device.ImageReady -= new ImageReadyDelegate(device_ImageReady);
            device.CaptureComplete -= new CaptureCompleteDelegate(device_CaptureComplete);
        }

        private void TakePhoto_ResizeEnd(object sender, EventArgs e)
        {
            CenterComponents();
        }

        private void TakePhoto_ClientSizeChanged(object sender, EventArgs e)
        {
            CenterComponents();
        }

        private void pictureBox6_Click(object sender, EventArgs e)
        {
            copyCount.Count += 1;
            if (coinSelector.RemainedCoin < copyCount.Count * Settings.Instance.COST)
            {
                pictureBox10.Visible = false;
                coinSelector.PollingEnabled = true;
            }
            else
            {
                pictureBox10.Visible = true;
            }
            double remainingValue = Settings.Instance.COST * copyCount.Count - coinSelector.RemainedCoin;
            double[] enabledValues = new double[3];
            if (remainingValue >= CoinValues.TRY025)
                enabledValues[0] = CoinValues.TRY025;
            if (remainingValue >= CoinValues.TRY050)
                enabledValues[1] = CoinValues.TRY050;
            if (remainingValue >= CoinValues.TRY100)
                enabledValues[2] = CoinValues.TRY100;
            coinSelector.SetEnabledCoins(enabledValues);
            pictureBox5.Image = generateADImage(copyCount.Count);
            pictureBox8.Image = generateTotalCoinImage(copyCount.Count * Settings.Instance.COST);
        }

        private void pictureBox7_Click(object sender, EventArgs e)
        {
            if ((copyCount.Count - 1) * Settings.Instance.COST >= coinSelector.RemainedCoin)
            {
                copyCount.Count -= 1;
                double remainingValue = Settings.Instance.COST * copyCount.Count - coinSelector.RemainedCoin;
                double[] enabledValues = new double[3];
                if (remainingValue >= CoinValues.TRY025)
                    enabledValues[0] = CoinValues.TRY025;
                if (remainingValue >= CoinValues.TRY050)
                    enabledValues[1] = CoinValues.TRY050;
                if (remainingValue >= CoinValues.TRY100)
                    enabledValues[2] = CoinValues.TRY100;
                coinSelector.SetEnabledCoins(enabledValues);
                pictureBox5.Image = generateADImage(copyCount.Count);
                pictureBox8.Image = generateTotalCoinImage(copyCount.Count * Settings.Instance.COST);
            }
            if (copyCount.Count * Settings.Instance.COST == coinSelector.RemainedCoin)
                pictureBox10.Visible = true;
            else
                pictureBox10.Visible = false;
        }

        private Image getNumberImage(int number)
        {
            switch (number)
            {
                case 0: return ImageProcessing.Properties.Resources._0;
                case 1: return ImageProcessing.Properties.Resources._1;
                case 2: return ImageProcessing.Properties.Resources._2;
                case 3: return ImageProcessing.Properties.Resources._3;
                case 4: return ImageProcessing.Properties.Resources._4;
                case 5: return ImageProcessing.Properties.Resources._5;
                case 6: return ImageProcessing.Properties.Resources._6;
                case 7: return ImageProcessing.Properties.Resources._7;
                case 8: return ImageProcessing.Properties.Resources._8;
                case 9: return ImageProcessing.Properties.Resources._9;
                default: return ImageProcessing.Properties.Resources._0;
            }
        }
        private Image generateADImage(int value)
        {

            float numberWidth = Resources._0.Width;
            float totalWidth = Resources.ad.Width + (value > 9 ? numberWidth*2 : numberWidth);
            int usedWidth = 0;
            Image image = new Bitmap((int)totalWidth, Resources.ad.Height);
            Graphics g = Graphics.FromImage(image);
            if (value > 9)
            {
                g.DrawImage(getNumberImage((value / 10) % 10), usedWidth, 0, numberWidth, image.Height);
                usedWidth += (int)numberWidth;
            }
            g.DrawImage(getNumberImage(value % 10), usedWidth, 0, numberWidth, image.Height);
            usedWidth += (int)numberWidth;
            g.DrawImage(Resources.ad, usedWidth, 0, Resources.ad.Width, image.Height);
            return image;
        }
        private Image generateCoinImage(double value)
        {
            float rate = Resources.tl.Height / (float)Resources._0.Height;
            float numberWidth = Resources._0.Width * rate;
            float totalWidth = Resources.inserted.Width + (value > 9.99 ? numberWidth * 4 : numberWidth * 3) + Resources.comma.Width + Resources.tl.Width;
            int usedWidth = 0;
            Image image = new Bitmap((int)totalWidth, Resources.tl.Height);
            Graphics g = Graphics.FromImage(image);
            g.DrawImage(Resources.inserted, usedWidth, 0, Resources.inserted.Width, image.Height);
            usedWidth += Resources.inserted.Width;
            if (value > 9.99)
            {
                g.DrawImage(getNumberImage((int)(value / 10) % 10 ), usedWidth, 0, numberWidth, image.Height);
                usedWidth += (int)numberWidth;
            }
            g.DrawImage(getNumberImage((int)value % 10), usedWidth, 0, numberWidth, image.Height);
            usedWidth += (int)numberWidth;
            g.DrawImage(Resources.comma, usedWidth, 0, Resources.comma.Width, image.Height);
            usedWidth += Resources.comma.Width;
            g.DrawImage(getNumberImage((int)(value * 10) % 10), usedWidth, 0, numberWidth, image.Height);
            usedWidth += (int)numberWidth;
            g.DrawImage(getNumberImage((int)(value * 100) % 10), usedWidth, 0, numberWidth, image.Height);
            usedWidth += (int)numberWidth;
            g.DrawImage(Resources.tl, usedWidth, 0, Resources.tl.Width, image.Height);
            return image;
        }
        private Image generateTotalCoinImage(double value)
        {
            float rate = Resources.tl.Height / (float)Resources._0.Height;
            float numberWidth = Resources._0.Width * rate;
            float totalWidth = Resources.toplam.Width + (value > 9.99 ? numberWidth * 4 : numberWidth * 3) + Resources.att.Width;
            int usedWidth = 0;
            Image image = new Bitmap((int)totalWidth, Resources.tl.Height);
            Graphics g = Graphics.FromImage(image);
            g.DrawImage(Resources.toplam, usedWidth, 0, Resources.toplam.Width, image.Height);
            usedWidth += Resources.toplam.Width;
            if (value > 9.99)
            {
                g.DrawImage(getNumberImage((int)(value / 10) % 10 ), usedWidth, 0, numberWidth, image.Height);
                usedWidth += (int)numberWidth;
            }
            g.DrawImage(getNumberImage((int)value % 10), usedWidth, 0, numberWidth, image.Height);
            usedWidth += (int)numberWidth;
            g.DrawImage(Resources.comma, usedWidth, 0, Resources.comma.Width, image.Height);
            usedWidth += Resources.comma.Width;
            g.DrawImage(getNumberImage((int)(value * 10) % 10), usedWidth, 0, numberWidth, image.Height);
            usedWidth += (int)numberWidth;
            g.DrawImage(getNumberImage((int)(value * 100) % 10), usedWidth, 0, numberWidth, image.Height);
            usedWidth += (int)numberWidth;
            g.DrawImage(Resources.att, usedWidth, 0, Resources.att.Width, image.Height);
            return image;
        }

        private void pictureBox10_Click(object sender, EventArgs e)
        {
            if (copyCount.Count * Settings.Instance.COST <= coinSelector.RemainedCoin)
            {
                Print(printImage, copyCount.Count);
                setPictureCountSelectVisible(false);
                pictureBox3.Image = ImageProcessing.Properties.Resources.alin;
                pictureBox1.Image = null;
                pictureBox2.Image = null;
                closingTimer.Elapsed += new ElapsedEventHandler(CloseForm);
                closingTimer.Start();
            }
        }

        private void pictureBox11_Click(object sender, EventArgs e)
        {
            if(closingTimer.Enabled)
                closingTimer.Stop();
            closingTimer.Elapsed -= new ElapsedEventHandler(CloseForm);
            coinSelector.OnCoinDetected -= new CoinSelector.CoinDetectedEventHandler(coinSelector_OnCoinDetected);
            coinSelector.PollingEnabled = false;
            this.Invoke((MethodInvoker)delegate
            {
                this.Close();
            });
        }

    }
}
