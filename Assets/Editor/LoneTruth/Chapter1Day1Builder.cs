using System.Collections.Generic;
using LoneTruth.Core;
using LoneTruth.Interaction;
using LoneTruth.Story;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LoneTruth.EditorTools
{
    /// <summary>
    /// Chapter 1, day 1 «Свободное расследование» (design doc 8.2 step 4; maps «Карты и квесты»):
    /// 1-08 sheriff's office, 1-09 chapel nave, 1-14 alley, 1-15 Agnes's house, 1-16 the shed.
    /// Day-1 beats only: the report and «Взрывали в 4:17», the starts of side quests 1.1–1.3.
    /// Night steps, the cemetery, crypt, belfry, vestry and the air shaft stay closed stubs.
    /// </summary>
    public static partial class Chapter1LocationBuilder
    {
        private const string SheriffScene = SceneDir + "/SheriffOffice.unity";
        private const string ChapelScene = SceneDir + "/ChapelNave.unity";
        private const string AlleyScene = SceneDir + "/Alley.unity";
        private const string AgnesScene = SceneDir + "/AgnesHouse.unity";
        private const string ShedScene = SceneDir + "/Shed.unity";
        private const string AlleyEnvDir = EnvDir + "/Alley";

        // Looks from «Персонажи и локации».
        private static Human Dolan => new Human
        {
            Height = 1.74f, Coat = PlaceholderPainter.C("#6E6656"), Trousers = PlaceholderPainter.C("#3A3530"),
            Hat = PlaceholderPainter.C("#5A5248"), Accent = PlaceholderPainter.C("#B08D3E"), Skin = PlaceholderPainter.C("#B89880"),
            HatStyle = 6, LongCoat = false, Width = 1.55f
        };

        private static Human Agnes => new Human
        {
            Height = 1.63f, Coat = PlaceholderPainter.C("#6B6156"), Trousers = PlaceholderPainter.C("#5E554B"),
            Hat = PlaceholderPainter.C("#4A3828"), Accent = PlaceholderPainter.C("#8A7E6A"), Skin = PlaceholderPainter.C("#C2AE98"),
            HatStyle = 5, LongCoat = true, Width = 0.88f
        };

        private static Human Matthew => new Human
        {
            Height = 1.88f, Coat = PlaceholderPainter.C("#1C1C20"), Trousers = PlaceholderPainter.C("#1C1C20"),
            Hat = PlaceholderPainter.C("#B9B4A8"), Accent = PlaceholderPainter.C("#E8E4DA"), Skin = PlaceholderPainter.C("#BCA790"),
            HatStyle = 6, LongCoat = true, Width = 0.85f, Glasses = true
        };

        private static Human Jesse => new Human
        {
            Height = 1.76f, Coat = PlaceholderPainter.C("#6B6252"), Trousers = PlaceholderPainter.C("#2E2B28"),
            Hat = PlaceholderPainter.C("#3A3026"), Accent = PlaceholderPainter.C("#8A2E2A"), Skin = PlaceholderPainter.C("#B39A84"),
            HatStyle = 0, LongCoat = false
        };

        // ------------------------------------------------------------------ placeholder art

        private static void DrawDay1Placeholders()
        {
            var people = new (string Key, Human Look, int Seed)[] { ("Dolan", Dolan, 130), ("Agnes", Agnes, 140), ("Matthew", Matthew, 150), ("Jesse", Jesse, 160) };
            foreach (var (key, look, seed) in people)
            {
                Char($"CH_{key}_Ref", look, -1, 0, 0, seed);
                for (int i = 0; i < 2; i++) Char($"CH_{key}_Idle_{i:00}", look, -1, 0, i, seed + 1 + i);
            }

            Wall("TEX_Wall_Sheriff", "#4A4436", "#3A352A", false);
            Wall("TEX_Wall_Chapel", "#6B6458", "#57514A", false);
            Wall("TEX_Wall_House", "#5A4E44", "#4A4038", true);
            Wall("TEX_Wall_Shed", "#3E3428", "#2E261E", true);

            // 1-08 sheriff
            Prop("PRP_SheriffDesk", 2.2f, 1.0f, p =>
            {
                p.Rect(0, 0, 26, 82, PlaceholderPainter.C("#3A2E24"));
                p.Rect(p.W - 56, 0, 56, 82, PlaceholderPainter.C("#3A2E24"));
                p.Rect(0, 82, p.W, 14, PlaceholderPainter.C("#54412F"));
                p.Rect(36, 96, 60, 5, PlaceholderPainter.C("#C9BFA9"));          // papers
                p.Rect(140, 96, 52, 4, PlaceholderPainter.C("#3B302A"));         // hat brim
                p.Rect(150, 100, 32, 12, PlaceholderPainter.C("#3B302A"));       // hat on the desk
            });
            Prop("PRP_WantedBoard", 1.4f, 1.1f, p =>
            {
                p.Rect(0, 0, p.W, p.H, PlaceholderPainter.C("#4A3A2C"));
                for (int i = 0; i < 4; i++) p.Rect(14 + i * 32, 24 + (i % 2) * 18, 26, 34, PlaceholderPainter.C("#CFC5B0"));
            });
            Prop("PRP_JailCells", 5f, 2.6f, p =>
            {
                Color iron = PlaceholderPainter.C("#202226");
                p.Rect(0, 0, p.W, 10, iron);
                p.Rect(0, p.H - 12, p.W, 12, iron);
                for (int x = 0; x < p.W; x += 22) p.Rect(x, 0, 6, p.H, iron);
                p.Rect(p.W / 2f - 5, 0, 10, p.H, iron);                           // the wall between the two cells
                p.Rect(40, 20, 120, 30, PlaceholderPainter.C("#3E3A34"));         // bunk
            });
            Prop("PRP_Spittoon", 0.35f, 0.35f, p => p.Ellipse(17, 14, 15, 13, PlaceholderPainter.C("#8A6A3E")));

            // 1-09 chapel
            Prop("PRP_Pew", 3f, 0.95f, p =>
            {
                Color wood = PlaceholderPainter.C("#3E3026");
                p.Rect(0, 40, p.W, 10, wood);
                p.Rect(0, 50, p.W, 45, PlaceholderPainter.C("#33281F"));
                p.Rect(10, 0, 10, 40, wood); p.Rect(p.W - 20, 0, 10, 40, wood);
            });
            Prop("PRP_Altar", 2.2f, 1.25f, p =>
            {
                p.Rect(10, 0, p.W - 20, 95, PlaceholderPainter.C("#4A3A2C"));
                p.Rect(0, 95, p.W, 12, PlaceholderPainter.C("#D8CFBE"));         // altar cloth
                for (int i = 0; i < 5; i++)
                {
                    p.Rect(30 + i * 38, 107, 7, 14, PlaceholderPainter.C("#E8E1CF"));
                    p.Ellipse(33 + i * 38, 124, 3, 4, PlaceholderPainter.C("#F2C26B"));
                }
            });
            Prop("PRP_StainedGlass", 1.4f, 2.4f, p =>
            {
                p.Rect(0, 0, p.W, p.H, PlaceholderPainter.C("#2A2622"));
                p.Rect(10, 10, p.W - 20, p.H - 70, PlaceholderPainter.C("#2E4A6B"));
                p.Ellipse(p.W / 2f, p.H - 60, p.W / 2f - 10, 50, PlaceholderPainter.C("#2E4A6B"));
                p.Trap(p.W / 2f, 30, 50, 30, 110, PlaceholderPainter.C("#6B2E2A"));  // St Barbara's robe
                p.Ellipse(p.W / 2f, 150, 14, 16, PlaceholderPainter.C("#D8C7A0"));
                p.Rect(p.W / 2f + 26, 40, 14, 60, PlaceholderPainter.C("#8A8F95"));   // her tower
                p.Ellipse(p.W / 2f - 34, 90, 8, 10, PlaceholderPainter.C("#F2C26B")); // her lamp
            });
            Prop("PRP_NamePlaques", 1.8f, 1.1f, p =>
            {
                for (int i = 0; i < 3; i++)
                {
                    float x = 6 + i * 58;
                    p.Rect(x, 6, 52, 96, PlaceholderPainter.C("#6B5528"));
                    for (int y = 16; y < 92; y += 9) p.Rect(x + 8, y, 36, 3, PlaceholderPainter.C("#2A2210"));
                }
            });
            Prop("PRP_Counterweight", 0.5f, 3.4f, p =>
            {
                p.Rect(22, 60, 6, p.H - 60, PlaceholderPainter.C("#2A2B2E"));     // chain
                p.Rect(6, 0, 38, 60, PlaceholderPainter.C("#3E4044"));            // weight
            });
            Prop("PRP_FloorHatch", 1.3f, 0.2f, p =>
            {
                p.Rect(0, 0, p.W, p.H, PlaceholderPainter.C("#2A221C"));
                p.Rect(p.W / 2f - 10, 6, 20, 6, PlaceholderPainter.C("#6A6D74"));
            });
            Prop("PRP_BelfryLadder", 0.8f, 3.6f, p =>
            {
                Color wood = PlaceholderPainter.C("#3B2E24");
                p.Rect(6, 0, 8, p.H, wood); p.Rect(p.W - 14, 0, 8, p.H, wood);
                for (int y = 20; y < p.H; y += 40) p.Rect(6, y, p.W - 12, 6, wood);
                p.Rect(0, p.H - 16, p.W, 16, PlaceholderPainter.C("#2A221C"));    // the hatch above
            });

            // 1-15 Agnes's house
            Prop("PRP_Stove", 1.1f, 1.5f, p =>
            {
                p.Rect(0, 0, p.W, 80, PlaceholderPainter.C("#24262A"));
                p.Rect(10, 20, 40, 34, PlaceholderPainter.C("#6B3A1E"));          // the fire door
                p.Rect(p.W - 24, 80, 14, 70, PlaceholderPainter.C("#24262A"));    // flue
            });
            Prop("PRP_Dresser", 1.2f, 1.05f, p =>
            {
                p.Rect(0, 0, p.W, p.H - 6, PlaceholderPainter.C("#4A3A2C"));
                for (int y = 12; y < p.H - 10; y += 30) p.Frame(8, y, p.W - 16, 24, 2, PlaceholderPainter.C("#2E241C"));
            });
            Prop("PRP_KitchenTable", 1.4f, 0.9f, p =>
            {
                Color wood = PlaceholderPainter.C("#4A3A2C");
                p.Rect(0, 68, p.W, 10, wood);
                p.Rect(8, 0, 8, 68, wood); p.Rect(p.W - 16, 0, 8, 68, wood);
                p.Rect(50, 78, 18, 12, PlaceholderPainter.C("#BDB7AA"));          // a cup
            });
            Prop("PRP_WindowLetters", 1.3f, 1.6f, p =>
            {
                p.Rect(0, 0, p.W, p.H, PlaceholderPainter.C("#2A2622"));
                p.Rect(10, 20, p.W - 20, p.H - 30, PlaceholderPainter.C("#3A4A5A"));
                p.Rect(p.W / 2f - 3, 20, 6, p.H - 30, PlaceholderPainter.C("#2A2622"));
                p.Rect(0, 0, p.W, 20, PlaceholderPainter.C("#4A3A2C"));           // sill
                for (int i = 0; i < 7; i++) p.Rect(20 + i * 13, 20, 11, 8 + i % 3 * 2, PlaceholderPainter.C("#D8CFBE")); // the letters
            });
            Prop("PRP_CarbideLamp", 0.4f, 0.6f, p =>
            {
                p.Rect(14, 0, 14, 30, PlaceholderPainter.C("#8A6A3E"));
                p.Ellipse(21, 36, 12, 8, PlaceholderPainter.C("#B08D3E"));
                p.Rect(19, 44, 4, 14, PlaceholderPainter.C("#2A2B2E"));           // the hook
            });

            // 1-16 shed
            Prop("PRP_HayPile", 3f, 0.75f, p =>
            {
                p.Ellipse(p.W / 2f, 0, p.W / 2f, 70, PlaceholderPainter.C("#8A7444"));
                for (int i = 0; i < 40; i++) p.Rect(p.Rand(30, p.W - 30), p.Rand(0, 55), 2, 12, PlaceholderPainter.C("#A88F55"));
            });
            Prop("PRP_HayBales", 2.4f, 1.3f, p =>
            {
                for (int i = 0; i < 3; i++) p.Rect(i * 80, 0, 74, 60, PlaceholderPainter.C("#7A6640"));
                for (int i = 0; i < 2; i++) p.Rect(40 + i * 80, 60, 74, 60, PlaceholderPainter.C("#7A6640"));
            });

            // 1-14 alley (exterior)
            Prop("FAC_AgnesHouse", 7f, 4.2f, p =>
            {
                p.Rect(0, 0, p.W, 330, PlaceholderPainter.C("#3A3530"));
                p.Trap(p.W / 2f, 330, p.W + 20, 40, 90, PlaceholderPainter.C("#2A2622"));   // roof
                for (int y = 20; y < 330; y += 26) p.Rect(0, y, p.W, 3, PlaceholderPainter.C("#2E2A26"));
                p.Rect(420, 110, 120, 110, PlaceholderPainter.C("#E0B46A"));   // the lit kitchen window (x ≈ +1 m right of the door)
                p.Rect(478, 110, 6, 110, PlaceholderPainter.C("#3A3530"));
            }, "Alley");
            Prop("FAC_Stable", 7.5f, 4.6f, p =>
            {
                p.Rect(0, 0, p.W, 360, PlaceholderPainter.C("#3E3428"));
                p.Trap(p.W / 2f, 360, p.W + 30, 60, 100, PlaceholderPainter.C("#2A2420"));
                for (int x = 0; x < p.W; x += 30) p.Rect(x, 0, 4, 360, PlaceholderPainter.C("#2E261E"));
                p.Rect(80, 120, 140, 90, PlaceholderPainter.C("#1E1A16"));     // a stall opening
            }, "Alley");
            Prop("PRP_LaundryLine", 3.4f, 2.4f, p =>
            {
                Color pole = PlaceholderPainter.C("#3B2E24");
                p.Rect(0, 0, 8, p.H, pole); p.Rect(p.W - 8, 0, 8, p.H, pole);
                p.Rect(8, p.H - 30, p.W - 16, 3, PlaceholderPainter.C("#6A6458"));
                p.Rect(50, p.H - 110, 60, 80, PlaceholderPainter.C("#BDB7AA"));
                p.Rect(150, p.H - 90, 40, 60, PlaceholderPainter.C("#7A8A96"));
                p.Rect(230, p.H - 120, 50, 90, PlaceholderPainter.C("#A89A80"));
            }, "Alley");
            Prop("DOOR_AgnesHouse", 1.0f, 2.1f, p =>
            {
                p.Rect(0, 0, p.W, p.H, PlaceholderPainter.C("#3E3026"));
                for (int x = 12; x < p.W; x += 22) p.Rect(x, 0, 3, p.H, PlaceholderPainter.C("#2A1E16"));
                p.Rect(p.W - 22, 100, 8, 8, PlaceholderPainter.C("#8A8F95"));
            });
            Prop("DOOR_Shed", 1.8f, 2.4f, p =>
            {
                p.Rect(0, 0, p.W, p.H, PlaceholderPainter.C("#3B3024"));
                for (int x = 14; x < p.W; x += 26) p.Rect(x, 0, 3, p.H, PlaceholderPainter.C("#241C14"));
                p.Rect(0, 100, p.W, 10, PlaceholderPainter.C("#241C14"));
                p.Rect(p.W / 2f - 2, 0, 4, p.H, PlaceholderPainter.C("#1A140E"));
            });
        }

        // 0 sheriff wall, 1 chapel wall, 2 house wall, 3 shed wall
        private static Material[] BuildDay1Materials()
        {
            Material WallMat(string name, string tex)
            {
                Material m = Mat(name, "Universal Render Pipeline/Lit", tex, new Vector2(6, 1), 0.1f);
                m.SetColor("_BaseColor", new Color(0.85f, 0.82f, 0.78f));
                return m;
            }

            return new[]
            {
                WallMat("M_Wall_Sheriff", $"{InteriorDir}/TEX_Wall_Sheriff.png"),
                WallMat("M_Wall_Chapel", $"{InteriorDir}/TEX_Wall_Chapel.png"),
                WallMat("M_Wall_House", $"{InteriorDir}/TEX_Wall_House.png"),
                WallMat("M_Wall_Shed", $"{InteriorDir}/TEX_Wall_Shed.png")
            };
        }

        /// <summary>Placeholder art faces right; the door (and Holt coming in) is on the left.</summary>
        private static void FaceDoor(GameObject npc)
        {
            npc.GetComponentInChildren<SpriteRenderer>().flipX = true;
        }

        private static void BuildDay1Scenes(Prefabs p, Dictionary<string, RuntimeAnimatorController> ac, VolumeProfile profile, Items items)
        {
            BuildSheriff(p, ac, profile, items);
            BuildChapel(p, ac, profile, items);
            BuildAlley(p, ac, profile, items);
            BuildAgnesHouse(p, ac, profile, items);
            BuildShed(p, ac, profile, items);
        }

        /// <summary>Opens a new interior scene with the shared systems, its walls and bounds.</summary>
        private static Scene NewInterior(Prefabs p, VolumeProfile profile, ref Items items, float width, float playerX, int wall, out Material[] im)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // A Single-mode NewScene unloads assets nothing references any more: reload them by path.
            ReloadAssets(ref items, ref profile);
            im = BuildInteriorMaterials();
            Material[] walls = BuildDay1Materials();
            float camMin = Mathf.Min(6f, width / 2f), camMax = Mathf.Max(width - 6f, width / 2f);
            CommonSystems(p, playerX, camMin, camMax, profile);
            SetupTimeOfDay(false, null, null);
            InteriorShell(width, walls[wall], im[3], Group("--- Geometry"));
            AddBounds(width, Group("--- Collision & Bounds"));
            return scene;
        }

        // ------------------------------------------------------------------ 1-08 Sheriff's office

        private static void BuildSheriff(Prefabs p, Dictionary<string, RuntimeAnimatorController> ac, VolumeProfile profile, Items items)
        {
            Scene scene = NewInterior(p, profile, ref items, 16f, 2.5f, 0, out _);

            Transform set = Group("--- Set Dressing");
            SpriteObj("SheriffDesk_x6", $"{PropDir}/PRP_SheriffDesk.png", new Vector3(6f, 0f, 1.5f), set);
            SpriteObj("Spittoon_x7.6", $"{PropDir}/PRP_Spittoon.png", new Vector3(7.6f, 0f, 1.2f), set);
            SpriteObj("WantedBoard_x8", $"{PropDir}/PRP_WantedBoard.png", new Vector3(8.4f, 1.3f, 3.5f), set);
            SpriteObj("JailCells_11-15", $"{PropDir}/PRP_JailCells.png", new Vector3(13.2f, 0f, 3.1f), set);
            PointLight("Lamp_Light", new Vector3(6f, 3.4f, 1.4f), set, "#E3C893", 4f, 7f);
            PointLight("Cells_Light", new Vector3(13f, 3.2f, 2.2f), set, "#9AA6B5", 1.6f, 5f);

            Transform play = Group("--- Gameplay");
            SceneDoor(p, "1-08_to_1-02", "MainStreet", "from_1-08", "Выйти на улицу", $"{PropDir}/DOOR_Interior.png", 1f, play);
            Spawn("from_1-02", 2.5f, play);
            GameObject dolan = Npc(p, ac, "NPC_Dolan_x6", "Dolan", new Vector3(6f, 0f, 0f), play, "Поговорить", "", 2.2f);
            FaceDoor(dolan);
            GameObject cells = InspectSpot("Cells_x13", new Vector3(13f, 0f, 0f), play, 2f);

            Transform story = Group("--- Story");
            // Step 4: the report and the slip — «Взрывали в 4:17» — against father's watch.
            CutsceneSequence report = Sequence("4b_SheriffReport", story, StoryFlags.SheriffReport,
                WalkTo(4.6f),
                Line("Шериф Долан", "Чикагский сыщик. Компания предупреждала, что вы зайдёте."),
                Line("Холт", "Мне нужен отчёт об обвале."),
                Line("Шериф Долан", "Отчёт… Вот. Подписан, заверен. Несчастный случай, двадцать три человека."),
                Flag(StoryFlags.ClueCollapseReport),
                Note("Улика в блокноте: отчёт шерифа об обвале"),
                Line("Холт", "В котором часу это случилось?"),
                Line("Шериф Долан", "Ночью. Взрывали в 4:17."),
                Line("Шериф Долан", "Взрывали, я сказал? Обрушилось. Обрушилось в 4:17."),
                Inner("4:17. Часы отца остановились в 4:17 — когда его самого завалило в этой шахте тридцать лет назад."),
                Inner("Совпадение. Наверное."),
                Goal("Свободное расследование. Закончив дела в городе — отдохнуть в номере до темноты."));
            Talk(dolan, "Поговорить", "Шериф Долан", "Отчёт у вас, мистер Холт. Больше мне сказать нечего.")
                .AddBranch(report, null, F(StoryFlags.SheriffReport));

            Talk(cells, "Осмотреть", "", "Две пустые камеры. Пахнет хлоркой и виски.");

            EditorSceneManager.SaveScene(scene, SheriffScene);
        }

        // ------------------------------------------------------------------ 1-09 Chapel nave

        private static void BuildChapel(Prefabs p, Dictionary<string, RuntimeAnimatorController> ac, VolumeProfile profile, Items items)
        {
            Scene scene = NewInterior(p, profile, ref items, 22f, 2.5f, 1, out _);

            Transform set = Group("--- Set Dressing");
            SpriteObj("StainedGlass_StBarbara_x8.5", $"{PropDir}/PRP_StainedGlass.png", new Vector3(8.5f, 1.4f, 3.5f), set);
            SpriteObj("Altar_x8.5", $"{PropDir}/PRP_Altar.png", new Vector3(8.5f, 0f, 3.1f), set);
            SpriteObj("NamePlaques_x11", $"{PropDir}/PRP_NamePlaques.png", new Vector3(11f, 1.3f, 3.5f), set);
            SpriteObj("Counterweight_x14", $"{PropDir}/PRP_Counterweight.png", new Vector3(14f, 0.4f, 3.3f), set);
            foreach (float x in new[] { 4.5f, 12.5f, 16f })
            {
                SpriteObj($"Pew_x{x:0.#}", $"{PropDir}/PRP_Pew.png", new Vector3(x, 0f, 1.9f), set);
            }

            PointLight("Candles_Light", new Vector3(8.5f, 1.6f, 2.4f), set, "#F2B45A", 3.5f, 6f);
            PointLight("StainedGlass_Light", new Vector3(8.5f, 2.8f, 2.6f), set, "#7A90B8", 2f, 6f);
            PointLight("Nave_Light", new Vector3(15f, 3.6f, 1.6f), set, "#D8C090", 2.5f, 8f);

            Transform play = Group("--- Gameplay");
            SceneDoor(p, "1-09_to_1-02", "MainStreet", "from_1-09", "Выйти на улицу", $"{PropDir}/DOOR_Interior.png", 1f, play);
            Spawn("from_1-02", 2.5f, play);
            Stub(p, "1-09_to_1-13", "1-13", "Кладбище", "Задняя дверь на кладбище заперта.", $"{PropDir}/DOOR_BackDoor_Closed.png", 5f, play);
            GameObject crypt = Stub(p, "1-09_to_1-11", "1-11", "Люк в крипту", "Внизу темно. Без лампы туда лучше не лезть.", $"{PropDir}/PRP_FloorHatch.png", 17f, play);
            crypt.transform.Find("DoorArt").localPosition = new Vector3(0f, 0f, 0.6f); // the hatch is in the floor
            GameObject belfry = Stub(p, "1-09_to_1-12", "1-12", "Люк колокольни", "Люк колокольни заперт. Ключ, говорят, только у отца Мэтью.", $"{PropDir}/PRP_BelfryLadder.png", 19f, play);
            belfry.transform.Find("DoorArt").localPosition = new Vector3(0f, 0f, 2.8f);
            Stub(p, "1-09_to_1-10", "1-10", "Ризница", "Ризница заперта.", $"{PropDir}/DOOR_Interior.png", 21f, play);

            GameObject matthew = Npc(p, ac, "NPC_Matthew_x9", "Matthew", new Vector3(9f, 0f, 0f), play, "Поговорить", "");
            FaceDoor(matthew);
            GameObject plaques = InspectSpot("Clue_Plaques_x11", new Vector3(11f, 0f, 0f), play);
            GameObject weight = InspectSpot("Counterweight_x14", new Vector3(14f, 0f, 0f), play, 0.8f);

            Transform story = Group("--- Story");
            // Side quest 1.3 «Ночной колокол»: the start.
            CutsceneSequence bell = Sequence("Q1_3_Start", story, StoryFlags.Quest13Started,
                WalkTo(7.6f),
                Line("Отец Мэтью", "Мир вам, сын мой. Вы ведь тот сыщик из Чикаго?"),
                Line("Холт", "Сэм Холт. Говорят, у вас колокол звонит сам по себе."),
                Line("Отец Мэтью", "Каждую ночь. В 4:17. А колокольня заперта, и ключ только у меня."),
                Line("Отец Мэтью", "Узнайте, кто звонит, мистер Холт. Прошу вас. Только… это слышно лишь ночью."),
                Inner("Опять 4:17. Он чего-то боится — и, кажется, не колокола."),
                Note("Новое дело в блокноте: «Ночной колокол»"));
            Talk(matthew, "Поговорить", "Отец Мэтью", "Колокол звонит в 4:17, мистер Холт. Ночью вы всё услышите сами.")
                .AddBranch(bell, null, F(StoryFlags.Quest13Started));

            CutsceneSequence plaqueClue = Sequence("Clue_NamePlaques", story, StoryFlags.ClueNamePlaques,
                Inner("Таблички с именами погибших шахтёров. 1882 — двенадцать имён. 1906 — семнадцать."),
                Inner("И новая, 1936-го: двадцать три. Краска ещё свежая."),
                Note("Улика в блокноте: таблички с именами погибших"));
            Talk(plaques, "Осмотреть", "", "Таблички с именами погибших: 1882, 1906, 1936.").AddBranch(plaqueClue, null, null);

            Talk(weight, "Осмотреть", "", "Противовес на цепи. Цепь уходит под потолок, к колокольне.");

            EditorSceneManager.SaveScene(scene, ChapelScene);
        }

        // ------------------------------------------------------------------ 1-14 Alley (exterior)

        private static void BuildAlley(Prefabs p, Dictionary<string, RuntimeAnimatorController> ac, VolumeProfile profile, Items items)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ReloadAssets(ref items, ref profile);
            Material[] mats = BuildMaterials();
            CommonSystems(p, 2.5f, 6f, 14f, profile);

            Transform bg = Group("--- Background");
            Backdrop(mats[0], 10f, bg);
            SetupTimeOfDay(true, mats[0], AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/M_Sky_OvercastDay.mat"));

            Transform geo = Group("--- Geometry");
            Quad("Ground_Mud", mats[2], new Vector3(10f, 0f, 0.5f), new Vector3(90f, 0f, 0f), new Vector3(26f, 8f, 1f), geo);

            Transform fac = Group("--- Facades");
            Color tint = new Color(0.82f, 0.83f, 0.88f);
            SpriteObj("FAC_AgnesHouse_3-10", $"{AlleyEnvDir}/FAC_AgnesHouse.png", new Vector3(6.6f, 0f, 3f), fac, 1f, tint);
            SpriteObj("FAC_Stable_11-18", $"{AlleyEnvDir}/FAC_Stable.png", new Vector3(14.6f, 0f, 3f), fac, 1f, tint);
            GameObject kitchenGlow = PointLight("AgnesWindow_Light", new Vector3(8f, 1.8f, 2.2f), fac, "#E8A84E", 2.5f, 5f);

            Transform set = Group("--- Set Dressing");
            SpriteObj("LaundryLine_x10.6", $"{AlleyEnvDir}/PRP_LaundryLine.png", new Vector3(10.6f, 0f, 2.5f), set);
            PropInst(p, "Barrels_x2.6", $"{PropDir}/PRP_Barrels.png", new Vector3(2.6f, 0f, 2.2f), set);

            Transform fg = Group("--- Foreground (silhouettes)");
            PropInst(p, "FG_Crates_x12", $"{PropDir}/PRP_CrateStack.png", new Vector3(12f, -1.0f, -3f), fg, 1.1f, new Color(0.12f, 0.12f, 0.14f));

            Transform play = Group("--- Gameplay");
            SceneDoor(p, "1-14_to_1-02", "MainStreet", "from_1-14", "Выйти на Главную улицу", $"{PropDir}/DOOR_AlleyGate_Closed.png", 1f, play);
            SceneDoor(p, "1-14_to_1-15", "AgnesHouse", "from_1-14", "Дом Агнес Бирн", $"{PropDir}/DOOR_AgnesHouse.png", 6f, play);
            SceneDoor(p, "1-14_to_1-16", "Shed", "from_1-14", "Сарай за конюшней", $"{PropDir}/DOOR_Shed.png", 14f, play);
            Stub(p, "1-14_to_1-17", "1-17", "Тропа на окраину", "Дальше пустырь и отвалы шахты. Сейчас мне там делать нечего.", $"{PropDir}/DOOR_AlleyGate_Closed.png", 19f, play);
            Spawn("from_1-02", 2.5f, play);
            Spawn("from_1-15", 6f, play);
            Spawn("from_1-16", 14f, play);

            Transform story = Group("--- Story");
            Gate("KitchenLightAtNight", story, null, F(StoryFlags.TimeDay), kitchenGlow);

            Transform fx = Group("--- FX");
            Inst(p.Rain, "Rain", new Vector3(10f, 9f, 1f), fx);

            AddBounds(20f, Group("--- Collision & Bounds"));
            EditorSceneManager.SaveScene(scene, AlleyScene);
        }

        // ------------------------------------------------------------------ 1-15 Agnes's house

        private static void BuildAgnesHouse(Prefabs p, Dictionary<string, RuntimeAnimatorController> ac, VolumeProfile profile, Items items)
        {
            Scene scene = NewInterior(p, profile, ref items, 12f, 2.5f, 2, out _);

            Transform set = Group("--- Set Dressing");
            SpriteObj("Stove_x2.6", $"{PropDir}/PRP_Stove.png", new Vector3(2.6f, 0f, 3.1f), set);
            SpriteObj("Dresser_x4", $"{PropDir}/PRP_Dresser.png", new Vector3(4.2f, 0f, 3.1f), set);
            SpriteObj("KitchenTable_x7", $"{PropDir}/PRP_KitchenTable.png", new Vector3(7.2f, 0f, 2f), set);
            SpriteObj("WindowLetters_x9", $"{PropDir}/PRP_WindowLetters.png", new Vector3(9f, 1f, 3.5f), set);
            SpriteObj("CarbideLamp_x11", $"{PropDir}/PRP_CarbideLamp.png", new Vector3(11f, 1.4f, 3.5f), set);
            PointLight("Stove_Light", new Vector3(2.6f, 0.8f, 2.4f), set, "#E07A3A", 2.5f, 4.5f);
            PointLight("Table_Light", new Vector3(7f, 2.6f, 1.4f), set, "#E3C893", 3.5f, 6f);
            PointLight("Window_Light", new Vector3(9f, 2f, 2.6f), set, "#9AA6B5", 1.6f, 4f);

            Transform play = Group("--- Gameplay");
            SceneDoor(p, "1-15_to_1-14", "Alley", "from_1-15", "Выйти в переулок", $"{PropDir}/DOOR_Interior.png", 1f, play);
            Spawn("from_1-14", 2.5f, play);
            GameObject agnes = Npc(p, ac, "NPC_Agnes_x6", "Agnes", new Vector3(6f, 0f, 0f), play, "Поговорить", "");
            FaceDoor(agnes);
            GameObject dresser = InspectSpot("Clue_OldLetter_x4", new Vector3(4f, 0f, 0f), play);
            GameObject window = InspectSpot("Clue_Letters_x9", new Vector3(9f, 0f, 0f), play);
            GameObject lamp = InspectSpot("CarbideLamp_x11", new Vector3(11f, 0f, 0f), play, 0.8f);

            Transform story = Group("--- Story");
            // Side quest 1.1 «Письма из-под земли»: the start.
            CutsceneSequence letters = Sequence("Q1_1_Start", story, StoryFlags.Quest11Started,
                WalkTo(4.8f),
                Line("Агнес Бирн", "Мистер Холт? Вы приехали. Я знала, что приедете."),
                Line("Холт", "Ваше письмо, миссис Бирн. Томас пишет вам?"),
                Line("Агнес Бирн", "Семь писем. Его рука, его слова. И угольная пыль на сгибах."),
                Line("Агнес Бирн", "Они появляются на подоконнике. Каждую ночь. Я ни разу не видела, кто их приносит."),
                Line("Агнес Бирн", "Компания говорит — завалило. Двадцать три человека. А мой муж пишет мне по ночам."),
                Inner("Кто-то же их приносит. Ночью стоит подождать в тени у её окна."),
                Note("Новое дело в блокноте: «Письма из-под земли»"));
            Talk(agnes, "Поговорить", "Агнес Бирн", "Он жив, мистер Холт. Я знаю.")
                .AddBranch(letters, null, F(StoryFlags.Quest11Started));

            CutsceneSequence windowClue = Sequence("Clue_ThomasLetters", story, StoryFlags.ClueThomasLetters,
                Inner("Семь писем, почерк Томаса. Угольная пыль на сгибах, бумага пахнет шахтой."),
                Note("Улика в блокноте: письма Томаса"));
            Talk(window, "Осмотреть", "", "Пачка писем на подоконнике. Чужие письма без спроса не читают.")
                .AddBranch(windowClue, F(StoryFlags.Quest11Started), null);

            CutsceneSequence oldLetter = Sequence("Clue_ThomasOldLetter", story, StoryFlags.ClueThomasOldLetter,
                Line("Агнес Бирн", "В комоде — его письмо, ещё с тех пор, как ухаживал. Сравните, если хотите."),
                Inner("Старое письмо Томаса. Тот же наклон, та же рука, что и в ночных письмах."),
                Note("Улика в блокноте: старое письмо Томаса"));
            Talk(dresser, "Осмотреть", "", "Комод. Чужие вещи без спроса не трогают.")
                .AddBranch(oldLetter, F(StoryFlags.Quest11Started), null);

            Talk(lamp, "Осмотреть", "", "Карбидная лампа Томаса висит на гвозде у двери.");

            EditorSceneManager.SaveScene(scene, AgnesScene);
        }

        // ------------------------------------------------------------------ 1-16 The shed

        private static void BuildShed(Prefabs p, Dictionary<string, RuntimeAnimatorController> ac, VolumeProfile profile, Items items)
        {
            Scene scene = NewInterior(p, profile, ref items, 12f, 2.5f, 3, out _);

            Transform set = Group("--- Set Dressing");
            SpriteObj("HayBales_x3", $"{PropDir}/PRP_HayBales.png", new Vector3(3.4f, 0f, 3.1f), set);
            SpriteObj("HayPile_x9.5", $"{PropDir}/PRP_HayPile.png", new Vector3(9.5f, 0f, 0.7f), set);
            PointLight("Cracks_Light", new Vector3(8f, 3.4f, 1.4f), set, "#8A96A6", 2.2f, 7f);

            Transform play = Group("--- Gameplay");
            SceneDoor(p, "1-16_to_1-14", "Alley", "from_1-16", "Выйти в переулок", $"{PropDir}/DOOR_Shed.png", 1f, play);
            Spawn("from_1-14", 2.5f, play);
            GameObject jesse = Npc(p, ac, "NPC_Jesse_x9", "Jesse", new Vector3(9f, 0f, 0f), play, "Поговорить", "", 1.2f);
            FaceDoor(jesse);

            Transform story = Group("--- Story");
            // Side quest 1.2 «Раненый в сарае» (design doc 8.4). Jesse aims until the revolver is put
            // away («NPC реагируют настороженно»); then the first contact and the deal are dialogues
            // with answers. With the revolver drawn he always cuts the talk short.
            CutsceneSequence gunDrawn = Sequence("Q1_2_GunDrawn", story, "",
                Line("Джесси Коул", "Стой! Убери ствол, мистер, — или я пальну."),
                Inner("Он на взводе. Револьвер лучше убрать (выберите его и нажмите G)."));
            CutsceneSequence contact = Sequence("Q1_2_FirstContact", story, "", Talking(JesseContactDialogue()));
            CutsceneSequence deal = Sequence("Q1_2_Deal", story, "", Talking(JesseDealDialogue(items)));
            CutsceneSequence afterVestry = Sequence("Q1_2_AfterVestry", story, "",
                Line("Джесси Коул", "Ночью отец Мэтью меня заберёт. Спасибо, мистер."));
            CutsceneSequence afterMap = Sequence("Q1_2_AfterMap", story, "",
                Line("Джесси Коул", "Карта у тебя. Чего ещё?"));
            CutsceneInteractable talk = Talk(jesse, "Поговорить", "Джесси Коул", "Бинт и виски, мистер. И ни слова людям Кэллахана.");
            talk.AddBranch(gunDrawn, F(StoryFlags.WeaponDrawn), null);
            talk.AddBranch(contact, null, F(StoryFlags.Quest12FoundJesse));
            talk.AddBranch(afterVestry, F("jesse_in_vestry"), null);
            talk.AddBranch(afterMap, F("jesse_left_alone"), null);
            talk.AddBranch(deal, F(StoryFlags.Quest12FoundJesse), null);

            EditorSceneManager.SaveScene(scene, ShedScene);
        }
    }
}
