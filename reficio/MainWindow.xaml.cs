using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Configuration;
using System.Windows.Controls;
using Shark.PdfConvert;
using RtfPipe;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Threading;

namespace reficio
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        public int count { get; set; }

        public string TimeElapsed { get; set; }
        public double Average { get; set; }
        public int Exception_Count { get; set; }
        private DispatcherTimer timer;
        private Stopwatch stopWatch;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;

            in_folder_text.Text = ConfigurationManager.AppSettings.Get("in_folder");
            out_folder_text.Text = ConfigurationManager.AppSettings.Get("out_folder");
            log_file_text.Text = ConfigurationManager.AppSettings.Get("log_file");
            error_file_text.Text = ConfigurationManager.AppSettings.Get("error_file");
            overwritefiles_checkbox.IsChecked = bool.Parse(ConfigurationManager.AppSettings.Get("overwrite_files").ToString());

            TextBoxOutputter outputter;
            outputter = new TextBoxOutputter(textbox_console);
            Console.SetOut(outputter);
            //Console.WriteLine("Started");
        }

        public void StartTimer()
        {
            timer = new DispatcherTimer();
            timer.Tick += dispatcherTimerTick_;
            timer.Interval = new TimeSpan(0, 0, 0, 1, 0);
            stopWatch = new Stopwatch();
            stopWatch.Start();
            timer.Start();
        }

        public void StopTimer()
        {
            stopWatch.Stop();
            timer.Stop();
        }

        private void dispatcherTimerTick_(object sender, EventArgs e)
        {
            TimeElapsed = displayTimer(stopWatch.Elapsed.TotalMilliseconds); // Format as you wish
            PropertyChanged(this, new PropertyChangedEventArgs("TimeElapsed"));
            Average = Math.Round(count / (stopWatch.Elapsed.TotalMilliseconds / 1000.0), 2);
            PropertyChanged(this, new PropertyChangedEventArgs("Average"));
        }

        private void in_folder_text_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateSetting("in_folder", in_folder_text.Text);
        }

        private void out_folder_text_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateSetting("out_folder", out_folder_text.Text);
        }

        private void log_file_text_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateSetting("log_file", log_file_text.Text);
        }

        private void error_file_text_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateSetting("error_file", error_file_text.Text);
        }

        private void overwritefiles_checkbox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateSetting("overwrite_files", overwritefiles_checkbox.IsChecked.ToString());
        }

        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            // ... A List.
            List<int> data = new List<int>();

            if (Environment.ProcessorCount <= 4)
            {
                data = Enumerable.Range(1, Environment.ProcessorCount).ToList();
            } else
            {
                data = Enumerable.Range(1, Environment.ProcessorCount - 2).ToList();
            };

            // ... Get the ComboBox reference.
            var comboBox = sender as ComboBox;

            // ... Assign the ItemsSource to the List.
            comboBox.ItemsSource = data;

            // ... Make the first item selected.
            comboBox.SelectedIndex = 0;
        }

        private static void UpdateSetting(string key, string value)
        {
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            configuration.AppSettings.Settings[key].Value = value;
            configuration.Save();
            ConfigurationManager.RefreshSection("appSettings");
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {

            //int cores = cores.ToString();
            string in_folder = in_folder_text.Text;
            string out_folder = out_folder_text.Text;
            string log_file = log_file_text.Text;
            string error_file = error_file_text.Text;
            bool overwrite_files = overwritefiles_checkbox.IsChecked.HasValue ? overwritefiles_checkbox.IsChecked.Value : false;
            int cores = Int32.Parse(cmbCores.Text);

            //processFiles(in_folder, out_folder, log_file, error_file, overwrite_files, cores);
            //new Thread(processFiles(in_folder, out_folder, log_file, error_file, overwrite_files, cores)).Start;

            var watch = System.Diagnostics.Stopwatch.StartNew();

            count = 0;
            Average = 0;
            TimeElapsed = "";
            Count_Display = 0;
            Exception_Count = 0;
            PropertyChanged(this, new PropertyChangedEventArgs("TimeElapsed"));
            PropertyChanged(this, new PropertyChangedEventArgs("Average"));
            PropertyChanged(this, new PropertyChangedEventArgs("Count_Display"));
            PropertyChanged(this, new PropertyChangedEventArgs("Exception_Count"));

            StartTimer();

            //await Task.Run(System.IO.Directory.GetFiles(in_folder, "*", SearchOption.AllDirectories) => processFiles(in_folder, out_folder, log_file, error_file, overwrite_files, cores));

            await Task.Run(() => processFiles(in_folder, out_folder, log_file, error_file, overwrite_files, cores));

            watch.Stop();
            var elapsedMs = milliReadable(watch.ElapsedMilliseconds);

            Console.WriteLine("Elapsed time - {0}", elapsedMs);
            Console.WriteLine("Files processed  - {0}", count);
            Console.WriteLine("Average files per second - {0}", count / (watch.ElapsedMilliseconds / 1000.0));

            FileStream fs = new FileStream(ConfigurationManager.AppSettings["log_file"], FileMode.Append, FileAccess.Write, FileShare.Read);

            StreamWriter sw = new StreamWriter(fs);

            Object locker = new Object();


            lock (locker)
            {
                sw.WriteLine("Elapsed time - " + elapsedMs);
                sw.WriteLine("Files processed  - " + count);
                sw.WriteLine("Average files per second - " + count / (watch.ElapsedMilliseconds / 1000.0));
                sw.Flush();
            }

            StopTimer();

            sw.Close();
            fs.Close();
        }

        private void processFiles(string in_folder, string out_folder, string log_file, string error_file, bool overwrite_files, int cores)
        {

            FileStream fs = new FileStream(ConfigurationManager.AppSettings["log_file"], FileMode.Append, FileAccess.Write, FileShare.Read);

            StreamWriter sw = new StreamWriter(fs);

            Object locker = new Object();

            Parallel.ForEach(System.IO.Directory.GetFiles(in_folder, "*", SearchOption.AllDirectories), new ParallelOptions() { MaxDegreeOfParallelism = cores }, (file) =>
            {

                string parent_folder = System.IO.Path.GetDirectoryName(file).Replace(in_folder, out_folder);
                //string pdf_out = System.IO.Path.ChangeExtension(file.Replace(in_folder, out_folder), ".pdf");
                string pdf_out = System.IO.Path.ChangeExtension(file.Replace(in_folder, out_folder), ".pdf");

                //bool.Parse(ConfigurationManager.AppSettings.Get("overwrite_files");

                if (overwrite_files == false && File.Exists(pdf_out))
                {
                    return;
                }
                else
                {
                    string file_html = toHTML(file, error_file);
                    generatePDF(parent_folder, file_html, pdf_out, error_file);
                    count++;
                    Count_Display++;
                    lock (locker)
                    {
                        sw.WriteLine(pdf_out);
                        sw.Flush();
                    }


                    //Console.WriteLine(pdf_out); //lag city, why write this to console anyway
                    
                }
            });

            sw.Close();
            fs.Close();
        }

        private void generatePDF(string out_folder, string html_stream, string pdf_out, string error_file)
        {
            System.IO.Directory.CreateDirectory(out_folder);
            //convert to pdf - shark.pdfconvert
            //PdfConvert.Convert(new PdfConversionSettings
            //{
            //    Title = "My Static Content",
            //    Content = html_stream,
            //    OutputPath = pdf_out
            //});

            try
            {
                PdfConvert.Convert(new PdfConversionSettings
                {
                    Title = "My Static Content",
                    Content = html_stream,
                    OutputPath = pdf_out
                });
            }
            catch (Exception ex)
            {
                //File.AppendAllText(log_file, ex);
                using (StreamWriter writer = new StreamWriter(error_file, true))
                {
                    writer.WriteLine("Message :" + ex.Message + "<br/>" + Environment.NewLine + "StackTrace :" + ex.StackTrace +
                       "" + Environment.NewLine + "Date :" + DateTime.Now.ToString() + Environment.NewLine + pdf_out);
                    writer.WriteLine(Environment.NewLine + "-----------------------------------------------------------------------------" + Environment.NewLine);
                }
                Exception_Count++;
                PropertyChanged(this, new PropertyChangedEventArgs("Exception_Count"));
            }

        }

        private static string PlainTextToRtf(string plainText) //simple method to wrap RTF tags around plain text - allows it to be treated as RTF by rtfpipe
        {
            string escapedPlainText = plainText.Replace(@"\", @"\\").Replace("{", @"\{").Replace("}", @"\}");
            string rtf = @"{\rtf1\ansi{\fonttbl\f0\fswiss Helvetica;}\f0\pard ";
            rtf += escapedPlainText.Replace(Environment.NewLine, @" \par ");
            rtf += " }";
            return rtf;
        }

        private static string milliReadable(long ms) //turn milliseconds into a nice readable string
        {
            TimeSpan t = TimeSpan.FromMilliseconds(ms);
            string answer = t.ToString(@"d\ \d\a\y\s\ h\ \h\o\u\r\s\ mm\ \m\i\n\u\t\e\s\ ss\ \s\e\c\o\n\d\s\ fff\ \m\i\l\l\i\s\e\c\o\n\d\s");
            return answer;
        }

        private static string displayTimer(double ms) //turn milliseconds into a nice readable string
        {
            TimeSpan t = TimeSpan.FromMilliseconds(ms);
            string answer = t.ToString(@"h\:mm\:ss");
            return answer;
        }

        private string toHTML(string file, string error_file)
        {
            string html = ""; //clear html variable for current file

            switch (System.IO.Path.GetExtension(file))
            {

                case ".rtf":

                    //read in file
                    using (var content = File.OpenText(file))
                    {
                        char[] chunk = new char[4];
                        content.Read(chunk, 0, 4);
                        char[] rtf_array_upper = { '{', '\\', 'R', 'T' };
                        char[] rtf_array_lower = { '{', '\\', 'r', 't' };
                        //string hexLetters = BitConverter.ToString(chunk);

                        if (chunk.SequenceEqual(rtf_array_upper) || chunk.SequenceEqual(rtf_array_lower))
                        {
                            using (var content_full = File.OpenText(file))
                            {
                                try
                                {
                                    html = Rtf.ToHtml(content_full);
                                }
                                catch (Exception ex)
                                {
                                    //File.AppendAllText(log_file, ex);
                                    using (StreamWriter writer = new StreamWriter(error_file, true))
                                    {
                                        writer.WriteLine("Message :" + ex.Message + "<br/>" + Environment.NewLine + "StackTrace :" + ex.StackTrace +
                                           "" + Environment.NewLine + "Date :" + DateTime.Now.ToString() + Environment.NewLine + file);
                                        writer.WriteLine(Environment.NewLine + "-----------------------------------------------------------------------------" + Environment.NewLine);
                                    }
                                    Exception_Count++;
                                    PropertyChanged(this, new PropertyChangedEventArgs("Exception_Count"));
                                }
                            }
                        }
                        else
                        {
                            using (var content_full = File.OpenText(file))
                            {
                                //read to end of stream - necessary for rtfpipe to work
                                string text = content_full.ReadToEnd();
                                //convert to html - rtfpipe
                                html = Rtf.ToHtml(PlainTextToRtf(text));
                            }
                        }
                    }

                    break;

                case ".txt":

                    //read in file
                    using (var content = File.OpenText(file))
                    {
                        //read to end of stream - necessary for rtfpipe to work
                        string text = content.ReadToEnd();
                        //convert to html - rtfpipe
                        html = Rtf.ToHtml(PlainTextToRtf(text));
                    }

                    break;

                case ".html":

                    //read in file
                    using (var content = File.OpenText(file))
                    {
                        //read to end of stream
                        html = content.ReadToEnd();
                    }

                    break;

            }

            return html;
        }

        private int count_display;

        public int Count_Display
        {
            get { return count_display; }
            set
            {
                count_display = value;
                RaisePropertyChanged("Count_display");
            }
        }

        private void RaisePropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
        public event PropertyChangedEventHandler PropertyChanged;

    }
}
