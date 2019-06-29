using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;


namespace CRMC.Client
{
    public class ScreenRecoder : IDisposable
    {
        private bool running;
        //private bool initilized;

        public int Size { get; private set; }
        public ScreenRecoder()
        {

        }

        public ImageFormat ImageFormat { get; set; } = ImageFormat.Bmp;
        public TimeSpan MinDealy { get; set; } = TimeSpan.Zero;

        public Action<byte[]> ScreenCaptured = null;
        Device device;
        Output1 output1;
        //int count = 0;
        Texture2D screenTexture;
        int width;
        int height;
        OutputDuplication duplicatedOutput;
        public void Initialize()
        {
            running = true;
            Factory1 factory = new Factory1();
            //Get first adapter
            Adapter1 adapter = factory.GetAdapter1(0);
            //Get device from adapter
            device = new Device(adapter);
            //Get front buffer of the adapter
            Output output = adapter.GetOutput(0);
            output1 = output.QueryInterface<Output1>();

            // Width/Height of desktop to capture
            width = output.Description.DesktopBounds.Right;
            height = output.Description.DesktopBounds.Bottom;

            // Create Staging texture CPU-accessible
            var textureDesc = new Texture2DDescription
            {
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = width,
                Height = height,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            };
            screenTexture = new Texture2D(device, textureDesc);
            duplicatedOutput = output1.DuplicateOutput(device);
        }
        public void Start()
        {

            Initialize();
            Task.Factory.StartNew(() =>
            {
                while (running)
                {
                    // Duplicate the output
                    CaptureScreenBitmap(true);
                    if (MinDealy != TimeSpan.Zero)
                    {
                        Thread.Sleep(MinDealy);
                    }
                }
            });
            //while (!initilized) ;
        }

        public byte[] CaptureScreenBytes()
        {
            using (Bitmap bitmap = CaptureScreenBitmap(false))
            {
                using (var ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat);
                    var bytes = ms.ToArray();
                    return bytes;
                }
            }
        }
        public Bitmap CaptureScreenBitmap(bool raiseEvent)
        {
            try
            {
                // 尝试在给定时间内获得重复帧是ms
                duplicatedOutput.AcquireNextFrame(5,
                    out OutputDuplicateFrameInformation duplicateFrameInformation,
                    out SharpDX.DXGI.Resource screenResource);

                // 将资源复制到可由cpu访问的内存中
                using (var screenTexture2D = screenResource.QueryInterface<Texture2D>())
                    device.ImmediateContext.CopyResource(screenTexture2D, screenTexture);

                // 获取桌面捕获纹理
                var mapSource = device.ImmediateContext.MapSubresource(screenTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);

                byte[] bytes = null;
                // 创建 Drawing.Bitmap
                Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

                var boundsRect = new Rectangle(0, 0, width, height);

                // 将像素从屏幕捕获纹理复制到gdi位图
                var mapDest = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
                var sourcePtr = mapSource.DataPointer;
                var destPtr = mapDest.Scan0;
                for (int y = 0; y < height; y++)
                {
                    // 复制一行
                    Utilities.CopyMemory(destPtr, sourcePtr, width * 4);

                    // 增加指针
                    sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                    destPtr = IntPtr.Add(destPtr, mapDest.Stride);
                }

                // 释放源和目标锁
                bitmap.UnlockBits(mapDest);
                device.ImmediateContext.UnmapSubresource(screenTexture, 0);
                if (raiseEvent)
                {
                    using (var ms = new MemoryStream())
                    {
                        bitmap.Save(ms, ImageFormat);
                        bytes = ms.ToArray();
                        ScreenCaptured?.Invoke(bytes);
                        //ScreenRefreshed?.Invoke(this, ms.ToArray());
                        //initilized = true;
                    }
                }
                screenResource.Dispose();
                duplicatedOutput.ReleaseFrame();
                if(bitmap==null)
                {

                }
                return bitmap;
            }
            catch (SharpDXException e)
            {
                if (e.ResultCode.Code != SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
                {
                    Trace.TraceError(e.Message);
                    Trace.TraceError(e.StackTrace);
                }
                return null;
            }


        }

        public void Stop()
        {
            running = false;
        }

        ~ScreenRecoder()
        {
            Dispose();
        }
        public void Dispose()
        {
            duplicatedOutput?.Dispose();
        }

    }
}
