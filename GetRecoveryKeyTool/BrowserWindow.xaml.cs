using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Text.RegularExpressions;

namespace GetRecoveryKeyTool
{
    public partial class BrowserWindow : Window
    {
        static string scope = "dds.reverse_logistics";
        static string client_id = "000000004C177416";
        static Uri signInUrl = new Uri(String.Format(@"https://login.live.com/oauth20_authorize.srf?client_id={0}&scope={1}&redirect_uri=https://login.live.com/oauth20_desktop.srf&response_type=code", client_id, scope));
        MainWindow mainWindow = new MainWindow();
        
        public BrowserWindow(){
            InitializeComponent();
            Console.WriteLine("[TCL]url = " + signInUrl);
            webBrowser.Navigate(signInUrl);
        }

        private void webBrowser_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            if (e.Uri.AbsoluteUri.Contains("code="))
            {
                if (App.Current.Properties.Contains("auth_code"))
                {
                    App.Current.Properties.Clear();
                }
                string auth_code = Regex.Split(e.Uri.AbsoluteUri, "code=")[1];
                Console.WriteLine("[TCL]auth_code=" + auth_code);
                App.Current.Properties.Add("auth_code", auth_code);
                this.Close();
            }
        }
    }
}