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
using System.Web.Script.Serialization;
using System.Net;
using System.IO;
using System.Security.Cryptography.X509Certificates;


namespace GetRecoveryKeyTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    // [TCL-zhaoyang.peng-2016-01-18] add enum {
    /// <summary>
    /// Result of the unprotect operation
    /// </summary>
    public enum UnprotectResult
    {
        /// <summary>
        /// Device was not found in DDS
        /// </summary>
        DeviceNotFound,

        /// <summary>
        /// Device was already unprotected
        /// </summary>
        DeviceAlreadyUnprotected,

        /// <summary>
        /// Device has been unprotected
        /// </summary>
        DeviceUnprotected,

        /// <summary>
        /// IF we find more than 1 device, we don't currently have a way to resolve the conflict. So, we don't unprotect.
        /// </summary>
        MultipleDevicesFound,
    }
    // [TCL-zhaoyang.peng-2016-01-18] add enum }

    public partial class MainWindow : Window
    {
        static string client_id = "000000004C177416";
        static string client_secret = "8-578PFr0Gr27VgY78WDydR6I5VEEpiT";
        static string accessTokenUrl = String.Format(
            @"https://login.live.com/oauth20_token.srf?client_id={0}&client_secret={1}&redirect_uri=https://login.live.com/oauth20_desktop.srf&grant_type=authorization_code&code=",
            client_id, client_secret);
        //static string accessTokenUrl = String.Format(
            //@"https://login.live.com/oauth20_token.srf?client_id={0}&client_secret={1}&redirect_uri=https://cs.dds.microsoft.com/oauth20_desktop.srf&grant_type=authorization_code&code=",
            //client_id, client_secret);
        static string apiUrl = @"https://apis.live.net/v5.0/";
        //static string apiUrl = @"https://cs.dds.microsoft.com/Command/ExternalClientCert/AdministrativeUnprotect/ALCATELONETOUCH/014551000024691";
        
        public Dictionary<string, string> tokenData = new Dictionary<string, string>();
        CookieContainer cookie = new CookieContainer();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void getAccessToken()
        {
            if (App.Current.Properties.Contains("auth_code"))
            {
                Console.WriteLine("[TCL]App.Current.Properties[\"auth_code\"] = " + App.Current.Properties["auth_code"]);
                makeAccessTokenRequest(accessTokenUrl + App.Current.Properties["auth_code"]);
            }
        }

        private void makeAccessTokenRequest(string requestUrl)
        {
            Console.WriteLine("[TCL] makeAccessTokenRequest requestUrl=" + requestUrl);
            WebClient wc = new WebClient();
            Console.WriteLine("[TCL]makeAccessTokenRequest parameter:" + requestUrl);
            wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(accessToken_DownloadStringCompleted);
            wc.DownloadStringAsync(new Uri(requestUrl));
        }

        void accessToken_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            Console.WriteLine("[TCL] accessToken_DownloadStringCompleted e.Result=" + e.Result);
            tokenData = deserializeJson(e.Result);
            if (tokenData.ContainsKey("access_token"))
            {
                App.Current.Properties.Add("access_token", tokenData["access_token"]);
                //getUserInfo();
            }
        }

        private Dictionary<string, string> deserializeJson(string json)
        {
            var jss = new JavaScriptSerializer();
            var d = jss.Deserialize<Dictionary<string, string>>(json);
            return d;
        }

        private void getUserInfo()
        {
            if (App.Current.Properties.Contains("access_token"))
            {
                makeApiRequest(apiUrl + "me?access_token=" + App.Current.Properties["access_token"]);
            }
        }

        private void makeApiRequest(string requestUrl)
        {
            WebClient wc = new WebClient();
            wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(client_DownloadStringCompleted);
            wc.DownloadStringAsync(new Uri(requestUrl));
        }

        void client_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            changeView(e.Result);
        }

        private void changeView(string result)
        {
            btnSignIn.Visibility = Visibility.Collapsed;
            txtUserInfo.Text = result;
            string imgUrl = apiUrl + "me/picture?access_token=" + App.Current.Properties["access_token"];
            imgUser.Source = new BitmapImage(new Uri(imgUrl, UriKind.RelativeOrAbsolute));
            txtToken.Text += "access_token = " + App.Current.Properties["access_token"] + "\r\n\r\n";
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
            imgUser.Source = null;
            txtUserInfo.Text = "";
        }

        //[TCL-zhaoyang.peng-2016-1-30] for a GetRecovery {

        private string HttpPost(string Url, string postDataStr)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = Encoding.UTF8.GetByteCount(postDataStr);
            //request.CookieContainer = cookie;
            Stream myRequestStream = request.GetRequestStream();
            StreamWriter myStreamWriter = new StreamWriter(myRequestStream, Encoding.GetEncoding("gb2312"));
            myStreamWriter.Write(postDataStr);
            myStreamWriter.Close();

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            response.Cookies = cookie.GetCookies(response.ResponseUri);
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();

            return retString;
        }

        public string HttpGet(string Url, string postDataStr)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url + (postDataStr == "" ? "" : "?") + postDataStr);
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();

            return retString;
        }

        private void btnGetRecovery_Click(object sender, RoutedEventArgs e)
        {
            string RecoveryKeyAPI = @"https://cs.dds.microsoft.com/Command/ExternalClientCert/AdministrativeUnprotect/YanLi/ImeiOrMeid[014551000025599]";    
            if (App.Current.Properties.Contains("access_token")) {
                Console.WriteLine("[TCL] RecoveryKeyAPI =" + RecoveryKeyAPI);
                Console.WriteLine("[TCL] access_token =" + App.Current.Properties["access_token"]);
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(RecoveryKeyAPI);
                request.ClientCertificates.Add(X509Certificate.CreateFromCertFile("SysDevPub.cer"));
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Headers.Add("Authorization", "" + App.Current.Properties["access_token"]);
                Stream myRequestStream = request.GetRequestStream();
                StreamWriter myStreamWriter = new StreamWriter(myRequestStream, Encoding.GetEncoding("gb2312"));

                myStreamWriter.Write("");
                myStreamWriter.Close();


                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                response.Cookies = cookie.GetCookies(response.ResponseUri);
                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("UTF-8"));
                string retString = myStreamReader.ReadToEnd();
                Console.WriteLine("[TCL] retString=" + retString);
                myStreamReader.Close();
                myResponseStream.Close();

            } else {
                Console.WriteLine("[TCL] error");
            }
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

    }
}
