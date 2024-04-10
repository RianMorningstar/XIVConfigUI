using Dalamud.Interface.Internal;
using Svg;
using System.Collections.Concurrent;
using System.Drawing.Imaging;
using static Dalamud.Plugin.Services.ITextureProvider;

namespace XIVConfigUI;
public static class ImageLoader
{
    private static ConcurrentDictionary<string, IDalamudTextureWrap?> _cachedTextures = [];
    private static ConcurrentDictionary<(uint ID, IconFlags HQ), IDalamudTextureWrap?> _cachedIcons = [];
    private static readonly Dictionary<uint, uint> _actionIcons = [];


    private static readonly List<Func<byte[], byte[]>> _conversionsToBitmap = 
    [
        b => b,
        SvgToPng,
    ];

    public static void Init()
    {
        GetTexture(0, IconFlags.HiRes, out _);
        GetTextureRaw("ui/uld/image2.tex", out _);
    }

    private static byte[] SvgToPng(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var outStream = new MemoryStream();
        var svgDocument = SvgDocument.Open<SvgDocument>(stream);
        using var bitmap = svgDocument.Draw();
        bitmap.Save(outStream, ImageFormat.Png);
        return outStream.ToArray();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="texture"></param>
    /// <returns></returns>
    public static bool GetTextureAction(uint id, out IDalamudTextureWrap texture)
    {
        if (id == 0)
        {
            return ImageLoader.GetTexture(0, out texture, 0);
        }
        if (id == 3)
        {
            return ImageLoader.GetTexture(104, out texture, 0);
        }

        if (!_actionIcons.TryGetValue(id, out var iconId))
        {
            iconId = Service.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.Action>()?
                .GetRow(id)?.Icon ?? 0;
            _actionIcons[id] = iconId;
        }
        return ImageLoader.GetTexture(iconId, out texture);
    }

    public static bool GetTexture(uint icon, out IDalamudTextureWrap texture, uint @default = 0)
        => GetTexture(icon, IconFlags.HiRes, out texture)
        || GetTexture(icon, IconFlags.None, out texture)
        || GetTexture(@default, IconFlags.HiRes, out texture)
        || GetTexture(@default, IconFlags.None, out texture)
        || GetTexture(0, IconFlags.HiRes, out texture);

    public static bool GetTexture(uint icon, IconFlags hq, out IDalamudTextureWrap texture)
    {
        if (!_cachedIcons.TryGetValue((icon, hq), out texture!))
        {
            _cachedIcons[(icon, hq)] = null;
            Task.Run(() =>
            {
                _cachedIcons[(icon, hq)] = Service.Texture.GetIcon(icon, hq);
                Service.Log.Verbose($"Logged the image id {icon} with {hq}!");
            });
        }
        return texture is not null;
    }

    public static bool GetTexture(string path, out IDalamudTextureWrap texture)
        => GetTextureRaw(path, out texture)
        || IsUrl(path) && GetTextureRaw("ui/uld/image2.tex", out texture)
        || GetTexture(0, IconFlags.HiRes, out texture); // loading pics.

    private static bool GetTextureRaw(string url, out IDalamudTextureWrap texture)
    {
        if (!_cachedTextures.TryGetValue(url, out texture!))
        {
            _cachedTextures[url] = null;
            Task.Run(() =>
            {
                _cachedTextures[url] = LoadTexture(url);
                Service.Log.Verbose($"Logged the image at {url}!");

            });
        }
        return texture is not null;
    }

    private static bool IsUrl(string url) => url.StartsWith("http:", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https:", StringComparison.OrdinalIgnoreCase);

    private static IDalamudTextureWrap? LoadTexture(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return null;
        }
        else if (IsUrl(url)) //On the web.
        {
            using var httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(10),
            };
            var result = httpClient.GetAsync(url).Result;
            result.EnsureSuccessStatusCode();
            var content = result.Content.ReadAsByteArrayAsync().Result;

            Service.Log.Verbose($"Downloaded data from {url} successfully!");

            return LoadTexture(content);
        }
        else if (File.Exists(url))
        {
            return LoadTexture(File.ReadAllBytes(url));
        }
        else
        {
            return Service.Texture.GetTextureFromGame(url);
        }
    }

    private static IDalamudTextureWrap? LoadTexture(byte[] array)
    {
        foreach (var convert in _conversionsToBitmap)
        {
            try
            {
                return Service.PluginInterface.UiBuilder.LoadImage(convert(array));
            }
            catch
            {

            }
        }
        Service.Log.Verbose($"Failed to convert the data to an image!");
        return null;
    }
}
