using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace HukeVideo
{
    public partial class Form1 : Form
    {
        static IntPtr nextClipboardViewer;

        public Form1()
        {
            InitializeComponent();
            nextClipboardViewer = (IntPtr)SetClipboardViewer(this.Handle);

            this.txtUrl.Text = "https://huke88.com/course/8325.html?pageType=1&key=C4D";
        }

        [DllImport("User32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

        [DllImport("User32.dll", CharSet = CharSet.Unicode)]
        private static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        // defined in winuser.h
        const int WM_DRAWCLIPBOARD = 0x308;
        const int WM_CHANGECBCHAIN = 0x030D;

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_DRAWCLIPBOARD:
                    SendMessage(nextClipboardViewer, m.Msg, m.WParam, m.LParam);
                    TriggerRun();
                    break;

                case WM_CHANGECBCHAIN:
                    if (m.WParam == nextClipboardViewer)
                        nextClipboardViewer = m.LParam;
                    else
                        SendMessage(nextClipboardViewer, m.Msg, m.WParam, m.LParam);
                    break;

                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            this.txtResult.Clear();
            Thread thread = new Thread(x => {

                try
                {
                    GetInfo();
                }
                catch(Exception ex)
                {
                    MessageBox.Show("获取失败！:" + ex.Message);
                }
                finally
                {
                    this.Invoke(new SwitchBtn(SwitchButtonState));
                }

            });

            thread.Start();
        }

        private delegate void SetInputValue(TextBox textbox, string value);
        private delegate void SwitchBtn();
        private delegate void ShowTipInfo(string msg);

        void GetInfo()
        {
            var url = this.txtUrl.Text;

            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("请先输入视频原网址！");
                return;
            }

            this.Invoke(new SwitchBtn(SwitchButtonState));

            var client = new RestClient("https://design.sjcjn.com/robot/index/index");
            var request = new RestRequest(Method.POST);
            request.AddHeader("Postman-Token", "0ddc4d2c-9df2-499e-a623-ac954b4b2475");
            request.AddHeader("Cache-Control", "no-cache");
            request.AddHeader("Referer", url);
            request.AddHeader("content-type", "multipart/form-data; boundary=----WebKitFormBoundary7MA4YWxkTrZu0gW");
            request.AddParameter(
                "multipart/form-data; boundary=----WebKitFormBoundary7MA4YWxkTrZu0gW",
                "------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"version\"\r\n\r\n1.0.3\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"token\"\r\n\r\n\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"website\"\r\n\r\n" + url + "\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"type\"\r\n\r\nvideo\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW\r\nContent-Disposition: form-data; name=\"sid\"\r\n\r\n\r\n------WebKitFormBoundary7MA4YWxkTrZu0gW--",
                ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                MessageBox.Show(string.Format("请求失败 {0}", response.StatusCode));
                return;
            }

            var content = response.Content;

            if (string.IsNullOrEmpty(content))
            {
                MessageBox.Show("获取视频信息错误!");
                return;
            }

            JObject jo = (JObject)JsonConvert.DeserializeObject(content);

            var msg = jo["msg"].Value<string>();

            this.Invoke(new SetInputValue(ShowResult), this.txtMsg, msg);

            if (!jo["data"].HasValues)
            {
                MessageBox.Show(string.Format("解析信息出错！:\r\n {0}", content));
                return;
            }

            var resultUrl = jo["data"]["url"].Value<string>();

            var js = @"
                $('#huke88-video').hkPlayer({
                    'playerVideoUrl': url,
                    'playerVideoUrlTwo': url,
                    'error': function (err) {
                        sendVideoPlayError(playerTypeForSend, (new Date()).valueOf());
                    },
                    'play': function () {
                        $('#huke88-video-play').remove();
            
                        if( $('#huke88-video .video-title').size()>0 ){
                            $('#huke88-video .video-title').remove();
                        }

                        $('#loginModal').hide();
                        layer.closeAll();
                    }
                });
            ".Replace("\r\n", "").Replace(" ", "").Replace("\t", "");

            var fristJs = "var url = '" + resultUrl + "';";

            js = fristJs + js;

            this.Invoke(new SetInputValue(ShowResult), this.txtResult, js);
            var tipMsg = string.Format("处理 {0} 完成，现在你可以粘贴了。", url);
            this.Invoke(new ShowTipInfo(ShowNotifyTipInfo), tipMsg);
        }

        void ShowResult(TextBox textbox,string value)
        {
            textbox.Text = value;
            if (textbox.Name == "txtResult")
                Clipboard.SetText(value);
        }

        void SwitchButtonState()
        {
            if (this.btnRun.Enabled)
            {
                this.btnRun.Enabled = false;
                this.btnRun.Text = "处理中请稍候";
            }
            else
            {
                this.btnRun.Enabled = true;
                this.btnRun.Text = "获取";
            }
            
        }

        void SetClip()
        {
            this.txtResult.SelectAll();
            var selText = this.txtResult.Text;
            if (!string.IsNullOrEmpty(selText))
                Clipboard.SetText(selText);
        }

        void TriggerRun()
        {
            var text = Clipboard.GetText();
            if (text.ToLower().StartsWith("https://huke88.com"))
            {
                this.txtUrl.Text = Clipboard.GetText();
                this.btnRun.PerformClick();
            }
        }

        void ShowNotifyTipInfo(string msg)
        {
            this.notifyIcon1.ShowBalloonTip(1000, "提示", msg, ToolTipIcon.Info);
        }

        private void txtResult_DoubleClick(object sender, EventArgs e)
        {
            SetClip();
        }

        private void Form1_Activated(object sender, EventArgs e)
        {
            TriggerRun();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            ChangeClipboardChain(this.Handle, nextClipboardViewer);
        }
    }
}
