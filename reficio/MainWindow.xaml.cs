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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Shark.PdfConvert;
using RtfPipe;
using System.Reflection;
using System.Collections.Concurrent;

namespace reficio
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {


        public MainWindow()
        {
            InitializeComponent();
            //DataContext = this;

            in_folder_text.Text = ConfigurationManager.AppSettings.Get("in_folder");
            out_folder_text.Text = ConfigurationManager.AppSettings.Get("out_folder");
            log_file_text.Text = ConfigurationManager.AppSettings.Get("log_file");
            error_file_text.Text = ConfigurationManager.AppSettings.Get("error_file");
            overwritefiles_checkbox.IsChecked = bool.Parse(ConfigurationManager.AppSettings.Get("overwrite_files").ToString());

            TextBoxOutputter outputter;
            outputter = new TextBoxOutputter(textbox_console);
            Console.SetOut(outputter);
            Console.WriteLine("Started");
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
            data =  Enumerable.Range(1, Environment.ProcessorCount - 2).ToList();

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

        private void Button_Click(object sender, RoutedEventArgs e)
        {


            //int cores = cores.ToString();
            string in_folder = in_folder_text.Text;
            string out_folder = out_folder_text.Text;
            string log_file = log_file_text.Text;
            string error_file = error_file_text.Text;
            bool overwrite_files = overwritefiles_checkbox.IsChecked.HasValue ? overwritefiles_checkbox.IsChecked.Value : false;
            int cores = Int32.Parse(cmbCores.Text);

            processFiles(in_folder, out_folder, log_file, error_file, overwrite_files, cores);
            //new Thread(processFiles(in_folder, out_folder, log_file, error_file, overwrite_files, cores)).Start;
        }

        private static void processFiles(string in_folder, string out_folder, string log_file, string error_file, bool overwrite_files, int cores)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            int count = 0;

            FileStream fs = new FileStream(ConfigurationManager.AppSettings["log_file"], FileMode.Append, FileAccess.Write, FileShare.Read);

            StreamWriter sw = new StreamWriter(fs);

            Object locker = new Object();

            Task.Factory.StartNew(() => Parallel.ForEach(System.IO.Directory.GetFiles(in_folder, "*", SearchOption.AllDirectories), new ParallelOptions() { MaxDegreeOfParallelism = cores }, (file) =>
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
                    generatePDF(parent_folder, file_html, pdf_out);

                    //lock (locker)
                    //{
                    //    sw.WriteLine(pdf_out);
                    //    sw.Flush();
                    //}
                    count++;
                    Console.WriteLine(pdf_out);
                }

            }));

            watch.Stop();
            var elapsedMs = milliReadable(watch.ElapsedMilliseconds);

            Console.WriteLine("Elapsed time - {0}", elapsedMs);
            Console.WriteLine("Files processed  - {0}", count);
            Console.WriteLine("Average files per second - {0}", count / (watch.ElapsedMilliseconds / 1000.0));

            lock (locker)
            {
                sw.WriteLine("Elapsed time - " + elapsedMs);
                sw.WriteLine("Files processed  - " + count);
                sw.WriteLine("Average files per second - " + count / (watch.ElapsedMilliseconds / 1000.0));
                sw.Flush();
            }

            sw.Close();
            fs.Close();
        }

        private static void generatePDF(string out_folder, string html_stream, string pdf_out)
        {
            System.IO.Directory.CreateDirectory(out_folder);
            //convert to pdf - shark.pdfconvert
            PdfConvert.Convert(new PdfConversionSettings
            {
                Title = "My Static Content",
                Content = html_stream,
                OutputPath = pdf_out
            });
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

        private static string toHTML(string file, string error_file)
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
    }
}
