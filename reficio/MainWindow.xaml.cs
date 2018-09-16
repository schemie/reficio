using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Configuration;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            DataContext = this;
            in_folder_text.Text = ConfigurationManager.AppSettings.Get("in_folder");
            out_folder_text.Text = ConfigurationManager.AppSettings.Get("out_folder");
            log_file_text.Text = ConfigurationManager.AppSettings.Get("log_file");
            error_file_text.Text = ConfigurationManager.AppSettings.Get("error_file");
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

        private static void UpdateSetting(string key, string value)
        {
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            configuration.AppSettings.Settings[key].Value = value;
            configuration.Save();

            ConfigurationManager.RefreshSection("appSettings");
        }

    }
}
