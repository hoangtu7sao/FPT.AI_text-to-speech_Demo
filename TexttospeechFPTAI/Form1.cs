using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;

namespace TexttospeechFPTAI
{
    public partial class Form1 : Form
    {
        private static readonly string ApiUrl = "https://api.fpt.ai/hmi/tts/v5";
        private static readonly string ApiKey = "Lấy KeyAPI text-to-speech FPT.AI bỏ vào đây"; // Thay thế bằng API key của bạn
        public Form1()
        {
            InitializeComponent();
        }
        // Phần xử lý phản hồi JSON và tải file mp3
        private async void ProcessResponse(string jsonResponse)
        {
            JObject json = JObject.Parse(jsonResponse);
            int errorCode = (int)json["error"];
            if (errorCode == 0)
            {
                string asyncLink = (string)json["async"];
                string localFilePath = await DownloadFileAsync(asyncLink);
                PlayMp3FromUrl(localFilePath);
            }
            else
            {
                string errorMessage = (string)json["message"];
                MessageBox.Show($"Lỗi: {errorMessage}");
            }
        }
        // Phần tải file mp3 từ URL

        //private async Task<string> DownloadFileAsync(string url)
        //{
        //    using (WebClient client = new WebClient())
        //    {
        //        string tempFilePath = Path.GetTempFileName() + ".mp3";
        //        await client.DownloadFileTaskAsync(new Uri(url), tempFilePath);
        //        return tempFilePath;
        //    }
        //}
        private async Task<string> DownloadFileAsync(string url, int retries = 3, int delay = 5000)
        {
            using (WebClient client = new WebClient())
            {
                try
                {
                    string tempFilePath = Path.GetTempFileName() + ".mp3";
                    await client.DownloadFileTaskAsync(new Uri(url), tempFilePath);
                    return tempFilePath;
                }
                catch (WebException ex) when ((ex.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotFound)
                {
                    if (retries > 0)
                    {
                        await Task.Delay(delay); // Chờ một khoảng thời gian trước khi thử lại
                        return await DownloadFileAsync(url, retries - 1, delay); // Thử lại với số lần giảm xuống
                    }
                    else
                    {
                        throw; // Nếu hết số lần thử, ném lỗi ra ngoài
                    }
                }
            }
        }

        // Phần phát file mp3 từ đường dẫn file
        private void PlayMp3FromUrl(string filePath)
        {
            WMPLib.WindowsMediaPlayer wplayer = new WMPLib.WindowsMediaPlayer();
            wplayer.URL = filePath;
            wplayer.controls.play();
        }
        private async void btnSpeak_Click(object sender, EventArgs e)
        {
            string textToSpeak = txtBoxText.Text;
            if (textToSpeak.Length < 3 || textToSpeak.Length > 5000)
            {
                MessageBox.Show("Văn bản phải có từ 3 đến 5000 ký tự.");
                return;
            }

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("api_key", ApiKey);
                client.DefaultRequestHeaders.Add("voice", "banmai"); // Chọn giọng đọc
                client.DefaultRequestHeaders.Add("speed", "0"); // Tốc độ bình thường

                var content = new StringContent(textToSpeak, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(ApiUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    ProcessResponse(jsonResponse); // Gọi hàm xử lý phản hồi
                }
                else
                {
                    MessageBox.Show("Có lỗi xảy ra khi gửi yêu cầu.");
                }
            }
        }
    }
}
