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

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
   
        //set properties to update in real time in UI
        public int count { get; set; }
        public string TimeElapsed { get; set; }
        public double Average { get; set; }
        public int Exception_Count { get; set; }
        public int count_display;

        //initalize timer and stopwatch
        private DispatcherTimer timer;
        private Stopwatch stopWatch;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;

            //read in settings from config file
            in_folder_text.Text = ConfigurationManager.AppSettings.Get("in_folder");
            out_folder_text.Text = ConfigurationManager.AppSettings.Get("out_folder");
            log_file_text.Text = ConfigurationManager.AppSettings.Get("log_file");
            error_file_text.Text = ConfigurationManager.AppSettings.Get("error_file");
            overwritefiles_checkbox.IsChecked = bool.Parse(ConfigurationManager.AppSettings.Get("overwrite_files").ToString());

            //initialize log window
            TextBoxOutputter outputter;
            outputter = new TextBoxOutputter(textbox_console);
            Console.SetOut(outputter);
            //Console.WriteLine("Started");
        }

        //start the timer and stopwatch
        public void StartTimer()
        {
            timer = new DispatcherTimer();
            timer.Tick += dispatcherTimerTick_;
            timer.Interval = new TimeSpan(0, 0, 0, 1, 0);
            stopWatch = new Stopwatch();
            stopWatch.Start();
            timer.Start();
        }

        //stop the timer and stopwatch
        public void StopTimer()
        {
            stopWatch.Stop();
            timer.Stop();
        }

        //update UI timer and average files processed
        private void dispatcherTimerTick_(object sender, EventArgs e)
        {
            TimeElapsed = displayTimer(stopWatch.Elapsed.TotalMilliseconds); // Format as you wish
            PropertyChanged(this, new PropertyChangedEventArgs("TimeElapsed"));
            Average = Math.Round(count / (stopWatch.Elapsed.TotalMilliseconds / 1000.0), 2);
            PropertyChanged(this, new PropertyChangedEventArgs("Average"));
        }

        //update config file
        private void in_folder_text_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateSetting("in_folder", in_folder_text.Text);
        }

        //update config file
        private void out_folder_text_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateSetting("out_folder", out_folder_text.Text);
        }

        //update config file
        private void log_file_text_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateSetting("log_file", log_file_text.Text);
        }

        //update config file
        private void error_file_text_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateSetting("error_file", error_file_text.Text);
        }

        //update config file
        private void overwritefiles_checkbox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateSetting("overwrite_files", overwritefiles_checkbox.IsChecked.ToString());
        }

        //populate combobox with total processor count - 2
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

        //update config file
        private static void UpdateSetting(string key, string value)
        {
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            configuration.AppSettings.Settings[key].Value = value;
            configuration.Save();
            ConfigurationManager.RefreshSection("appSettings");
        }

        //the big bad button
        private async void Button_Click(object sender, RoutedEventArgs e)
        {

            //create and populate variable from config file/UI
            //int cores = cores.ToString();
            string in_folder = in_folder_text.Text;
            string out_folder = out_folder_text.Text;
            string log_file = log_file_text.Text;
            string error_file = error_file_text.Text;
            bool overwrite_files = overwritefiles_checkbox.IsChecked.HasValue ? overwritefiles_checkbox.IsChecked.Value : false;
            int cores = Int32.Parse(cmbCores.Text);

            //processFiles(in_folder, out_folder, log_file, error_file, overwrite_files, cores);
            //new Thread(processFiles(in_folder, out_folder, log_file, error_file, overwrite_files, cores)).Start;

            //create and start watch
            var watch = System.Diagnostics.Stopwatch.StartNew();

            //set or reset variables
            count = 0;
            Average = 0;
            TimeElapsed = "";
            Count_Display = 0;
            Exception_Count = 0;
            PropertyChanged(this, new PropertyChangedEventArgs("TimeElapsed"));
            PropertyChanged(this, new PropertyChangedEventArgs("Average"));
            PropertyChanged(this, new PropertyChangedEventArgs("Count_Display"));
            PropertyChanged(this, new PropertyChangedEventArgs("Exception_Count"));

            //start timer
            StartTimer();

            //await Task.Run(System.IO.Directory.GetFiles(in_folder, "*", SearchOption.AllDirectories) => processFiles(in_folder, out_folder, log_file, error_file, overwrite_files, cores));

            //launch processing in a new task - waits for process to finish - separate thread keeps UI responsive
            await Task.Run(() => processFiles(in_folder, out_folder, log_file, error_file, overwrite_files, cores));

            //stop stopwatch
            watch.Stop();
            var elapsedMs = milliReadable(watch.ElapsedMilliseconds);

            //output results to 
            Console.WriteLine("Elapsed time - {0}", elapsedMs);
            Console.WriteLine("Files processed  - {0}", count);
            Console.WriteLine("Average files per second - {0}", count / (watch.ElapsedMilliseconds / 1000.0));

            //create filestream, streamwriter, and object
            FileStream fs = new FileStream(ConfigurationManager.AppSettings["log_file"], FileMode.Append, FileAccess.Write, FileShare.Read);

            StreamWriter sw = new StreamWriter(fs);

            Object locker = new Object();

            //lock file for file writing
            lock (locker)
            {
                sw.WriteLine("Elapsed time - " + elapsedMs);
                sw.WriteLine("Files processed  - " + count);
                sw.WriteLine("Average files per second - " + count / (watch.ElapsedMilliseconds / 1000.0));
                sw.Flush();
            }

            //stop timer
            StopTimer();

            //close filestream, and streamwriter
            sw.Close();
            fs.Close();
        }

        //process files - main function of button press - parallel processing based on core count selected
        private void processFiles(string in_folder, string out_folder, string log_file, string error_file, bool overwrite_files, int cores)
        {

            //create filestream, streamwriter, and object locker for log file
            FileStream fs = new FileStream(ConfigurationManager.AppSettings["log_file"], FileMode.Append, FileAccess.Write, FileShare.Read);

            StreamWriter sw = new StreamWriter(fs);

            Object locker = new Object();

            //kick of parallel processing for the actual file conversion
            Parallel.ForEach(System.IO.Directory.GetFiles(in_folder, "*", SearchOption.AllDirectories), new ParallelOptions() { MaxDegreeOfParallelism = cores }, (file) =>
            {

                //generate output folder and file names
                string parent_folder = System.IO.Path.GetDirectoryName(file).Replace(in_folder, out_folder);
                string pdf_out = System.IO.Path.ChangeExtension(file.Replace(in_folder, out_folder), ".pdf");

                //bool.Parse(ConfigurationManager.AppSettings.Get("overwrite_files");

                //if statement depending on if overwrite files is checked
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

            //close streams
            sw.Close();
            fs.Close();
        }

        //generate PDF from HTML - thanks wkhtmltopdf and the shark.pdfconvert C# wapper
        private void generatePDF(string out_folder, string html_stream, string pdf_out, string error_file)
        {
            System.IO.Directory.CreateDirectory(out_folder);

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

        //convert plain text to PDF - wrap rtf tags around the text string
        private static string PlainTextToRtf(string plainText) //simple method to wrap RTF tags around plain text - allows it to be treated as RTF by rtfpipe
        {
            string escapedPlainText = plainText.Replace(@"\", @"\\").Replace("{", @"\{").Replace("}", @"\}");
            string rtf = @"{\rtf1\ansi{\fonttbl\f0\fswiss Helvetica;}\f0\pard ";
            rtf += escapedPlainText.Replace(Environment.NewLine, @" \par ");
            rtf += " }";
            return rtf;
        }

        //convert milliseconds into a nicer format to look at using timespan for the logging output
        private static string milliReadable(long ms)
        {
            TimeSpan t = TimeSpan.FromMilliseconds(ms);
            string answer = t.ToString(@"d\ \d\a\y\s\ h\ \h\o\u\r\s\ mm\ \m\i\n\u\t\e\s\ ss\ \s\e\c\o\n\d\s\ fff\ \m\i\l\l\i\s\e\c\o\n\d\s");
            return answer;
        }

        //convert milliseconds into a nicer format to look at using timespan for the UI timer
        private static string displayTimer(double ms)
        {
            TimeSpan t = TimeSpan.FromMilliseconds(ms);
            string answer = t.ToString(@"h\:mm\:ss");
            return answer;
        }

        //convert rtf or plaintext into HTML - thanks to the RTFPipe C# library
        private string toHTML(string file, string error_file)
        {
            string html = ""; //clear html variable for current file

            //process .txt and .rtf using different cases
            switch (System.IO.Path.GetExtension(file))
            {

                case ".rtf":

                    //read in file
                    using (var content = File.OpenText(file))
                    {
                        //double check for RTF content
                        char[] chunk = new char[4];
                        content.Read(chunk, 0, 4);
                        char[] rtf_array_upper = { '{', '\\', 'R', 'T' };
                        char[] rtf_array_lower = { '{', '\\', 'r', 't' };
                        //string hexLetters = BitConverter.ToString(chunk);

                        //if RTF
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
                        //else process as plain text
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
