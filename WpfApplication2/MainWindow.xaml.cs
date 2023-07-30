using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using NAudio;
using NAudio.Wave;
using System.Numerics;


namespace WpfApplication2
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public class FFT
    {
        /// <summary>
        /// Вычисление поворачивающего модуля e^(-i*2*PI*k/N)
        /// </summary>
        /// <param name="k"></param>
        /// <param name="N"></param>
        /// <returns></returns>
        private static Complex w(int k, int N)
        {
            if (k % N == 0) return 1;
            double arg = -2 * Math.PI * k / N;
            return new Complex(Math.Cos(arg), Math.Sin(arg));
        }
        /// <summary>
        /// Возвращает спектр сигнала
        /// </summary>
        /// <param name="x">Массив значений сигнала. Количество значений должно быть степенью 2</param>
        /// <returns>Массив со значениями спектра сигнала</returns>
        public static Complex[] fft(Complex[] x)
        {
            Complex[] X;
            int N = x.Length;
            if (N == 2)
            {
                X = new Complex[2];
                X[0] = x[0] + x[1];
                X[1] = x[0] - x[1];
            }
            else
            {
                Complex[] x_even = new Complex[N / 2];
                Complex[] x_odd = new Complex[N / 2];
                for (int i = 0; i < N / 2; i++)
                {
                    x_even[i] = x[2 * i];
                    x_odd[i] = x[2 * i + 1];
                }
                Complex[] X_even = fft(x_even);
                Complex[] X_odd = fft(x_odd);
                X = new Complex[N];
                for (int i = 0; i < N / 2; i++)
                {
                    X[i] = X_even[i] + w(i, N) * X_odd[i];
                    X[i + N / 2] = X_even[i] - w(i, N) * X_odd[i];
                }
            }
            return X;
        }
        /// <summary>
        /// Центровка массива значений полученных в fft (спектральная составляющая при нулевой частоте будет в центре массива)
        /// </summary>
        /// <param name="X">Массив значений полученный в fft</param>
        /// <returns></returns>
        public static Complex[] nfft(Complex[] X)
        {
            int N = X.Length;
            Complex[] X_n = new Complex[N];
            for (int i = 0; i < N / 2; i++)
            {
                X_n[i] = X[N / 2 + i];
                X_n[N / 2 + i] = X[i];
            }
            return X_n;
        }
    }
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // StartRecording(10);
        }
        WaveIn wi;
        WaveFileWriter wfw;
        Polyline pl;
        Polyline ln;

        double canH = 0;
        double canW = 0;
        double plH = 0;
        double plW = 0;
        double lnH = 0;
        double lnW = 0;
       public int time = 0;
       public double seconds = 0;
        public bool saveflag = true;



        List<byte> totalbytes;
        Queue<Point> displaypts;
        //Queue<short> displaysht;
        Queue<Complex> displaysht;
        int t=0;
        long[,] mas = new long[13, 8192];
       public long count = 0;
        int numtodisplay = 8192;
        //sample 1/100, display for 5 seconds


        void StartRecording(int time)
        {
            wi = new WaveIn();
            int waveInDevices = WaveIn.DeviceCount;
            wi.DataAvailable += new EventHandler<WaveInEventArgs>(wi_DataAvailable);
            wi.RecordingStopped += new EventHandler<StoppedEventArgs>(wi_RecordingStopped);
            wi.WaveFormat = new WaveFormat(44100, 32, 2);
            wfw = new WaveFileWriter("record.wav", wi.WaveFormat);
            canH = waveCanvas.Height;
            canW = waveCanvas.Width;


            pl = new Polyline();
            pl.Stroke = Brushes.Blue;
            pl.Name = "waveform";
            pl.StrokeThickness = 1;
            pl.MaxHeight = canH - 4;
            pl.MaxWidth = canW - 4;
            plH = pl.MaxHeight;
            plW = pl.MaxWidth;

           // ln = new Line();
          //  ln.Width = 4;
          //  ln.Stroke = Brushes.White;
           // lnH = canva.Height;

            this.time = time;
            displaypts = new Queue<Point>();
            totalbytes = new List<byte>();
            //displaysht = new Queue<short>();
            displaysht = new Queue<Complex>();

            
            wi.StartRecording();
        }


        void wi_RecordingStopped(object sender, EventArgs e)
        {
            wi.Dispose();
            wi = null;
            wfw.Close();
            wfw.Dispose();   


            wfw = null;

        }


        void wi_DataAvailable(object sender, WaveInEventArgs e)
        {
            seconds += (double)(1.0 * e.BytesRecorded / wi.WaveFormat.AverageBytesPerSecond * 1.0);
            if (seconds > time)
            {
                wi.StopRecording();
            }


            wfw.Write(e.Buffer, 0, e.BytesRecorded);
            totalbytes.AddRange(e.Buffer);


            //byte[] shts = new byte[2];
            byte[] shts = new byte[4];


            for (int i = 0; i < 32767; i += 4)
            {
                shts[0] = e.Buffer[i];
                shts[1] = e.Buffer[i + 1];
                shts[2] = e.Buffer[i + 2];
                shts[3] = e.Buffer[i + 3];
                if (count < numtodisplay)
                {
                    displaysht.Enqueue(BitConverter.ToInt32(shts, 0));
                    ++count;
                }
                else
                {
                    displaysht.Dequeue();

                    Int32 y = BitConverter.ToInt32(shts, 0);

                    displaysht.Enqueue((Complex)y);
                }
            }
            this.waveCanvas.Children.Clear();
            pl.Points.Clear();
            //short[] shts2 = displaysht.ToArray();
            Complex[] shts2 = displaysht.ToArray();
            for (int x = 0; x < shts2.Length; ++x)
            {
                pl.Points.Add(Normalize(x, (Int32)(shts2[x].Real)));
            }



            this.waveCanvas.Children.Add(pl);


            Complex[] Q = FFT.fft(shts2);
            int k = 0;
            //    File.AppendAllLines(@"D:\vig\Spectr.txt",Q);
            StreamWriter sw = new StreamWriter("Spectr.txt", true, Encoding.ASCII);

            foreach (Complex i in Q)

            {

                sw.Write((long)i.Magnitude);
                k++;
                if (k != 8192)
                    sw.Write((char)32);
                else k = 0;
            }

            sw.Write((char)33);
            //sw.Write((char)32);
            sw.Close();
            //   this.canva.Children.Clear();

         

          
            
        }

      



        Point Normalize(Int32 x, Int32 y)
        {
            Point p = new Point();


            p.X = 1.0 * x / numtodisplay * plW;
            //p.Y = plH/2.0 - y / (short.MaxValue*1.0) * (plH/2.0);
            p.Y = plH / 2.0 - y / (Int32.MaxValue * 1.0) * (plH / 2.0);
            return p;
        }

        Point Normaliz(long x, long y)
        {
            Point p = new Point();


            p.X = 1.0 * x / 4096 * lnW;
            //p.Y = plH/2.0 - y / (short.MaxValue*1.0) * (plH/2.0);
            p.Y = lnH - y / (long.MaxValue * 1.0/2) * (lnH);  // надо найти мак значение масива и поставить еего за место Maxvaule
            return p;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            saveflag = true;
            seconds = 0;
            time = 0;
            count = 0;
            StartRecording(1);
            File.Delete("Spectr.txt");

        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog myDialog = new Microsoft.Win32.OpenFileDialog();
            myDialog.Filter = "Текстовый файл(*.txt)|*txt" + "|Все файлы (*.*)|*.* ";
            myDialog.CheckFileExists = true;
            myDialog.Multiselect = true;
            myDialog.FileName = "";
            if (myDialog.ShowDialog() == true)
            {
                string filename = myDialog.FileName;
                var a2 = File.ReadAllText(filename);
             
                string[] str = a2.Split('!').Select(x => x).ToArray();

                
                
               int i=0;
                foreach (string s in str)
                {
                        if(s == "") { continue; }
                        long[] a = s.Split(' ').Select(x => long.Parse(x)).ToArray();
                                               
                        for (int j = 0; j < a.Length; j++)
                            mas[i, j] = a[j];
                        i++; 
                    
                        }


            }
            Console.WriteLine("yes");
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
          
            ln = new Polyline();
            //ln.Width = 10;
            ln.Stroke = Brushes.White;
            ln.Name = "canva";
            ln.StrokeThickness = 1;
            lnH = canva.Height;
            lnW = canva.Width;
            ln.MaxHeight = canva.Height - 4;
            ln.MaxWidth = canva.Width - 4;
            t++;
            this.canva.Children.Clear();
            ln.Points.Clear();
            if (t > 12) t = 0;
            for (int j = 0; j < 4096; j++)
            {
                ln.Points.Add(new Point(j, 330-(mas[t,j]/100000000)));//Normaliz (j,mas[t, j])
                //ln.Y2 =lnH-mas[t, j] / long.MaxValue/10 * 1.0 * lnH;
            }
            canva.Children.Add(ln);
            textT.Content = t;
        }

        private void button3_Click_1(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog1 = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog1.DefaultExt = ".txt";
            saveFileDialog1.InitialDirectory = "d:\\";
            saveFileDialog1.CheckPathExists = true;
            saveFileDialog1.OverwritePrompt = true;
            if (saveFileDialog1.ShowDialog() == true)
            {
                string filename = saveFileDialog1.FileName;
                File.Copy("Spectr.txt", filename);
            }
        }

      
    }
}



