using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ImageMagick;
using ImageMagick.ImageOptimizers;
using Microsoft.AspNetCore.StaticFiles;
using UltralightNet;
using UltralightNet.AppCore;
using UltralightNet.Platform;
using UltralightNet.Platform.HighPerformance;

namespace BotApi.Services
{
    public class ResultRenderer
    {
        private readonly ILogger<ResultRenderer> _logger;
        private readonly Thread _ulThread;
        private readonly Queue<Action> _renderQueue = new Queue<Action>();
        private Renderer _renderer;

        public ResultRenderer(ILogger<ResultRenderer> logger)
        {
            _logger = logger;

            _ulThread = new Thread(UltralightThread);
            _ulThread.Start();
        }

        private static IntPtr AllocateDelegate<TDelegate>(TDelegate d, out GCHandle handle) where TDelegate : Delegate
        {
            handle = GCHandle.Alloc(d);
            return Marshal.GetFunctionPointerForDelegate(d);
        }

        private void UltralightThread()
        {
            ULPlatform.Logger = new LoggerImpl(_logger);

            AppCoreMethods.ulEnablePlatformFileSystem(Path.GetFullPath("./assets"));
            AppCoreMethods.SetPlatformFontLoader();

            var cfg = new ULConfig();
            cfg.FontHinting = ULFontHinting.Smooth;

            _renderer = ULPlatform.CreateRenderer(cfg);

            while (true)
            {
                while (_renderQueue.Count > 0)
                    _renderQueue.Dequeue()();

                Thread.Sleep(16);
            }
        }

        public async Task<byte[]> RenderUrl(string url, int width, int height, object? data = null, MagickFormat format = MagickFormat.Jpeg)
        {
            var completed = false;
            byte[] result = null!;

            _renderQueue.Enqueue(() =>
            {
                using var view = _renderer.CreateView((uint)width, (uint)height, new ULViewConfig
                {
                    IsTransparent = true,
                    EnableImages = true,
                    EnableJavaScript = true,
                    FontFamilyStandard = "Microsoft YaHei UI",
                    FontFamilyFixed = "Microsoft YaHei UI",
                    FontFamilySansSerif = "Microsoft YaHei UI",
                    FontFamilySerif = "Microsoft YaHei UI",
                });

                var loaded = false;

                view.OnFinishLoading += (_, _, _) =>
                {
                    loaded = true;
                };

                if (data != null)
                {
                    view.OnWindowObjectReady += (_, _, _) =>
                    {
                        var json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                        view.EvaluateScript("window.data = " + json, out string exception);

                        if (!string.IsNullOrEmpty(exception))
                        {
                            _logger.Log(LogLevel.Warning, exception);
                        }
                    };
                }

                view.URL = url;

                var start = DateTime.Now;

                do
                {
                    _renderer.Update();
                    Thread.Sleep(10);
                }
                while (!loaded);

                _renderer.Render();

                var dur = DateTime.Now - start;
                _logger.LogInformation($"Render took {dur.TotalMilliseconds}ms");

                start = DateTime.Now;

                var surface = view.Surface ?? throw new Exception("Surface is null");
                var bitmap = surface.Bitmap;

                unsafe
                {
                    var pixels = bitmap.LockPixels();

                    using var image = new MagickImage(
                        new ReadOnlySpan<byte>(pixels, (int)(width * height * bitmap.Bpp)),
                        new PixelReadSettings(width, height, StorageType.Char, PixelMapping.BGRA)
                    );

                    image.Format = format;
                    result = image.ToByteArray();
                }

                bitmap.UnlockPixels();

                dur = DateTime.Now - start;
                _logger.LogInformation($"Encoding took {dur.TotalMilliseconds}ms");

                completed = true;
            });

            while (!completed)
                await Task.Delay(10);

            return result;
        }

        class LoggerImpl : UltralightNet.Platform.ILogger
        {
            private readonly ILogger<ResultRenderer> _logger;

            public LoggerImpl(ILogger<ResultRenderer> logger)
            {
                this._logger = logger;
            }

            public void Dispose()
            {

            }

            public void LogMessage(ULLogLevel logLevel, string message)
            {
                _logger.Log(logLevel switch
                {
                    ULLogLevel.Info => LogLevel.Information,
                    ULLogLevel.Error => LogLevel.Error,
                    ULLogLevel.Warning => LogLevel.Warning,
                    _ => throw new NotImplementedException(),
                }, message);
            }
        }
    }
}
