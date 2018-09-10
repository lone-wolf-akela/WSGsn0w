using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
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
        private string ResUrl;
        private string ResUrlWu;
        private int port = 8000;
        public void Run()
        {
            var proxyServer = new ProxyServer();

            proxyServer.BeforeRequest += OnRequest;
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
            Console.WriteLine($"端口{port}上的代理服务器已启动");
            Console.WriteLine();


            Console.WriteLine("WSGsn0w v0.0.4 by Lone_Wolf");
            Console.WriteLine("此工具为个人作品，作者不对使用此工具带来的任何后果负责。");
            Console.WriteLine("下面的操作指南只针对iOS设备。尽管该工具可能对安卓设备也有效，但我没有做过任何安卓设备的测试；要在安卓设备上使用该工具，操作方法也可能与iOS有一定的不同之处。");

            Console.WriteLine("");
            Console.WriteLine("请保持手机和电脑连入同一WiFi/路由器/局域网");
            Console.WriteLine("在手机的网络设置中为WiFi链接设置HTTP代理服务器");
            Console.WriteLine("代理方式选择“手动”");
            Console.WriteLine("服务器地址填入电脑在局域网内的IP地址");
            Console.WriteLine($"端口填入\t{port}");
            Console.WriteLine("然后启动游戏");

            Console.WriteLine("");
            Console.WriteLine("启动游戏后，该工具应显示进一步的指示。如果启动游戏后没有显示任何新提示，请检查：");
            Console.WriteLine("\t手机代理设置是否正确？");
            Console.WriteLine("\t电脑上是否有任何防火墙（包括系统自带的Windows防火墙）阻止了该工具访问网络？");
            Console.WriteLine("");

            Console.WriteLine("等待游戏启动……");
            Console.WriteLine("");

            //wait here
            Console.ReadLine();

            //Unsubscribe & Quit
            //proxyServer.BeforeResponse -= OnResponse;

            //proxyServer.Stop();
        }

        public async Task OnRequest(object sender, SessionEventArgs e)
        {
            string server = "";
            string orgpath = "";
            //Console.WriteLine(e.WebSession.Request.RequestUri.AbsoluteUri);
            if (e.WebSession.Request.RequestUri.AbsoluteUri.Contains(ResUrl))
            {
                orgpath =
                    e.WebSession.Request.RequestUri.AbsoluteUri.Replace(ResUrl, ResUrlWu);
                server = ResUrlWu;
            }
            else if (e.WebSession.Request.RequestUri.AbsoluteUri.Contains(ResUrlWu))
            {
                orgpath = e.WebSession.Request.RequestUri.AbsoluteUri;
                server = ResUrlWu;
            }

            if (server != "")
            {
                string path = orgpath.Replace(server, "");
                path = path.TrimStart('/');
                var paths = path.Split(new[] { "?md5=" }, StringSplitOptions.None);
                paths[0] = "hot/" + paths[0];
                if (File.Exists(paths[0]) &&
                    GetMD5FromFile(paths[0]).ToLower() == paths[1].ToLower())
                {
                    Console.WriteLine("从本地缓存加载文件：" + paths[0]);
                    
                }
                else
                {
                    var c = new WebClient();
                    string[] t = paths[0].Split(new[] { "?v=" }, StringSplitOptions.None);
                    paths[0] = t[0];
                    string folder = Path.GetDirectoryName(paths[0]);
                    Directory.CreateDirectory(folder);
                    Console.WriteLine("从服务器下载文件：" + paths[0]);
                    c.DownloadFile(orgpath, paths[0]);                   
                }

                byte[] file = File.ReadAllBytes(paths[0]);
                await e.Ok(file,
                    new Dictionary<string, HttpHeader>{
                        { "Connection",new HttpHeader("Connection","keep-alive")},
                        { "Accept-Ranges",new HttpHeader("Accept-Ranges","bytes")},
                        { "Content-Type",new HttpHeader("Content-Type","application/octet-stream")},
                        { "Content-Length",new HttpHeader("Content-Length",file.Length.ToString())}
                    }
                );

            }
        }

        //Modify response
        public async Task OnResponse(object sender, SessionEventArgs e)
        {
            if (e.WebSession.Request.Url.Contains("version.jr.moefantasy.com"))
            {
                if (e.WebSession.Response.ResponseStatusCode == 200)
                {
                    JsonText version_txt;
                    string body = await e.GetResponseBodyAsString();
                    //body = body.Replace("\"snowing\":0", "\"snowing\":1");
                    body = body.Replace("\"cheatsCheck\":0", "\"cheatsCheck\":1");
                    body = Regex.Replace(
                        body,
                        @"(?<first>""ResUrlWu"".*?)censor(?<second>.*?)",
                        @"${first}2${second}"
                    );
                    await e.SetResponseBodyString(body);

                    version_txt = new JsonText(body);
                    if (version_txt["ResUrlWu"] != null)
                    {
                        WebClient client = new WebClient();
                        JsonText proj_manifest;
                        proj_manifest = new JsonText(client.DownloadData((string)version_txt["ResUrlWu"]).decompressGZipData());
                        ResUrlWu = (string)proj_manifest["packageUrl"];
                        proj_manifest = new JsonText(client.DownloadData((string)version_txt["ResUrl"]).decompressGZipData());
                        ResUrl = (string)proj_manifest["packageUrl"];
                    }
                    else
                    {
                        throw new Exception("没有找到反和谐ResUrlWu地址，无法获取必须的信息。工具因而出错退出。");
                    }

                    Console.WriteLine("检测到游戏已启动");
                    Console.WriteLine("");
                    Console.WriteLine("如果在输入账号密码登录时游戏提示“请检查您的网络连接”，此时不要惊慌，按照以下步骤即可进入游戏：");
                    Console.WriteLine("\t请点击一次“快速登陆”，游戏此时会提示“暂未开放”");
                    Console.WriteLine("\t这时候再次点击游戏的“登录”按钮，即可进入游戏");
                    Console.WriteLine("");

                    notLoginIn = true;
                }
            }  
            
            else if (notLoginIn && e.WebSession.Request.Url.Contains("/api/initGame"))
            {
                Console.WriteLine("检测到您已经登录游戏。");
                Console.WriteLine("请进入游戏设置，正常AB即可");
                Console.WriteLine("（如果下载文件过程中卡住了的话，不必担心，重启游戏就好）");
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

    static class Extension
    {
        public static string decompressZlibData(this byte[] data)
        {
            using (var memstream = new MemoryStream(data))
            {
                memstream.ReadByte();
                memstream.ReadByte();
                using (var dzip = new DeflateStream(memstream, CompressionMode.Decompress))
                {
                    using (var sr = new StreamReader(dzip))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
        }

        public static string decompressGZipData(this byte[] data)
        {
            using (var memstream = new MemoryStream(data))
            {
                using (var dzip = new GZipStream(memstream, CompressionMode.Decompress))
                {
                    using (var sr = new StreamReader(dzip))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
        }
    }
}
