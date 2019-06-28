using CRMC.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRMC.Client.Controlled
{
    public static class ScreenHelper
    {
        static bool sending = false;
        static ScreenHelper()
        {
            screen.Initialize();
            Telnet.Instance.SendNextScreen += async (p1, p2) =>
               {
                   await SendScreen();
               };
            /*
            不知道为什么，发送了不定（10~500）张截图以后，双方都不发内容了，
            只好用这种奇淫办法来应对一下，弥补自己知识的漏洞
            */
            Task.Run(async () =>
            {
                while (true)
                {
                    if (sending)
                    {
                        if ((DateTime.Now - lastSendTime).Seconds > 1)
                        {
                            await SendScreen();
                        }
                    }
                    await Task.Delay(1000);
                }
            });
        }
        static DateTime lastSendTime;
        private static ScreenRecoder screen = new ScreenRecoder()
        {
            ImageFormat = ImageFormat.Jpeg,
            //MinDealy = TimeSpan.FromSeconds(0.05),
            //MaxCount = 1000,
        };
        public static async Task StartSendScreen()
        {
            if (sending)
            {
                return;
            }
            await SendScreen();
            sending = true;
        }

        public static void StopSendScreen()
        {
            sending = false;
        }
        static int i = 0;
        private static Task SendScreen()
        {
            return Task.Run(async () =>
            {
                try
                {
                    byte[] bytes = null;
                    //bytes = new byte[i++];
                    do
                    {
                        Bitmap bitmap = screen.CaptureScreenBitmap(false);
                        using (var stream = new MemoryStream())
                        {
                            bitmap.Save(stream, ImageFormat.Png);
                            bytes = stream.ToArray();
                        }
                        //bytes = screen.CaptureScreenBytes();
                        await Task.Delay(16);
                    } while (bytes == null);
                    Telnet.Instance.Send(new Common.Model.CommandBody(ApiCommand.Screen_NewScreen, default, Global.CurrentClient.Id, bytes));
                    //Debug.WriteLine("发送");
                    //ok = true;
                    lastSendTime = DateTime.Now;
                }
                catch (Exception ex)
                {

                }
                //await Task.Delay(200);
            });
            //next = false;
        }
    }
}
