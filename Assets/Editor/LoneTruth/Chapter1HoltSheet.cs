using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace LoneTruth.EditorTools
{
    /// <summary>
    /// Holt's hand-drawn spritesheet (Assets/Sprites/Generated/ProtagonistSpritesheet.png).
    /// Rows, top to bottom: idle gestures (6), walk (8), run (8), aim / fire / lower (8). When the
    /// sheet exists it replaces the placeholder Holt frames in AC_Holt and P_Player.
    /// Slicing runs only while the sheet has no Holt_* sprites, so hand edits made in the Sprite
    /// Editor (rects, pivots) survive rebuilds; delete the sprites there to re-slice.
    /// </summary>
    public static partial class Chapter1LocationBuilder
    {
        private const string HoltSheetPath = "Assets/Sprites/Generated/ProtagonistSpritesheet.png";

        // The walk / aim rows show Holt (1.85 m) about 301 px tall.
        private const float HoltSheetPpu = 163f;

        // Revolver muzzle in the aim frames, relative to the feet pivot (metres, facing right).
        private static readonly Vector3 HoltSheetMuzzle = new Vector3(0.91f, 1.45f, 0f);

        // Row name and whether frames are anchored on the head (moving rows) or on the feet.
        private static readonly (string Name, bool AnchorOnHead)[] HoltSheetRows =
        {
            ("Idle", false), ("Walk", true), ("Run", true), ("Aim", false)
        };

        private static bool HasHoltSheet => File.Exists(HoltSheetPath);

        private static void ImportHoltSheet()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(HoltSheetPath);
            if (importer == null)
            {
                return;
            }

            bool configured = importer.textureType == TextureImporterType.Sprite &&
                              importer.spriteImportMode == SpriteImportMode.Multiple;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = HoltSheetPpu;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            // The sheet is 2400 px wide; the default 2048 limit would downscale it.
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
                Debug.LogError("[HoltSheet] The importer does not allow creating sprites; the sheet was not sliced.");
                return;
            }

            if (configured && provider.GetSpriteRects().Any(r => r.name.StartsWith("Holt_")))
            {
                return;
            }

            SpriteRect[] rects = SliceHoltSheet();
            provider.SetSpriteRects(rects);
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (names != null)
            {
                names.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());
            }

            provider.Apply();
            importer.SaveAndReimport();
            Debug.Log($"[HoltSheet] Sliced {rects.Length} frames.");
        }

        /// <summary>
        /// Finds the figures on the transparent sheet: rows of opaque pixels, then figures within a
        /// row. Each sprite reaches down to the row's ground line, with the pivot on the ground under
        /// the feet (idle, aim) or under the head (walk, run — the feet move there).
        /// </summary>
        private static SpriteRect[] SliceHoltSheet()
        {
            // Read the source file itself: slicing works in original-image pixels.
            var tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes(HoltSheetPath));
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
            for (int b = 0; b < bands.Count && b < HoltSheetRows.Length; b++)
            {
                (int lo, int hi) = bands[b];
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

                for (int i = 0; i < segments.Count; i++)
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
                    bool head = HoltSheetRows[b].AnchorOnHead;
                    int yFrom = head ? top - Mathf.Max(4, figureHeight * 12 / 100) : lo;
                    int yTo = head ? top : lo + Mathf.Max(4, figureHeight * 4 / 100);
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
                        name = $"Holt_{HoltSheetRows[b].Name}_{i:00}",
                        rect = rect,
                        alignment = SpriteAlignment.Custom,
                        pivot = new Vector2((anchorX - rect.x) / rect.width, 0f),
                        spriteID = GUID.Generate()
                    });
                }
            }

            return rects.ToArray();
        }

        private static Sprite HoltFrame(string name)
        {
            return AssetDatabase.LoadAllAssetsAtPath(HoltSheetPath).OfType<Sprite>().FirstOrDefault(sp => sp.name == name);
        }

        /// <summary>Builds a clip from sheet frames, e.g. HoltSheetClip("Holt_Walk", 10, true, 1f, "Walk_00", …).</summary>
        private static AnimationClip HoltSheetClip(string clipName, float fps, bool loop, float scale, params string[] frames)
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
                string frame = frames[i];
                Sprite sprite = HoltFrame("Holt_" + frame);
                if (sprite == null)
                {
                    Debug.LogWarning($"[HoltSheet] No sprite Holt_{frame} for {clipName}.");
                }

                keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprite };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);

            // Every Holt clip writes the scale, so a scaled row (idle) never leaks into the next state.
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

        /// <summary>
        /// The idle row is drawn ~11% larger than the rest of the sheet; scale it down to match.
        /// </summary>
        private static float HoltIdleScale()
        {
            Sprite idle = HoltFrame("Holt_Idle_00");
            Sprite aim = HoltFrame("Holt_Aim_00");
            return idle != null && aim != null ? aim.rect.height / idle.rect.height : 1f;
        }

        /// <summary>AC_Holt from the sheet: idle / walk, interact and letter triggers, aim → fire → lower.</summary>
        private static RuntimeAnimatorController BuildHoltSheetController(string controllerPath)
        {
            float idleScale = HoltIdleScale();
            AssetDatabase.DeleteAsset(controllerPath);
            var holt = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            holt.AddParameter("Speed", AnimatorControllerParameterType.Float);
            holt.AddParameter("Interact", AnimatorControllerParameterType.Trigger);
            holt.AddParameter("ReadLetter", AnimatorControllerParameterType.Trigger);
            holt.AddParameter("Aim", AnimatorControllerParameterType.Bool);
            holt.AddParameter("Fire", AnimatorControllerParameterType.Trigger);
            UnityEditor.Animations.AnimatorStateMachine sm = holt.layers[0].stateMachine;

            UnityEditor.Animations.AnimatorState State(string name, Vector3 pos, AnimationClip clip)
            {
                UnityEditor.Animations.AnimatorState st = sm.AddState(name, pos);
                st.motion = clip;
                return st;
            }

            var idle = State("Idle", new Vector3(250, 0), HoltSheetClip("Holt_Idle", 1, true, idleScale, "Idle_00"));
            // Two-pose walk: the 2nd and 7th frames of the walk row, one per step.
            var walk = State("Walk", new Vector3(250, 100), HoltSheetClip("Holt_Walk", 4, true, 1f, "Walk_01", "Walk_06"));
            // Interact: hand to the chin; ReadLetter: hand at the coat — closest gestures on the sheet.
            var interact = State("Interact", new Vector3(500, 0), HoltSheetClip("Holt_Interact", 5, false, idleScale,
                "Idle_00", "Idle_03", "Idle_03", "Idle_00"));
            var read = State("ReadLetter", new Vector3(500, 100), HoltSheetClip("Holt_ReadLetter", 3, false, idleScale,
                "Idle_00", "Idle_04", "Idle_04", "Idle_04", "Idle_04", "Idle_00"));
            var raise = State("AimRaise", new Vector3(0, 250), HoltSheetClip("Holt_AimRaise", 14, false, 1f, "Aim_07", "Aim_06", "Aim_00"));
            var hold = State("AimHold", new Vector3(250, 250), HoltSheetClip("Holt_Aim", 1, true, 1f, "Aim_00"));
            var fire = State("Fire", new Vector3(500, 250), HoltSheetClip("Holt_Fire", 12, false, 1f, "Aim_04", "Aim_05", "Aim_00"));
            var lower = State("AimLower", new Vector3(250, 350), HoltSheetClip("Holt_AimLower", 12, false, 1f, "Aim_06", "Aim_07"));
            // Run frames are cut and ready for a future run; nothing plays them yet.
            HoltSheetClip("Holt_Run", 12, true, 1f, "Run_00", "Run_01", "Run_02", "Run_03", "Run_04", "Run_05", "Run_06", "Run_07");
            sm.defaultState = idle;

            UnityEditor.Animations.AnimatorStateTransition Go(UnityEditor.Animations.AnimatorState from, UnityEditor.Animations.AnimatorState to, bool exitTime)
            {
                var t = from.AddTransition(to);
                t.hasExitTime = exitTime;
                t.exitTime = 1f;
                t.duration = 0f;
                return t;
            }

            Go(idle, walk, false).AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.1f, "Speed");
            Go(walk, idle, false).AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.1f, "Speed");
            foreach (var pair in new[] { (interact, "Interact"), (read, "ReadLetter") })
            {
                var t = sm.AddAnyStateTransition(pair.Item1);
                t.hasExitTime = false; t.duration = 0f; t.canTransitionToSelf = false;
                t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0f, pair.Item2);
                Go(pair.Item1, idle, true);
            }

            // Aim is entered from idle / walk only (not AnyState: the bool stays true while aiming).
            Go(idle, raise, false).AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0f, "Aim");
            Go(walk, raise, false).AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0f, "Aim");
            Go(raise, hold, true);
            Go(hold, fire, false).AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0f, "Fire");
            Go(fire, hold, true);
            foreach (var from in new[] { raise, hold, fire })
            {
                Go(from, lower, false).AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0f, "Aim");
            }

            Go(lower, idle, true);
            return holt;
        }
    }
}
