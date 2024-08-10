using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;

namespace XIVConfigUI.Overlay;

/// <summary>
/// A static class for drawing extension.
/// </summary>
public static class DrawingExtensions
{
    /// <summary>
    /// The view Padding range.
    /// </summary>
    public static Vector4 ViewPadding { get; set; } = Vector4.One * 50;

    /// <summary>
    /// The length of sample, please don't set this too low!
    /// </summary>
    public static float SampleLength { get; set; } = 1;

    /// <summary>
    /// Can the point be seen by the active camera.
    /// </summary>
    /// <param name="point">testing point in world.</param>
    /// <returns>can be seen.</returns>
    public static unsafe bool CanSee(in this Vector3 point)
    {
        var camera = (Vector3)CameraManager.Instance()->CurrentCamera->Object.Position;

        var vec = point - camera;
        var dis = vec.Length() - 0.1f;

        int* unknown = stackalloc int[] { 0x4000, 0, 0x4000, 0 };

        RaycastHit hit = default;

        return !FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->BGCollisionModule
            ->RaycastMaterialFilter(&hit, &camera, &vec, dis, 1, unknown);
    }

    internal static void SegmentAction<T>(IEnumerable<T> pts, Action<T?, T> pairAction, in bool closed = true)
    {
        if (pairAction == null) return;
        if (pts == null || !pts.Any()) return;

        T? prePt = default;
        bool isFirst = true;
        foreach (var pt in pts)
        {
            if (isFirst)
            {
                isFirst = false;
                prePt = pt;
                continue;
            }
            pairAction(prePt, pt);
            prePt = pt;
        }

        if (closed) pairAction(prePt, pts.First());
    }

    #region Trasform
    /// <summary>
    /// Make the world point project into the screen.
    /// </summary>
    /// <param name="pts"></param>
    /// <param name="isClosed">Is pts closed</param>
    /// <param name="inScreen">Must be draw in the screen.</param>
    /// <returns></returns>
    public static Vector2[] GetPtsOnScreen(IEnumerable<Vector3> pts, bool isClosed, bool inScreen)
    {
        var cameraPts = DivideCurve(pts, SampleLength, isClosed)
            .Select(WorldToCamera).ToArray();
        var changedPts = ChangePtsBehindCamera(cameraPts);

        return changedPts.Select(p => CameraToScreen(p, inScreen)).ToArray();
    }

    private static IEnumerable<Vector3> DivideCurve(IEnumerable<Vector3> worldPts, float length, bool isClosed)
    {
        if (worldPts.Count() < 2 || length <= 0.01f) return worldPts;

        IEnumerable<Vector3> pts = [];

        SegmentAction(worldPts, (a, b) =>
        {
            pts = pts.Union(DashPoints(a, b, length));
        }, isClosed);

        if (!isClosed) pts = pts.Append(worldPts.Last());

        return pts;
    }

    private static Vector3[] DashPoints(Vector3 previous, Vector3 next, float length)
    {
        var dir = next - previous;
        var count = Math.Max(1, (int)(dir.Length() / length));
        var points = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            points[i] = previous + dir * i / count;
        }
        return points;
    }

    private static IEnumerable<Vector3> ChangePtsBehindCamera(Vector3[] cameraPts)
    {
        var changedPts = new List<Vector3>(cameraPts.Length * 2);

        for (int i = 0; i < cameraPts.Length; i++)
        {
            var pt1 = cameraPts[(i - 1 + cameraPts.Length) % cameraPts.Length];
            var pt2 = cameraPts[i];

            if (pt1.Z > 0 && pt2.Z <= 0)
            {
                GetPointOnPlane(pt1, ref pt2);
            }
            if (pt2.Z > 0 && pt1.Z <= 0)
            {
                GetPointOnPlane(pt2, ref pt1);
            }

            if (changedPts.Count > 0 && Vector3.Distance(pt1, changedPts[^1]) > 0.001f)
            {
                changedPts.Add(pt1);
            }

            changedPts.Add(pt2);
        }

        return changedPts.Where(p => p.Z > 0);
    }

    private const float PLANE_Z = 0.001f;
    private static void GetPointOnPlane(Vector3 front, ref Vector3 back)
    {
        if (front.Z <= 0) return;
        if (back.Z > 0) return;

        var ratio = (PLANE_Z - back.Z) / (front.Z - back.Z);
        back.X = (front.X - back.X) * ratio + back.X;
        back.Y = (front.Y - back.Y) * ratio + back.Y;
        back.Z = PLANE_Z;
    }

    private static unsafe Vector3 WorldToCamera(Vector3 worldPos)
    {
        var camera = CameraManager.Instance()->CurrentCamera;
        var pCoords = Vector4.Transform(new Vector4(worldPos, 1f), camera->ViewMatrix * camera->RenderCamera->ProjectionMatrix);
        return new(pCoords.X, pCoords.Y, pCoords.W);
    }

    private static unsafe Vector2 CameraToScreen(Vector3 cameraPos, bool inScreen)
    {
        var screenPos = new Vector2(cameraPos.X / MathF.Abs(cameraPos.Z), cameraPos.Y / MathF.Abs(cameraPos.Z));
        var windowPos = ImGuiHelpers.MainViewport.Pos;

        var device = Device.Instance();
        float width = device->Width;
        float height = device->Height;

        screenPos.X = 0.5f * width * (screenPos.X + 1f) + windowPos.X;
        screenPos.Y = 0.5f * height * (1f - screenPos.Y) + windowPos.Y;

        if (inScreen)
        {
            screenPos = GetPtInRect(windowPos, new Vector2(width, height), screenPos);
        }
        return screenPos;
    }

    /// <summary>
    /// Make the <paramref name="pt"/> into the Rectangle.
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="size"></param>
    /// <param name="pt"></param>
    /// <returns></returns>
    public static Vector2 GetPtInRect(Vector2 pos, Vector2 size, Vector2 pt)
    {
        var rec = size / 2;
        var center = pos + rec;
        return GetPtInRect(rec, pt - center) + center;
    }

    private static Vector2 GetPtInRect(Vector2 rec, Vector2 pt)
    {
        if (rec.X > 0)
        {
            pt.X /= rec.X;
        }
        else
        {
            pt.X = 0;
        }

        if (rec.Y > 0)
        {
            pt.Y /= rec.Y;
        }
        else
        {
            pt.Y = 0;
        }

        return GetPtIn1Rect(pt) * rec;
    }

    private static Vector2 GetPtIn1Rect(Vector2 pt)
    {
        if (pt.X is >= -1 and <= 1 && pt.Y is >= -1 and <= 1) return pt;

        var rate = Math.Max(Math.Abs(pt.X), Math.Abs(pt.Y));
        if (rate == 0) return pt;

        return new Vector2(pt.X / rate, pt.Y / rate);
    }
    #endregion
}
