using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures;
using Svg;
using System.Collections.Concurrent;
using System.Drawing.Imaging;

namespace XIVConfigUI;

/// <summary>
/// A image loader.
/// </summary>
public static class ImageLoader
{
    private static readonly ConcurrentDictionary<string, ISharedImmediateTexture> _cachedSharedTextures = [];
    private static readonly ConcurrentDictionary<string, IDalamudTextureWrap> _cachedTextures = [];
    private static readonly Dictionary<uint, uint> _actionIcons = [];

    private static readonly List<Func<byte[], byte[]>> _conversionsToBitmap = 
    [
        b => b,
        SvgToPng,
    ];

    internal static void Init()
    {
        GetTexture(new GameIconLookup(0, false, true), out _);
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
            return GetTexture(0, out texture, 0);
        }
        if (id == 3)
        {
            return GetTexture(104, out texture, 0);
        }

        if (!_actionIcons.TryGetValue(id, out var iconId))
        {
            iconId = Service.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.Action>()?
                .GetRow(id)?.Icon ?? 0;
            _actionIcons[id] = iconId;
        }
        return GetTexture(iconId, out texture);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="icon"></param>
    /// <param name="texture"></param>
    /// <param name="default"></param>
    /// <returns></returns>
    public static bool GetTexture(uint icon, out IDalamudTextureWrap texture, uint @default = 0)
        => GetTexture(new GameIconLookup(icon, false, true), out texture)
        || GetTexture(new GameIconLookup(icon, false, false), out texture)
        || GetTexture(new GameIconLookup(@default, false, true), out texture)
        || GetTexture(new GameIconLookup(@default, false, false), out texture)
        || GetTexture(new GameIconLookup(0, false, true), out texture);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="lookup"></param>
    /// <param name="hq"></param>
    /// <param name="texture"></param>
    /// <returns></returns>
    public static bool GetTexture(GameIconLookup lookup, out IDalamudTextureWrap texture)
    {
        texture = Service.Texture.GetFromGameIcon(lookup).GetWrapOrEmpty();
        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="texture"></param>
    /// <returns></returns>
    public static bool GetTexture(string path, out IDalamudTextureWrap texture)
        => GetTextureRaw(path, out texture)
        || IsUrl(path) && GetTextureRaw("ui/uld/image2.tex", out texture)
        || GetTexture(new GameIconLookup(0, false, true), out texture); // loading pics.

    private static bool GetTextureRaw(string url, out IDalamudTextureWrap texture)
    {
        if (_cachedTextures.TryGetValue(url, out texture!)) return true;
        if (_cachedSharedTextures.TryGetValue(url, out var share) && share != null)
        {
            return share.TryGetWrap(out texture!, out _);
        }
        LoadTexture(url);
        return false;
    }

    private static bool IsUrl(string url) => url.StartsWith("http:", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https:", StringComparison.OrdinalIgnoreCase);

    private static void LoadTexture(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return;
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

            var i = LoadTexture(content);
            if (i != null)
            {
                _cachedTextures[url] = i;
            }
        }
        else if (File.Exists(url))
        {
            var i = LoadTexture(File.ReadAllBytes(url));
            if (i != null)
            {
                _cachedTextures[url] = i;
            }
        }
        else
        {
            _cachedSharedTextures[url] = Service.Texture.GetFromGame(url);
        }
    }

    private static IDalamudTextureWrap? LoadTexture(byte[] array)
    {
        foreach (var convert in _conversionsToBitmap)
        {
            try
            {
                return Service.Texture.CreateFromImageAsync(convert(array)).Result;
            }
#if DEBUG
            catch (Exception ex) 
            {
                Service.Log.Error(ex, "Failed to load the image");
#else
            catch
            {
#endif
            }
    }
        Service.Log.Verbose($"Failed to convert the data to an image!");
        return null;
    }
}
