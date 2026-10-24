using System.IO;
using UnityEditor;
using UnityEngine;

namespace LoneTruth.EditorTools
{
    /// <summary>
    /// Draws flat silhouette placeholders for sprites that the art team has not delivered yet.
    /// A placeholder is written only when no file exists at the path, so dropping the final PNG
    /// over the same path replaces it without breaking references (the GUID stays the same).
    /// </summary>
    public class PlaceholderPainter
    {
        public readonly int W;
        public readonly int H;
        private readonly Color32[] px;
        private readonly System.Random rnd;

        public PlaceholderPainter(int w, int h, int seed = 1)
        {
            W = Mathf.Max(4, w);
            H = Mathf.Max(4, h);
            px = new Color32[W * H];
            rnd = new System.Random(seed);
        }

        public static Color C(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            return c;
        }

        public void Set(int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= W || y >= H)
            {
                return;
            }

            px[y * W + x] = c;
        }

        public Color Get(int x, int y)
        {
            if (x < 0 || y < 0 || x >= W || y >= H)
            {
                return Color.clear;
            }

            return px[y * W + x];
        }

        public void Fill(Color c)
        {
            for (int i = 0; i < px.Length; i++)
            {
                px[i] = c;
            }
        }

        public void Rect(float x, float y, float w, float h, Color c)
        {
            int x0 = Mathf.RoundToInt(x), y0 = Mathf.RoundToInt(y);
            int x1 = Mathf.RoundToInt(x + w), y1 = Mathf.RoundToInt(y + h);
            for (int yy = y0; yy < y1; yy++)
            {
                for (int xx = x0; xx < x1; xx++)
                {
                    Set(xx, yy, c);
                }
            }
        }

        public void Frame(float x, float y, float w, float h, int t, Color c)
        {
            Rect(x, y, w, t, c);
            Rect(x, y + h - t, w, t, c);
            Rect(x, y, t, h, c);
            Rect(x + w - t, y, t, h, c);
        }

        public void Ellipse(float cx, float cy, float rx, float ry, Color c)
        {
            for (int yy = Mathf.FloorToInt(cy - ry); yy <= Mathf.CeilToInt(cy + ry); yy++)
            {
                for (int xx = Mathf.FloorToInt(cx - rx); xx <= Mathf.CeilToInt(cx + rx); xx++)
                {
                    float dx = (xx - cx) / rx, dy = (yy - cy) / ry;
                    if (dx * dx + dy * dy <= 1f)
                    {
                        Set(xx, yy, c);
                    }
                }
            }
        }

        // Trapezoid: bottom width wb at y, top width wt at y+h, centred on cx.
        public void Trap(float cx, float y, float wb, float wt, float h, Color c)
        {
            for (int yy = 0; yy < Mathf.RoundToInt(h); yy++)
            {
                float t = yy / Mathf.Max(1f, h);
                float w = Mathf.Lerp(wb, wt, t);
                Rect(cx - w / 2f, y + yy, w, 1, c);
            }
        }

        public void VGradient(Color bottom, Color top)
        {
            for (int y = 0; y < H; y++)
            {
                Color c = Color.Lerp(bottom, top, y / (float)(H - 1));
                for (int x = 0; x < W; x++)
                {
                    px[y * W + x] = c;
                }
            }
        }

        public void Noise(float amount)
        {
            for (int i = 0; i < px.Length; i++)
            {
                if (px[i].a == 0)
                {
                    continue;
                }

                float n = ((float)rnd.NextDouble() - 0.5f) * amount;
                Color c = px[i];
                px[i] = new Color(Mathf.Clamp01(c.r + n), Mathf.Clamp01(c.g + n), Mathf.Clamp01(c.b + n), c.a);
            }
        }

        public float Rand(float a, float b)
        {
            return a + (float)rnd.NextDouble() * (b - a);
        }

        /// <summary>Darken edges where an opaque pixel touches a transparent one — the ink outline.</summary>
        public void InkOutline(Color ink, int thickness = 2)
        {
            Color32[] copy = (Color32[])px.Clone();
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    if (copy[y * W + x].a == 0)
                    {
                        continue;
                    }

                    bool edge = false;
                    for (int d = 1; d <= thickness && !edge; d++)
                    {
                        edge = A(copy, x + d, y) || A(copy, x - d, y) || A(copy, x, y + d) || A(copy, x, y - d);
                    }

                    if (edge)
                    {
                        px[y * W + x] = ink;
                    }
                }
            }
        }

        private bool A(Color32[] src, int x, int y)
        {
            if (x < 0 || y < 0 || x >= W || y >= H)
            {
                return true;
            }

            return src[y * W + x].a == 0;
        }

        public bool SaveIfMissing(string assetPath)
        {
            string full = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            if (File.Exists(full))
            {
                return false;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(full));
            Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(full, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            return true;
        }
    }
}
