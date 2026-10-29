using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace LoneTruth.EditorTools
{
    /// <summary>
    /// Hand-drawn character spritesheets in Assets/Sprites/Generated: slicing into named frames
    /// and building animation clips from them. A sheet is sliced only while it has no sprites
    /// with its prefix, so hand edits made in the Sprite Editor (rects, pivots) survive rebuilds;
    /// delete the sprites there to re-slice. Characters without a sheet keep the placeholder art;
    /// which frames each NPC uses is in Chapter1NpcSheets.
    /// </summary>
    public static partial class Chapter1LocationBuilder
    {
        /// <summary>One row of figures on a sheet.</summary>
        private readonly struct SheetRow
        {
            public readonly int Band;          // row of figures on the sheet, top = 0
            public readonly string Name;       // frames are named {Prefix}_{Name}_{00..}
            public readonly bool AnchorOnHead; // pivot under the head (walk, run) or the feet (standing)
            public readonly int MaxFrames;

            public SheetRow(int band, string name, bool anchorOnHead, int maxFrames = int.MaxValue)
            {
                Band = band;
                Name = name;
                AnchorOnHead = anchorOnHead;
                MaxFrames = maxFrames;
            }
        }

        private sealed class CharacterSheet
        {
            public string Path;
            public string Prefix;
            public float Ppu;
            public SheetRow[] Rows;

            public bool Exists => File.Exists(Path);
        }

        private static void ImportSheet(CharacterSheet sheet)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(sheet.Path);
            if (importer == null)
            {
                return;
            }

            bool configured = importer.textureType == TextureImporterType.Sprite &&
                              importer.spriteImportMode == SpriteImportMode.Multiple;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = sheet.Ppu;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            // Sheets can be wider than the default 2048 limit, which would downscale them.
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            var editCapability = provider.GetDataProvider<ISpriteFrameEditCapability>();
            if (editCapability == null ||
                !editCapability.GetEditCapability().HasCapability(EEditCapability.CreateAndDeleteSprite))
            {
                Debug.LogError($"[Sheets] The importer of {sheet.Path} does not allow creating sprites; it was not sliced.");
                return;
            }

            if (configured && provider.GetSpriteRects().Any(r => r.name.StartsWith(sheet.Prefix + "_")))
            {
                return;
            }

            SpriteRect[] rects = SliceSheet(sheet);
            provider.SetSpriteRects(rects);
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (names != null)
            {
                names.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());
            }

            provider.Apply();
            importer.SaveAndReimport();
            Debug.Log($"[Sheets] Sliced {rects.Length} {sheet.Prefix} frames.");
        }

        /// <summary>
        /// Finds the figures on the transparent sheet: rows of opaque pixels, then figures within a
        /// row. Each sprite reaches down to the row's ground line, with the pivot on the ground under
        /// the feet or the head.
        /// </summary>
        private static SpriteRect[] SliceSheet(CharacterSheet sheet)
        {
            // Read the source file itself: slicing works in original-image pixels.
            var tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes(sheet.Path));
            int w = tex.width, h = tex.height;
            Color32[] px = tex.GetPixels32();
            Object.DestroyImmediate(tex);
            bool Opaque(int x, int y) => px[y * w + x].a > 40;

            // Row bands, top of the image first (texture y grows upward).
            var bands = new List<(int Lo, int Hi)>();
            int start = -1;
            for (int y = h - 1; y >= -1; y--)
            {
                bool any = false;
                for (int x = 0; y >= 0 && x < w && !any; x++)
                {
                    any = Opaque(x, y);
                }

                if (any && start < 0) start = y;
                if (!any && start >= 0)
                {
                    if (start - y > 40) bands.Add((y + 1, start));
                    start = -1;
                }
            }

            var rects = new List<SpriteRect>();
            foreach (SheetRow row in sheet.Rows)
            {
                if (row.Band >= bands.Count)
                {
                    Debug.LogWarning($"[Sheets] {sheet.Path} has no row {row.Band} for {row.Name}.");
                    continue;
                }

                (int lo, int hi) = bands[row.Band];
                // Figures: runs of opaque columns, bridging small gaps (scarf tips, muzzle flash).
                var segments = new List<(int X0, int X1)>();
                int s = -1, lastOpaque = -100;
                for (int x = 0; x <= w; x++)
                {
                    bool any = false;
                    for (int y = lo; x < w && y <= hi && !any; y++)
                    {
                        any = Opaque(x, y);
                    }

                    if (any)
                    {
                        if (s < 0) s = x;
                        lastOpaque = x;
                    }
                    else if (s >= 0 && x - lastOpaque >= 12)
                    {
                        if (lastOpaque - s > 20) segments.Add((s, lastOpaque));
                        s = -1;
                    }
                }

                if (s >= 0 && lastOpaque - s > 20)
                {
                    segments.Add((s, lastOpaque));
                }

                for (int i = 0; i < segments.Count && i < row.MaxFrames; i++)
                {
                    (int x0, int x1) = segments[i];
                    int top = lo;
                    for (int y = hi; y >= lo && top == lo; y--)
                    {
                        for (int x = x0; x <= x1; x++)
                        {
                            if (Opaque(x, y)) { top = y; break; }
                        }
                    }

                    int figureHeight = top - lo;
                    int yFrom = row.AnchorOnHead ? top - Mathf.Max(4, figureHeight * 12 / 100) : lo;
                    int yTo = row.AnchorOnHead ? top : lo + Mathf.Max(4, figureHeight * 4 / 100);
                    double sum = 0;
                    int count = 0;
                    for (int y = yFrom; y <= yTo; y++)
                    {
                        for (int x = x0; x <= x1; x++)
                        {
                            if (Opaque(x, y)) { sum += x; count++; }
                        }
                    }

                    float anchorX = count > 0 ? (float)(sum / count) : (x0 + x1) / 2f;
                    var rect = new Rect(Mathf.Max(0, x0 - 2), lo, Mathf.Min(w, x1 + 3) - Mathf.Max(0, x0 - 2), Mathf.Min(h, top + 3) - lo);
                    rects.Add(new SpriteRect
                    {
                        name = $"{sheet.Prefix}_{row.Name}_{i:00}",
                        rect = rect,
                        alignment = SpriteAlignment.Custom,
                        pivot = new Vector2((anchorX - rect.x) / rect.width, 0f),
                        spriteID = GUID.Generate()
                    });
                }
            }

            return rects.ToArray();
        }

        /// <summary>A frame by its row name, e.g. SheetFrame(RuthSheet, "Stand_05").</summary>
        private static Sprite SheetFrame(CharacterSheet sheet, string frame)
        {
            string name = sheet.Prefix + "_" + frame;
            return AssetDatabase.LoadAllAssetsAtPath(sheet.Path).OfType<Sprite>().FirstOrDefault(sp => sp.name == name);
        }

        /// <summary>How much to scale a row drawn at a different size from the reference row.</summary>
        private static float SheetScale(CharacterSheet sheet, string frame, string referenceFrame)
        {
            Sprite sprite = SheetFrame(sheet, frame);
            Sprite reference = SheetFrame(sheet, referenceFrame);
            return sprite != null && reference != null ? reference.rect.height / sprite.rect.height : 1f;
        }

        /// <summary>Builds a clip from sheet frames, e.g. SheetClip(HoltSheet, "Holt_Walk", 4, true, 1f, "Walk_01", "Walk_06").</summary>
        private static AnimationClip SheetClip(CharacterSheet sheet, string clipName, float fps, bool loop, float scale, params string[] frames)
        {
            string path = $"{AnimDir}/{clipName}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }

            clip.ClearCurves();
            clip.frameRate = fps;
            // One key per frame: Unity holds the last sprite key for one frame, so the clip lasts
            // exactly frames / fps and every frame gets the same time.
            var keys = new ObjectReferenceKeyframe[frames.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                Sprite sprite = SheetFrame(sheet, frames[i]);
                if (sprite == null)
                {
                    Debug.LogWarning($"[Sheets] No sprite {sheet.Prefix}_{frames[i]} for {clipName}.");
                }

                keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprite };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);

            // Every sheet clip writes the scale, so a scaled row never leaks into the next state.
            float length = frames.Length / fps;
            foreach (string axis in new[] { "x", "y", "z" })
            {
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalScale." + axis),
                    AnimationCurve.Constant(0f, length, scale));
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimatorStateTransition Go(AnimatorState from, AnimatorState to, bool exitTime)
        {
            AnimatorStateTransition t = from.AddTransition(to);
            t.hasExitTime = exitTime;
            t.exitTime = 1f;
            t.duration = 0f;
            return t;
        }
    }
}
