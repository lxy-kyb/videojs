using MahApps.Metro.Controls;
using Oraycn.MCapture;
using Oraycn.MFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;

namespace MapTester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private ISoundcardCapturer soundcardCapturer;
        private IDesktopCapturer desktopCapturer;

        private VideoFileMaker videoFileMaker;
        private SilenceVideoFileMaker silenceVideoFileMaker;
        private AudioFileMaker audioFileMaker;
        private int frameRate = 10; // 采集视频的帧频
        private bool sizeRevised = false;// 是否需要将图像帧的长宽裁剪为4的整数倍
        private volatile bool isRecording = false;
        private volatile bool isParsing = false;
        private System.Windows.Forms.Timer timer;
        private int seconds = 0;
        private bool justRecordVideo = false;
        private bool justRecordAudio = false;

        public MainWindow()
        {
            InitializeComponent();
            Oraycn.MCapture.GlobalUtil.SetAuthorizedUser("FreeUser", "");
            Oraycn.MFile.GlobalUtil.SetAuthorizedUser("FreeUser", "");
            this.timer = new System.Windows.Forms.Timer();
            this.timer.Interval = 1000;
            this.timer.Tick += timer_Tick;
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if (this.isRecording && !this.isParsing)
            {
                var ts = new TimeSpan(0, 0, ++seconds);
            }
        }

        String url = AppDomain.CurrentDomain.BaseDirectory + "index.html";
        Uri uri;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //IEVersion.BrowserEmulationSet();
            url = url.Replace('\\', '/');
            string[] Array = url.Split(':');
            string newurl = String.Format("file://127.0.0.1/{0}$", Array[0]) + Array[1];
            uri = new Uri(newurl);
            ObjectForScriptingHelper helper = new ObjectForScriptingHelper(this);
            webMap.ObjectForScripting = helper;
            webMap.Navigate(uri);
        }

        #region 开始
        private void button_Start_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show(this.user.PointFromScreen(new System.Windows.Point()).X.ToString());
            System.Drawing.Rectangle r = new System.Drawing.Rectangle((int)this.webMap.PointToScreen(new System.Windows.Point()).X, (int)this.webMap.PointToScreen(new System.Windows.Point()).Y, (int)webMap.Width, (int)webMap.Height);
            System.Windows.MessageBox.Show(r.X + " " + r.Y + " " + r.Width + " " + r.Height);
            //TODO 开始录制桌面，依据 声音复选框 来选择使用 声卡 麦克风 还是混合录制
            //TODO label 中显示实际录制的时间，需要考虑暂停和恢复这种情况。 格式为 hh:mm:ss
            try
            {
                int audioSampleRate = 16000;
                int channelCount = 2;
                seconds = 0;

                System.Drawing.Size videoSize = Screen.PrimaryScreen.Bounds.Size;

                #region 设置采集器

                //桌面采集器

                //如果需要录制鼠标的操作，第二个参数请设置为true
                this.desktopCapturer = CapturerFactory.CreateDesktopCapturer(frameRate, false, r);
                this.desktopCapturer.ImageCaptured += ImageCaptured;
                videoSize = this.desktopCapturer.VideoSize;

                //声卡采集器 【目前声卡采集仅支持vista以及以上系统】
                this.soundcardCapturer = CapturerFactory.CreateSoundcardCapturer();
                this.soundcardCapturer.CaptureError += capturer_CaptureError;
                audioSampleRate = this.soundcardCapturer.SampleRate;
                this.soundcardCapturer.AudioCaptured += audioMixter_AudioMixed;

                #endregion

                #region 开始采集

                this.soundcardCapturer.Start();
                this.desktopCapturer.Start();

                #endregion

                #region 录制组件              

                this.justRecordVideo = false;
                this.videoFileMaker = new VideoFileMaker();
                this.videoFileMaker.Initialize("test.mp4", VideoCodecType.H264, videoSize.Width, videoSize.Height, frameRate, VideoQuality.High, AudioCodecType.AAC, audioSampleRate, channelCount, true);

                #endregion

                this.isRecording = true;
                this.isParsing = false;
                this.timer.Start();

                this.btn_Start.IsEnabled = false;
            }
            catch (Exception ee)
            {
                System.Windows.MessageBox.Show(ee.Message);
            }
        }
        #endregion

        #region 结束
        private void button_Stop_Click(object sender, RoutedEventArgs e)
        {
            ////TODO 结束录制，保存文件 

            this.btn_Start.IsEnabled = true;

            this.soundcardCapturer.Stop();

            this.desktopCapturer.Stop();

            if (this.justRecordAudio)
            {
                this.audioFileMaker.Close(true);
            }
            else
            {
                if (!this.justRecordVideo)
                {
                    this.videoFileMaker.Close(true);
                }
                else
                {
                    this.silenceVideoFileMaker.Close(true);
                }
            }

            this.isRecording = false;
            System.Windows.MessageBox.Show("录制完成！");
        }
        #endregion

        void audioMixter_AudioMixed(byte[] audioData)
        {
            if (this.isRecording && !this.isParsing)
            {
                if (this.justRecordAudio)
                {
                    this.audioFileMaker.AddAudioFrame(audioData);
                }
                else
                {
                    if (!this.justRecordVideo)
                    {
                        this.videoFileMaker.AddAudioFrame(audioData);
                    }
                }
            }
        }

        void capturer_CaptureError(Exception ex)
        {

        }

        #region ImageCaptured
        void ImageCaptured(Bitmap bm)
        {
            if (this.isRecording && !this.isParsing)
            {
                //这里可能要裁剪
                Bitmap imgRecorded = bm;
                if (this.sizeRevised) // 对图像进行裁剪，  MFile要求录制的视频帧的长和宽必须是4的整数倍。
                {
                    imgRecorded = ESBasic.Helpers.ImageHelper.RoundSizeByNumber(bm, 4);
                    bm.Dispose();
                }
                if (!this.justRecordVideo)
                {
                    this.videoFileMaker.AddVideoFrame(imgRecorded);
                }
                else
                {
                    this.silenceVideoFileMaker.AddVideoFrame(imgRecorded);
                }
            }
        }
        #endregion

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (this.isRecording)
            {
                System.Windows.MessageBox.Show("正在录制视频，请先停止！");
                e.Cancel = true;
                return;
            }
            e.Cancel = false;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void btn_PLAY_Click(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke(DispatcherPriority.Normal,
                                     new Action(() => webMap.InvokeScript("PlayVideo", new object[] { "oceans.mp4" })));
        }

        private void btn_Percent_Click(object sender, RoutedEventArgs e)
        {
            Thread thread = new Thread(Proc);
            thread.Start();
        }

        private void Proc()
        {
            for(int i = 100; i >=0 ; i--)
            {
                this.Dispatcher.Invoke(DispatcherPriority.Normal,
                         new Action(() => webMap.InvokeScript("SetPercent", new object[] { i })));
                Thread.Sleep(1000);
            }
        }

        private void btn_Clock_Click(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke(DispatcherPriority.Normal,
                         new Action(() => webMap.InvokeScript("SetClock", new object[] { 15*1000 })));
        }

        private void btn_Normal_Click(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke(DispatcherPriority.Normal,
                         new Action(() => webMap.InvokeScript("SetTextClass", new object[] { false })));
        }

        private void btn_Bling_Click(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke(DispatcherPriority.Normal,
             new Action(() => webMap.InvokeScript("SetTextClass", new object[] { true })));
        }
    }
}
