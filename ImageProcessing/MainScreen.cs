﻿using ImageProcessing.Properties;
using Nikon;
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

namespace ImageProcessing
{
    public partial class MainScreen : Form
    {
        int viewCount;
        ImageList list;
        ImageCollection[] collection;
        ImageRead imRead;
        private NikonManager manager;
        private NikonDevice device;
        Timer imageSenderTimer;
        System.Threading.Thread coinSelectorThread;
        CoinSelector coinSelector;
        CoinSelectorError coinSelectorError;
        ImageCollection _selectedImage;
        CopyCount copyCount;
        public MainScreen()
        {
            InitializeComponent();
            CenterComponents();
            this.Cursor = new Cursor(GetType(), "hand128.cur");

            coinSelectorThread = new System.Threading.Thread(new System.Threading.ThreadStart(coinSelectorThreadFunction));
            coinSelectorThread.Start();

            imageSenderTimer =new Timer();
            imageSenderTimer.Interval = 60000;
            imageSenderTimer.Tick += imageSenderTimer_Tick;
            imageSenderTimer.Start();


            manager = new NikonManager("D40_Mod.md3");
            manager.DeviceAdded += new DeviceAddedDelegate(manager_DeviceAdded);
            manager.DeviceRemoved += new DeviceRemovedDelegate(manager_DeviceRemoved);

            viewCount = 3;
            list = new ImageList(viewCount);
            collection = new ImageCollection[viewCount];
            imRead = new ImageRead("images");
            while (imRead.GetImage())
                list.Add(imRead.CurrentImage);
            try
            {
                collection = list.CurrentItems;
                FillPictureBoxes();
            }
            catch (NoItemFoundException)
            {
                AutoClosingMessageBox.Show(this,"Sisteme kayıtlı fotoğraf bulunamadı", "Hata", 3000,MessageBoxIcon.Error);
               // MessageBox.Show(this, "Sisteme kayıtlı fotoğraf bulunamadı.","Hata",MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
        }



        void CenterComponents()
        {
            int width = Width - (Width / 20);
            int oneParcelWidth = width / 23;
            int oneParcelHeight = (Height / 25);
            /*button1.Top = ClientSize.Height - button1.Height - oneParcelHeight;
            button1.Left = (ClientSize.Width - button1.Width) / 2;*/
            pictureBox1.Height = oneParcelHeight *4;
            pictureBox1.Width = oneParcelWidth * 12;
            pictureBox1.Top = oneParcelHeight;
            pictureBox1.Left = (ClientSize.Width - pictureBox1.Width) / 2;
            pictureBox5.Height = oneParcelHeight * 9;
            pictureBox5.Width = oneParcelWidth * 2;
            pictureBox5.Top = pictureBox1.Top + pictureBox1.Height + (oneParcelHeight *4);
            pictureBox5.Left = (Width / 40);
            pictureBox2.Height = oneParcelHeight * 9;
            pictureBox2.Width = oneParcelWidth * 5;
            pictureBox2.Left = pictureBox5.Left + pictureBox5.Width + oneParcelWidth;
            pictureBox2.Top = pictureBox1.Top + pictureBox1.Height + (oneParcelHeight * 4);
            pictureBox3.Height = oneParcelHeight * 9;
            pictureBox3.Width = oneParcelWidth * 5;
            pictureBox3.Left = pictureBox2.Left + pictureBox2.Width + oneParcelWidth;
            pictureBox3.Top = pictureBox1.Top + pictureBox1.Height + (oneParcelHeight * 4);
            pictureBox4.Height = oneParcelHeight * 9;
            pictureBox4.Width = oneParcelWidth * 5;
            pictureBox4.Left = pictureBox3.Left + pictureBox3.Width +oneParcelWidth;
            pictureBox4.Top = pictureBox1.Top + pictureBox1.Height + (oneParcelHeight * 4);
            pictureBox6.Height = oneParcelHeight * 9;
            pictureBox6.Width = oneParcelWidth * 2;
            pictureBox6.Top = pictureBox1.Top + pictureBox1.Height + (oneParcelHeight * 4);
            pictureBox6.Left = pictureBox4.Left + pictureBox4.Width + oneParcelWidth;     
        }

        public void coinSelectorThreadFunction()
        {
            coinSelector = new CoinSelector();
            coinSelectorError = coinSelector.SearchDevices();
            if (coinSelectorError == CoinSelectorError.OK)
                coinSelectorError = coinSelector.ConnectDevice();
            double[] coinValues = new double[3];
            coinValues[0] = CoinValues.TRY025;
            coinValues[1] = CoinValues.TRY050;
            coinValues[2] = CoinValues.TRY100;
            if (coinSelectorError == CoinSelectorError.OK)
            {
                coinSelectorError = coinSelector.SetEnabledCoins(coinValues);
                if (coinSelectorError == CoinSelectorError.OK)
                {
                    pictureBox7.Invoke((MethodInvoker)delegate
                    {
                        pictureBox7.Visible = false;
                    });
                }
                else
                {
                    LogManager.Log(new LogData(string.Format("Geçerli para birimleri kaydedilemedi."), LogType.Error));
                }
            }
            else
            {
                LogManager.Log(new LogData(string.Format("Para tanıma cihazı bulunamadı."), LogType.Error));
            }
            coinSelectorThread.Abort();
        }


        void imageSenderTimer_Tick(object sender, EventArgs e)
        {
            imageSenderTimer.Stop();
            SQL sql = new SQL();
            List<DBImageData> list = sql.GetUnsendImages();
            int count = 0;

            foreach (DBImageData d in list)
            {
                ImageFormat format = ImageFormat.Jpeg;
                if (d.ImageType == "Jpeg") format = ImageFormat.Jpeg;
                else if (d.ImageType == "Png") format = ImageFormat.Png;
                else if (d.ImageType == "Bmp") format = ImageFormat.Bmp;
                try
                {
                    if (Server.Instance.SaveImage(Image.FromFile(@"output/combined/" + d.ImageID + "." + d.ImageType), format, d.ImageID))
                    {
                        while (!sql.SetSend(d.ImageID)) ;
                        count++;
                    }
                    else
                    {
                        LogManager.Log(new LogData(string.Format("{0}.{1} sunucuya yüklenirken hata oluştu.", d.ImageID, d.ImageType), LogType.Warning));
                    }
                }
                catch (UnAutExeption)
                {
                    LogManager.Log(new LogData("Sunucu kullanıcı hatası. Tekrar giriş yapılıyor..."));
                    if (Server.Instance.Login())
                    {
                        LogManager.Log(new LogData("Sunucu bağlantısı tekrar kuruldu."));
                        if (Server.Instance.SaveImage(Image.FromFile(@"output/combined/" + d.ImageID + "." + d.ImageType), format, d.ImageID))
                        {
                            while (!sql.SetSend(d.ImageID)) ;
                            count++;
                        }
                        else
                        {
                            LogManager.Log(new LogData(string.Format("{0}.{1} sunucuya yüklenirken hata oluştu.", d.ImageID, d.ImageType), LogType.Warning));
                        }
                    }
                    else
                    {
                        LogManager.Log(new LogData("Sunucuya giriş yapılamadı. Giriş bilgilerinizi kontrol ediniz",LogType.Error));
                        return;
                    }
                        
                }
            }
            if (count > 0) 
                LogManager.Log(new LogData(string.Format("{0} adet fotoğraf sunucuya başarıyla yüklendi.",count)));

            imageSenderTimer.Start();

        }
        

        void manager_DeviceAdded(NikonManager sender, NikonDevice device)
        {
            this.device = device;

        }
        void manager_DeviceRemoved(NikonManager sender, NikonDevice device)
        {
            this.device = null;
        }
        private void button1_MouseHover(object sender, EventArgs e)
        {
        }

        private void button1_MouseEnter(object sender, EventArgs e)
        {
           
        }

        private void tableLayoutPanel5_Paint(object sender, PaintEventArgs e)
        {
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            if (list.Collection.Count == 0) return;
            if (coinSelector.Connected == false) return;
            if (device == null)
            {
                AutoClosingMessageBox.Show(this,"Bağlı fotoğraf makinesi bulunamadı. Lütfen kontrol ediniz", "Hata", 3000,MessageBoxIcon.Error);
                //MessageBox.Show(this,"Bağlı fotoğraf makinesi bulunamadı. Lütfen kontrol ediniz","Hata",MessageBoxButtons.OK,MessageBoxIcon.Error);
                return;
            }
            new TakePhoto(collection[0], device, coinSelector).Show();

        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            if (list.Collection.Count == 0) return;
            if (device == null)
            {
                AutoClosingMessageBox.Show(this,"Bağlı fotoğraf makinesi bulunamadı. Lütfen kontrol ediniz", "Hata", 3000,MessageBoxIcon.Error);
                //MessageBox.Show(this, "Bağlı fotoğraf makinesi bulunamadı. Lütfen kontrol ediniz", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            new TakePhoto(collection[1], device, coinSelector).Show();
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            if (list.Collection.Count == 0) return;
            if (device == null)
            {
                AutoClosingMessageBox.Show(this,"Bağlı fotoğraf makinesi bulunamadı. Lütfen kontrol ediniz", "Hata", 3000,MessageBoxIcon.Error);
                //MessageBox.Show(this, "Bağlı fotoğraf makinesi bulunamadı. Lütfen kontrol ediniz", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            new TakePhoto(collection[2], device, coinSelector).Show();
        }
        private void pictureBox5_Click(object sender, EventArgs e)
        {
            if (list.Collection.Count == 0) return;
            list.Previous();
            collection = list.CurrentItems;
            FillPictureBoxes();
        }

        private void pictureBox6_Click(object sender, EventArgs e)
        {
            if (list.Collection.Count == 0) return;
            list.Next();
            collection = list.CurrentItems;
            FillPictureBoxes();

        }

        void FillPictureBoxes()
        {
            if (list.Collection.Count == 0) return;
            pictureBox2.Image = collection[0].ButtonImage;
            pictureBox3.Image = collection[1].ButtonImage;
            pictureBox4.Image = collection[2].ButtonImage;
        }

        private void MainScreen_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyData)
            {
                case Keys.Right:
                    pictureBox6_Click(null, null);
                    break;
                case Keys.Left:
                    pictureBox5_Click(null, null);
                    break;
                case Keys.D1:
                case Keys.NumPad1:
                    pictureBox2_Click(null, null);
                    break;
                case Keys.D2:
                case Keys.NumPad2:
                    pictureBox3_Click(null, null);
                    break;
                case Keys.D3:
                case Keys.NumPad3:
                    pictureBox4_Click(null, null);
                    break;
            }
        }

        private void MainScreen_FormClosing(object sender, FormClosingEventArgs e)
        {
            manager.DeviceAdded -= new DeviceAddedDelegate(manager_DeviceAdded);
            manager.DeviceRemoved -= new DeviceRemovedDelegate(manager_DeviceRemoved);
            manager.Shutdown();
            device = null;
            manager = null;
        }

        private void MainScreen_ClientSizeChanged(object sender, EventArgs e)
        {
            CenterComponents();
        }

        private void btnIncrease_Click(object sender, EventArgs e)
        {

        }

        private void btnDecrease_Click(object sender, EventArgs e)
        {

        }


    }
}
