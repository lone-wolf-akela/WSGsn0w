using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;

namespace WSGsn0w
{
    class Program
    {
        static void Main(string[] args)
        {
            Proxy proxy = new Proxy();
            proxy.Run();
        }
    }

    class Proxy
    {
        private bool notLoginIn = true;
        private int port = 8000;
        public void Run()
        {
            var proxyServer = new ProxyServer();

            proxyServer.BeforeResponse += OnResponse;
            //proxyServer.UpStreamHttpProxy = new ExternalProxy { HostName = "localhost", Port = 8888 };
            retry:
            try
            {
                var explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, port, true);
                proxyServer.ProxyEndPoints.Clear();
                proxyServer.AddEndPoint(explicitEndPoint);                
                proxyServer.Start();
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.Message);
                if (port > 10000)
                {
                    Console.WriteLine("多次尝试后仍未成功，程序无法正常运行，请按回车键退出。");
                    Console.ReadLine();
                    Environment.Exit(1);
                }
                Console.WriteLine($"端口{port}被占用，尝试改用其他端口……");
                port++;
                goto retry;
            }

            Console.WriteLine("WSGsn0w v0.0.5 by Lone_Wolf & MikuAlpha");
            Console.WriteLine("此工具为个人作品，作者不对使用此工具带来的任何后果负责。");
            Console.WriteLine("本工具适用于iOS与Android官方客户端(iOS安卓版客户端未做测试)。");

            Console.WriteLine("");
            Console.WriteLine("请保持手机/模拟器和电脑连入同一局域网(WiFi)");
            Console.WriteLine("在手机的网络设置中为WiFi链接设置HTTP代理服务器");
            Console.WriteLine("代理方式选择“手动”");
            Console.WriteLine("服务器地址填入电脑在局域网内的IP地址(一般为192.168.x.x)");
            Console.WriteLine($"端口填入  {port}");
            Console.WriteLine("然后启动游戏");

            Console.WriteLine("");
            Console.WriteLine("启动游戏后，该工具应显示相关消息。如果启动游戏后没有显示任何新提示，请检查：");
            Console.WriteLine("\t手机代理设置是否正确？");
            Console.WriteLine("\t电脑上是否有软件/防火墙阻止该工具访问网络？");
            Console.WriteLine("");

            Console.WriteLine("等待游戏启动……");
            Console.WriteLine("");

            //wait here
            Console.ReadLine();

            //Unsubscribe & Quit
            //proxyServer.BeforeResponse -= OnResponse;

            //proxyServer.Stop();
        }

        //Modify response
        public async Task OnResponse(object sender, SessionEventArgs e)
        {
            if (e.WebSession.Request.Url.Contains("version.jr.moefantasy.com/index/checkVer/"))
            {
                if (e.WebSession.Response.StatusCode == 200)
                {
                    string body = await e.GetResponseBodyAsString();
                    //body = body.Replace("\"snowing\":0", "\"snowing\":1");
                    body = body.Replace("\"cheatsCheck\":0", "\"cheatsCheck\":1");
                    body = body.Replace("_censor", "");
                    e.SetResponseBodyString(body);

                    Console.WriteLine("检测到游戏已启动");
                    Console.WriteLine("");

                    notLoginIn = true;
                }
            } else if (notLoginIn && e.WebSession.Request.Url.Contains("/api/initGame")) {
                Console.WriteLine("检测到您已经登录游戏。");
                Console.WriteLine("请进入游戏设置，进行文件校验即可");
            }
        }

        public static string GetMD5FromFile(string fileName)
        {
            var file = new FileStream(fileName, FileMode.Open);
            var md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(file);
            file.Close();
            return BitConverter.ToString(hash).Replace("-", "");
        }
    }
    [DebuggerDisplay("{" + nameof(text) + "}")]
    public class JsonText
    {
        public string text;
        public JObject obj;

        public JsonText(string txt)
        {
            text = txt;
            Parse();
        }

        public JToken this[string index]
        {
            get { return obj[index]; }
            set { obj[index] = value; }
        }

        public void Parse()
        {
            obj = JObject.Parse(text);
        }

        public override string ToString()
        {
            return obj.ToString();
        }
    }

}
