using System.Collections.Generic;
using System.IO;
using LoneTruth.Combat;
using LoneTruth.Core;
using LoneTruth.Environment;
using LoneTruth.Interaction;
using LoneTruth.Inventory;
using LoneTruth.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LoneTruth.EditorTools
{
    /// <summary>
    /// Builds locations 1-01 "Station" (30 m) and 1-02 "Main Street" (60 m) from the
    /// "Lone Truth — карты и квесты" canvas. Re-runnable: placeholder art is drawn only where no
    /// file exists yet, prefabs and scenes are regenerated. X in the scene = metres from the
    /// room's left edge, ground at y = 0, play line at z = 0, background facades at z ≈ 3.
    /// </summary>
    public static class Chapter1LocationBuilder
    {
        private const string ArtRoot = "Assets/Art/Chapter1";
        private const string CharDir = ArtRoot + "/Characters";
        private const string EnvDir = ArtRoot + "/Environment";
        private const string PropDir = ArtRoot + "/Props";
        private const string FxDir = ArtRoot + "/FX";
        private const string MatDir = ArtRoot + "/Materials";
        private const string AnimDir = ArtRoot + "/Animation";
        private const string PrefabDir = "Assets/Prefabs/Chapter1";
        private const string SceneDir = "Assets/Scenes/Chapter1";
        private const string StationScene = SceneDir + "/Station.unity";
        private const string StreetScene = SceneDir + "/MainStreet.unity";
        private const int Ppu = 100;
        private const int RainOrder = 10;
        private const int ForegroundOrder = 20;

        private static readonly Color Ink = PlaceholderPainter.C("#0E0F11");

        [MenuItem("Lone Truth/Build/Chapter 1 — 1-01 Station + 1-02 Main Street")]
        public static void BuildFromMenu()
        {
            Build();
        }

        public static string Build()
        {
            EnsureFolders();
            DrawPlaceholders();
            ConfigureImports();
            Dictionary<string, RuntimeAnimatorController> controllers = BuildAnimation();
            Prefabs prefabs = BuildPrefabs(controllers);
            Material[] mats = BuildMaterials();
            VolumeProfile profile = BuildVolumeProfile();

            BuildStation(prefabs, controllers, mats, profile);
            BuildStreet(prefabs, controllers, mats, profile);

            AddToBuildSettings(StationScene, StreetScene);
            AssetDatabase.SaveAssets();
            return "Built " + StationScene + " and " + StreetScene;
        }

        // ------------------------------------------------------------------ folders

        private static void EnsureFolders()
        {
            foreach (string dir in new[] { CharDir, EnvDir + "/Station", EnvDir + "/MainStreet", EnvDir + "/Shared", PropDir, FxDir, MatDir, AnimDir, PrefabDir, SceneDir })
            {
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), dir));
            }

            AssetDatabase.Refresh();
        }

        // ------------------------------------------------------------------ placeholder art

        private struct Human
        {
            public float Height;   // metres
            public Color Coat, Trousers, Hat, Accent, Skin;
            public int HatStyle;   // 0 none, 1 fedora, 2 wide brim, 3 cap, 4 newsboy cap
            public bool LongCoat;
        }

        private static Human Holt => new Human
        {
            Height = 1.85f, Coat = PlaceholderPainter.C("#2E3033"), Trousers = PlaceholderPainter.C("#24262A"),
            Hat = PlaceholderPainter.C("#3A3431"), Accent = PlaceholderPainter.C("#5E2A2C"), Skin = PlaceholderPainter.C("#B8A48E"),
            HatStyle = 1, LongCoat = true
        };

        private static Human Callahan => new Human
        {
            Height = 1.9f, Coat = PlaceholderPainter.C("#6E5B45"), Trousers = PlaceholderPainter.C("#3E352C"),
            Hat = PlaceholderPainter.C("#3B2F27"), Accent = PlaceholderPainter.C("#8A8F95"), Skin = PlaceholderPainter.C("#A88F78"),
            HatStyle = 2, LongCoat = true
        };

        private static Human Conductor => new Human
        {
            Height = 1.75f, Coat = PlaceholderPainter.C("#2B3240"), Trousers = PlaceholderPainter.C("#22262E"),
            Hat = PlaceholderPainter.C("#2B3240"), Accent = PlaceholderPainter.C("#9C8650"), Skin = PlaceholderPainter.C("#B39C86"),
            HatStyle = 3, LongCoat = false
        };

        private static Human Liam => new Human
        {
            Height = 1.35f, Coat = PlaceholderPainter.C("#5C5F63"), Trousers = PlaceholderPainter.C("#3F4146"),
            Hat = PlaceholderPainter.C("#4A4C50"), Accent = PlaceholderPainter.C("#8E7A5A"), Skin = PlaceholderPainter.C("#BCA690"),
            HatStyle = 4, LongCoat = false
        };

        private static Human PasserbyMiner => new Human
        {
            Height = 1.78f, Coat = PlaceholderPainter.C("#4A4642"), Trousers = PlaceholderPainter.C("#33302C"),
            Hat = PlaceholderPainter.C("#3A3733"), Accent = PlaceholderPainter.C("#A57B3A"), Skin = PlaceholderPainter.C("#A08A76"),
            HatStyle = 3, LongCoat = false
        };

        private static Human PasserbyWoman => new Human
        {
            Height = 1.65f, Coat = PlaceholderPainter.C("#5B5450"), Trousers = PlaceholderPainter.C("#4B4542"),
            Hat = PlaceholderPainter.C("#3E3A37"), Accent = PlaceholderPainter.C("#7A5E48"), Skin = PlaceholderPainter.C("#B8A28C"),
            HatStyle = 0, LongCoat = true
        };

        /// <param name="walk">walk phase in [0,1), or -1 for standing</param>
        /// <param name="arm">0 down, 1 reaching forward (interact), 2 holding a letter at chest</param>
        /// <param name="bob">vertical breathing offset in px</param>
        private static PlaceholderPainter PaintHuman(Human h, float walk, int arm, int bob, int seed)
        {
            int H = Mathf.RoundToInt(h.Height * Ppu) + 8;
            int W = Mathf.RoundToInt(0.9f * Ppu);
            var p = new PlaceholderPainter(W, H, seed);
            float s = h.Height / 1.85f;
            float cx = W / 2f;
            float hip = 86 * s, shoulder = 150 * s, headC = 166 * s + bob;

            // legs
            float swing = walk < 0 ? 0f : Mathf.Sin(walk * Mathf.PI * 2f) * 10f * s;
            p.Rect(cx - 9 * s + swing, 0, 8 * s, hip, h.Trousers);
            p.Rect(cx + 1 * s - swing, 0, 8 * s, hip, h.Trousers);
            p.Rect(cx - 12 * s + swing, 0, 13 * s, 5, Ink);
            p.Rect(cx - 1 * s - swing, 0, 13 * s, 5, Ink);

            // torso / coat
            float coatBottom = h.LongCoat ? 40 * s : 78 * s;
            p.Trap(cx, coatBottom, h.LongCoat ? 46 * s : 34 * s, 36 * s, shoulder - coatBottom + bob, h.Coat);
            p.Rect(cx - 13 * s, shoulder - 6 * s + bob, 26 * s, 8 * s, h.Accent); // scarf / collar / badge band

            // arms
            if (arm == 0)
            {
                float a = walk < 0 ? 0f : -swing * 0.6f;
                p.Rect(cx + 14 * s + a, 92 * s + bob, 7 * s, 56 * s, h.Coat);
                p.Rect(cx + 14 * s + a, 86 * s + bob, 7 * s, 7 * s, h.Skin);
            }
            else if (arm == 1)
            {
                p.Rect(cx + 12 * s, 128 * s + bob, 30 * s, 8 * s, h.Coat);
                p.Rect(cx + 40 * s, 127 * s + bob, 6 * s, 9 * s, h.Skin);
            }
            else
            {
                p.Rect(cx + 6 * s, 112 * s + bob, 18 * s, 8 * s, h.Coat);
                p.Rect(cx + 16 * s, 108 * s + bob, 14 * s, 18 * s, PlaceholderPainter.C("#D8CFBE")); // the letter
            }

            // head
            p.Ellipse(cx + 2 * s, headC, 10 * s, 12 * s, h.Skin);

            // hat
            switch (h.HatStyle)
            {
                case 1:
                    p.Rect(cx - 18 * s, headC + 6 * s, 38 * s, 4 * s, h.Hat);
                    p.Rect(cx - 10 * s, headC + 9 * s, 23 * s, 11 * s, h.Hat);
                    break;
                case 2:
                    p.Rect(cx - 24 * s, headC + 6 * s, 50 * s, 4 * s, h.Hat);
                    p.Rect(cx - 11 * s, headC + 9 * s, 25 * s, 10 * s, h.Hat);
                    break;
                case 3:
                    p.Rect(cx - 10 * s, headC + 8 * s, 22 * s, 9 * s, h.Hat);
                    p.Rect(cx + 8 * s, headC + 7 * s, 10 * s, 3 * s, h.Hat);
                    break;
                case 4:
                    p.Ellipse(cx + 2 * s, headC + 11 * s, 14 * s, 7 * s, h.Hat);
                    break;
            }

            p.InkOutline(Ink, 2);
            return p;
        }

        private static void Char(string name, Human h, float walk, int arm, int bob, int seed)
        {
            PaintHuman(h, walk, arm, bob, seed).SaveIfMissing($"{CharDir}/{name}.png");
        }

        private static void DrawPlaceholders()
        {
            // --- Sam Holt: reference + the actions used in 1-01 and 1-02
            Char("CH_Holt_Ref", Holt, -1, 0, 0, 1);
            for (int i = 0; i < 4; i++) Char($"CH_Holt_Idle_{i:00}", Holt, -1, 0, i == 1 || i == 2 ? 1 : 0, 10 + i);
            for (int i = 0; i < 8; i++) Char($"CH_Holt_Walk_{i:00}", Holt, i / 8f, 0, (i % 4 == 1) ? 1 : 0, 20 + i);
            for (int i = 0; i < 4; i++) Char($"CH_Holt_Interact_{i:00}", Holt, -1, i == 0 || i == 3 ? 0 : 1, 0, 30 + i);
            for (int i = 0; i < 6; i++) Char($"CH_Holt_ReadLetter_{i:00}", Holt, -1, i == 0 || i == 5 ? 0 : 2, i % 2, 40 + i);

            // --- NPCs present in 1-01 / 1-02
            Char("CH_Conductor_Ref", Conductor, -1, 0, 0, 50);
            for (int i = 0; i < 2; i++) Char($"CH_Conductor_Idle_{i:00}", Conductor, -1, 0, i, 51 + i);
            Char("CH_Callahan_Ref", Callahan, -1, 0, 0, 60);
            for (int i = 0; i < 2; i++) Char($"CH_Callahan_Idle_{i:00}", Callahan, -1, 0, i, 61 + i);
            Char("CH_Liam_Ref", Liam, -1, 0, 0, 70);
            Char("CH_PasserbyMiner_Ref", PasserbyMiner, -1, 0, 0, 80);
            for (int i = 0; i < 2; i++) Char($"CH_PasserbyMiner_Idle_{i:00}", PasserbyMiner, -1, 0, i, 81 + i);
            Char("CH_PasserbyWoman_Ref", PasserbyWoman, -1, 0, 0, 90);
            for (int i = 0; i < 2; i++) Char($"CH_PasserbyWoman_Idle_{i:00}", PasserbyWoman, -1, 0, i, 91 + i);

            // --- shared environment
            PaintSky().SaveIfMissing($"{EnvDir}/Shared/ENV_Sky_NightRain.png");
            PaintMountains().SaveIfMissing($"{EnvDir}/Shared/ENV_Mountains_Fog.png");
            PaintPlanks(PlaceholderPainter.C("#4A4038"), 3).SaveIfMissing($"{EnvDir}/Shared/TEX_Planks_Wet.png");
            PaintMud().SaveIfMissing($"{EnvDir}/MainStreet/TEX_Street_Mud.png");
            PaintRails().SaveIfMissing($"{EnvDir}/Station/TEX_Station_Trackbed.png");
            PaintLamppost().SaveIfMissing($"{PropDir}/PRP_Lamppost.png");
            PaintCrates().SaveIfMissing($"{PropDir}/PRP_CrateStack.png");
            PaintBarrels().SaveIfMissing($"{PropDir}/PRP_Barrels.png");
            PaintTelegraphPole().SaveIfMissing($"{PropDir}/PRP_TelegraphPole.png");
            PaintHitchingPost().SaveIfMissing($"{PropDir}/PRP_HitchingPost.png");
            PaintNoticeBoard().SaveIfMissing($"{PropDir}/PRP_NoticeBoard.png");
            PaintTelegrams().SaveIfMissing($"{PropDir}/PRP_TelegramStack.png");
            PaintRainStreak().SaveIfMissing($"{FxDir}/FX_RainStreak.png");

            // --- 1-01 Station
            PaintTrain().SaveIfMissing($"{EnvDir}/Station/ENV_Station_Train.png");
            PaintCanopy().SaveIfMissing($"{EnvDir}/Station/ENV_Station_Canopy.png");
            PaintBooth().SaveIfMissing($"{EnvDir}/Station/ENV_Station_TelegraphBooth.png");
            PaintStairs(false).SaveIfMissing($"{EnvDir}/Station/ENV_Station_StairsDown.png");
            PaintSign(3f, 0.8f).SaveIfMissing($"{PropDir}/PRP_Station_NameSign.png");

            // --- 1-02 Main Street facades and doors
            PaintFacade(8, 7, "#4E4034", "#2A221C", 3, true, 41).SaveIfMissing($"{EnvDir}/MainStreet/FAC_Saloon.png");
            PaintFacade(8, 8, "#5B5852", "#33312D", 4, false, 42).SaveIfMissing($"{EnvDir}/MainStreet/FAC_CompanyOffice.png");
            PaintFacade(6, 6, "#4A4A47", "#2B2A28", 2, false, 43).SaveIfMissing($"{EnvDir}/MainStreet/FAC_SheriffOffice.png");
            PaintAlley().SaveIfMissing($"{EnvDir}/MainStreet/FAC_AlleyMouth.png");
            PaintFence(9, 3.5f).SaveIfMissing($"{EnvDir}/MainStreet/FAC_SawmillFence.png");
            PaintChapel().SaveIfMissing($"{EnvDir}/MainStreet/FAC_Chapel.png");
            PaintFence(4, 3f).SaveIfMissing($"{EnvDir}/MainStreet/FAC_MineRoadFence.png");
            PaintHeadframe().SaveIfMissing($"{EnvDir}/MainStreet/ENV_Headframe_Distant.png");
            PaintStairs(true).SaveIfMissing($"{EnvDir}/MainStreet/ENV_Street_StationSteps.png");

            PaintDoor(1.2f, 2.3f, "#3B2E24", 0).SaveIfMissing($"{PropDir}/DOOR_Saloon_Closed.png");
            PaintDoor(1.3f, 2.5f, "#2D2B29", 1).SaveIfMissing($"{PropDir}/DOOR_CompanyOffice_Closed.png");
            PaintDoor(1.1f, 2.3f, "#3A3530", 0).SaveIfMissing($"{PropDir}/DOOR_Sheriff_Closed.png");
            PaintGate(2.2f, 2.2f).SaveIfMissing($"{PropDir}/DOOR_AlleyGate_Closed.png");
            PaintGate(3.2f, 3.0f).SaveIfMissing($"{PropDir}/DOOR_SawmillGate_Closed.png");
            PaintDoor(1.4f, 2.8f, "#33291F", 2).SaveIfMissing($"{PropDir}/DOOR_Chapel_Closed.png");
            PaintGate(3.0f, 2.8f).SaveIfMissing($"{PropDir}/DOOR_MineGate_Closed.png");
        }

        private static PlaceholderPainter Sprite(float wm, float hm, int seed)
        {
            return new PlaceholderPainter(Mathf.RoundToInt(wm * Ppu), Mathf.RoundToInt(hm * Ppu), seed);
        }

        private static PlaceholderPainter PaintSky()
        {
            var p = new PlaceholderPainter(256, 256, 2);
            p.VGradient(PlaceholderPainter.C("#232A33"), PlaceholderPainter.C("#0B0D11"));
            p.Noise(0.03f);
            return p;
        }

        private static PlaceholderPainter PaintMountains()
        {
            var p = Sprite(20, 6, 3);
            float hgt = 3.5f * Ppu;
            for (int x = 0; x < p.W; x++)
            {
                hgt += p.Rand(-6f, 6f);
                hgt = Mathf.Clamp(hgt, 1.5f * Ppu, 5.8f * Ppu);
                for (int y = 0; y < hgt; y++)
                {
                    p.Set(x, y, Color.Lerp(PlaceholderPainter.C("#1B2027"), PlaceholderPainter.C("#2A313A"), y / hgt));
                }
            }

            return p;
        }

        private static PlaceholderPainter PaintPlanks(Color baseColor, int seed)
        {
            var p = new PlaceholderPainter(256, 256, seed);
            p.Fill(baseColor);
            for (int y = 0; y < 256; y += 32)
            {
                p.Rect(0, y, 256, 3, Ink);
                int seam = (int)p.Rand(20, 230);
                p.Rect(seam, y, 3, 32, Ink);
            }

            p.Noise(0.08f);
            return p;
        }

        private static PlaceholderPainter PaintMud()
        {
            var p = new PlaceholderPainter(256, 256, 4);
            p.Fill(PlaceholderPainter.C("#3A332C"));
            for (int i = 0; i < 40; i++)
            {
                p.Ellipse(p.Rand(0, 256), p.Rand(0, 256), p.Rand(6, 28), p.Rand(3, 10), PlaceholderPainter.C("#2B2A2C")); // puddles
            }

            p.Noise(0.1f);
            return p;
        }

        private static PlaceholderPainter PaintRails()
        {
            var p = new PlaceholderPainter(256, 256, 5);
            p.Fill(PlaceholderPainter.C("#2C2926"));
            for (int x = 0; x < 256; x += 40)
            {
                p.Rect(x, 0, 18, 256, PlaceholderPainter.C("#3B3127"));
            }

            p.Rect(0, 70, 256, 8, PlaceholderPainter.C("#6A6D74"));
            p.Rect(0, 180, 256, 8, PlaceholderPainter.C("#6A6D74"));
            p.Noise(0.08f);
            return p;
        }

        private static PlaceholderPainter PaintLamppost()
        {
            var p = Sprite(0.7f, 4.3f, 6);
            Color iron = PlaceholderPainter.C("#24262A");
            p.Rect(25, 0, 20, 14, iron);
            p.Rect(31, 0, 8, 380, iron);
            p.Rect(18, 380, 34, 8, iron);
            p.Trap(35, 388, 30, 18, 30, PlaceholderPainter.C("#F2C26B")); // glass
            p.Rect(20, 418, 30, 8, iron);
            p.InkOutline(Ink, 2);
            return p;
        }

        private static PlaceholderPainter PaintCrates()
        {
            var p = Sprite(2.2f, 1.7f, 7);
            Color wood = PlaceholderPainter.C("#3E3328");
            p.Rect(0, 0, 110, 90, wood);
            p.Rect(110, 0, 100, 80, wood);
            p.Rect(40, 90, 100, 76, wood);
            p.Frame(0, 0, 110, 90, 4, Ink);
            p.Frame(110, 0, 100, 80, 4, Ink);
            p.Frame(40, 90, 100, 76, 4, Ink);
            p.Noise(0.06f);
            return p;
        }

        private static PlaceholderPainter PaintBarrels()
        {
            var p = Sprite(1.7f, 1.3f, 8);
            Color wood = PlaceholderPainter.C("#4A3A2C");
            p.Rect(5, 0, 75, 110, wood);
            p.Rect(88, 0, 75, 100, wood);
            for (int y = 18; y < 110; y += 40)
            {
                p.Rect(5, y, 75, 5, Ink);
                p.Rect(88, y, 75, 5, Ink);
            }

            p.InkOutline(Ink, 2);
            p.Noise(0.06f);
            return p;
        }

        private static PlaceholderPainter PaintTelegraphPole()
        {
            var p = Sprite(1.4f, 8f, 9);
            Color wood = PlaceholderPainter.C("#1C1B1A");
            p.Rect(62, 0, 16, 800, wood);
            p.Rect(10, 740, 120, 10, wood);
            p.Rect(25, 700, 90, 8, wood);
            return p;
        }

        private static PlaceholderPainter PaintHitchingPost()
        {
            var p = Sprite(2.2f, 1.1f, 10);
            Color wood = PlaceholderPainter.C("#3E3328");
            p.Rect(10, 0, 12, 100, wood);
            p.Rect(198, 0, 12, 100, wood);
            p.Rect(0, 86, 220, 12, wood);
            p.InkOutline(Ink, 2);
            return p;
        }

        private static PlaceholderPainter PaintNoticeBoard()
        {
            var p = Sprite(1.4f, 2.0f, 11);
            Color wood = PlaceholderPainter.C("#3B3026");
            p.Rect(20, 0, 10, 120, wood);
            p.Rect(110, 0, 10, 120, wood);
            p.Rect(0, 100, 140, 95, wood);
            for (int i = 0; i < 5; i++)
            {
                p.Rect(10 + i * 25, 115 + (i % 2) * 12, 20, 28, PlaceholderPainter.C("#BDB29C"));
            }

            p.InkOutline(Ink, 2);
            return p;
        }

        private static PlaceholderPainter PaintTelegrams()
        {
            var p = Sprite(0.5f, 0.25f, 12);
            for (int i = 0; i < 4; i++)
            {
                p.Rect(4 + i * 3, i * 4, 40, 8, i % 2 == 0 ? PlaceholderPainter.C("#D6CDB9") : PlaceholderPainter.C("#C2B79F"));
            }

            p.InkOutline(Ink, 1);
            return p;
        }

        private static PlaceholderPainter PaintRainStreak()
        {
            var p = new PlaceholderPainter(8, 64, 13);
            for (int y = 0; y < 64; y++)
            {
                float a = Mathf.Sin(y / 63f * Mathf.PI) * 0.8f;
                for (int x = 3; x < 5; x++)
                {
                    p.Set(x, y, new Color(0.75f, 0.8f, 0.88f, a));
                }
            }

            return p;
        }

        private static PlaceholderPainter PaintTrain()
        {
            var p = Sprite(13, 4.2f, 14);
            Color body = PlaceholderPainter.C("#1E2126");
            p.Rect(0, 60, 1300, 300, body);
            p.Rect(20, 360, 1260, 30, PlaceholderPainter.C("#16181B"));
            for (int x = 60; x < 1260; x += 110)
            {
                p.Rect(x, 220, 70, 80, PlaceholderPainter.C("#D9A752")); // lit windows
                p.Frame(x, 220, 70, 80, 4, Ink);
            }

            for (int x = 80; x < 1260; x += 300)
            {
                p.Ellipse(x, 50, 45, 45, PlaceholderPainter.C("#2A2C30"));
                p.Ellipse(x + 120, 50, 45, 45, PlaceholderPainter.C("#2A2C30"));
            }

            p.InkOutline(Ink, 3);
            return p;
        }

        private static PlaceholderPainter PaintCanopy()
        {
            var p = Sprite(14.4f, 4.6f, 15);
            Color iron = PlaceholderPainter.C("#22252A");
            for (int x = 20; x < 1440; x += 230)
            {
                p.Rect(x, 0, 14, 400, iron); // cast-iron columns
                p.Rect(x - 10, 0, 34, 16, iron);
            }

            p.Trap(720, 395, 1440, 1400, 50, PlaceholderPainter.C("#2E2B28")); // roof
            p.Rect(0, 385, 1440, 14, PlaceholderPainter.C("#3A3631"));
            p.InkOutline(Ink, 2);
            return p;
        }

        private static PlaceholderPainter PaintBooth()
        {
            var p = Sprite(4, 3.3f, 16);
            Color wood = PlaceholderPainter.C("#3B3530");
            p.Rect(10, 0, 380, 290, wood);
            p.Trap(200, 290, 400, 360, 40, PlaceholderPainter.C("#2A2622"));
            p.Rect(130, 110, 150, 110, PlaceholderPainter.C("#E3B25C")); // yellow window
            p.Frame(130, 110, 150, 110, 6, Ink);
            p.Rect(300, 0, 70, 210, PlaceholderPainter.C("#2C2723")); // door
            p.InkOutline(Ink, 3);
            p.Noise(0.05f);
            return p;
        }

        private static PlaceholderPainter PaintStairs(bool up)
        {
            var p = Sprite(4.5f, 2.6f, 17);
            Color stone = PlaceholderPainter.C("#3D3A36");
            for (int i = 0; i < 6; i++)
            {
                float w = up ? 450 - i * 60 : 450 - i * 60;
                float x = up ? 0 : i * 60;
                p.Rect(x, i * 30, w, 30, stone);
                p.Rect(x, i * 30 + 26, w, 4, Ink);
            }

            p.Rect(up ? 30 : 400, 0, 10, 250, PlaceholderPainter.C("#24262A")); // rail post
            p.InkOutline(Ink, 2);
            return p;
        }

        private static PlaceholderPainter PaintSign(float wm, float hm)
        {
            var p = Sprite(wm, hm + 1.4f, 18);
            Color wood = PlaceholderPainter.C("#2E2924");
            p.Rect(20, 0, 10, 140, wood);
            p.Rect(p.W - 30, 0, 10, 140, wood);
            p.Rect(0, 140, p.W, hm * Ppu, PlaceholderPainter.C("#C9BFA9"));
            p.Frame(0, 140, p.W, hm * Ppu, 6, Ink);
            return p;
        }

        private static PlaceholderPainter PaintFacade(float wm, float hm, string bodyHex, string trimHex, int windows, bool balcony, int seed)
        {
            var p = Sprite(wm, hm, seed);
            Color body = PlaceholderPainter.C(bodyHex), trim = PlaceholderPainter.C(trimHex);
            p.Rect(0, 0, p.W, p.H - 60, body);
            p.Rect(p.W * 0.15f, p.H - 60, p.W * 0.7f, 60, body); // false front
            p.Rect(0, p.H - 70, p.W, 12, trim);
            p.Rect(0, p.H * 0.48f, p.W, 10, trim);
            float step = p.W / (windows + 1f);
            for (int i = 1; i <= windows; i++)
            {
                bool lit = i % 2 == 1;
                p.Rect(step * i - 35, p.H * 0.55f, 70, 110, lit ? PlaceholderPainter.C("#C99A4E") : PlaceholderPainter.C("#16181B"));
                p.Frame(step * i - 35, p.H * 0.55f, 70, 110, 5, Ink);
            }

            p.Rect(p.W / 2f - 75, 0, 150, 250, PlaceholderPainter.C("#141517")); // door recess, the door sprite sits on top
            p.Rect(p.W * 0.2f, p.H * 0.48f - 70, p.W * 0.6f, 50, trim);       // sign band
            if (balcony)
            {
                p.Rect(0, p.H * 0.46f - 10, p.W, 8, Ink);
                for (int x = 10; x < p.W; x += 40) p.Rect(x, p.H * 0.46f, 5, 60, trim);
            }

            p.InkOutline(Ink, 3);
            p.Noise(0.06f);
            return p;
        }

        private static PlaceholderPainter PaintAlley()
        {
            var p = Sprite(6, 7, 44);
            Color wall = PlaceholderPainter.C("#363431");
            p.Rect(0, 0, 160, p.H, wall);
            p.Rect(p.W - 160, 0, 160, p.H - 40, wall);
            p.Rect(160, 0, p.W - 320, p.H - 80, PlaceholderPainter.C("#0D0E10")); // dark passage
            p.Rect(170, 400, p.W - 340, 4, PlaceholderPainter.C("#5A5550"));      // washing line
            p.InkOutline(Ink, 3);
            p.Noise(0.05f);
            return p;
        }

        private static PlaceholderPainter PaintFence(float wm, float hm)
        {
            var p = Sprite(wm, hm, 45);
            Color wood = PlaceholderPainter.C("#3A3128");
            for (int x = 0; x < p.W; x += 22)
            {
                p.Rect(x, 0, 18, p.H - (x / 22 % 3) * 10, wood);
            }

            p.Rect(0, p.H * 0.3f, p.W, 10, Ink);
            p.Rect(0, p.H * 0.75f, p.W, 10, Ink);
            p.Noise(0.06f);
            return p;
        }

        private static PlaceholderPainter PaintChapel()
        {
            var p = Sprite(7, 9.5f, 46);
            Color wood = PlaceholderPainter.C("#47433D");
            p.Rect(0, 0, p.W, 560, wood);
            p.Trap(p.W / 2f, 560, p.W, 40, 240, PlaceholderPainter.C("#2E2B27"));
            p.Rect(p.W / 2f - 50, 600, 100, 260, wood);                       // belfry
            p.Rect(p.W / 2f - 30, 690, 60, 70, PlaceholderPainter.C("#141517"));
            p.Rect(p.W / 2f - 6, 860, 12, 90, Ink);                           // cross
            p.Rect(p.W / 2f - 30, 900, 60, 12, Ink);
            p.Ellipse(p.W / 2f, 420, 70, 70, PlaceholderPainter.C("#5A3E52"));  // dark stained glass
            p.Rect(p.W / 2f - 85, 0, 170, 290, PlaceholderPainter.C("#141517"));
            p.InkOutline(Ink, 3);
            p.Noise(0.05f);
            return p;
        }

        private static PlaceholderPainter PaintHeadframe()
        {
            var p = Sprite(6, 13, 47);
            Color steel = PlaceholderPainter.C("#1D2128");
            p.Trap(300, 0, 500, 160, 1150, steel);
            p.Trap(300, 30, 380, 100, 1050, PlaceholderPainter.C("#262B33"));
            for (int y = 80; y < 1100; y += 120) p.Rect(120 + y / 12, y, 360 - y / 6, 8, steel);
            p.Ellipse(300, 1180, 110, 110, steel); // sheave wheel
            p.Ellipse(300, 1180, 80, 80, Color.clear);
            return p;
        }

        private static PlaceholderPainter PaintDoor(float wm, float hm, string hex, int style)
        {
            var p = Sprite(wm, hm, 48 + style);
            Color wood = PlaceholderPainter.C(hex);
            p.Rect(0, 0, p.W, style == 2 ? p.H - 40 : p.H, wood);
            if (style == 2) p.Ellipse(p.W / 2f, p.H - 40, p.W / 2f, 40, wood); // arched chapel door
            for (int x = 14; x < p.W; x += 28) p.Rect(x, 0, 3, p.H - 20, Ink);
            p.Rect(p.W - 26, p.H * 0.45f, 10, 10, PlaceholderPainter.C("#8A8F95"));         // handle
            if (style == 1) p.Rect(p.W * 0.2f, p.H * 0.6f, p.W * 0.6f, p.H * 0.25f, PlaceholderPainter.C("#1D2024")); // glass
            p.Rect(p.W * 0.2f, p.H * 0.4f, 34, 22, PlaceholderPainter.C("#5A5550"));      // padlock / bar
            p.InkOutline(Ink, 3);
            p.Noise(0.05f);
            return p;
        }

        private static PlaceholderPainter PaintGate(float wm, float hm)
        {
            var p = Sprite(wm, hm, 52);
            Color wood = PlaceholderPainter.C("#3A3128");
            for (int x = 0; x < p.W; x += 20) p.Rect(x, 0, 16, p.H, wood);
            p.Rect(0, p.H * 0.2f, p.W, 12, Ink);
            p.Rect(0, p.H * 0.8f, p.W, 12, Ink);
            for (int i = 0; i < p.W; i++) p.Rect(i, p.H * 0.2f + i * (p.H * 0.6f / p.W), 8, 8, Ink); // brace
            p.Rect(p.W / 2f - 12, p.H * 0.45f, 24, 30, PlaceholderPainter.C("#6A6D74"));          // chain + lock
            return p;
        }

        // ------------------------------------------------------------------ imports

        private static void ConfigureImports()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null)
                {
                    continue;
                }

                string file = Path.GetFileNameWithoutExtension(path);
                bool isTiledTexture = file.StartsWith("TEX_") || file.StartsWith("ENV_Sky");
                bool changed = false;

                if (isTiledTexture)
                {
                    if (ti.textureType != TextureImporterType.Default || ti.wrapMode != TextureWrapMode.Repeat)
                    {
                        ti.textureType = TextureImporterType.Default;
                        ti.wrapMode = TextureWrapMode.Repeat;
                        changed = true;
                    }
                }
                else
                {
                    var settings = new TextureImporterSettings();
                    ti.ReadTextureSettings(settings);
                    if (ti.textureType != TextureImporterType.Sprite || settings.spriteAlignment != (int)SpriteAlignment.BottomCenter || ti.spritePixelsPerUnit != Ppu)
                    {
                        ti.textureType = TextureImporterType.Sprite;
                        ti.spriteImportMode = SpriteImportMode.Single;
                        ti.spritePixelsPerUnit = Ppu;
                        ti.ReadTextureSettings(settings);
                        settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                        settings.spriteMeshType = SpriteMeshType.FullRect;
                        ti.SetTextureSettings(settings);
                        ti.alphaIsTransparency = true;
                        ti.mipmapEnabled = false;
                        changed = true;
                    }
                }

                if (changed)
                {
                    ti.SaveAndReimport();
                }
            }
        }

        private static Sprite S(string path)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s == null)
            {
                Debug.LogWarning("[Chapter1Builder] Missing sprite " + path);
            }

            return s;
        }

        // ------------------------------------------------------------------ animation

        private static AnimationClip SpriteClip(string name, string prefix, int frames, float fps, bool loop)
        {
            string path = $"{AnimDir}/{name}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }

            clip.frameRate = fps;
            var keys = new ObjectReferenceKeyframe[frames + 1];
            for (int i = 0; i <= frames; i++)
            {
                int f = i == frames ? frames - 1 : i;
                keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = S($"{CharDir}/{prefix}_{f:00}.png") };
            }

            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static Dictionary<string, RuntimeAnimatorController> BuildAnimation()
        {
            var result = new Dictionary<string, RuntimeAnimatorController>();

            // Holt: Speed drives idle/walk (PlayerController already writes it); Interact and
            // ReadLetter are triggers kept ready for the interaction code.
            string holtPath = $"{AnimDir}/AC_Holt.controller";
            AssetDatabase.DeleteAsset(holtPath);
            var holt = AnimatorController.CreateAnimatorControllerAtPath(holtPath);
            holt.AddParameter("Speed", AnimatorControllerParameterType.Float);
            holt.AddParameter("Interact", AnimatorControllerParameterType.Trigger);
            holt.AddParameter("ReadLetter", AnimatorControllerParameterType.Trigger);
            AnimatorStateMachine sm = holt.layers[0].stateMachine;
            AnimatorState idle = sm.AddState("Idle", new Vector3(250, 0));
            idle.motion = SpriteClip("Holt_Idle", "CH_Holt_Idle", 4, 4, true);
            AnimatorState walk = sm.AddState("Walk", new Vector3(250, 100));
            walk.motion = SpriteClip("Holt_Walk", "CH_Holt_Walk", 8, 10, true);
            AnimatorState interact = sm.AddState("Interact", new Vector3(500, 0));
            interact.motion = SpriteClip("Holt_Interact", "CH_Holt_Interact", 4, 10, false);
            AnimatorState read = sm.AddState("ReadLetter", new Vector3(500, 100));
            read.motion = SpriteClip("Holt_ReadLetter", "CH_Holt_ReadLetter", 6, 3, false);
            sm.defaultState = idle;

            AnimatorStateTransition t = idle.AddTransition(walk);
            t.hasExitTime = false; t.duration = 0f; t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            t = walk.AddTransition(idle);
            t.hasExitTime = false; t.duration = 0f; t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            foreach (var pair in new[] { (interact, "Interact"), (read, "ReadLetter") })
            {
                t = sm.AddAnyStateTransition(pair.Item1);
                t.hasExitTime = false; t.duration = 0f; t.canTransitionToSelf = false; t.AddCondition(AnimatorConditionMode.If, 0f, pair.Item2);
                t = pair.Item1.AddTransition(idle);
                t.hasExitTime = true; t.exitTime = 1f; t.duration = 0f;
            }

            result["Holt"] = holt;

            foreach (string npc in new[] { "Conductor", "Callahan", "PasserbyMiner", "PasserbyWoman" })
            {
                string path = $"{AnimDir}/AC_{npc}.controller";
                AssetDatabase.DeleteAsset(path);
                var ac = AnimatorController.CreateAnimatorControllerAtPath(path);
                AnimatorState s = ac.layers[0].stateMachine.AddState("Idle");
                s.motion = SpriteClip($"{npc}_Idle", $"CH_{npc}_Idle", 2, 1.5f, true);
                result[npc] = ac;
            }

            return result;
        }

        // ------------------------------------------------------------------ materials / post

        // 0 sky (unlit), 1 planks (lit), 2 mud (lit), 3 trackbed (lit), 4 rain (particles)
        private static Material[] BuildMaterials()
        {
            return new[]
            {
                Mat("M_Sky_NightRain", "Universal Render Pipeline/Unlit", $"{EnvDir}/Shared/ENV_Sky_NightRain.png", Vector2.one),
                Mat("M_Planks_Wet", "Universal Render Pipeline/Lit", $"{EnvDir}/Shared/TEX_Planks_Wet.png", new Vector2(8, 4), 0.55f),
                Mat("M_Street_Mud", "Universal Render Pipeline/Lit", $"{EnvDir}/MainStreet/TEX_Street_Mud.png", new Vector2(16, 3), 0.7f),
                Mat("M_Station_Trackbed", "Universal Render Pipeline/Lit", $"{EnvDir}/Station/TEX_Station_Trackbed.png", new Vector2(10, 1), 0.3f),
                RainMat()
            };
        }

        private static Material Mat(string name, string shader, string texPath, Vector2 tiling, float smoothness = 0f)
        {
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find(shader));
                AssetDatabase.CreateAsset(m, path);
            }

            m.shader = Shader.Find(shader);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            m.SetTexture("_BaseMap", tex);
            m.SetTextureScale("_BaseMap", tiling);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness); // wet surfaces catch lamp highlights
            m.SetColor("_BaseColor", shader.EndsWith("/Lit") ? new Color(0.32f, 0.32f, 0.36f) : Color.white);
            if (m.HasProperty("_EnvironmentReflections")) m.SetFloat("_EnvironmentReflections", 0f); // night: keep the floor below the characters' value
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material RainMat()
        {
            string path = $"{MatDir}/M_FX_Rain.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(m, path);
            }

            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{FxDir}/FX_RainStreak.png"));
            m.SetColor("_BaseColor", new Color(0.7f, 0.76f, 0.85f, 0.35f));
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            return m;
        }

        private static VolumeProfile BuildVolumeProfile()
        {
            string path = $"{ArtRoot}/PP_Chapter1_NightRain.asset";
            AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.38f);
            vignette.smoothness.Override(0.45f);
            var grain = profile.Add<FilmGrain>(true);
            grain.type.Override(FilmGrainLookup.Medium3);
            grain.intensity.Override(0.35f);
            var color = profile.Add<ColorAdjustments>(true);
            color.saturation.Override(-35f);
            color.contrast.Override(18f);
            color.colorFilter.Override(new Color(0.86f, 0.9f, 1f));
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(0.5f);
            var ca = profile.Add<ChromaticAberration>(true);
            ca.intensity.Override(0.08f);

            foreach (VolumeComponent c in profile.components)
            {
                AssetDatabase.AddObjectToAsset(c, profile);
            }

            EditorUtility.SetDirty(profile);
            return profile;
        }

        // ------------------------------------------------------------------ prefabs

        private class Prefabs
        {
            public GameObject Player, GameMaster, Fader, Camera, Npc, Lamppost, TransitionStub, SceneDoor, Rain, Prop, InspectPoint;
        }

        private static GameObject SavePrefab(GameObject go, string name)
        {
            string path = $"{PrefabDir}/{name}.prefab";
            GameObject asset = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return asset;
        }

        private static void SetField(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogWarning($"[Chapter1Builder] {target.GetType().Name}.{field} not found");
                return;
            }

            switch (value)
            {
                case string s: p.stringValue = s; break;
                case float f: p.floatValue = f; break;
                case int i: p.intValue = i; break;
                case bool b: p.boolValue = b; break;
                case Vector3 v: p.vector3Value = v; break;
                case Vector2 v2: p.vector2Value = v2; break;
                case Color c: p.colorValue = c; break;
                case Object o: p.objectReferenceValue = o; break;
                case LayerMask m: p.intValue = m.value; break;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Prefabs BuildPrefabs(Dictionary<string, RuntimeAnimatorController> controllers)
        {
            var prefabs = new Prefabs();

            // Player — same component set as the MVP player, but feet at y = 0.
            var player = new GameObject("P_Player");
            var col = player.AddComponent<BoxCollider>();
            col.size = new Vector3(0.6f, 1.8f, 0.6f);
            col.center = new Vector3(0f, 0.9f, 0f);
            var rb = player.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            var spriteGo = new GameObject("Sprite");
            spriteGo.transform.SetParent(player.transform, false);
            var sr = spriteGo.AddComponent<SpriteRenderer>();
            sr.sprite = S($"{CharDir}/CH_Holt_Ref.png");
            var anim = spriteGo.AddComponent<Animator>();
            anim.runtimeAnimatorController = controllers["Holt"];
            var ground = new GameObject("GroundCheck");
            ground.transform.SetParent(player.transform, false);
            ground.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            var fire = new GameObject("FirePoint");
            fire.transform.SetParent(player.transform, false);
            fire.transform.localPosition = new Vector3(0.45f, 1.25f, 0f);
            var pc = player.AddComponent<PlayerController>();
            SetField(pc, "groundCheck", ground.transform);
            SetField(pc, "groundLayer", (LayerMask)1);
            SetField(pc, "spriteRenderer", sr);
            SetField(pc, "animator", anim);
            player.AddComponent<InventoryManager>();
            var wc = player.AddComponent<WeaponController>();
            SetField(wc, "firePoint", fire.transform);
            SetField(wc, "weaponItem", AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/ScriptableObjects/Weapon_Revolver.asset"));
            SetField(wc, "ammoItem", AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/ScriptableObjects/Ammo_Revolver.asset"));
            player.AddComponent<WeaponFireEffects>();
            var det = player.AddComponent<InteractionDetector>();
            SetField(det, "promptOffset", new Vector3(0f, 2.4f, 0f));
            prefabs.Player = SavePrefab(player, "P_Player");

            // Persistent systems
            var gm = new GameObject("P_GameMaster");
            gm.AddComponent<GameMaster>();
            prefabs.GameMaster = SavePrefab(gm, "P_GameMaster");

            var fader = new GameObject("P_ScreenFader");
            var canvas = fader.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var group = fader.AddComponent<CanvasGroup>();
            var faderComp = fader.AddComponent<ScreenFader>();
            SetField(faderComp, "canvasGroup", group);
            var black = new GameObject("Black");
            black.transform.SetParent(fader.transform, false);
            var img = black.AddComponent<Image>();
            img.color = Color.black;
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            group.alpha = 0f;
            prefabs.Fader = SavePrefab(fader, "P_ScreenFader");

            // Camera rig: perspective side view, tilted down 6°, follows X only (CameraFollow).
            var camGo = new GameObject("P_MainCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<UnityEngine.Camera>();
            cam.fieldOfView = 35f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 200f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = PlaceholderPainter.C("#0B0D11");
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            camGo.AddComponent<CameraFollow>();
            camGo.transform.rotation = Quaternion.Euler(6f, 0f, 0f);
            prefabs.Camera = SavePrefab(camGo, "P_MainCamera");

            // NPC: trigger collider so the player walks past; InteractableNPC shows a one-line bark.
            var npc = new GameObject("P_NPC");
            var ncol = npc.AddComponent<BoxCollider>();
            ncol.isTrigger = true;
            ncol.size = new Vector3(0.8f, 1.8f, 0.8f);
            ncol.center = new Vector3(0f, 0.9f, 0f);
            var nSprite = new GameObject("Sprite");
            nSprite.transform.SetParent(npc.transform, false);
            nSprite.AddComponent<SpriteRenderer>();
            nSprite.AddComponent<Animator>();
            var inpc = npc.AddComponent<InteractableNPC>();
            SetField(inpc, "textOffset", new Vector3(0f, 2.3f, 0f));
            SetField(inpc, "textCanvasSize", new Vector2(4.5f, 1.2f));
            SetField(inpc, "dialogueDisplayDuration", 4f);
            prefabs.Npc = SavePrefab(npc, "P_NPC");

            // Inspect point: an invisible interactable (story beat or clue) using the same bark UI.
            var inspect = new GameObject("P_InspectPoint");
            var icol = inspect.AddComponent<BoxCollider>();
            icol.isTrigger = true;
            icol.size = new Vector3(1.2f, 2f, 1.2f);
            icol.center = new Vector3(0f, 1f, 0f);
            var ipc = inspect.AddComponent<InteractableNPC>();
            SetField(ipc, "interactionPrompt", "Осмотреть");
            SetField(ipc, "textOffset", new Vector3(0f, 2.3f, 0f));
            SetField(ipc, "textCanvasSize", new Vector2(5.5f, 1.4f));
            SetField(ipc, "dialogueDisplayDuration", 5f);
            prefabs.InspectPoint = SavePrefab(inspect, "P_InspectPoint");

            // Lamppost with a warm point light at the head.
            var lamp = new GameObject("P_Lamppost");
            var lsr = new GameObject("Sprite");
            lsr.transform.SetParent(lamp.transform, false);
            lsr.AddComponent<SpriteRenderer>().sprite = S($"{PropDir}/PRP_Lamppost.png");
            var lightGo = new GameObject("Light");
            lightGo.transform.SetParent(lamp.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 3.9f, -0.4f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = PlaceholderPainter.C("#F2B45A");
            light.intensity = 4f;
            light.range = 8f;
            light.shadows = LightShadows.Soft;
            prefabs.Lamppost = SavePrefab(lamp, "P_Lamppost");

            // Closed transition: the door art sits on the facade (z = +2.9 relative to the
            // interaction trigger at z = 0), a solid blocker stands in the doorway.
            var stub = new GameObject("P_TransitionStub");
            var stubTrigger = stub.AddComponent<BoxCollider>();
            stubTrigger.isTrigger = true;
            stubTrigger.size = new Vector3(1.4f, 2.4f, 1.4f);
            stubTrigger.center = new Vector3(0f, 1.2f, 0f);
            stub.AddComponent<TransitionStub>();
            var stubArt = new GameObject("DoorArt");
            stubArt.transform.SetParent(stub.transform, false);
            stubArt.transform.localPosition = new Vector3(0f, 0f, 2.9f);
            stubArt.AddComponent<SpriteRenderer>();
            var blocker = new GameObject("Blocker");
            blocker.transform.SetParent(stub.transform, false);
            blocker.transform.localPosition = new Vector3(0f, 1.2f, 2.6f);
            blocker.AddComponent<BoxCollider>().size = new Vector3(1.4f, 2.4f, 0.3f);
            prefabs.TransitionStub = SavePrefab(stub, "P_TransitionStub");

            // Working transition to another built scene.
            var sdoor = new GameObject("P_SceneDoor");
            var sdTrigger = sdoor.AddComponent<BoxCollider>();
            sdTrigger.isTrigger = true;
            sdTrigger.size = new Vector3(1.6f, 2.4f, 1.4f);
            sdTrigger.center = new Vector3(0f, 1.2f, 0f);
            sdoor.AddComponent<AudioSource>().playOnAwake = false;
            sdoor.AddComponent<SceneTransitionDoor>();
            var sdArt = new GameObject("Art");
            sdArt.transform.SetParent(sdoor.transform, false);
            sdArt.transform.localPosition = new Vector3(0f, 0f, 2.5f);
            sdArt.AddComponent<SpriteRenderer>();
            prefabs.SceneDoor = SavePrefab(sdoor, "P_SceneDoor");

            // Generic sprite prop (lampposts aside, every repeated set-dressing piece).
            var prop = new GameObject("P_Prop");
            prop.AddComponent<SpriteRenderer>();
            prefabs.Prop = SavePrefab(prop, "P_Prop");

            // Rain: stretched particles falling over the whole strip.
            var rain = new GameObject("P_FX_Rain");
            rain.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var ps = rain.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 1.1f;
            main.startSpeed = 14f;
            main.startSize = 0.035f;
            main.maxParticles = 4000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true;
            var emission = ps.emission;
            emission.rateOverTime = 1800f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(40f, 14f, 0.1f);
            var psr = rain.GetComponent<ParticleSystemRenderer>();
            psr.renderMode = ParticleSystemRenderMode.Stretch;
            psr.velocityScale = 0.06f;
            psr.lengthScale = 1f;
            psr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/M_FX_Rain.mat") ?? RainMat();
            // Transparent sprites and particles are sorted by distance to their bounds centre, which
            // lets far facades win over the rain. An explicit order keeps rain over the background and
            // characters; foreground silhouettes (ForegroundOrder) stay on top of the rain.
            psr.sortingOrder = RainOrder;
            prefabs.Rain = SavePrefab(rain, "P_FX_Rain");

            return prefabs;
        }

        // ------------------------------------------------------------------ scene helpers

        private static GameObject Inst(GameObject prefab, string name, Vector3 pos, Transform parent)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            return go;
        }

        private static Transform Group(string name)
        {
            return new GameObject(name).transform;
        }

        private static GameObject SpriteObj(string name, string spritePath, Vector3 pos, Transform parent, float scale = 1f, Color? tint = null, int order = 0)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = S(spritePath);
            sr.color = tint ?? Color.white;
            sr.sortingOrder = order;
            return go;
        }

        private static GameObject PropInst(Prefabs p, string name, string spritePath, Vector3 pos, Transform parent, float scale = 1f, Color? tint = null)
        {
            GameObject go = Inst(p.Prop, name, pos, parent);
            go.transform.localScale = Vector3.one * scale;
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = S(spritePath);
            sr.color = tint ?? Color.white;
            if (parent != null && parent.name.StartsWith("--- Foreground"))
            {
                sr.sortingOrder = ForegroundOrder;
            }

            return go;
        }

        private static GameObject Quad(string name, Material m, Vector3 pos, Vector3 euler, Vector3 scale, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<MeshCollider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        private static void Box(string name, Vector3 center, Vector3 size, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.AddComponent<BoxCollider>().size = size;
        }

        private static GameObject Npc(Prefabs p, Dictionary<string, RuntimeAnimatorController> ac, string name, string key, Vector3 pos, Transform parent, string prompt, string line)
        {
            GameObject go = Inst(p.Npc, name, pos, parent);
            Transform sprite = go.transform.Find("Sprite");
            sprite.GetComponent<SpriteRenderer>().sprite = S($"{CharDir}/CH_{key}_Ref.png");
            if (ac.TryGetValue(key, out RuntimeAnimatorController c))
            {
                sprite.GetComponent<Animator>().runtimeAnimatorController = c;
            }

            var npc = go.GetComponent<InteractableNPC>();
            SetField(npc, "interactionPrompt", prompt);
            SetField(npc, "dialogueLine", line);
            return go;
        }

        private static GameObject Inspect(Prefabs p, string name, Vector3 pos, Transform parent, string prompt, string line)
        {
            GameObject go = Inst(p.InspectPoint, name, pos, parent);
            var npc = go.GetComponent<InteractableNPC>();
            SetField(npc, "interactionPrompt", prompt);
            SetField(npc, "dialogueLine", line);
            return go;
        }

        private static GameObject Stub(Prefabs p, string id, string target, string display, string line, string doorSprite, float x, Transform parent)
        {
            GameObject go = Inst(p.TransitionStub, $"Transition_{id}", new Vector3(x, 0f, 0f), parent);
            var stub = go.GetComponent<TransitionStub>();
            SetField(stub, "transitionId", id);
            SetField(stub, "targetLocationId", target);
            SetField(stub, "displayName", display);
            SetField(stub, "closedPrompt", "закрыто");
            SetField(stub, "closedLine", line);
            go.transform.Find("DoorArt").GetComponent<SpriteRenderer>().sprite = S(doorSprite);
            return go;
        }

        private static GameObject SceneDoor(Prefabs p, string id, string scene, string spawn, string prompt, string artSprite, float x, Transform parent)
        {
            GameObject go = Inst(p.SceneDoor, $"Transition_{id}", new Vector3(x, 0f, 0f), parent);
            var door = go.GetComponent<SceneTransitionDoor>();
            SetField(door, "transitionId", id);
            SetField(door, "targetScene", scene);
            SetField(door, "targetSpawnId", spawn);
            SetField(door, "prompt", prompt);
            go.transform.Find("Art").GetComponent<SpriteRenderer>().sprite = S(artSprite);
            return go;
        }

        private static void Spawn(string id, float x, Transform parent)
        {
            var go = new GameObject($"Spawn_{id}");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, 0f, 0f);
            SetField(go.AddComponent<SpawnPoint>(), "spawnId", id);
        }

        private static GameObject CommonSystems(Prefabs p, float playerX, float minCamX, float maxCamX, VolumeProfile profile)
        {
            // GameMaster and ScreenFader stay at the scene root: they call DontDestroyOnLoad.
            Inst(p.GameMaster, "GameMaster", Vector3.zero, null);
            Inst(p.Fader, "ScreenFader", Vector3.zero, null);
            Transform sys = Group("--- Systems");

            GameObject player = Inst(p.Player, "Player", new Vector3(playerX, 0.02f, 0f), null);
            GameObject cam = Inst(p.Camera, "Main Camera", new Vector3(playerX, 2.4f, -11f), null);
            var follow = cam.GetComponent<CameraFollow>();
            SetField(follow, "target", player.transform);
            SetField(follow, "minX", minCamX);
            SetField(follow, "maxX", maxCamX);
            cam.transform.position = new Vector3(Mathf.Clamp(playerX, minCamX, maxCamX), 2.4f, -11f);

            var volGo = new GameObject("PostProcess_Volume");
            volGo.transform.SetParent(sys, false);
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = profile;

            var moon = new GameObject("Moonlight");
            moon.transform.SetParent(sys, false);
            moon.transform.rotation = Quaternion.Euler(38f, -30f, 0f);
            var ml = moon.AddComponent<Light>();
            ml.type = LightType.Directional;
            ml.color = PlaceholderPainter.C("#7D8FB3");
            ml.intensity = 0.25f;
            ml.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = PlaceholderPainter.C("#1E242D");
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = PlaceholderPainter.C("#141820");
            RenderSettings.fogDensity = 0.018f;
            RenderSettings.skybox = null;
            return player;
        }

        private static void Backdrop(Material sky, float centerX, Transform parent)
        {
            Quad("Sky", sky, new Vector3(centerX, 12f, 70f), Vector3.zero, new Vector3(220f, 60f, 1f), parent);
            for (int i = 0; i < 4; i++)
            {
                SpriteObj($"Mountains_{i}", $"{EnvDir}/Shared/ENV_Mountains_Fog.png",
                    new Vector3(centerX - 75f + i * 50f, -6f, 48f), parent, 2.6f, new Color(0.75f, 0.78f, 0.85f));
            }
        }

        private static void AddToBuildSettings(params string[] paths)
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (string path in paths)
            {
                if (!list.Exists(s => s.path == path))
                {
                    list.Add(new EditorBuildSettingsScene(path, true));
                }
            }

            EditorBuildSettings.scenes = list.ToArray();
        }

        // ------------------------------------------------------------------ 1-01 Station

        private static void BuildStation(Prefabs p, Dictionary<string, RuntimeAnimatorController> ac, Material[] mats, VolumeProfile profile)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Player starts at the prologue point (x = 2); camera clamps keep the 30 m strip in view.
            CommonSystems(p, 2f, 6f, 24f, profile);

            Transform bg = Group("--- Background");
            Backdrop(mats[0], 15f, bg);
            SpriteObj("Train_Departing", $"{EnvDir}/Station/ENV_Station_Train.png", new Vector3(-1.5f, -1.2f, 6.5f), bg, 1f, new Color(0.8f, 0.82f, 0.88f));
            Quad("Trackbed", mats[3], new Vector3(15f, -1.2f, 6f), new Vector3(90f, 0f, 0f), new Vector3(50f, 4f, 1f), bg);

            Transform geo = Group("--- Geometry");
            Quad("Platform_Floor", mats[1], new Vector3(15f, 0f, 0.5f), new Vector3(90f, 0f, 0f), new Vector3(34f, 7f, 1f), geo);
            Quad("Platform_Edge", mats[1], new Vector3(15f, -0.6f, 4f), Vector3.zero, new Vector3(34f, 1.2f, 1f), geo);

            Transform set = Group("--- Set Dressing");
            SpriteObj("Canopy", $"{EnvDir}/Station/ENV_Station_Canopy.png", new Vector3(15f, 0f, 2.2f), set, 1f, new Color(0.85f, 0.85f, 0.9f));     // навес 8–22 м
            SpriteObj("TelegraphBooth", $"{EnvDir}/Station/ENV_Station_TelegraphBooth.png", new Vector3(16f, 0f, 3.2f), set);
            SpriteObj("StationNameSign", $"{PropDir}/PRP_Station_NameSign.png", new Vector3(5f, 0f, 3.4f), set, 1f, new Color(0.9f, 0.9f, 0.9f));
            Inst(p.Lamppost, "Lamp_x13", new Vector3(13f, 0f, 1.2f), set);                                                                  // карта: свет, x = 13

            var window = new GameObject("TelegraphWindow_Light");
            window.transform.SetParent(set, false);
            window.transform.localPosition = new Vector3(16f, 1.7f, 2.4f);
            var wl = window.AddComponent<Light>();
            wl.type = LightType.Point;
            wl.color = PlaceholderPainter.C("#F0C060");
            wl.intensity = 2.5f;
            wl.range = 5f;

            Transform fg = Group("--- Foreground (silhouettes)");
            Color sil = new Color(0.12f, 0.12f, 0.14f);
            PropInst(p, "FG_Lamppost_Silhouette", $"{PropDir}/PRP_Lamppost.png", new Vector3(4.5f, -1.0f, -3.2f), fg, 1.2f, sil);
            PropInst(p, "FG_CrateStack", $"{PropDir}/PRP_CrateStack.png", new Vector3(26.5f, -1.0f, -3.2f), fg, 1.2f, sil);

            Transform play = Group("--- Gameplay");
            Inspect(p, "Event_Prologue_Letter_x3", new Vector3(3f, 0f, 0f), play, "Перечитать письмо",
                "«Томас жив. Он пишет мне по ночам. В последнем письме он назвал ваше имя». — А. Бирн");
            Npc(p, ac, "NPC_Conductor_x6", "Conductor", new Vector3(6f, 0f, 0.6f), play, "Поговорить", "Дальше не поеду.");
            Inspect(p, "Clue_UnsentTelegrams_x16", new Vector3(16f, 0f, 0f), play, "Осмотреть",
                "Стопка неотправленных телеграмм родных шахтёров. Компания их держит.");
            SpriteObj("UnsentTelegrams_Art", $"{PropDir}/PRP_TelegramStack.png", new Vector3(16f, 1.25f, 3.05f), play);
            Npc(p, ac, "NPC_Callahan_x24", "Callahan", new Vector3(24f, 0f, 0.6f), play, "Поговорить",
                "Человек компании? Я провожу до салуна.");
            SceneDoor(p, "1-01_to_1-02", "MainStreet", "from_1-01", "Спуститься на Главную улицу",
                $"{EnvDir}/Station/ENV_Station_StairsDown.png", 29f, play);
            Spawn("from_1-02", 27.5f, play);

            Transform bounds = Group("--- Collision & Bounds");
            Box("Ground", new Vector3(15f, -0.5f, 0f), new Vector3(36f, 1f, 6f), bounds);
            Box("Wall_Left_x0", new Vector3(-0.3f, 2f, 0f), new Vector3(0.6f, 6f, 6f), bounds);       // край перрона
            Box("Wall_Right_x30", new Vector3(30.3f, 2f, 0f), new Vector3(0.6f, 6f, 6f), bounds);

            Transform fx = Group("--- FX");
            Inst(p.Rain, "Rain", new Vector3(15f, 9f, 1f), fx);

            EditorSceneManager.SaveScene(scene, StationScene);
        }

        // ------------------------------------------------------------------ 1-02 Main Street

        private static void BuildStreet(Prefabs p, Dictionary<string, RuntimeAnimatorController> ac, Material[] mats, VolumeProfile profile)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Default entry: arrival from the station (x = 2). Night state of the first evening.
            CommonSystems(p, 3.5f, 6f, 54f, profile);

            Transform bg = Group("--- Background");
            Backdrop(mats[0], 30f, bg);
            SpriteObj("Headframe_Distant", $"{EnvDir}/MainStreet/ENV_Headframe_Distant.png", new Vector3(46f, -2f, 32f), bg, 1.6f, new Color(0.55f, 0.6f, 0.68f));

            Transform geo = Group("--- Geometry");
            Quad("Road_Mud", mats[2], new Vector3(30f, 0f, -0.5f), new Vector3(90f, 0f, 0f), new Vector3(64f, 7f, 1f), geo);
            Quad("Boardwalk", mats[1], new Vector3(30f, 0.01f, 2.4f), new Vector3(90f, 0f, 0f), new Vector3(64f, 1.4f, 1f), geo);

            Transform fac = Group("--- Facades");
            Color night = new Color(0.82f, 0.83f, 0.88f);
            SpriteObj("FAC_Saloon_6-14", $"{EnvDir}/MainStreet/FAC_Saloon.png", new Vector3(10f, 0f, 3f), fac, 1f, night);
            SpriteObj("FAC_CompanyOffice_16-24", $"{EnvDir}/MainStreet/FAC_CompanyOffice.png", new Vector3(20f, 0f, 3f), fac, 1f, night);
            SpriteObj("FAC_SheriffOffice_25-31", $"{EnvDir}/MainStreet/FAC_SheriffOffice.png", new Vector3(28f, 0f, 3f), fac, 1f, night);
            SpriteObj("FAC_AlleyMouth_33-39", $"{EnvDir}/MainStreet/FAC_AlleyMouth.png", new Vector3(36f, 0f, 3f), fac, 1f, night);
            SpriteObj("FAC_SawmillFence_40-49", $"{EnvDir}/MainStreet/FAC_SawmillFence.png", new Vector3(44.5f, 0f, 3.2f), fac, 1f, night);
            SpriteObj("FAC_Chapel_49-56", $"{EnvDir}/MainStreet/FAC_Chapel.png", new Vector3(52.5f, 0f, 3f), fac, 1f, night);
            SpriteObj("FAC_MineRoadFence_56-60", $"{EnvDir}/MainStreet/FAC_MineRoadFence.png", new Vector3(58f, 0f, 3.2f), fac, 1f, night);
            PropInst(p, "NoticeBoard_x23", $"{PropDir}/PRP_NoticeBoard.png", new Vector3(23f, 0f, 2.7f), fac);

            var saloonGlow = new GameObject("Saloon_Windows_Light");
            saloonGlow.transform.SetParent(fac, false);
            saloonGlow.transform.localPosition = new Vector3(10f, 2.2f, 2.2f);
            var sg = saloonGlow.AddComponent<Light>();
            sg.type = LightType.Point;
            sg.color = PlaceholderPainter.C("#E8A84E");
            sg.intensity = 3f;
            sg.range = 6f;

            Transform set = Group("--- Set Dressing");
            Inst(p.Lamppost, "Lamp_x6", new Vector3(6f, 0f, 1.6f), set);
            Inst(p.Lamppost, "Lamp_x31", new Vector3(31f, 0f, 1.6f), set);
            Inst(p.Lamppost, "Lamp_x48", new Vector3(48f, 0f, 1.6f), set);
            PropInst(p, "Barrels_Hide_x41", $"{PropDir}/PRP_Barrels.png", new Vector3(41f, 0f, 1.8f), set);     // укрытие ночью (система укрытий — позже)

            Transform fg = Group("--- Foreground (silhouettes)");
            Color sil = new Color(0.12f, 0.12f, 0.14f);
            PropInst(p, "FG_HitchingPost_x4", $"{PropDir}/PRP_HitchingPost.png", new Vector3(4f, -1.0f, -3f), fg, 1.1f, sil);
            PropInst(p, "FG_TelegraphPole_x18", $"{PropDir}/PRP_TelegraphPole.png", new Vector3(18f, -0.6f, -3.6f), fg, 1f, sil);
            PropInst(p, "FG_Barrels_x34", $"{PropDir}/PRP_Barrels.png", new Vector3(34f, -1.0f, -3f), fg, 1.1f, sil);
            PropInst(p, "FG_TelegraphPole_x50", $"{PropDir}/PRP_TelegraphPole.png", new Vector3(50f, -0.6f, -3.6f), fg, 1f, sil);

            Transform play = Group("--- Gameplay");
            SceneDoor(p, "1-02_to_1-01", "Station", "from_1-02", "Подняться на станцию",
                $"{EnvDir}/MainStreet/ENV_Street_StationSteps.png", 2f, play);
            Spawn("from_1-01", 3.5f, play);

            // Closed transitions — kept at the map positions, never load anything.
            Stub(p, "1-02_to_1-03", "1-03", "Салун «Последняя лампа»", "Заперто. Внутри ещё горит свет.", $"{PropDir}/DOOR_Saloon_Closed.png", 10f, play);
            Stub(p, "1-02_to_1-05", "1-05", "Контора компании", "Контора заперта.", $"{PropDir}/DOOR_CompanyOffice_Closed.png", 20f, play);
            Stub(p, "1-02_to_1-08", "1-08", "Контора шерифа", "Заперто.", $"{PropDir}/DOOR_Sheriff_Closed.png", 28f, play);
            Stub(p, "1-02_to_1-14", "1-14", "Переулок", "Проход перегорожен калиткой.", $"{PropDir}/DOOR_AlleyGate_Closed.png", 36f, play);
            Stub(p, "1-02_to_1-18", "1-18", "Ворота лесопилки", "Ворота на цепи.", $"{PropDir}/DOOR_SawmillGate_Closed.png", 45f, play);
            Stub(p, "1-02_to_1-09", "1-09", "Часовня", "Двери часовни закрыты.", $"{PropDir}/DOOR_Chapel_Closed.png", 52f, play);
            Stub(p, "1-02_to_1-21", "1-21", "Дорога к шахте", "Ворота на замке.", $"{PropDir}/DOOR_MineGate_Closed.png", 58f, play);

            // Map NPCs that belong to other times of day stay in the scene, inactive, for the flag system.
            Transform day = Group("State_Day (inactive at arrival)");
            day.SetParent(play, false);
            Npc(p, ac, "NPC_PasserbyMiner_x13", "PasserbyMiner", new Vector3(13f, 0f, 0.8f), day, "Поговорить", "…");
            Npc(p, ac, "NPC_PasserbyWoman_x15", "PasserbyWoman", new Vector3(15f, 0f, 0.8f), day, "Поговорить", "…");
            day.gameObject.SetActive(false);
            Transform eveningDay2 = Group("State_Day2Evening (inactive)");
            eveningDay2.SetParent(play, false);
            Npc(p, ac, "NPC_Liam_x38", "Liam", new Vector3(38f, 0f, 0.6f), eveningDay2, "Поговорить", "…");
            eveningDay2.gameObject.SetActive(false);

            Transform bounds = Group("--- Collision & Bounds");
            Box("Ground", new Vector3(30f, -0.5f, 0f), new Vector3(66f, 1f, 6f), bounds);
            Box("Wall_Left_x0", new Vector3(-0.3f, 2f, 0f), new Vector3(0.6f, 6f, 6f), bounds);
            Box("Wall_Right_x60", new Vector3(60.3f, 2f, 0f), new Vector3(0.6f, 6f, 6f), bounds);     // за воротами к шахте

            Transform fx = Group("--- FX");
            Inst(p.Rain, "Rain_West", new Vector3(15f, 9f, 1f), fx);
            Inst(p.Rain, "Rain_East", new Vector3(45f, 9f, 1f), fx);

            EditorSceneManager.SaveScene(scene, StreetScene);
        }
    }
}
