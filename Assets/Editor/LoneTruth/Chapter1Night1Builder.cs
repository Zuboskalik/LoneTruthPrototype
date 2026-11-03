using System.Collections.Generic;
using LoneTruth.Combat;
using LoneTruth.Core;
using LoneTruth.Dialogue;
using LoneTruth.Interaction;
using LoneTruth.Investigation;
using LoneTruth.Story;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LoneTruth.EditorTools
{
    /// <summary>
    /// Night 1 (design doc 8.2 step 5–6, 8.3–8.5; maps sheets B–E): the locations that open at night,
    /// the enemies, and the dialogues that close quests 1.1 and 1.3. The one thing the plot needs
    /// from this night is the first Hollow on the cemetery; see docs/Night1.md.
    /// </summary>
    public static partial class Chapter1LocationBuilder
    {
        private const string CemeteryScene = SceneDir + "/Cemetery.unity";
        private const string VestryScene = SceneDir + "/Vestry.unity";
        private const string CryptScene = SceneDir + "/Crypt.unity";
        private const string BelfryScene = SceneDir + "/Belfry.unity";
        private const string WellHeadScene = SceneDir + "/WellHead.unity";
        private const string SawmillScene = SceneDir + "/SawmillYard.unity";
        private const string HutScene = SceneDir + "/WatchmanHut.unity";
        private const string ArchiveScene = SceneDir + "/Archive.unity";

        // Placeholder looks; there are no sheets for these three yet.
        private static Human Thug => new Human
        {
            Height = 1.82f, Coat = PlaceholderPainter.C("#3B3A36"), Trousers = PlaceholderPainter.C("#26262A"),
            Hat = PlaceholderPainter.C("#2C2C2E"), Accent = PlaceholderPainter.C("#5A4A32"), Skin = PlaceholderPainter.C("#B39A84"),
            HatStyle = 3, LongCoat = false, Width = 1.2f
        };

        private static Human Watchman => new Human
        {
            Height = 1.7f, Coat = PlaceholderPainter.C("#4A4438"), Trousers = PlaceholderPainter.C("#322E28"),
            Hat = PlaceholderPainter.C("#3A3228"), Accent = PlaceholderPainter.C("#6E6250"), Skin = PlaceholderPainter.C("#B8A48E"),
            HatStyle = 2, LongCoat = true
        };

        // ------------------------------------------------------------------ placeholder art

        private static void DrawNight1Placeholders()
        {
            Char("CH_Watchman_Ref", Watchman, -1, 0, 0, 170);
            for (int i = 0; i < 2; i++) Char($"CH_Watchman_Idle_{i:00}", Watchman, -1, 0, i, 171 + i);
            Char("CH_Thug_Ref", Thug, -1, 0, 0, 180);
            for (int i = 0; i < 2; i++) Char($"CH_Thug_Idle_{i:00}", Thug, -1, 0, i, 181 + i);
            for (int i = 0; i < 4; i++) Char($"CH_Thug_Walk_{i:00}", Thug, i / 4f, 0, i % 2, 183 + i);
            for (int i = 0; i < 2; i++) Char($"CH_Thug_Attack_{i:00}", Thug, -1, 1, i, 187 + i);

            for (int i = 0; i < 2; i++) PaintDog(0f, i).SaveIfMissing($"{CharDir}/CH_Dog_Idle_{i:00}.png");
            for (int i = 0; i < 4; i++) PaintDog(i / 4f + 0.1f, 0).SaveIfMissing($"{CharDir}/CH_Dog_Run_{i:00}.png");

            Color coal = PlaceholderPainter.C("#23242A"), steel = PlaceholderPainter.C("#5E636B"), wood = PlaceholderPainter.C("#3E3026");
            Icon("ICO_Flare", p =>
            {
                p.Rect(50, 18, 28, 80, PlaceholderPainter.C("#8E2A22"));
                p.Rect(50, 92, 28, 8, PlaceholderPainter.C("#D8CFBE"));
            });
            Icon("ICO_FlareConsecrated", p =>
            {
                p.Rect(50, 18, 28, 80, PlaceholderPainter.C("#B5892E"));
                p.Rect(61, 36, 6, 40, PlaceholderPainter.C("#EFE4C8"));
                p.Rect(52, 58, 24, 6, PlaceholderPainter.C("#EFE4C8"));
            });
            Icon("ICO_Meat", p =>
            {
                p.Ellipse(70, 66, 40, 30, PlaceholderPainter.C("#8E3A32"));
                p.Rect(14, 58, 34, 12, PlaceholderPainter.C("#D8CFBE"));
            });
            Icon("ICO_Crowbar", p =>
            {
                p.Rect(58, 16, 12, 90, steel);
                p.Rect(58, 100, 34, 10, steel);
            });
            Icon("ICO_ArchiveKey", p =>
            {
                p.Ellipse(44, 78, 22, 22, PlaceholderPainter.C("#8E7A5A"));
                p.Rect(60, 72, 50, 10, PlaceholderPainter.C("#8E7A5A"));
                p.Rect(96, 54, 8, 20, PlaceholderPainter.C("#8E7A5A"));
            });

            Prop("PRP_Grave_1882", 5f, 1.2f, p => PaintGraves(p, "#55554F", 5, 1));
            Prop("PRP_Grave_1906", 5f, 1.2f, p => PaintGraves(p, "#5E5C54", 6, 2));
            Prop("PRP_Grave_1936", 5f, 1.1f, p => PaintGraves(p, "#6E6A5E", 8, 3));
            Prop("PRP_GraveEdgar", 1.1f, 1.2f, p =>
            {
                p.Rect(40, 0, p.W - 80, p.H - 30, PlaceholderPainter.C("#5E5C54"));
                p.Ellipse(p.W / 2f, p.H - 34, p.W / 2f - 40, 30, PlaceholderPainter.C("#5E5C54"));
            });
            Prop("PRP_Lodge", 4.4f, 3.4f, p =>
            {
                p.Rect(0, 0, p.W, 240, PlaceholderPainter.C("#3A342C"));
                p.Trap(p.W / 2f, 240, p.W + 20, 30, 100, PlaceholderPainter.C("#2A2622"));
                p.Rect(90, 60, 90, 90, PlaceholderPainter.C("#6E5A34"));
            });
            Prop("PRP_Spade", 0.4f, 1.3f, p =>
            {
                p.Rect(16, 34, 8, p.H - 34, wood);
                p.Rect(6, 0, 28, 40, steel);
            });
            Prop("PRP_Crowbar", 0.5f, 1.1f, p =>
            {
                p.Rect(10, 0, 9, p.H, steel);                   // the shaft
                p.Rect(10, p.H - 9, p.W - 14, 9, steel);         // the bent end: a Г, as on the icon
            });
            Prop("PRP_TokenGlint", 0.3f, 0.3f, p => p.Ellipse(15, 15, 11, 11, PlaceholderPainter.C("#A89A70")));
            Prop("PRP_Wardrobe", 1.4f, 2.6f, p =>
            {
                p.Rect(0, 0, p.W, p.H, wood);
                p.Rect(p.W / 2f - 2, 0, 4, p.H, coal);
            });
            Prop("PRP_ParishDesk", 1.3f, 1.3f, p =>
            {
                p.Rect(10, 0, p.W - 20, 86, wood);
                p.Rect(0, 86, p.W, 12, PlaceholderPainter.C("#2E241C"));
                p.Rect(30, 98, p.W - 60, 24, PlaceholderPainter.C("#8A7E6A"));
            });
            Prop("PRP_Chest", 1.6f, 0.7f, p =>
            {
                p.Rect(0, 0, p.W, 60, wood);
                p.Rect(0, 30, p.W, 5, coal);
            });
            Prop("PRP_CryptNiche", 2.2f, 2.4f, p =>
            {
                p.Rect(0, 0, p.W, p.H, PlaceholderPainter.C("#23232A"));
                p.Rect(14, 0, p.W - 28, 70, PlaceholderPainter.C("#3A342C"));
            });
            Prop("PRP_Tombs", 6f, 2.6f, p =>
            {
                for (int x = 0; x < p.W; x += 150)
                {
                    p.Rect(x + 8, 40, 134, 80, coal);
                    p.Rect(x + 16, 40, 118, 34, PlaceholderPainter.C("#332E28"));
                    p.Rect(x + 8, 150, 134, 60, coal);
                }
            });
            Prop("PRP_Bell", 2.2f, 2.2f, p =>
            {
                p.Trap(p.W / 2f, 0, p.W - 30, 90, 170, PlaceholderPainter.C("#6E5A34"));
                p.Rect(0, 186, p.W, 18, wood);
                p.Ellipse(p.W / 2f, 10, 20, 20, PlaceholderPainter.C("#4A3C24"));
            });
            Prop("PRP_BelfrySlats", 2.4f, 2.4f, p =>
            {
                p.Rect(0, 0, p.W, p.H, PlaceholderPainter.C("#1A2030"));
                for (int y = 8; y < p.H; y += 30) p.Rect(0, y, p.W, 16, wood);
            });
            Prop("PRP_WellHead", 3f, 2f, p =>
            {
                p.Rect(0, 0, p.W, 150, PlaceholderPainter.C("#50362C"));
                for (int y = 20; y < 150; y += 24) p.Rect(0, y, p.W, 3, PlaceholderPainter.C("#38261E"));
                p.Rect(60, 50, p.W - 120, 90, PlaceholderPainter.C("#14161A"));
                for (int x = 70; x < p.W - 60; x += 26) p.Rect(x, 50, 6, 90, PlaceholderPainter.C("#6E4A34"));
            });
            Prop("PRP_SpoilHeap", 8f, 3.4f, p => p.Trap(p.W / 2f, 0, p.W, 90, p.H - 6, PlaceholderPainter.C("#23232A")));
            Prop("PRP_LogStack", 3f, 1.8f, p =>
            {
                for (int row = 0; row < 4; row++)
                    for (int x = row * 18 + 10; x < p.W - row * 18 - 16; x += 38)
                        p.Ellipse(x + 18, row * 34 + 22, 19, 19, PlaceholderPainter.C(row % 2 == 0 ? "#5A4632" : "#4A3A2A"));
            });
            Prop("PRP_SawShed", 8f, 4f, p =>
            {
                for (int x = 10; x < p.W; x += 180) p.Rect(x, 0, 14, 320, wood);
                p.Trap(p.W / 2f, 300, p.W, p.W - 80, 90, PlaceholderPainter.C("#26262A"));
                p.Rect(120, 50, p.W - 240, 40, PlaceholderPainter.C("#3A3026"));
                p.Ellipse(p.W / 2f, 100, 60, 60, steel);
            });
            Prop("PRP_Searchlight", 0.9f, 5f, p =>
            {
                p.Rect(p.W / 2f - 6, 0, 12, p.H - 30, wood);
                p.Rect(10, p.H - 50, p.W - 20, 44, steel);
            });
            Prop("PRP_HutStove", 1.2f, 1.2f, p =>
            {
                p.Rect(14, 0, p.W - 28, 80, coal);
                p.Rect(36, 16, p.W - 72, 26, PlaceholderPainter.C("#A0522A"));
                p.Rect(p.W - 40, 80, 14, 40, coal);
            });
            Prop("PRP_KeyBoard", 1.3f, 0.9f, p =>
            {
                p.Rect(0, 0, p.W, p.H, wood);
                for (int x = 16; x < p.W - 8; x += 26) p.Rect(x, 30, 4, 34, PlaceholderPainter.C("#8E7A5A"));
            });
            Prop("PRP_ArchiveShelves", 6f, 3.4f, p =>
            {
                for (int y = 0; y < p.H; y += 80)
                {
                    p.Rect(0, y, p.W, 8, wood);
                    for (int x = 4; x < p.W - 14; x += 18) p.Rect(x, y + 8, 14, 50 + (x % 3) * 8, PlaceholderPainter.C(x % 2 == 0 ? "#4A4438" : "#3A3A38"));
                }
            });
            Prop("PRP_LedgerDesk", 2.2f, 1.2f, p =>
            {
                p.Rect(14, 0, 16, 80, wood);
                p.Rect(p.W - 30, 0, 16, 80, wood);
                p.Rect(0, 80, p.W, 12, wood);
                p.Rect(50, 92, 80, 14, PlaceholderPainter.C("#5A4632"));
            });
        }

        private static void PaintGraves(PlaceholderPainter p, string hex, int count, int style)
        {
            Color stone = PlaceholderPainter.C(hex);
            float step = p.W / (float)count;
            for (int i = 0; i < count; i++)
            {
                float x = i * step + step * 0.25f;
                float h = 50 + (i * 37 % 40);
                if (style == 1)
                {
                    p.Rect(x + 8, 0, 8, h + 20, stone);       // crooked crosses
                    p.Rect(x, h, 24, 7, stone);
                }
                else
                {
                    p.Rect(x, 0, 28, h, stone);               // headstones; the newest stand in fresh earth
                    p.Ellipse(x + 14, h, 14, 10, stone);
                    if (style == 3) p.Rect(x - 6, 0, 42, 6, PlaceholderPainter.C("#3A2E24"));
                }
            }
        }

        private static PlaceholderPainter PaintDog(float run, int bob)
        {
            var p = Sprite(1.1f, 0.75f, 190 + bob + (int)(run * 10f));
            Color fur = PlaceholderPainter.C("#3E3A34"), dark = PlaceholderPainter.C("#26241F");
            float swing = run <= 0f ? 0f : Mathf.Sin(run * Mathf.PI * 2f) * 12f;
            p.Rect(26 + swing, 0, 8, 34, dark);
            p.Rect(38 - swing, 0, 8, 34, fur);
            p.Rect(70 + swing, 0, 8, 34, dark);
            p.Rect(82 - swing, 0, 8, 34, fur);
            p.Ellipse(58, 44 + bob, 40, 15, fur);                       // lean body
            p.Ellipse(94, 58 + bob, 13, 11, fur);                       // head
            p.Rect(100, 52 + bob, 14, 7, fur);                          // muzzle
            p.Rect(90, 66 + bob, 6, 8, dark);                           // ear
            p.Rect(8, 50 + bob, 16, 4, fur);                            // tail
            p.Rect(84, 46 + bob, 8, 5, PlaceholderPainter.C("#6A6D74")); // spiked collar
            p.InkOutline(Ink, 2);
            return p;
        }

        // ------------------------------------------------------------------ enemy animators

        private static void BuildEnemyControllers()
        {
            if (HollowSheet.Exists)
            {
                EnemyController("AC_Hollow",
                    SheetClip(HollowSheet, "Hollow_Idle", 1, true, 1f, "Walk_00"),
                    SheetClip(HollowSheet, "Hollow_Walk", 6, true, 1f, "Walk_00", "Walk_01", "Walk_02", "Walk_03", "Walk_04"),
                    // The pick stays raised through the wind-up; the blow lands at frame 4.
                    SheetClip(HollowSheet, "Hollow_Attack", 5, false, 1f, "Swing_00", "Swing_01", "Swing_02", "Swing_03", "Swing_04", "Swing_05", "Swing_06", "Swing_07"),
                    SheetClip(HollowSheet, "Hollow_Blind", 4, true, 1f, "Blind_00", "Blind_01", "Blind_02", "Blind_03", "Blind_04"),
                    SheetClip(HollowSheet, "Hollow_Retreat", 5, true, 1f, "Recoil_00", "Recoil_01", "Recoil_02", "Recoil_01"),
                    SheetClip(HollowSheet, "Hollow_Sleep", 1, true, 1f, "Die_00"),
                    SheetClip(HollowSheet, "Hollow_Die", 5, false, 1f, "Die_00", "Die_01", "Die_02", "Die_03", "Die_04", "Die_05", "Die_06", "Die_07"));
            }

            AnimationClip thugIdle = SpriteClip("Thug_Idle", "CH_Thug_Idle", 2, 1.5f, true);
            EnemyController("AC_Thug", thugIdle,
                SpriteClip("Thug_Walk", "CH_Thug_Walk", 4, 6f, true),
                SpriteClip("Thug_Attack", "CH_Thug_Attack", 2, 3f, false),
                thugIdle, null, null, thugIdle);

            AnimationClip dogIdle = SpriteClip("Dog_Idle", "CH_Dog_Idle", 2, 2f, true);
            EnemyController("AC_Dog", dogIdle,
                SpriteClip("Dog_Run", "CH_Dog_Run", 4, 10f, true),
                dogIdle, dogIdle, null, null, dogIdle);
        }

        private static void EnemyController(string name, AnimationClip idle, AnimationClip walk, AnimationClip attack, AnimationClip blind, AnimationClip retreat, AnimationClip sleep, AnimationClip die)
        {
            string path = $"{AnimDir}/{name}.controller";
            AssetDatabase.DeleteAsset(path);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(path);
            ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ac.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            ac.AddParameter("Blind", AnimatorControllerParameterType.Bool);
            ac.AddParameter("Retreat", AnimatorControllerParameterType.Bool);
            ac.AddParameter("Sleep", AnimatorControllerParameterType.Bool);
            ac.AddParameter("Die", AnimatorControllerParameterType.Trigger);
            AnimatorStateMachine sm = ac.layers[0].stateMachine;

            AnimatorState State(string state, Vector3 pos, AnimationClip clip)
            {
                AnimatorState st = sm.AddState(state, pos);
                st.motion = clip;
                return st;
            }

            AnimatorState Any(string state, Vector3 pos, AnimationClip clip, AnimatorConditionMode mode, string param)
            {
                AnimatorState st = State(state, pos, clip);
                AnimatorStateTransition t = sm.AddAnyStateTransition(st);
                t.hasExitTime = false;
                t.duration = 0f;
                t.canTransitionToSelf = false;
                t.AddCondition(mode, 0f, param);
                return st;
            }

            AnimatorState idleState = State("Idle", new Vector3(250, 0), idle);
            AnimatorState walkState = State("Walk", new Vector3(250, 100), walk);
            sm.defaultState = idleState;
            Go(idleState, walkState, false).AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            Go(walkState, idleState, false).AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            Go(Any("Attack", new Vector3(500, 0), attack, AnimatorConditionMode.If, "Attack"), idleState, true);
            Go(Any("Blind", new Vector3(500, 100), blind, AnimatorConditionMode.If, "Blind"), idleState, false)
                .AddCondition(AnimatorConditionMode.IfNot, 0f, "Blind");
            if (retreat != null)
            {
                Go(Any("Retreat", new Vector3(500, 200), retreat, AnimatorConditionMode.If, "Retreat"), idleState, false)
                    .AddCondition(AnimatorConditionMode.IfNot, 0f, "Retreat");
            }

            if (sleep != null)
            {
                Go(Any("Sleep", new Vector3(0, 100), sleep, AnimatorConditionMode.If, "Sleep"), idleState, false)
                    .AddCondition(AnimatorConditionMode.IfNot, 0f, "Sleep");
            }

            Any("Die", new Vector3(500, 300), die, AnimatorConditionMode.If, "Die");
        }

        private static GameObject Enemy(string name, string controller, float x, Transform parent, EnemyKind kind, int hp, float minX, float maxX, float worldWidth, bool asleep, string deadFlag, Transform drop, Sprite first)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, 0f, 0f);
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(0.9f, 1.8f, 0.9f);
            box.center = new Vector3(0f, 0.9f, 0f);
            SetField(go.AddComponent<Health>(), "maxHealth", hp);
            var sprite = new GameObject("Sprite");
            sprite.transform.SetParent(go.transform, false);
            sprite.AddComponent<SpriteRenderer>().sprite = first;
            sprite.AddComponent<Animator>().runtimeAnimatorController =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{AnimDir}/{controller}.controller");
            go.AddComponent<EnemyController>().Configure(kind, minX, maxX, 0.5f, worldWidth - 0.5f, asleep, deadFlag, drop);
            return go;
        }

        private static GameObject Hollow(string name, float x, Transform parent, float minX, float maxX, float worldWidth, bool asleep, string deadFlag, Transform drop)
        {
            Sprite first = HollowSheet.Exists ? SheetFrame(HollowSheet, asleep ? "Die_00" : "Walk_00") : S($"{CharDir}/CH_Thug_Ref.png");
            return Enemy(name, "AC_Hollow", x, parent, EnemyKind.Hollow, 75, minX, maxX, worldWidth, asleep, deadFlag, drop, first);
        }

        // ------------------------------------------------------------------ scene helpers

        /// <summary>Opens a new open-air scene: sky, mud, rain, bounds.</summary>
        private static Scene NewExterior(Prefabs p, VolumeProfile profile, ref Items items, float width, float playerX)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ReloadAssets(ref items, ref profile);
            Material[] mats = BuildMaterials();
            CommonSystems(p, playerX, Mathf.Min(6f, width / 2f), Mathf.Max(width - 6f, width / 2f), profile);
            Transform bg = Group("--- Background");
            Backdrop(mats[0], width / 2f, bg);
            SpriteObj("Headframe_Distant", $"{EnvDir}/MainStreet/ENV_Headframe_Distant.png", new Vector3(width * 0.7f, -2f, 32f), bg, 1.6f, new Color(0.55f, 0.58f, 0.66f));
            SetupTimeOfDay(true, mats[0], AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/M_Sky_OvercastDay.mat"));
            Quad("Ground_Mud", mats[2], new Vector3(width / 2f, 0f, 0.5f), new Vector3(90f, 0f, 0f), new Vector3(width + 8f, 8f, 1f), Group("--- Geometry"));
            Inst(p.Rain, "Rain", new Vector3(width / 2f, 9f, 1f), Group("--- FX"));
            AddBounds(width, Group("--- Collision & Bounds"));
            return scene;
        }

        private static GameObject CombatArea(Transform parent)
        {
            var go = new GameObject("CombatZone");
            go.transform.SetParent(parent, false);
            go.AddComponent<CombatZone>();
            return go;
        }

        private static GameObject HideSpot(string name, float x, Transform parent, string hiddenFlag = "")
        {
            GameObject go = InspectSpot(name, new Vector3(x, 0f, 0f), parent, 1.4f);
            go.AddComponent<HidingSpot>().Configure(hiddenFlag);
            return go;
        }

        private static GameObject Pickup(string name, float x, Transform parent, LoneTruth.Inventory.ItemSO item, int amount, string takenFlag, string art)
        {
            GameObject go = InspectSpot(name, new Vector3(x, 0f, 0f), parent);
            go.AddComponent<StoryPickup>().Configure(item, amount, takenFlag);
            var sprite = new GameObject("Art").AddComponent<SpriteRenderer>();
            sprite.transform.SetParent(go.transform, false);
            sprite.transform.localPosition = new Vector3(0f, 0f, 0.4f);
            sprite.sprite = S(art);
            return go;
        }

        private static string[] NoQuest13Outcome => F("quest_1_3_hope", "quest_1_3_despair", "quest_1_3_truth");
        private static string[] NoQuest11Outcome => F("quest_1_1_hope", "quest_1_1_despair", "quest_1_1_truth");

        // ------------------------------------------------------------------ dialogues

        /// <summary>Quest 1.1, the choice (design doc 8.3). All three answers are about what Holt tells Agnes.</summary>
        private static DialogueSO AgnesFinaleDialogue()
        {
            var hope = DChoice("Томас, скорее всего, жив. Снизу стучат. Я его верну.", "hope");
            hope.questId = "1_1";
            hope.outcomeId = "hope";
            hope.setFlags = new[] { StoryFlags.ClueThomasLamp };

            var despair = DChoice("Письма — жестокая шутка мальчишки. Забудьте о них.", "despair");
            despair.questId = "1_1";
            despair.outcomeId = "despair";
            despair.money = 100;
            despair.setFlags = new[] { StoryFlags.VentSealed };

            var truth = DChoice("Тот же почерк. Но Томас не знал этих слов. Его рукой кто-то водит.", "truth");
            truth.presentClue = StoryFlags.ClueThomasOldLetter;
            truth.hideWhenLocked = true;
            truth.questId = "1_1";
            truth.outcomeId = "truth";
            truth.setFlags = new[] { StoryFlags.ClueThomasLamp };

            return DialogueAsset("Agnes_Finale", "ask",
                DNode("ask", "Агнес Бирн", "Вы были у скважины, мистер Холт? Что там?", "",
                    hope, truth, despair, DChoice("Пока не могу сказать. Я ещё вернусь.")),
                DNode("hope", "Агнес Бирн", "Я знала. Возьмите его лампу, мистер Холт. Внизу она вам нужнее.", "lamp"),
                DNode("lamp", "", "Карбидная лампа Томаса. Агнес держала её на гвозде у двери целый месяц."),
                DNode("despair", "Агнес Бирн", "Мальчишки… Значит, мальчишки. Уходите, мистер Холт.", "despair_inner"),
                DNode("despair_inner", "", "Записка под дверь конторы, сто долларов в ответ. Компания зальёт скважину бетоном."),
                DNode("truth", "Агнес Бирн", "«Десятина»… «Старик, что не стареет»… «Сэмюэл Холт, сын Эдгара». Томас так не говорил. Никогда.", "truth_end"),
                DNode("truth_end", "Агнес Бирн", "Кто бы ни водил его рукой — он там, внизу. Возьмите его лампу.", "lamp"));
        }

        /// <summary>Quest 1.3, the choice (design doc 8.5). Truth needs the 1882 token from the first Hollow.</summary>
        private static DialogueSO MatthewFinaleDialogue(Items items)
        {
            var hope = DChoice("Хватит хоронить пустые гробы. Помогите людям, пока не поздно.", "hope");
            hope.questId = "1_3";
            hope.outcomeId = "hope";
            hope.giveItem = items.FlareConsecrated;
            hope.giveAmount = 3;
            hope.setFlags = new[] { "clue_death_book" };

            var despair = DChoice("Книга и ваши пожертвования — или об этом узнает весь город.", "despair");
            despair.questId = "1_3";
            despair.outcomeId = "despair";
            despair.money = 80;
            despair.setFlags = new[] { "clue_death_book", StoryFlags.MatthewGone };

            var truth = DChoice("Этот жетон я снял с мёртвого шахтёра. Чьи гробы пустые?", "truth");
            truth.presentClue = "clue_token_1882";
            truth.hideWhenLocked = true;
            truth.questId = "1_3";
            truth.outcomeId = "truth";
            truth.giveItem = items.FlareConsecrated;
            truth.giveAmount = 3;
            truth.setFlags = new[] { "clue_death_book", StoryFlags.EdgarGraveKnown };

            return DialogueAsset("Matthew_Finale", "note",
                DNode("note", "Холт", "Эймос передал: «Священник знает. Спроси про книгу».", "confess"),
                DNode("confess", "Отец Мэтью", "Тридцать лет компания жертвует на часовню. Тридцать лет я хороню пустые гробы и записываю имена.", "ask"),
                DNode("ask", "Отец Мэтью", "Что вы теперь со мной сделаете, мистер Холт?", "",
                    hope, truth, despair, DChoice("Я ещё вернусь, отец.")),
                DNode("hope", "Отец Мэтью", "…Да. Вот книга смертей. И фальшфейеры, я их освятил — огонь берёт Пустых. Колокол будет звонить.", "end_hope"),
                DNode("end_hope", "", "Двенадцать, семнадцать, двадцать три. Ни одного тела."),
                DNode("despair", "Отец Мэтью", "Берите. Восемьдесят долларов и книга. Этой же ночью меня здесь не будет.", "end_despair"),
                DNode("end_despair", "", "Вместе с ним уйдёт и звонарь. Кто теперь будет держать их колоколом?"),
                DNode("truth", "Отец Мэтью", "Страница тысяча девятьсот шестого. Семнадцать имён… Читайте сами.", "truth_name"),
                DNode("truth_name", "", "Эдгар Холт. Мой отец. Его гроб тоже пустой?", "truth_end"),
                DNode("truth_end", "Отец Мэтью", "Могила на склоне, среди шестого года. Лопата у сторожа. И возьмите фальшфейеры — я их освятил.", ""));
        }

        // ------------------------------------------------------------------ scenes

        private static void BuildNight1Scenes(Prefabs p, Dictionary<string, RuntimeAnimatorController> ac, VolumeProfile profile, Items items)
        {
            BuildCemetery(p, ac, profile, items);
            BuildVestry(p, ac, profile, items);
            BuildCrypt(p, ac, profile, items);
            BuildBelfry(p, ac, profile, items);
            BuildWellHead(p, ac, profile, items);
            BuildSawmillYard(p, ac, profile, items);
            BuildWatchmanHut(p, ac, profile, items);
            BuildArchive(p, ac, profile, items);
        }

        // 1-13: the first Hollow; the grave of Edgar Holt (Truth of quest 1.3).
        private static void BuildCemetery(Prefabs p, Dictionary<string, RuntimeAnimatorController> ac, VolumeProfile profile, Items items)
        {
            Scene scene = NewExterior(p, profile, ref items, 36f, 2.5f);

            Transform set = Group("--- Set Dressing");
            Color tint = new Color(0.8f, 0.82f, 0.88f);
            SpriteObj("Graves_1882", $"{PropDir}/PRP_Grave_1882.png", new Vector3(9f, 0f, 2.6f), set, 1f, tint);
            SpriteObj("Graves_1906", $"{PropDir}/PRP_Grave_1906.png", new Vector3(17f, 0f, 2.6f), set, 1f, tint);
            SpriteObj("Graves_1936", $"{PropDir}/PRP_Grave_1936.png", new Vector3(26f, 0f, 2.6f), set, 1f, tint);
            SpriteObj("Grave_Edgar_x17", $"{PropDir}/PRP_GraveEdgar.png", new Vector3(17f, 0f, 1.4f), set);
            SpriteObj("Lodge_x33", $"{PropDir}/PRP_Lodge.png", new Vector3(33f, 0f, 3f), set, 1f, tint);
            SpriteObj("Spade_x35", $"{PropDir}/PRP_Spade.png", new Vector3(35f, 0f, 1.6f), set);
            GameObject lodgeLight = PointLight("Lodge_Light", new Vector3(32f, 1.6f, 2.2f), set, "#E0B46A", 2f, 5f);

            Transform play = Group("--- Gameplay");
            SceneDoor(p, "1-13_to_1-09", "ChapelNave", "from_1-13", "Задняя дверь часовни", $"{PropDir}/DOOR_BackDoor_Closed.png", 1f, play);
            Spawn("from_1-09", 2.5f, play);
            GameObject grave = InspectSpot("Grave_Edgar_Spot_x17", new Vector3(17f, 0f, 0f), play, 1.4f);
            GameObject spade = InspectSpot("Spade_Spot_x35", new Vector3(35f, 0f, 0f), play);
            GameObject watchman = Npc(p, ac, "NPC_Watchman_x33", "Watchman", new Vector3(32.6f, 0f, 0.6f), play, "Поговорить", "");
            FaceDoor(watchman);
            GameObject token = InspectSpot("Token_1882", new Vector3(29f, 0f, 0f), play);
            var tokenArt = new GameObject("Glint").AddComponent<SpriteRenderer>();
            tokenArt.transform.SetParent(token.transform, false);
            tokenArt.transform.localPosition = new Vector3(0f, 0.1f, -0.2f);
            tokenArt.sprite = S($"{PropDir}/PRP_TokenGlint.png");
            tokenArt.sortingOrder = 5;
            GameObject hollow = Hollow("Enemy_FirstHollow_x27", 27f, play, 25f, 29f, 36f, false, StoryFlags.FirstHollowDead, token.transform);
            GameObject combat = CombatArea(play);
            // A tin of rounds by the back door: no bare-handed fight with the Hollow, even if he spent all.
            Pickup("Rounds_x3", 3.6f, play, items.Ammo, 6, "cemetery_rounds_taken", $"{PropDir}/PRP_TokenGlint.png");

            Transform story = Group("--- Story");
            Gate("HollowAtNight", story, F(StoryFlags.Night1), null, hollow);
            Gate("WeaponsAtNight", story, F(StoryFlags.Night1), null, combat);
            Gate("TokenAfterHollow", story, F(StoryFlags.FirstHollowDead), F("clue_token_1882"), token);
            Gate("LodgeLight", story, F(StoryFlags.Night1), null, lodgeLight);

            CutsceneSequence first = Sequence("N1_FirstHollow", story, "n1_hollow_seen",
                Inner("Между могилами кто-то идёт. Шахтёр. Глаза светятся, как лампы."),
                Inner("Двадцать третий? Нет. На нём каска другого года."),
                Goal("Остановить Пустого на кладбище."),
                Note("Достать револьвер: выберите его (Q), G. Прицел: ПКМ или Ctrl. Выстрел: E. Перекат: Shift. Фальшфейер: выберите, G.", 8f));
            AreaTrigger(first, 19f, 1.5f, F(StoryFlags.Night1), F(StoryFlags.FirstHollowDead));

            CutsceneSequence after = Sequence("N1_HollowDown", story, "n1_hollow_after",
                Wait(1.8f),
                Inner("Рассыпался угольной пылью. Как будто в нём ничего другого и не было."),
                Goal("Подобрать то, что осталось от Пустого. Закончив ночные дела — лечь спать в номере."));
            FlagTrigger(after, F(StoryFlags.FirstHollowDead), null);

            CutsceneSequence takeToken = Sequence("Clue_Token1882", story, "clue_token_1882",
                Inner("Жетон шахтёра. Выбит год: 1882. Этому человеку должно быть за восемьдесят."),
                Note("Улика в блокноте: жетон шахтёра 1882 года"));
            Talk(token, "Подобрать жетон", "", "").AddBranch(takeToken, null, null);

            // Quest 1.3, Truth: the empty coffin of Holt's father.
            CutsceneSequence noSpade = Sequence("Grave_NoSpade", story, "",
                Inner("Могила отца. Голыми руками не раскопать. Лопата у сторожа, в дальнем конце кладбища."));
            CutsceneSequence dig = Sequence("Grave_Dig", story, StoryFlags.ClueHarmonica,
                WalkTo(16.2f),
                FadeOut(1.2f),
                Wait(1.2f),
                FadeIn(1.2f),
                Inner("Гроб лёгкий. Внутри ничего. Только губная гармошка."),
                Inner("Отец забыл её дома в то утро. Мать положила её сюда: больше от него не осталось ничего."),
                Note("Улика в блокноте: губная гармошка отца"));
            CutsceneInteractable graveTalk = Talk(grave, "Осмотреть могилу", "", "«Эдгар Холт. 1906». Краска на кресте давно облезла.");
            graveTalk.AddBranch(dig, F(StoryFlags.EdgarGraveKnown, StoryFlags.SpadeTaken), null);
            graveTalk.AddBranch(noSpade, F(StoryFlags.EdgarGraveKnown), F(StoryFlags.SpadeTaken));

            CutsceneSequence takeSpade = Sequence("Spade_Take", story, StoryFlags.SpadeTaken,
                Inner("Лопата сторожа. Он сказал бы: «Берите, мистер, сам я тут копать не стану»."),
                Note("Лопата взята"));
            Talk(spade, "Взять лопату", "", "Лопата сторожа. Сейчас мне нечего копать.").AddBranch(takeSpade, F(StoryFlags.EdgarGraveKnown), null);
            Talk(watchman, "Поговорить", "Сторож", "Тел тут нет, мистер. Я-то знаю, сколько весит гроб. Лопата у стены, если нужна.");

            EditorSceneManager.SaveScene(scene, CemeteryScene);
        }

        // 1-10: counterweight 2, the parish book of deaths (Key truth 1), Jesse's refuge.
        private static void BuildVestry(Prefabs p, Dictionary<string, RuntimeAnimatorController> ac, VolumeProfile profile, Items items)
        {
            Scene scene = NewInterior(p, profile, ref items, 10f, 2.2f, 1, out _);

            Transform set = Group("--- Set Dressing");
            SpriteObj("Wardrobe_x2.6", $"{PropDir}/PRP_Wardrobe.png", new Vector3(2.8f, 0f, 3.1f), set);
            SpriteObj("Counterweight_x4", $"{PropDir}/PRP_Counterweight.png", new Vector3(4.2f, 0.4f, 3.3f), set);
            SpriteObj("ParishDesk_x7", $"{PropDir}/PRP_ParishDesk.png", new Vector3(7f, 0f, 2.8f), set);
            SpriteObj("Chest_x9", $"{PropDir}/PRP_Chest.png", new Vector3(9f, 0f, 1.8f), set);
            PointLight("Candle_Light", new Vector3(6.6f, 1.8f, 2f), set, "#F2B45A", 2.6f, 6f);

            Transform play = Group("--- Gameplay");
            SceneDoor(p, "1-10_to_1-09", "ChapelNave", "from_1-10", "Вернуться в неф", $"{PropDir}/DOOR_Interior.png", 1f, play);
            Spawn("from_1-09", 2.2f, play);
            GameObject weight = InspectSpot("Counterweight2_x4", new Vector3(4.2f, 0f, 0f), play, 0.9f);
            GameObject book = InspectSpot("Clue_DeathBook_x7", new Vector3(7f, 0f, 0f), play);
            GameObject jesse = Npc(p, ac, "NPC_Jesse_Vestry_x9", "Jesse", new Vector3(9f, 0f, 0f), play, "Поговорить", "", 0.6f);
            FaceDoor(jesse);
            jesse.AddComponent<FlagAnimatorBool>().Configure("Calm", null, null, "Idle");

            Transform story = Group("--- Story");
            Gate("JesseHere", story, F(StoryFlags.JesseMoved), null, jesse);

            CutsceneSequence lower = Sequence("Counterweight2", story, StoryFlags.Counterweight2,
                Inner("Второй противовес. Цепь пошла вниз и звякнула где-то под потолком."),
                Note("Опущен противовес. Всего их три: неф, ризница, крипта"));
            Talk(weight, "Опустить противовес", "", "Противовес на цепи. Цепь уходит в потолок.").AddBranch(lower, F(StoryFlags.Quest13Started), null);

            CutsceneSequence read = Sequence("Clue_DeathBook", story, "clue_death_book",
                Inner("Приходская книга смертей. 1882 — двенадцать. 1906 — семнадцать. 1936 — двадцать три."),
                Inner("Тела не найдены ни разу. Ни разу. Переписал страницы тайком."),
                Note("Улика в блокноте: приходская книга смертей"));
            Talk(book, "Открыть книгу", "", "Приходская книга смертей. Выписки уже у меня.").AddBranch(read, null, null);

            CutsceneSequence afterTruth = Sequence("Jesse_Vestry_AfterTruth", story, "", Talking(JesseAfterTruthDialogue(items)));
            CutsceneInteractable jesseTalk = Talk(jesse, "Поговорить", "Джесси Коул", "Здесь тихо, мистер. Спасибо. Отец Мэтью приносит воду.");
            jesseTalk.AddBranch(afterTruth, F("quest_1_2_truth"), F(JesseBandagedFlag));

            EditorSceneManager.SaveScene(scene, VestryScene);
        }

        // 1-11: dark; a sleeping Hollow between the hatch and counterweight 3.
        private static void BuildCrypt(Prefabs p, Dictionary<string, RuntimeAnimatorController> ac, VolumeProfile profile, Items items)
        {
            Scene scene = NewInterior(p, profile, ref items, 18f, 2.2f, 0, out _);
            Object.FindAnyObjectByType<TimeOfDayEnvironment>().UseDarkPreset();

            Transform set = Group("--- Set Dressing");
            SpriteObj("Tombs_3-9", $"{PropDir}/PRP_Tombs.png", new Vector3(5f, 0f, 3.2f), set);
            SpriteObj("Tombs_10-16", $"{PropDir}/PRP_Tombs.png", new Vector3(12f, 0f, 3.2f), set);
            SpriteObj("Niche_x6", $"{PropDir}/PRP_CryptNiche.png", new Vector3(6f, 0f, 1.4f), set);
            SpriteObj("Counterweight_x16", $"{PropDir}/PRP_Counterweight.png", new Vector3(16f, 0.4f, 3.3f), set);

            Transform play = Group("--- Gameplay");
            SceneDoor(p, "1-11_to_1-09", "ChapelNave", "from_1-11", "Подняться в неф", $"{PropDir}/DOOR_Interior.png", 1f, play);
            Spawn("from_1-09", 2.2f, play);
            HideSpot("Hide_Niche_x6", 6f, play);
            CombatArea(play);
            Hollow("Enemy_SleepingHollow_x10", 10f, play, 10f, 10f, 18f, true, "crypt_hollow_dead", null);
            GameObject weight = InspectSpot("Counterweight3_x16", new Vector3(16f, 0f, 0f), play, 0.9f);

            Transform story = Group("--- Story");
            CutsceneSequence enter = Sequence("Crypt_Enter", story, "crypt_warned",
                Inner("Темно, хоть глаз коли. Впереди кто-то дышит, хотя дышать тут нечем."),
                Note("Пустой спит. Его будит шум и огонь: выстрел, фальшфейер, перекат. Иди шагом.", 6f));
            StartTrigger(enter, null, F("crypt_hollow_dead"));

            CutsceneSequence lower = Sequence("Counterweight3", story, StoryFlags.Counterweight3,
                Inner("Третий противовес. Цепь пошла вниз. Наверху что-то щёлкнуло."),
                Note("Опущен противовес. Всего их три: неф, ризница, крипта"));
            Talk(weight, "Опустить противовес", "", "Противовес на цепи.").AddBranch(lower, F(StoryFlags.Quest13Started), null);

            EditorSceneManager.SaveScene(scene, CryptScene);
        }

        // 1-12: Amos, the mute bell-ringer, and his note.
        private static void BuildBelfry(Prefabs p, Dictionary<string, RuntimeAnimatorController> ac, VolumeProfile profile, Items items)
        {
            Scene scene = NewInterior(p, profile, ref items, 8f, 2.2f, 3, out _);

            Transform set = Group("--- Set Dressing");
            SpriteObj("Slats_x6", $"{PropDir}/PRP_BelfrySlats.png", new Vector3(6.4f, 1.2f, 3.5f), set);
            SpriteObj("Bell_x3", $"{PropDir}/PRP_Bell.png", new Vector3(3.4f, 2.6f, 2.6f), set);
            PointLight("Moon_Light", new Vector3(6f, 3f, 2.4f), set, "#7D8FB3", 2.4f, 6f);

            Transform play = Group("--- Gameplay");
            SceneDoor(p, "1-12_to_1-09", "ChapelNave", "from_1-12", "Спуститься в неф", $"{PropDir}/PRP_FloorHatch.png", 1f, play);
            Spawn("from_1-09", 2.2f, play);
            GameObject amos = Npc(p, ac, "NPC_Amos_x5", "Amos", new Vector3(5f, 0f, 0f), play, "Подойти", "", 0.3f);
            FaceDoor(amos);

            Transform story = Group("--- Story");
            Gate("AmosStays", story, null, F(StoryFlags.MatthewGone), amos);
            CutsceneSequence meet = Sequence("Q1_3_Amos", story, StoryFlags.AmosNote,
                WalkTo(3.8f),
                Inner("Старик тянет верёвку. Немой: только губы шевелятся в такт."),
                Inner("Он звонит, потому что колокол не пускает их в город. Не с кем об этом поговорить. Некому и сказать."),
                ActorAnim(amos, "React"),
                Wait(1.2f),
                Card("«Священник знает. Спроси про книгу».", 0f),
                Note("Улика в блокноте: записка Эймоса"),
                Goal("Спросить отца Мэтью про книгу."));
            Talk(amos, "Подойти", "Эймос", "Старик кивает и снова берётся за верёвку.").AddBranch(meet, null, null);

            EditorSceneManager.SaveScene(scene, BelfryScene);
        }

        // 1-17: the rusted grate, the fresh letter, the knocking from 200 metres down.
        private static void BuildWellHead(Prefabs p, Dictionary<string, RuntimeAnimatorController> ac, VolumeProfile profile, Items items)
        {
            Scene scene = NewExterior(p, profile, ref items, 18f, 2.5f);

            Transform set = Group("--- Set Dressing");
            SpriteObj("SpoilHeap_x6", $"{PropDir}/PRP_SpoilHeap.png", new Vector3(6f, 0f, 5f), set, 1f, new Color(0.7f, 0.72f, 0.78f));
            SpriteObj("SpoilHeap_x15", $"{PropDir}/PRP_SpoilHeap.png", new Vector3(15f, 0f, 6f), set, 1.2f, new Color(0.6f, 0.62f, 0.68f));
            SpriteObj("WellHead_x12", $"{PropDir}/PRP_WellHead.png", new Vector3(12f, 0f, 2.4f), set);

            Transform play = Group("--- Gameplay");
            SceneDoor(p, "1-17_to_1-14", "Alley", "from_1-17", "Тропа в переулок", $"{PropDir}/DOOR_AlleyGate_Closed.png", 1f, play);
            Spawn("from_1-14", 2.5f, play);
            GameObject grate = InspectSpot("Grate_x12", new Vector3(12f, 0f, 0f), play, 2f);
            grate.AddComponent<ItemLock>().Configure(items.Crowbar, StoryFlags.GrateOpen, "Решётка скважины",
                "Решётка проржавела насквозь, из-под неё тянет тёплым воздухом. Руками не сдвинуть: нужен лом.");

            Transform story = Group("--- Story");
            CutsceneSequence open = Sequence("Q1_1_Grate", story, StoryFlags.Quest11Knock,
                WalkTo(12.6f),
                Note("Использован лом"),
                Inner("Лом вошёл под прутья. Решётка подалась. За ней на выступе лежит письмо: свежее, с угольной пылью на сгибах."),
                Flag(StoryFlags.ClueFreshLetter),
                Note("Улика в блокноте: свежее письмо из скважины"),
                Inner("Снизу, с глубины двести метров, идёт стук. Три коротких. Три длинных."),
                Inner("Кто-то жив и просит помощи. Но скважина узкая, сантиметров сорок: письмо пройдёт, человек нет."),
                Goal("Рассказать Агнес о том, что нашлось в скважине."));
            FlagTrigger(open, F(StoryFlags.GrateOpen), F(StoryFlags.Quest11Knock));

            EditorSceneManager.SaveScene(scene, WellHeadScene);
        }

        // 1-18: thugs with lanterns and a dog between the gate and the warehouse; the watchman's hut.
        private static void BuildSawmillYard(Prefabs p, Dictionary<string, RuntimeAnimatorController> ac, VolumeProfile profile, Items items)
        {
            Scene scene = NewExterior(p, profile, ref items, 40f, 2.5f);

            Transform set = Group("--- Set Dressing");
            SpriteObj("SawShed_x20", $"{PropDir}/PRP_SawShed.png", new Vector3(20f, 0f, 3.2f), set, 1f, new Color(0.75f, 0.77f, 0.82f));
            SpriteObj("Searchlight_x22", $"{PropDir}/PRP_Searchlight.png", new Vector3(26f, 0f, 2.6f), set);
            PointLight("Searchlight_Light", new Vector3(26f, 4.4f, 1.4f), set, "#DDE3EE", 5f, 12f);
            foreach (float x in new[] { 3.4f, 13f, 29f })
            {
                SpriteObj($"LogStack_x{x:0}", $"{PropDir}/PRP_LogStack.png", new Vector3(x, 0f, 1.2f), set);
            }

            Transform play = Group("--- Gameplay");
            SceneDoor(p, "1-18_to_1-02", "MainStreet", "from_1-18", "Ворота на Главную улицу", $"{PropDir}/DOOR_SawmillGate_Closed.png", 1f, play);
            SceneDoor(p, "1-18_to_1-19", "WatchmanHut", "from_1-18", "Будка сторожа", $"{PropDir}/DOOR_Shed.png", 7f, play);
            Stub(p, "1-18_to_1-20", "1-20", "Склад взрывчатки", "Склад на замке. У дверей пост.", $"{PropDir}/DOOR_SawmillGate_Closed.png", 39f, play);
            Spawn("from_1-02", 2.5f, play);
            Spawn("from_1-19", 7f, play);
            CombatArea(play);
            HideSpot("Hide_Logs_x3", 3.4f, play);
            HideSpot("Hide_Logs_x13", 13f, play);
            HideSpot("Hide_Logs_x29", 29f, play);

            Sprite thug = S($"{CharDir}/CH_Thug_Ref.png");
            GameObject thugA = Enemy("Enemy_Thug_A", "AC_Thug", 18f, play, EnemyKind.Thug, 50, 6f, 22f, 40f, false, "sawmill_thug_a_gone", null, thug);
            GameObject thugB = Enemy("Enemy_Thug_B", "AC_Thug", 33f, play, EnemyKind.Thug, 50, 28f, 37f, 40f, false, "sawmill_thug_b_gone", null, thug);
            foreach (GameObject t in new[] { thugA, thugB })
            {
                PointLight("Lantern", new Vector3(0.4f, 1.1f, -0.2f), t.transform, "#F2B45A", 2.6f, 5f);
            }

            Enemy("Enemy_Dog", "AC_Dog", 25f, play, EnemyKind.Dog, 25, 14f, 27f, 40f, false, "sawmill_dog_gone", null, S($"{CharDir}/CH_Dog_Idle_00.png"));

            Transform story = Group("--- Story");
            CutsceneSequence enter = Sequence("Sawmill_Enter", story, "sawmill_entered",
                Inner("Двор лесопилки. Двое с фонарями и пёс. Ключи сторож держит в будке, слева."),
                Note("Спрятаться за брёвнами: E. Фонарь видит только вперёд.", 7f));
            StartTrigger(enter, null, null);

            EditorSceneManager.SaveScene(scene, SawmillScene);
        }

        // 1-19: the key to the company archive.
        private static void BuildWatchmanHut(Prefabs p, Dictionary<string, RuntimeAnimatorController> ac, VolumeProfile profile, Items items)
        {
            Scene scene = NewInterior(p, profile, ref items, 6f, 1.8f, 3, out _);

            Transform set = Group("--- Set Dressing");
            SpriteObj("Stove_x3", $"{PropDir}/PRP_HutStove.png", new Vector3(3f, 0f, 2.8f), set);
            SpriteObj("KeyBoard_x5", $"{PropDir}/PRP_KeyBoard.png", new Vector3(4.9f, 1.3f, 3.5f), set);
            PointLight("Stove_Light", new Vector3(3f, 0.9f, 2.2f), set, "#E07A3A", 3f, 5f);

            Transform play = Group("--- Gameplay");
            SceneDoor(p, "1-19_to_1-18", "SawmillYard", "from_1-19", "Выйти во двор", $"{PropDir}/DOOR_Interior.png", 1f, play);
            Spawn("from_1-18", 1.8f, play);
            Pickup("Key_Archive_x5", 4.9f, play, items.ArchiveKey, 1, StoryFlags.ArchiveKeyTaken, $"{PropDir}/PRP_TokenGlint.png");

            EditorSceneManager.SaveScene(scene, HutScene);
        }

        // 1-06: the ledger — half of Key truth 1.
        private static void BuildArchive(Prefabs p, Dictionary<string, RuntimeAnimatorController> ac, VolumeProfile profile, Items items)
        {
            Scene scene = NewInterior(p, profile, ref items, 14f, 12f, 0, out _);

            Transform set = Group("--- Set Dressing");
            SpriteObj("Shelves_1-7", $"{PropDir}/PRP_ArchiveShelves.png", new Vector3(4f, 0f, 3.2f), set);
            SpriteObj("LedgerDesk_x9", $"{PropDir}/PRP_LedgerDesk.png", new Vector3(9f, 0f, 2.4f), set);
            SpriteObj("DeskLamp_x8", $"{PropDir}/PRP_DeskLampGreen.png", new Vector3(8.4f, 1.2f, 2.3f), set);
            PointLight("DeskLamp_Light", new Vector3(8.4f, 1.8f, 1.8f), set, "#9BC79A", 4f, 7f);

            Transform play = Group("--- Gameplay");
            Stub(p, "1-06_to_1-05", "1-05", "Лестница в контору", "Дверь в контору заперта на засов снаружи.", $"{PropDir}/DOOR_Cellar_Closed.png", 1f, play);
            SceneDoor(p, "1-06_to_1-07", "OfficeBackYard", "from_1-06", "Люк во двор", $"{PropDir}/DOOR_Interior.png", 13f, play);
            Spawn("from_1-07", 12f, play);
            GameObject ledger = InspectSpot("Clue_Ledger_x9", new Vector3(9f, 0f, 0f), play, 1.6f);

            Transform story = Group("--- Story");
            CutsceneSequence read = Sequence("Clue_Ledger", story, "clue_ledger",
                WalkTo(8.8f),
                Inner("Гроссбух добычи серебра. 1882, 1906, 1936: перед каждым обвалом жила иссякает."),
                Inner("На полях одна и та же пометка: «Д. уплачена». И время: 4:17."),
                Inner("Через месяц после обвалов 1882 и 1906 находят новую богатую жилу. В 1936-м её нет."),
                Note("Улика в блокноте: гроссбух. Сопоставьте его с книгой смертей (Tab)", 5f));
            Talk(ledger, "Открыть гроссбух", "", "Гроссбух компании. Выписки уже у меня.").AddBranch(read, null, null);

            EditorSceneManager.SaveScene(scene, ArchiveScene);
        }
    }
}
