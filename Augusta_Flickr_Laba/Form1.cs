using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Text;


namespace Augusta_Flickr_Laba
{
    public partial class Form1 : Form
    {


        //secret key from flickr app
        private static string Secret = "9d63557b78e83144";
        //consumer key from flickr app
        private static string ConsumerKey = "2bdc4a3b081b925b48be3e67146d0a5a";

        //request token (can save it to settings, to use it with auth)
        private static string request_token = "";
        private string oauth_token = "";
        private string signature = "";

        public Form1()
        {
            InitializeComponent();
        }



        private void button1_Click(object sender, EventArgs e)
        {

            ConsumerKey = textBox1.Text;
            Secret = textBox2.Text;


            //button press start process
            if (ConsumerKey != "" && Secret != "" && !ConsumerKey.Contains(" ") && !Secret.Contains(" "))
            {
                GetAuth();
            }
            else
            {
                MessageBox.Show("Enter Key & Secret", "Error!");
            }

        }


        private async void GetAuth()
        {
            button1.Enabled = false;



            //Signing Requests
            string requestString = "https://www.flickr.com/services/oauth/request_token";

            //generate a random nonce and a timestamp
            Random rand = new Random();
            string nonce = rand.Next(999999).ToString();
            string timestamp = GetTimestamp();

            //create the parameter string in alphabetical order
            string parameters = "oauth_callback=" + UrlHelper.Encode("http://www.example.com");
            parameters += "&oauth_consumer_key=" + ConsumerKey;
            parameters += "&oauth_nonce=" + nonce;
            parameters += "&oauth_signature_method=HMAC-SHA1";
            parameters += "&oauth_timestamp=" + timestamp;
            parameters += "&oauth_version=1.0";

            //generate a signature base on the current requeststring and parameters
            signature = generateSignature("GET", requestString, parameters);



            //Getting a Request Token
            //add the parameters and signature to the requeststring
            string RequestTokenString = requestString + "?" + parameters + "&oauth_signature=" + signature;
            HttpClient web = new HttpClient();
            string result = await web.GetStringAsync(RequestTokenString);
            textBox3.Text = result;



            //Getting the User Authorization
            StringBuilder ab = new StringBuilder(result);
            oauth_token = ab.Remove(0, 42).ToString();
            int index = 0;
            for (int i = 0; i < oauth_token.Length; i++)
                if (oauth_token[i] == '&')
                {
                    index = i;
                    break;
                }
            oauth_token = ab.Remove(index, oauth_token.Length - index).ToString();
            string authorizeString = "https://www.flickr.com/services/oauth/authorize" + "?oauth_token=" + oauth_token;

            webBrowser1.Navigate(new Uri(authorizeString));
            while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
            {
                Application.DoEvents();
            }

            //webBrowser1.Document.GetElementById("Username").SetAttribute("value", "avgustabehtina");
            ////здесь нужно заполнить поле пароль  
            //webBrowser1.Document.GetElementById("passwd").InnerText = "Nachpts12";
            //// здесь нужно нажать кнопку войти
            //foreach (HtmlElement he in webBrowser1.Document.GetElementsByTagName("button"))
            //{
            //    if (he.GetAttribute("id").Equals("login-signin"))
            //    {
            //        he.InvokeMember("click");
            //    }
            //}

            //webBrowser1.Navigate(new Uri(webBrowser1.Document.Url.OriginalString));
            //while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
            //{
            //    Application.DoEvents();
            //}
            //foreach (HtmlElement he in webBrowser1.Document.GetElementsByTagName("input"))
            //{
            //    if (he.GetAttribute("type").Equals("submit"))
            //    {
            //        he.InvokeMember("click");
            //    }
            //}
            //webBrowser1.Navigate(new Uri(webBrowser1.Document.Url.OriginalString));
            //while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
            //{
            //    Application.DoEvents();
            //}
            //foreach (HtmlElement he in webBrowser1.Document.GetElementsByTagName("input"))
            //{
            //    if (he.GetAttribute("type").Equals("submit"))
            //    {
            //        he.InvokeMember("click");
            //    }
            //}
            //while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
            //{
            //    Application.DoEvents();
            //}
            //string s = await webBrowser1.Url.OriginalString;


        }

        private async void GetPhoto()
        {

            //new http client (.net 4 or better)
            HttpClient client = new HttpClient();


            //request json data
            string url = "https://api.flickr.com/services/rest/" +
                "?method=flickr.photos.getRecent" +
                "&api_key={0}" +
                "&format=json" +
                "&nojsoncallback=1";

            //format string
            string baseUrl = string.Format(url, ConsumerKey);


            //get result
            string flickrResult = await client.GetStringAsync(baseUrl);


            //get json data
            FlickrData apiData = JsonConvert.DeserializeObject<FlickrData>(flickrResult);

            if (apiData.stat == "ok")
            {
                foreach (Photo data in apiData.photos.photo)
                {

                    // to retrieve one photo, use this format:
                    //http://farm{farm-id}.staticflickr.com/{server-id}/{id}_{secret}{size}.jpg
                    string photoUrl = "http://farm{0}.staticflickr.com/{1}/{2}_{3}_n.jpg";

                    //reformat string
                    string baseFlickrUrl = string.Format(photoUrl,
                        data.farm,
                        data.server,
                        data.id,
                        data.secret);


                    //generate WEbRequest to load image
                    WebRequest request = WebRequest.Create(new Uri(baseFlickrUrl));
                    WebResponse response = request.GetResponse();
                    Stream responseStream = response.GetResponseStream();
                    Bitmap img = new Bitmap(responseStream);


                    //set image to picturebox 
                    pictureBox1.Image = ResizeBitmap(img, pictureBox1.Width, pictureBox1.Height);
                    button1.Enabled = true;

                    //break to load only one image
                    break;
                }
            }
        }


        //image resize
        private static Bitmap ResizeBitmap(Bitmap sourceBMP, int width, int height)
        {
            Bitmap result = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(result))
                g.DrawImage(sourceBMP, 0, 0, width, height);
            return result;
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            ConsumerKey = textBox1.Text;
            Secret = textBox2.Text;

            label3.Text = "Result: ";
        }



        private static string generateSignature(string httpMethod, string ApiEndpoint, string parameters)
        {
            //encoded text should contain uppercase characters: '=' => %3D !!! (not %3d )
            //the HtmlUtility.UrlEncode creates lowercase encoded tags!
            //Here I use a urlencode class by Ian Hopkins
            string encodedUrl = UrlHelper.Encode(ApiEndpoint);
            string encodedParameters = UrlHelper.Encode(parameters);

            //generate the basestring
            string basestring = httpMethod + "&" + encodedUrl + "&";
            parameters = UrlHelper.Encode(parameters);
            basestring = basestring + parameters;

            //hmac-sha1 encryption:
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();

            //create key (request_token can be an empty string)
            string key = Secret + "&" + request_token;
            byte[] keyByte = encoding.GetBytes(key);

            //create message to encrypt
            byte[] messageBytes = encoding.GetBytes(basestring);

            //encrypt message using hmac-sha1 with the provided key
            HMACSHA1 hmacsha1 = new HMACSHA1(keyByte);
            byte[] hashmessage = hmacsha1.ComputeHash(messageBytes);

            //signature is the base64 format for the genarated hmac-sha1 hash
            string signature = System.Convert.ToBase64String(hashmessage);

            //encode the signature to make it url safe and return the encoded url
            return UrlHelper.Encode(signature);

        }

        //generator of unix epoch time
        public static String GetTimestamp()
        {
            int epoch = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            return epoch.ToString();
        }

        public async void GetA(string s)
        {
            //Exchanging the Request Token for an Access Token

            //Signing Requests
            string requestString = "https://www.flickr.com/services/oauth/request_token";

            //generate a random nonce and a timestamp
            Random rand = new Random();
            string nonce = rand.Next(999999).ToString();
            string timestamp = GetTimestamp();

            //create the parameter string in alphabetical order
            string parameters = "oauth_callback=" + UrlHelper.Encode("http://www.example.com");
            parameters += "&oauth_consumer_key=" + ConsumerKey;
            parameters += "&oauth_nonce=" + nonce;
            parameters += "&oauth_signature_method=HMAC-SHA1";
            parameters += "&oauth_timestamp=" + timestamp;
            parameters += "&oauth_version=1.0";

            //generate a signature base on the current requeststring and parameters
            string signature = generateSignature("GET", requestString, parameters);

            requestString = "https://www.flickr.com/services/oauth/access_token";
            //generate a random nonce and a timestamp
            rand = new Random();
            nonce = rand.Next(999999).ToString();
            timestamp = GetTimestamp();

            //Getting the User Authorization
            StringBuilder ab = new StringBuilder(s);
            int index = 0;
            for (int i = s.Length - 1; i >= 0; i--)
                if (s[i] == '&')
                {
                    index = i;
                    break;
                }
            string oauth_verifier = ab.Remove(0, index).ToString();

            //create the parameter string in alphabetical order
            parameters = "";
            parameters += "oauth_nonce=" + nonce;
            parameters += "&oauth_timestamp=" + timestamp;
            parameters += oauth_verifier;
            parameters += "&oauth_consumer_key=" + ConsumerKey;
            parameters += "&oauth_signature_method=HMAC-SHA1";
            parameters += "&oauth_version=1.0";
            parameters += "&oauth_token=" + oauth_token;
            parameters += "&oauth_signature=" + signature;

            //generate a signature base on the current requeststring and parameters

            string RequestTokenString = requestString + "?" + parameters;
            HttpClient web = new HttpClient();
            string result = await web.GetStringAsync(RequestTokenString);


            //Calling the Flickr API with OAuth
            //Signing Requests
            requestString = "https://www.flickr.com/services/oauth/request_token";

            //generate a random nonce and a timestamp
            rand = new Random();
            nonce = rand.Next(999999).ToString();
            timestamp = GetTimestamp();

            //create the parameter string in alphabetical order
            parameters = "";
            parameters = "oauth_callback=" + UrlHelper.Encode("http://www.example.com");
            parameters += "&oauth_consumer_key=" + ConsumerKey;
            parameters += "&oauth_nonce=" + nonce;
            parameters += "&oauth_signature_method=HMAC-SHA1";
            parameters += "&oauth_timestamp=" + timestamp;
            parameters += "&oauth_version=1.0";

            //generate a signature base on the current requeststring and parameters
            signature = generateSignature("GET", requestString, parameters);



            requestString = "https://api.flickr.com/services/rest";
            rand = new Random();
            nonce = rand.Next(999999).ToString();
            timestamp = GetTimestamp();

            parameters = "";
            parameters += "nojsoncallback=1 ";
            parameters += "&oauth_nonce=" + nonce;
            parameters += "&format=json";
            parameters += "&oauth_consumer_key=" + ConsumerKey;
            parameters += "&oauth_timestamp=" + timestamp;
            parameters += "&oauth_signature_method=HMAC-SHA1";
            parameters += "&oauth_version=1.0";
            parameters += "&oauth_token=" + oauth_token;
            parameters += "&oauth_signature=" + signature;
            parameters += "&method=flickr.test.login";

            string RestString = requestString + "?" + parameters;
            web = new HttpClient();
            result = await web.GetStringAsync(RestString);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string s = webBrowser1.Url.ToString();
            GetA(s);
        }
    }



    //converted from json to c#

    public class Photo
    {
        public string id { get; set; }
        public string owner { get; set; }
        public string secret { get; set; }
        public string server { get; set; }
        public int farm { get; set; }
        public string title { get; set; }
        public int ispublic { get; set; }
        public int isfriend { get; set; }
        public int isfamily { get; set; }
    }

    public class Photos
    {
        public int page { get; set; }
        public int pages { get; set; }
        public int perpage { get; set; }
        public string total { get; set; }
        public List<Photo> photo { get; set; }
    }

    public class FlickrData
    {
        public Photos photos { get; set; }
        public string stat { get; set; }
    }
}
