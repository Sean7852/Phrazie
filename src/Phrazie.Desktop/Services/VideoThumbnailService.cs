using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Media.Imaging;

namespace Phrazie.Desktop.Services;

public static class VideoThumbnailService
{
    [ComImport, Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemImageFactory
    {
        void GetImage([In, MarshalAs(UnmanagedType.Struct)] SIZE size,
                      [In] uint flags, [Out] out IntPtr phbm);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SIZE { public int cx; public int cy; }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void SHCreateItemFromParsingName(
        string pszPath, IntPtr pbc, ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory ppv);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    private static readonly Guid _guid = new("bcc18b79-ba16-442f-80c4-8a59c30c463b");

    public static Task<Bitmap?> GetThumbnailAsync(string filePath, int w = 160, int h = 90)
        => Task.Run(() => GetThumbnail(filePath, w, h));

    private static Bitmap? GetThumbnail(string filePath, int w, int h)
    {
        if (!File.Exists(filePath)) return null;
        IntPtr hBitmap = IntPtr.Zero;
        try
        {
            var guid = _guid;
            SHCreateItemFromParsingName(filePath, IntPtr.Zero, ref guid, out var factory);
            factory.GetImage(new SIZE { cx = w, cy = h }, 0x08 /* SIIGBF_THUMBNAILONLY */, out hBitmap);
            if (hBitmap == IntPtr.Zero) return null;
            using var bmp = System.Drawing.Image.FromHbitmap(hBitmap);
            using var ms  = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Position = 0;
            return new Bitmap(ms);
        }
        catch { return null; }
        finally { if (hBitmap != IntPtr.Zero) DeleteObject(hBitmap); }
    }
}
