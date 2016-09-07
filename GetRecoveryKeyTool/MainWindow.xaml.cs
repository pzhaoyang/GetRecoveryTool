using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace GetRecoveryKeyTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        static string client_id = "000000004C177416";
        static string client_secret = "8-578PFr0Gr27VgY78WDydR6I5VEEpiT";

        static string PartnerName = "ALCATELONETOUCH";//"YanLi";
        static string ReadyForUnProtectionIMEI ="";//"014551000025599";
        
        static string accessTokenUrl = String.Format(
            @"https://login.live.com/oauth20_token.srf?client_id={0}&client_secret={1}&redirect_uri=https://login.live.com/oauth20_desktop.srf&grant_type=authorization_code&code=",
            client_id, client_secret);

        static string RecoveryKeyAPI = String.Format(@"https://cs.dds.microsoft.com/Command/ExternalClientCert/AdministrativeUnprotect/{0}/ImeiOrMeid[{1}]", PartnerName, ReadyForUnProtectionIMEI);
        
        public Dictionary<string, string> tokenData = new Dictionary<string, string>();

        public MainWindow(){
            InitializeComponent();
        }

        private void getAccessToken(){
            if (App.Current.Properties.Contains("auth_code")){
                Console.WriteLine("[TCL]App.Current.Properties[\"auth_code\"] = " + App.Current.Properties["auth_code"]);
                makeAccessTokenRequest(accessTokenUrl + App.Current.Properties["auth_code"]);
            }
        }

        private void makeAccessTokenRequest(string requestUrl){
            Console.WriteLine("[TCL] makeAccessTokenRequest requestUrl=" + requestUrl);
            WebClient wc = new WebClient();
            wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(accessToken_DownloadStringCompleted);
            wc.DownloadStringAsync(new Uri(requestUrl));
        }

        void accessToken_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e){
            Console.WriteLine("[TCL] accessToken_DownloadStringCompleted e.Result=" + e.Result);
            tokenData = deserializeJson(e.Result);
            if (tokenData.ContainsKey("access_token")){
                App.Current.Properties.Add("access_token", tokenData["access_token"]);
                //for show access token at interface.
                txtToken.Text += "access_token = " + App.Current.Properties["access_token"];
            }

        }

        private Dictionary<string, string> deserializeJson(string json)
        {
            var jss = new JavaScriptSerializer();
            var d = jss.Deserialize<Dictionary<string, string>>(json);
            return d;
        }

        private void btnSignIn_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[TCL]click sign in");
            BrowserWindow browser = new BrowserWindow();
            browser.Closed += new EventHandler(browser_Closed);
            browser.Show();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[TCL]click Clear");
            App.Current.Properties.Clear();
            btnSignIn.Visibility = Visibility.Visible;
            txtToken.Text = "";
            ReadyIMEI.Text = "";
            UnprotectionResult.Text = "";
        }

        //[TCL-zhaoyang.peng-2016-1-30] for a GetRecovery {

        private string HttpPost(string Url, string postDataStr)
        {
            string accesstk = "" + App.Current.Properties["access_token"];
            Console.WriteLine("[TCL] RecoveryKeyAPI =" + RecoveryKeyAPI);
            Console.WriteLine("[TCL] access_token =" + accesstk);
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(RecoveryKeyAPI);
            HttpWebResponse response = null;
            string retString = null;
            request.ClientCertificates.Add(X509Certificate.CreateFromCertFile("SysDevPub.cer"));
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Headers.Add("Authorization", accesstk);
            Stream myRequestStream = request.GetRequestStream();
            StreamWriter myStreamWriter = new StreamWriter(myRequestStream, Encoding.GetEncoding("gb2312"));

            myStreamWriter.Write("N/A");
            myStreamWriter.Close();


            try {
                response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("UTF-8"));
                retString = myStreamReader.ReadToEnd();
                Console.WriteLine("[TCL] retString=" + retString);
                myStreamReader.Close();
                myResponseStream.Close();
            }
            catch (System.Net.WebException ex)
            {

                MessageBox.Show("The account is not available：" + ((System.Net.HttpWebResponse)ex.Response).StatusCode, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                //    retString = (System.Net.HttpWebResponse)ex.Response.ToString;
                //System.Console.WriteLine("[TCL] ExCeptionMessage=" + ((System.Net.HttpWebResponse)ex.Response).StatusCode);
                /*  if (response == null)
                  {
                      MessageBox.Show("Error Code" + response == null ? "NULL" : response.StatusCode.ToString(), "Error", MessageBoxButton.OK);
                  }
                  else {
                      MessageBox.Show("Error Code" + response.StatusCode.ToString(), "Error", MessageBoxButton.OK);
                  }*/
            }
            //if (response.StatusCode != HttpStatusCode.OK) {
                
            //}



            return retString;
        }

        private void btnGetRecovery_Click(object sender, RoutedEventArgs e){
            Dictionary<string, string> JsonResult = new Dictionary<string, string>();
            string IMEIs = ReadyIMEI.Text;
            string[] aIMEIs = IMEIs.Split(',');
            foreach (string imei in aIMEIs){
                if (imei.Length != 15){
                    MessageBox.Show("IMEI's Length need equal 15", "Error", MessageBoxButton.OK,MessageBoxImage.Error);
                    return;
                }
            }
            foreach (string imei in aIMEIs){
                 if (App.Current.Properties.Contains("access_token")){
                     string result = HttpPost(GetIMEI_From_Input(PartnerName, imei), "");
                    if (result != null){
                        JsonResult = deserializeJson(result);
                        if (JsonResult.ContainsKey("UnprotectResult"))
                        {
                            if (JsonResult["UnprotectResult"].Equals("DeviceUnprotected") || JsonResult["UnprotectResult"].Equals("DeviceAlreadyUnprotected"))
                            {
                                UnprotectionResult.Text += imei + ": " + JsonResult["UnprotectResult"] + " " + JsonResult["RecoveryKey"] + "\r\n";
                            }
                            else
                            {
                                UnprotectionResult.Text += imei + ": " + JsonResult["UnprotectResult"] + "\r\n";
                            }
                        }
                        else
                        {
                            UnprotectionResult.Text += imei + ": " + "GetRecoveryKeyError" + "\r\n";
                        }
                    }
                 }
            }
        }

        private string GetIMEI_From_Input(string PartnerName, string ReadyForUnProtectionIMEI){
             RecoveryKeyAPI = String.Format(
                @"https://cs.dds.microsoft.com/Command/ExternalClientCert/AdministrativeUnprotect/{0}/ImeiOrMeid[{1}]", 
                PartnerName, ReadyForUnProtectionIMEI);
             Console.WriteLine("[TCL] RecoveryKeyAPI=" + RecoveryKeyAPI);
            return RecoveryKeyAPI;
        }
        //[TCL-zhaoyang.peng-2016-1-30] for a GetRecovery }

        void browser_Closed(object sender, EventArgs e)
        {
            Console.WriteLine("[TCL]browser closed");
            getAccessToken();
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }


        public string MessageBoxButtons { get; set; }
    }
}
