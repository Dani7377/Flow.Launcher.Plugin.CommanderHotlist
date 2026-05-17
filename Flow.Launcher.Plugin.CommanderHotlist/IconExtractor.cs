using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Flow.Launcher.Plugin.CommanderHotlist
{
    internal static class IconExtractor
    {
        private static readonly string IconExtractorClassName = nameof(IconExtractor);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern uint ExtractIconEx(string lpszFile, int nIconIndex, IntPtr[] phiconLarge, IntPtr[] phiconSmall, uint nIcons);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        /// <summary>
        /// Tries to extract the icon from an exe file or returns the default icon of this plugin
        /// </summary>
        public static (ImageSource Icon, ActionResult Result) GetIconFromExe(string exePath, PluginInitContext context)
        {
            // STEP 1: try to get the icon from exePath

            if (!string.IsNullOrWhiteSpace(exePath))
            {
                try
                {
                    if (File.Exists(exePath))
                    {
                        _ = File.GetAttributes(exePath);

                        IntPtr[] largeIcons = new[] { IntPtr.Zero };
                        IntPtr[] smallIcons = new[] { IntPtr.Zero };

                        try
                        {
                            uint readIconCount = ExtractIconEx(exePath, 0, largeIcons, smallIcons, 1);

                            if (readIconCount != 0 && readIconCount != uint.MaxValue && largeIcons[0] != IntPtr.Zero)
                            {
                                ImageSource src = Imaging.CreateBitmapSourceFromHIcon(
                                    largeIcons[0],
                                    Int32Rect.Empty,
                                    BitmapSizeOptions.FromEmptyOptions()
                                );

                                src.Freeze();
                                return (src, ActionResult.Success());
                            }
                        }
                        finally
                        {
                            if (largeIcons[0] != IntPtr.Zero) DestroyIcon(largeIcons[0]);
                            if (smallIcons[0] != IntPtr.Zero) DestroyIcon(smallIcons[0]);
                        }
                    }
                }
                catch (Exception)
                {
                    // Icon extraction failed, we fallback to the default plugin's icon (step2)
                }
            }

            // STEP 2: Fallback to the default plugin icon
            try
            {
                string iconPath = Path.Combine(context.CurrentPluginMetadata.PluginDirectory, IconAssets.AppImage);
                if (File.Exists(iconPath))
                {
                    BitmapImage bitmap = new BitmapImage(new Uri(iconPath, UriKind.Absolute));
                    bitmap.Freeze();

                    return (bitmap, ActionResult.Success());
                }
            }
            catch (Exception ex)
            {
                return (GetEmptyFallbackIcon(), ActionResult.Fail("Failed to load the results icon.", ex, IconExtractorClassName));
            }

            return (GetEmptyFallbackIcon(), ActionResult.Fail($"Failed to load the results icon.", null, IconExtractorClassName));
        }

        private static ImageSource GetEmptyFallbackIcon()
        {
            var bitmap = new RenderTargetBitmap(1, 1, 96, 96, PixelFormats.Pbgra32);
            bitmap.Freeze();
            return bitmap;
        }
    }
}