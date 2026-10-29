using System.Collections.Generic;
using LoneTruth.Core;
using LoneTruth.Investigation;
using LoneTruth.Story;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LoneTruth.EditorTools
{
    /// <summary>
    /// 1-07 «Задний двор конторы» (maps «Карты и квесты», sheet B): the muddy yard behind the
    /// company office. The .45 casing for quest 1.2 lies in the mud by the back porch — found by
    /// examining the spot, or seen glinting with the Truth bonus (design doc 8.4). The cellar hatch
    /// to the archive (night, archive key) stays a stub.
    /// </summary>
    public static partial class Chapter1LocationBuilder
    {
        private const string BackYardScene = SceneDir + "/OfficeBackYard.unity";
        private const string BackYardEnvDir = EnvDir + "/BackYard";

        private static void DrawBackYardPlaceholders()
        {
            Prop("FAC_OfficeBack", 9f, 4.8f, p =>
            {
                Color stone = PlaceholderPainter.C("#4A4A48");
                p.Rect(0, 0, p.W, 440, stone);
                for (int y = 0; y < 440; y += 34)
                    for (int x = (y / 34) % 2 * 40; x < p.W; x += 80)
                        p.Rect(x, y, 3, 34, PlaceholderPainter.C("#3A3A38"));
                for (int y = 34; y < 440; y += 34) p.Rect(0, y, p.W, 3, PlaceholderPainter.C("#3A3A38"));
                p.Rect(0, 440, p.W, 40, PlaceholderPainter.C("#2E2E2C"));                 // cornice
                p.Rect(560, 180, 130, 150, PlaceholderPainter.C("#2A3440"));             // back window
                p.Rect(622, 180, 6, 150, PlaceholderPainter.C("#4A4A48"));
            }, "BackYard");
            Prop("PRP_Fence", 4f, 2.2f, p =>
            {
                for (int x = 0; x < p.W; x += 26) p.Rect(x, 0, 22, p.H - (x / 26 % 3) * 6, PlaceholderPainter.C("#3B3024"));
                p.Rect(0, 50, p.W, 8, PlaceholderPainter.C("#2A2018"));
                p.Rect(0, 160, p.W, 8, PlaceholderPainter.C("#2A2018"));
            }, "BackYard");
            Prop("PRP_BackPorch", 2.2f, 0.7f, p =>
            {
                Color wood = PlaceholderPainter.C("#3B3026");
                p.Rect(0, 0, p.W, 24, wood);
                p.Rect(30, 24, p.W - 30, 22, wood);
                p.Rect(60, 46, p.W - 60, 24, wood);
            });
            Prop("PRP_Puddle", 1.6f, 0.12f, p => p.Ellipse(p.W / 2f, 6, p.W / 2f - 4, 6, PlaceholderPainter.C("#1C2228")));
            Prop("PRP_ShellGlint", 0.4f, 0.4f, p =>
            {
                Color glint = new Color(1f, 0.92f, 0.65f, 1f);
                p.Rect(18, 2, 4, 36, glint);
                p.Rect(2, 18, 36, 4, glint);
                p.Ellipse(20, 20, 6, 6, Color.white);
            });
            Prop("PRP_CellarHatch", 1.4f, 0.35f, p =>
            {
                p.Rect(0, 0, p.W, 26, PlaceholderPainter.C("#2E3034"));
                p.Rect(10, 26, p.W - 20, 8, PlaceholderPainter.C("#3E4044"));
                p.Rect(p.W / 2f - 8, 8, 16, 14, PlaceholderPainter.C("#6A6D74"));          // padlock
            });
            Prop("PRP_Cart", 2.6f, 1.5f, p =>
            {
                Color wood = PlaceholderPainter.C("#3E3026");
                p.Rect(20, 50, p.W - 40, 60, wood);
                p.Rect(p.W - 30, 70, 30, 6, wood);                                         // shaft
                p.Ellipse(70, 40, 38, 38, PlaceholderPainter.C("#2A2018"));
                p.Ellipse(70, 40, 26, 26, PlaceholderPainter.C("#3E3026"));
                p.Ellipse(p.W - 90, 40, 38, 38, PlaceholderPainter.C("#2A2018"));
                p.Ellipse(p.W - 90, 40, 26, 26, PlaceholderPainter.C("#3E3026"));
            });
        }

        private static void BuildBackYard(Prefabs p, Dictionary<string, RuntimeAnimatorController> ac, VolumeProfile profile, Items items)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ReloadAssets(ref items, ref profile);
            Material[] mats = BuildMaterials();
            CommonSystems(p, 2.5f, 6f, 10f, profile);

            Transform bg = Group("--- Background");
            Backdrop(mats[0], 8f, bg);
            SetupTimeOfDay(true, mats[0], AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/M_Sky_OvercastDay.mat"));

            Transform geo = Group("--- Geometry");
            Quad("Ground_Mud", mats[2], new Vector3(8f, 0f, 0.5f), new Vector3(90f, 0f, 0f), new Vector3(22f, 8f, 1f), geo);

            Transform fac = Group("--- Facades");
            Color tint = new Color(0.82f, 0.83f, 0.88f);
            SpriteObj("FAC_OfficeBack_0-9", $"{BackYardEnvDir}/FAC_OfficeBack.png", new Vector3(4.5f, 0f, 3f), fac, 1f, tint);
            SpriteObj("Fence_9-13", $"{BackYardEnvDir}/PRP_Fence.png", new Vector3(11f, 0f, 3.2f), fac, 1f, tint);
            SpriteObj("Fence_13-17", $"{BackYardEnvDir}/PRP_Fence.png", new Vector3(15f, 0f, 3.2f), fac, 1f, tint);
            GameObject window = PointLight("BackWindow_Light", new Vector3(6.2f, 2.4f, 2.4f), fac, "#E8C890", 1.6f, 4f);

            Transform set = Group("--- Set Dressing");
            SpriteObj("BackPorch_x2.6", $"{PropDir}/PRP_BackPorch.png", new Vector3(2.4f, 0f, 2.2f), set);
            SpriteObj("Puddle_x4", $"{PropDir}/PRP_Puddle.png", new Vector3(4.2f, 0.01f, 0.6f), set);
            PropInst(p, "Crates_x7", $"{PropDir}/PRP_CrateStack.png", new Vector3(7.2f, 0f, 2.4f), set);
            SpriteObj("Cart_x13", $"{PropDir}/PRP_Cart.png", new Vector3(13f, 0f, 1.6f), set);

            Transform fg = Group("--- Foreground (silhouettes)");
            PropInst(p, "FG_Barrels_x9", $"{PropDir}/PRP_Barrels.png", new Vector3(9f, -1.0f, -3f), fg, 1.1f, new Color(0.12f, 0.12f, 0.14f));

            Transform play = Group("--- Gameplay");
            SceneDoor(p, "1-07_to_1-05", "CompanyOffice", "from_1-07", "Вернуться в контору", $"{PropDir}/DOOR_BackDoor_Closed.png", 1f, play);
            Spawn("from_1-05", 2f, play);
            GameObject hatch = Stub(p, "1-07_to_1-06", "1-06", "Люк в подвал", "Люк заперт на висячий замок. Нужен ключ от архива.", $"{PropDir}/PRP_CellarHatch.png", 10f, play);
            hatch.transform.Find("DoorArt").localPosition = new Vector3(0f, 0f, 1.6f);
            GameObject shellSpot = InspectSpot("Clue_Shell45_x4", new Vector3(4.2f, 0f, 0f), play, 1.4f);
            GameObject cart = InspectSpot("Cart_x13", new Vector3(13f, 0f, 0f), play, 2f);

            // «Взгляд сыщика»: with the Truth bonus the casing glints in the mud until it is picked up.
            var glint = new GameObject("Shell45_Glint");
            glint.transform.SetParent(set, false);
            glint.transform.localPosition = new Vector3(4.4f, 0.03f, -0.2f);
            var glintSprite = glint.AddComponent<SpriteRenderer>();
            glintSprite.sprite = S($"{PropDir}/PRP_ShellGlint.png");
            glintSprite.sortingOrder = 5;
            var glintHost = new GameObject("Shell45_GlintControl");
            glintHost.transform.SetParent(set, false);
            glintHost.AddComponent<HiddenClueGlint>().Configure("clue_shell_45", glint);

            Transform story = Group("--- Story");
            CutsceneSequence shell = Sequence("Clue_Shell45", story, "clue_shell_45",
                Inner("У крыльца лужа. В грязи что-то блестит."),
                Inner("Гильза. Сорок пятый калибр, свежая — ещё не позеленела. Кто-то стрелял прямо здесь, у конторы."),
                Note("Улика в блокноте: гильза .45"));
            Talk(shellSpot, "Осмотреть", "", "Лужа у заднего крыльца. Больше здесь ничего нет.").AddBranch(shell, null, null);
            Talk(cart, "Осмотреть", "", "Старая телега. За ней легко укрыться.");
            Gate("BackWindowLitAtNight", story, null, F(StoryFlags.TimeDay), window);

            Transform fx = Group("--- FX");
            Inst(p.Rain, "Rain", new Vector3(8f, 9f, 1f), fx);

            AddBounds(16f, Group("--- Collision & Bounds"));
            EditorSceneManager.SaveScene(scene, BackYardScene);
        }
    }
}
