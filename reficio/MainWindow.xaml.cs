using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace reficio
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
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


            var watch = System.Diagnostics.Stopwatch.StartNew();

            //loop through each folder inside the in folder - write folder name to console
            //foreach (var patientFolder in Directory.EnumerateDirectories(in_folder))
            //{
            //    List<string> file_names = ProcessFiles(patientFolder, in_folder, out_folder, cores, overwrite_files, error_file);
            //    foreach (String s in file_names)
            //    {
            //        File.AppendAllText(ConfigurationManager.AppSettings["log_file"], s + Environment.NewLine);
            //    }
            //}

            foreach (string file in System.IO.Directory.GetFiles(in_folder, "*", SearchOption.AllDirectories))
            {
                //do something with file
                //ProcessFiles(file, in_folder, out_folder, cores, overwrite_files, error_file);
                string file_html = toHTML(file, error_file);
                string file_folder = System.IO.Path.GetDirectoryName(file).Replace(in_folder, out_folder);
                string pdf_out = file.Replace(in_folder, out_folder) + ".pdf";
                generatePDF(in_folder, out_folder, file_folder, file_html);
                File.AppendAllText(ConfigurationManager.AppSettings["log_file"], pdf_out + Environment.NewLine);
            }

            watch.Stop();
            var elapsedMs = milliReadable(watch.ElapsedMilliseconds);
            //Console.WriteLine("Elapsed time - {0}", elapsedMs);
            //Console.WriteLine("Files processed  - {0}", count);
            //Console.WriteLine("Average files per second - {0}", count / (watch.ElapsedMilliseconds / 1000.0));

            //File.AppendAllText(log_file, "Elapsed time - " + elapsedMs + Environment.NewLine);
            //File.AppendAllText(log_file, "Files processed  - " + count + Environment.NewLine);
            //File.AppendAllText(log_file, "Average files per second - " + count / (watch.ElapsedMilliseconds / 1000.0) + Environment.NewLine);

            //keep console open until a key is pressed
            //Console.WriteLine("Press enter to close window...");
            //Console.ReadLine();
        }

        private static void generatePDF(string in_folder, string out_folder, string sdid_folder, string html_stream)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(sdid_folder).Replace(in_folder, out_folder));
            string pdf_out = sdid_folder.Replace(in_folder, out_folder) + ".pdf";

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

        private void cmbCores_Selected(object sender, RoutedEventArgs e)
        {
            int cores = (int)(cmbCores.SelectedItem as PropertyInfo).GetValue(null, null);
        }
    }
}
