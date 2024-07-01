using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Svg;
using System.Collections.Concurrent;
using System.Drawing.Imaging;

namespace XIVConfigUI;

/// <summary>
/// A image loader.
/// </summary>
public static class ImageLoader
{
    private static readonly ConcurrentDictionary<string, ImageResult?> _cachedTextures = [];
    private static readonly ConcurrentDictionary<GameIconLookup, ImageResult?> _cachedIcons = [];
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
    /// <param name="icon"></param>
    /// <param name="hq"></param>
    /// <param name="texture"></param>
    /// <returns></returns>
    public static bool GetTexture(GameIconLookup icon, out IDalamudTextureWrap texture)
    {
        texture = null!;
        if (!_cachedIcons.TryGetValue(icon, out var result))
        {
            _cachedIcons[icon] = null;
            Task.Run(() =>
            {
                _cachedIcons[icon] = new(Service.Texture.GetFromGameIcon(icon), null);
                Service.Log.Verbose($"Logged the image {icon}!");
            });
        }
        return result?.HasTexture(out texture!) ?? false;
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
        texture = null!;
        if (!_cachedTextures.TryGetValue(url, out var result))
        {
            _cachedTextures[url] = null;
            Task.Run(async () =>
            {
                _cachedTextures[url] = await LoadTexture(url);
                Service.Log.Verbose($"Logged the image at {url}!");
            });
        }
        return result?.HasTexture(out texture!) ?? false;
    }

    private static bool IsUrl(string url) => url.StartsWith("http:", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https:", StringComparison.OrdinalIgnoreCase);

    private static async Task<ImageResult?> LoadTexture(string url)
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

            return new(null, await LoadTexture(content));
        }
        else if (File.Exists(url))
        {
            return new(null, await LoadTexture(File.ReadAllBytes(url)));
        }
        else
        {
            return new(Service.Texture.GetFromGame(url), null);
        }
    }

    private static async Task<IDalamudTextureWrap?> LoadTexture(byte[] array)
    {
        foreach (var convert in _conversionsToBitmap)
        {
            try
            {
                return await Service.Texture.CreateFromImageAsync(convert(array));
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

internal readonly record struct ImageResult(ISharedImmediateTexture? Shared, IDalamudTextureWrap? Texture)
{
    public bool HasTexture(out IDalamudTextureWrap? texture)
    {
        if (Shared != null)
        {
            texture = Shared.GetWrapOrDefault();
        }
        else
        {
            texture = Texture;
        }
        return texture != null;
    }
}