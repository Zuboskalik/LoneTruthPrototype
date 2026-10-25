using System.Collections.Generic;
using LoneTruth.Core;
using LoneTruth.Interaction;
using LoneTruth.Inventory;
using LoneTruth.Story;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LoneTruth.EditorTools
{
    /// <summary>
    /// The story half of the chapter 1 builder: items, interiors 1-03 / 1-04 / 1-05 and the opening
    /// sequences (prologue, platform meeting, the walk to the saloon, the night, the morning at the
    /// company office) up to the start of the day-1 free investigation.
    /// </summary>
    public static partial class Chapter1LocationBuilder
    {
        private const string SaloonScene = SceneDir + "/Saloon.unity";
        private const string RoomScene = SceneDir + "/HoltRoom.unity";
        private const string OfficeScene = SceneDir + "/CompanyOffice.unity";
        private const string IconDir = ArtRoot + "/UI/Icons";
        private const string InteriorDir = EnvDir + "/Interiors";
        private const string ItemDir = "Assets/ScriptableObjects/Items/Chapter1";

        private const string AgnesLetterText =
            "«Мистер Холт.\n\nТомас жив. Он пишет мне по ночам.\nВ последнем письме он назвал ваше имя.»\n\n— Агнес Бирн, Холлоу-Рок";

        // ------------------------------------------------------------------ items

        private class Items
        {
            public ItemSO Revolver, Ammo, Letter, RoomKey, CompanyPass;
        }

        private static ItemSO Item(string file, string id, string name, ItemType type, int maxStack, string icon, string description, string documentText = "")
        {
            string path = $"{ItemDir}/{file}.asset";
            var item = AssetDatabase.LoadAssetAtPath<ItemSO>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemSO>();
                AssetDatabase.CreateAsset(item, path);
            }

            var so = new SerializedObject(item);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("itemName").stringValue = name;
            so.FindProperty("icon").objectReferenceValue = S($"{IconDir}/{icon}.png");
            so.FindProperty("itemType").enumValueIndex = (int)type;
            so.FindProperty("maxStack").intValue = maxStack;
            so.FindProperty("description").stringValue = description;
            so.FindProperty("documentText").stringValue = documentText;
            so.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }

        private static Items BuildItems()
        {
            return new Items
            {
                Revolver = Item("Weapon_Revolver38", "weapon_revolver_38", "Револьвер .38", ItemType.Weapon, 1, "ICO_Revolver38",
                    "Служебный .38, шесть патронов в барабане. G — достать / убрать."),
                Ammo = Item("Ammo_38", "ammo_38", "Патроны .38", ItemType.Ammo, 24, "ICO_Ammo38",
                    "G — зарядить барабан."),
                Letter = Item("Doc_AgnesLetter", "doc_agnes_letter", "Письмо Агнес Бирн", ItemType.Document, 1, "ICO_AgnesLetter",
                    "Письмо, из-за которого Холт приехал. G — перечитать.", AgnesLetterText),
                RoomKey = Item("Key_SaloonRoom3", "key_saloon_room_3", "Ключ от номера 3", ItemType.Key, 1, "ICO_RoomKey",
                    "Ключ Руфи от номера наверху. Выберите и примените (G) у двери."),
                CompanyPass = Item("Key_CompanyPass", "key_company_pass", "Пропуск компании", ItemType.Key, 1, "ICO_CompanyPass",
                    "Пропуск «Уитлок Майнинг» за подписью Корнелиуса Уитлока.")
            };
        }

        // ------------------------------------------------------------------ placeholder art

        private static Human Ruth => new Human
        {
            Height = 1.68f, Coat = PlaceholderPainter.C("#2F2B28"), Trousers = PlaceholderPainter.C("#3A332E"),
            Hat = PlaceholderPainter.C("#9A4B2E"), Accent = PlaceholderPainter.C("#CFC5B2"), Skin = PlaceholderPainter.C("#B8A08A"),
            HatStyle = 5, LongCoat = true, Width = 1.15f
        };

        private static Human Cornelius => new Human
        {
            Height = 1.72f, Coat = PlaceholderPainter.C("#34363F"), Trousers = PlaceholderPainter.C("#2C2E35"),
            Hat = PlaceholderPainter.C("#6B5E52"), Accent = PlaceholderPainter.C("#B08D3E"), Skin = PlaceholderPainter.C("#C4A890"),
            HatStyle = 6, LongCoat = false, Width = 1.4f, Glasses = true
        };

        private static Human Clerk => new Human
        {
            Height = 1.74f, Coat = PlaceholderPainter.C("#3A3B3F"), Trousers = PlaceholderPainter.C("#2E2F33"),
            Hat = PlaceholderPainter.C("#3B3530"), Accent = PlaceholderPainter.C("#CFC5B2"), Skin = PlaceholderPainter.C("#B39E88"),
            HatStyle = 6, LongCoat = false, Glasses = true
        };

        private static void DrawStoryPlaceholders()
        {
            Char("CH_Ruth_Ref", Ruth, -1, 0, 0, 100);
            for (int i = 0; i < 2; i++) Char($"CH_Ruth_Idle_{i:00}", Ruth, -1, 0, i, 101 + i);
            Char("CH_Cornelius_Ref", Cornelius, -1, 0, 0, 110);
            for (int i = 0; i < 2; i++) Char($"CH_Cornelius_Idle_{i:00}", Cornelius, -1, 0, i, 111 + i);
            Char("CH_Clerk_Ref", Clerk, -1, 0, 0, 120);
            for (int i = 0; i < 2; i++) Char($"CH_Clerk_Idle_{i:00}", Clerk, -1, 0, i, 121 + i);

            // Item icons for the inventory bar (shown at ~72 px).
            Icon("ICO_Revolver38", p =>
            {
                Color steel = PlaceholderPainter.C("#5E636B");
                p.Rect(20, 66, 70, 16, steel);           // barrel
                p.Rect(62, 52, 30, 30, steel);           // cylinder
                p.Trap(98, 20, 26, 20, 40, PlaceholderPainter.C("#4A3A2C")); // grip
                p.Rect(86, 56, 16, 6, steel);
            });
            Icon("ICO_Ammo38", p =>
            {
                for (int i = 0; i < 3; i++)
                {
                    p.Rect(30 + i * 24, 30, 16, 50, PlaceholderPainter.C("#A88A4A"));
                    p.Ellipse(38 + i * 24, 82, 8, 9, PlaceholderPainter.C("#6E5A3A"));
                }
            });
            Icon("ICO_AgnesLetter", p =>
            {
                p.Rect(22, 34, 84, 60, PlaceholderPainter.C("#D8CFBE"));
                for (int y = 46; y < 86; y += 10) p.Rect(32, y, 60, 3, PlaceholderPainter.C("#6A6458"));
            });
            Icon("ICO_RoomKey", p =>
            {
                Color brass = PlaceholderPainter.C("#B08D3E");
                p.Ellipse(36, 64, 18, 18, brass);
                p.Ellipse(36, 64, 9, 9, Color.clear);
                p.Rect(52, 60, 50, 8, brass);
                p.Rect(88, 46, 8, 14, brass);
                p.Rect(76, 50, 8, 10, brass);
                p.Rect(38, 20, 52, 18, PlaceholderPainter.C("#8A6A3E")); // wooden tag "3"
            });
            Icon("ICO_CompanyPass", p =>
            {
                p.Rect(20, 30, 88, 64, PlaceholderPainter.C("#C9BFA9"));
                p.Rect(20, 80, 88, 14, PlaceholderPainter.C("#34363F"));
                p.Ellipse(42, 54, 12, 12, PlaceholderPainter.C("#5E2A2C")); // company seal
                for (int y = 44; y < 70; y += 9) p.Rect(62, y, 38, 3, PlaceholderPainter.C("#6A6458"));
            });

            // Sky by day (overcast, rain).
            var sky = new PlaceholderPainter(256, 256, 60);
            sky.VGradient(PlaceholderPainter.C("#5C636B"), PlaceholderPainter.C("#3A4048"));
            sky.Noise(0.04f);
            sky.SaveIfMissing($"{EnvDir}/Shared/ENV_Sky_OvercastDay.png");

            Wall("TEX_Wall_Saloon", "#4A3528", "#3A2A20", true);
            Wall("TEX_Wall_Room", "#4D4A44", "#403D38", true);
            Wall("TEX_Wall_Office", "#3E3A34", "#2F2C28", false);
            PaintPlanks(PlaceholderPainter.C("#3E3228"), 61).SaveIfMissing($"{InteriorDir}/TEX_Floor_Interior.png");

            Prop("PRP_BarCounter", 12f, 1.15f, p =>
            {
                p.Rect(0, 0, p.W, 100, PlaceholderPainter.C("#3B2B20"));
                p.Rect(0, 100, p.W, 15, PlaceholderPainter.C("#5A4232"));
                for (int x = 60; x < p.W; x += 120) p.Rect(x, 10, 6, 80, PlaceholderPainter.C("#2A1E16"));
            });
            Prop("PRP_BottleShelves", 10f, 2.4f, p =>
            {
                Color wood = PlaceholderPainter.C("#2E241C");
                for (int y = 0; y < p.H; y += 80) p.Rect(0, y, p.W, 10, wood);
                for (int x = 10; x < p.W - 20; x += 26)
                    for (int row = 0; row < 3; row++)
                        p.Rect(x, row * 80 + 10, 12, (int)p.Rand(36, 60), PlaceholderPainter.C(row == 1 ? "#5A6B4A" : "#6B4A2E"));
            });
            Prop("PRP_Piano", 1.7f, 1.4f, p =>
            {
                p.Rect(0, 0, p.W, 120, PlaceholderPainter.C("#1E1916"));
                p.Rect(10, 70, p.W - 20, 14, PlaceholderPainter.C("#D8CFBE"));
                p.Rect(0, 120, p.W, 20, PlaceholderPainter.C("#2A2320"));
            });
            Prop("PRP_SaloonTable", 1.6f, 0.95f, p =>
            {
                Color wood = PlaceholderPainter.C("#3E3026");
                p.Rect(20, 70, 120, 10, wood);
                p.Rect(74, 0, 10, 70, wood);
                p.Rect(0, 0, 8, 60, wood); p.Rect(0, 40, 26, 8, wood);       // chair left
                p.Rect(152, 0, 8, 60, wood); p.Rect(134, 40, 26, 8, wood);   // chair right
                p.Rect(40, 80, 10, 12, PlaceholderPainter.C("#6B4A2E"));      // glass
            });
            Prop("ENV_Saloon_Stairs", 3.2f, 3.6f, p =>
            {
                Color wood = PlaceholderPainter.C("#3B2C22");
                for (int i = 0; i < 12; i++) p.Rect(i * 26, i * 30, p.W - i * 26, 30, wood);
                for (int i = 0; i < 12; i++) p.Rect(i * 26, i * 30 + 26, 26, 4, PlaceholderPainter.C("#1C1612"));
                for (int i = 0; i < 12; i += 2) p.Rect(i * 26 + 4, i * 30 + 30, 5, 90, PlaceholderPainter.C("#2A1E16"));
            }, "Interiors");
            Prop("PRP_HangingLamp", 0.5f, 1.3f, p =>
            {
                p.Rect(23, 40, 4, 90, PlaceholderPainter.C("#1C1C1E"));
                p.Trap(25, 0, 46, 20, 40, PlaceholderPainter.C("#F2C26B"));
            });
            Prop("PRP_Bed", 2.1f, 1.05f, p =>
            {
                Color iron = PlaceholderPainter.C("#24262A");
                p.Rect(0, 0, 8, 100, iron); p.Rect(p.W - 8, 0, 8, 80, iron);
                p.Rect(8, 30, p.W - 16, 34, PlaceholderPainter.C("#6B665C"));  // mattress
                p.Rect(14, 60, 46, 16, PlaceholderPainter.C("#C9BFA9"));       // pillow
                p.Rect(60, 58, p.W - 70, 12, PlaceholderPainter.C("#4B4A44")); // blanket
            });
            Prop("PRP_Washstand", 0.75f, 1.15f, p =>
            {
                p.Rect(5, 0, 65, 80, PlaceholderPainter.C("#3B3026"));
                p.Ellipse(37, 88, 28, 10, PlaceholderPainter.C("#BDB7AA"));
                p.Rect(20, 92, 6, 20, PlaceholderPainter.C("#BDB7AA"));
            });
            Prop("PRP_Suitcase", 0.8f, 0.55f, p =>
            {
                p.Rect(0, 0, 80, 42, PlaceholderPainter.C("#4A3A2C"));
                p.Rect(28, 42, 24, 8, PlaceholderPainter.C("#2A1E16"));
                p.Rect(0, 18, 80, 4, PlaceholderPainter.C("#2A1E16"));
            });
            Prop("PRP_RoomTable", 1.0f, 0.95f, p =>
            {
                Color wood = PlaceholderPainter.C("#3E3026");
                p.Rect(0, 70, 100, 10, wood);
                p.Rect(8, 0, 8, 70, wood); p.Rect(84, 0, 8, 70, wood);
                p.Rect(30, 80, 30, 6, PlaceholderPainter.C("#D8CFBE"));       // the letter
                p.Rect(70, 80, 14, 14, PlaceholderPainter.C("#F2C26B"));      // oil lamp
            });
            Prop("PRP_Window", 1.3f, 1.6f, p =>
            {
                p.Rect(0, 0, p.W, p.H, PlaceholderPainter.C("#2A2622"));
                p.Rect(10, 10, p.W - 20, p.H - 20, PlaceholderPainter.C("#1F2A36"));
                p.Rect(p.W / 2f - 3, 10, 6, p.H - 20, PlaceholderPainter.C("#2A2622"));
                p.Rect(10, p.H / 2f - 3, p.W - 20, 6, PlaceholderPainter.C("#2A2622"));
                for (int i = 0; i < 18; i++) p.Rect(p.Rand(14, p.W - 16), p.Rand(14, p.H - 30), 2, 14, PlaceholderPainter.C("#5A6E80"));
            });
            Prop("PRP_ClerkCounter", 3f, 1.2f, p =>
            {
                p.Rect(0, 0, p.W, 104, PlaceholderPainter.C("#2E2620"));
                p.Rect(0, 104, p.W, 16, PlaceholderPainter.C("#4A3A2C"));
                p.Rect(200, 120, 50, 6, PlaceholderPainter.C("#C9BFA9")); // ledger
            });
            Prop("PRP_Desk", 2.4f, 0.95f, p =>
            {
                p.Rect(0, 0, 30, 80, PlaceholderPainter.C("#2E2620"));
                p.Rect(p.W - 60, 0, 60, 80, PlaceholderPainter.C("#2E2620"));
                p.Rect(0, 80, p.W, 15, PlaceholderPainter.C("#4A3A2C"));
            });
            Prop("PRP_DeskLampGreen", 0.4f, 0.55f, p =>
            {
                p.Rect(17, 0, 6, 35, PlaceholderPainter.C("#B08D3E"));
                p.Trap(20, 30, 40, 26, 20, PlaceholderPainter.C("#2F6B4A"));
            });
            Prop("PRP_Safe", 1.0f, 1.3f, p =>
            {
                p.Rect(0, 0, 100, 130, PlaceholderPainter.C("#2B2D31"));
                p.Frame(8, 8, 84, 114, 4, PlaceholderPainter.C("#1A1B1E"));
                p.Ellipse(50, 70, 14, 14, PlaceholderPainter.C("#8A8F95"));
            });
            Prop("PRP_Portrait_Iezekiel", 1.4f, 1.9f, p =>
            {
                p.Rect(0, 0, p.W, p.H, PlaceholderPainter.C("#6B5528"));        // gilt frame
                p.Rect(12, 12, p.W - 24, p.H - 24, PlaceholderPainter.C("#1E1C1A"));
                p.Trap(70, 12, 90, 60, 90, PlaceholderPainter.C("#141312"));  // frock coat
                p.Ellipse(70, 118, 20, 24, PlaceholderPainter.C("#9C8A76"));  // face
                p.Rect(48, 138, 44, 8, PlaceholderPainter.C("#141312"));      // hat brim
                p.Rect(56, 146, 28, 30, PlaceholderPainter.C("#141312"));     // top hat
            });
            Prop("PRP_Flowers", 0.5f, 0.6f, p =>
            {
                p.Rect(18, 0, 14, 22, PlaceholderPainter.C("#5A6B6E"));
                for (int i = 0; i < 6; i++) p.Ellipse(p.Rand(10, 40), p.Rand(30, 54), 6, 6, PlaceholderPainter.C("#E3DED3"));
            });
            Prop("PRP_GlassPartition", 1.6f, 2.6f, p =>
            {
                p.Rect(0, 0, p.W, p.H, new Color(0.5f, 0.56f, 0.55f, 0.28f));
                p.Frame(0, 0, p.W, p.H, 6, PlaceholderPainter.C("#2E2620"));
                p.Rect(0, 100, p.W, 6, PlaceholderPainter.C("#2E2620"));
            });
            Prop("DOOR_Interior", 1.1f, 2.3f, p =>
            {
                p.Rect(0, 0, p.W, p.H, PlaceholderPainter.C("#3B2E24"));
                p.Frame(10, 120, p.W - 20, 90, 4, PlaceholderPainter.C("#2A1E16"));
                p.Frame(10, 14, p.W - 20, 90, 4, PlaceholderPainter.C("#2A1E16"));
                p.Rect(p.W - 22, 110, 8, 8, PlaceholderPainter.C("#8A8F95"));
            });
            Prop("DOOR_Cellar_Closed", 1.1f, 2.2f, p =>
            {
                p.Rect(0, 0, p.W, p.H, PlaceholderPainter.C("#2C2723"));
                for (int x = 14; x < p.W; x += 24) p.Rect(x, 0, 3, p.H, PlaceholderPainter.C("#1A1714"));
                p.Rect(20, 100, 70, 10, PlaceholderPainter.C("#5A5550"));      // bar
                p.Rect(48, 86, 16, 22, PlaceholderPainter.C("#6A6D74"));       // padlock
            });
            Prop("DOOR_BackDoor_Closed", 1.0f, 2.2f, p =>
            {
                p.Rect(0, 0, p.W, p.H, PlaceholderPainter.C("#332A22"));
                p.Rect(10, 100, 80, 8, PlaceholderPainter.C("#5A5550"));
            });
        }

        private static void Icon(string name, System.Action<PlaceholderPainter> draw)
        {
            var p = new PlaceholderPainter(128, 128, name.GetHashCode());
            draw(p);
            p.InkOutline(Ink, 3);
            p.SaveIfMissing($"{IconDir}/{name}.png");
        }

        private static void Prop(string name, float wm, float hm, System.Action<PlaceholderPainter> draw, string folder = null)
        {
            var p = Sprite(wm, hm, name.GetHashCode());
            draw(p);
            p.InkOutline(Ink, 2);
            p.Noise(0.05f);
            p.SaveIfMissing(folder == null ? $"{PropDir}/{name}.png" : $"{EnvDir}/{folder}/{name}.png");
        }

        private static void Wall(string name, string top, string bottom, bool stripes)
        {
            var p = new PlaceholderPainter(256, 256, name.GetHashCode());
            p.VGradient(PlaceholderPainter.C(bottom), PlaceholderPainter.C(top));
            if (stripes)
            {
                for (int x = 0; x < 256; x += 32) p.Rect(x, 0, 6, 256, PlaceholderPainter.C(bottom));
            }
            else
            {
                for (int y = 0; y < 256; y += 64) p.Rect(0, y, 256, 4, PlaceholderPainter.C("#1E1C19")); // wood panelling
            }

            p.Rect(0, 0, 256, 50, PlaceholderPainter.C("#2A221C"));   // wainscot
            p.Noise(0.06f);
            p.SaveIfMissing($"{InteriorDir}/{name}.png");
        }

        // 0 saloon wall, 1 room wall, 2 office wall, 3 interior floor, 4 overcast sky
        private static Material[] BuildInteriorMaterials()
        {
            Material WallMat(string name, string tex)
            {
                Material m = Mat(name, "Universal Render Pipeline/Lit", tex, new Vector2(6, 1), 0.1f);
                m.SetColor("_BaseColor", new Color(0.85f, 0.82f, 0.78f));
                return m;
            }

            Material floor = Mat("M_Floor_Interior", "Universal Render Pipeline/Lit", $"{InteriorDir}/TEX_Floor_Interior.png", new Vector2(6, 3), 0.25f);
            floor.SetColor("_BaseColor", new Color(0.72f, 0.68f, 0.64f));
            return new[]
            {
                WallMat("M_Wall_Saloon", $"{InteriorDir}/TEX_Wall_Saloon.png"),
                WallMat("M_Wall_Room", $"{InteriorDir}/TEX_Wall_Room.png"),
                WallMat("M_Wall_Office", $"{InteriorDir}/TEX_Wall_Office.png"),
                floor,
                Mat("M_Sky_OvercastDay", "Universal Render Pipeline/Unlit", $"{EnvDir}/Shared/ENV_Sky_OvercastDay.png", Vector2.one)
            };
        }

        // ------------------------------------------------------------------ cutscene helpers

        private static CutsceneStep Line(string speaker, string text) => new CutsceneStep { type = CutsceneStepType.Line, speaker = speaker, text = text };
        private static CutsceneStep Inner(string text) => Line("", text);
        private static CutsceneStep Card(string text, float seconds) => new CutsceneStep { type = CutsceneStepType.Card, text = text, duration = seconds };
        private static CutsceneStep Black(bool on) => new CutsceneStep { type = CutsceneStepType.SetBlack, boolValue = on };
        private static CutsceneStep FadeOut(float s) => new CutsceneStep { type = CutsceneStepType.FadeOut, duration = s };
        private static CutsceneStep FadeIn(float s) => new CutsceneStep { type = CutsceneStepType.FadeIn, duration = s };
        private static CutsceneStep Wait(float s) => new CutsceneStep { type = CutsceneStepType.Wait, duration = s };
        private static CutsceneStep WalkTo(float x) => new CutsceneStep { type = CutsceneStepType.MovePlayer, x = x, wait = true };
        private static CutsceneStep Teleport(float x) => new CutsceneStep { type = CutsceneStepType.TeleportPlayer, x = x };
        private static CutsceneStep Move(GameObject actor, float x, float speed, bool wait) => new CutsceneStep { type = CutsceneStepType.MoveActor, actor = actor.transform, x = x, speed = speed, wait = wait };
        private static CutsceneStep Face(GameObject actor, float x) => new CutsceneStep { type = CutsceneStepType.FaceActor, actor = actor.transform, x = x };
        private static CutsceneStep Active(GameObject target, bool on) => new CutsceneStep { type = CutsceneStepType.SetActive, target = target, boolValue = on };
        private static CutsceneStep Flag(string key) => new CutsceneStep { type = CutsceneStepType.SetFlag, key = key, boolValue = true };
        private static CutsceneStep Give(ItemSO item, int amount, bool announce) => new CutsceneStep { type = CutsceneStepType.GiveItem, item = item, amount = amount, boolValue = announce };
        private static CutsceneStep Money(int amount) => new CutsceneStep { type = CutsceneStepType.GiveMoney, amount = amount, boolValue = true };
        private static CutsceneStep Goal(string text) => new CutsceneStep { type = CutsceneStepType.Objective, text = text };
        private static CutsceneStep Note(string text, float seconds = 3.5f) => new CutsceneStep { type = CutsceneStepType.Toast, text = text, duration = seconds };
        private static CutsceneStep Anim(string trigger) => new CutsceneStep { type = CutsceneStepType.PlayerAnimation, key = trigger };
        private static CutsceneStep Load(string scene, string spawn, bool fadeIn = true) => new CutsceneStep { type = CutsceneStepType.LoadScene, scene = scene, spawnId = spawn, boolValue = fadeIn };

        private static CutsceneSequence Sequence(string name, Transform parent, string doneFlag, params CutsceneStep[] steps)
        {
            var go = new GameObject("Seq_" + name);
            go.transform.SetParent(parent, false);
            var seq = go.AddComponent<CutsceneSequence>();
            seq.doneFlag = doneFlag;
            seq.steps = new List<CutsceneStep>(steps);
            return seq;
        }

        private static void StartTrigger(CutsceneSequence seq, string[] required, string[] forbidden)
        {
            seq.gameObject.AddComponent<CutsceneTrigger>().Configure(seq, CutsceneTriggerMode.OnSceneStart, required, forbidden);
        }

        private static void AreaTrigger(CutsceneSequence seq, float x, float width, string[] required, string[] forbidden)
        {
            var area = new GameObject("Area_" + seq.name);
            area.transform.SetParent(seq.transform.parent, false);
            area.transform.localPosition = new Vector3(x, 1f, 0f);
            var box = area.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(width, 2f, 3f);
            area.AddComponent<CutsceneTrigger>().Configure(seq, CutsceneTriggerMode.OnEnterArea, required, forbidden);
        }

        private static CutsceneInteractable Talk(GameObject host, string prompt, string fallbackSpeaker, string fallback)
        {
            InteractableNPC bark = host.GetComponent<InteractableNPC>();
            if (bark != null)
            {
                Object.DestroyImmediate(bark);
            }

            var talk = host.AddComponent<CutsceneInteractable>();
            talk.Configure(prompt, fallbackSpeaker, fallback);
            if (!string.IsNullOrEmpty(fallbackSpeaker) && host.GetComponentInChildren<Animator>() != null)
            {
                // Lets the character play its Talk animation during its cutscene lines.
                host.AddComponent<CutsceneActor>().Configure(fallbackSpeaker);
            }

            return talk;
        }

        private static GameObject InspectSpot(string name, Vector3 pos, Transform parent, float width = 1.2f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(width, 2f, 1.2f);
            box.center = new Vector3(0f, 1f, 0f);
            return go;
        }

        private static void Gate(string name, Transform parent, string[] required, string[] forbidden, params GameObject[] targets)
        {
            var go = new GameObject("Gate_" + name);
            go.transform.SetParent(parent, false);
            go.AddComponent<FlagGate>().Configure(required, forbidden, targets);
        }

        private static string[] F(params string[] flags) => flags;

        private static void ReloadAssets(ref Items items, ref VolumeProfile profile)
        {
            items = BuildItems();
            profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>($"{ArtRoot}/PP_Chapter1_NightRain.asset");
        }

        private static void SetupTimeOfDay(bool exterior, Material nightSky, Material daySky)
        {
            var go = new GameObject("TimeOfDay");
            var tod = go.AddComponent<TimeOfDayEnvironment>();
            tod.Configure(lastKeyLight, exterior ? lastSky : null, nightSky, daySky, exterior);
            if (!exterior)
            {
                tod.UseInteriorPreset();
            }
        }

        private static GameObject PointLight(string name, Vector3 pos, Transform parent, string hex, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = PlaceholderPainter.C(hex);
            l.intensity = intensity;
            l.range = range;
            return go;
        }

        private static void InteriorShell(float width, Material wall, Material floor, Transform parent)
        {
            Quad("Floor", floor, new Vector3(width / 2f, 0f, 0.8f), new Vector3(90f, 0f, 0f), new Vector3(width + 2f, 6f, 1f), parent);
            Quad("BackWall", wall, new Vector3(width / 2f, 2.6f, 3.6f), Vector3.zero, new Vector3(width, 5.2f, 1f), parent);
            Quad("SideWall_Left", wall, new Vector3(0f, 2.6f, 1f), new Vector3(0f, -90f, 0f), new Vector3(5.2f, 5.2f, 1f), parent);
            Quad("SideWall_Right", wall, new Vector3(width, 2.6f, 1f), new Vector3(0f, 90f, 0f), new Vector3(5.2f, 5.2f, 1f), parent);
        }

        private static void AddBounds(float width, Transform parent)
        {
            Box("Ground", new Vector3(width / 2f, -0.5f, 0f), new Vector3(width + 6f, 1f, 6f), parent);
            Box("Wall_Left", new Vector3(-0.3f, 2f, 0f), new Vector3(0.6f, 6f, 6f), parent);
            Box("Wall_Right", new Vector3(width + 0.3f, 2f, 0f), new Vector3(0.6f, 6f, 6f), parent);
        }

        // ------------------------------------------------------------------ 1-01 Station

        private static void BuildStation(Prefabs p, Dictionary<string, RuntimeAnimatorController> ac, Material[] mats, VolumeProfile profile, Items items)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // A Single-mode NewScene unloads assets nothing references any more, which turns the
            // builder's cached ItemSO / Material references into dead objects: reload them by path.
            ReloadAssets(ref items, ref profile);
            mats = BuildMaterials();
            CommonSystems(p, 3f, 6f, 24f, profile);

            Transform bg = Group("--- Background");
            Backdrop(mats[0], 15f, bg);
            GameObject train = SpriteObj("Train_Departing", $"{EnvDir}/Station/ENV_Station_Train.png", new Vector3(-1.5f, -1.2f, 6.5f), bg, 1f, new Color(0.8f, 0.82f, 0.88f));
            Quad("Trackbed", mats[3], new Vector3(15f, -1.2f, 6f), new Vector3(90f, 0f, 0f), new Vector3(50f, 4f, 1f), bg);
            SetupTimeOfDay(true, mats[0], AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/M_Sky_OvercastDay.mat"));

            Transform geo = Group("--- Geometry");
            Quad("Platform_Floor", mats[1], new Vector3(15f, 0f, 0.5f), new Vector3(90f, 0f, 0f), new Vector3(34f, 7f, 1f), geo);
            Quad("Platform_Edge", mats[1], new Vector3(15f, -0.6f, 4f), Vector3.zero, new Vector3(34f, 1.2f, 1f), geo);

            Transform set = Group("--- Set Dressing");
            SpriteObj("Canopy", $"{EnvDir}/Station/ENV_Station_Canopy.png", new Vector3(15f, 0f, 2.2f), set, 1f, new Color(0.85f, 0.85f, 0.9f));
            SpriteObj("TelegraphBooth", $"{EnvDir}/Station/ENV_Station_TelegraphBooth.png", new Vector3(16f, 0f, 3.2f), set);
            SpriteObj("StationNameSign", $"{PropDir}/PRP_Station_NameSign.png", new Vector3(5f, 0f, 3.4f), set, 1f, new Color(0.9f, 0.9f, 0.9f));
            Inst(p.Lamppost, "Lamp_x13", new Vector3(13f, 0f, 1.2f), set);
            PointLight("TelegraphWindow_Light", new Vector3(16f, 1.7f, 2.4f), set, "#F0C060", 2.5f, 5f);

            Transform fg = Group("--- Foreground (silhouettes)");
            Color sil = new Color(0.12f, 0.12f, 0.14f);
            PropInst(p, "FG_Lamppost_Silhouette", $"{PropDir}/PRP_Lamppost.png", new Vector3(4.5f, -1.0f, -3.2f), fg, 1.2f, sil);
            PropInst(p, "FG_CrateStack", $"{PropDir}/PRP_CrateStack.png", new Vector3(26.5f, -1.0f, -3.2f), fg, 1.2f, sil);

            Transform play = Group("--- Gameplay");
            GameObject conductor = Npc(p, ac, "NPC_Conductor_x6", "Conductor", new Vector3(6f, 0f, 0.6f), play, "Поговорить", "Дальше не поеду.");
            GameObject callahan = Npc(p, ac, "NPC_Callahan_x24", "Callahan", new Vector3(24f, 0f, 0.6f), play, "Поговорить", "Пойдёмте, мистер Холт.");
            Talk(callahan, "Поговорить", "Кэллахан", "Пойдёмте, мистер Холт. Ночью здесь не стоят.");

            GameObject telegrams = InspectSpot("Clue_UnsentTelegrams_x16", new Vector3(16f, 0f, 0f), play);
            SpriteObj("UnsentTelegrams_Art", $"{PropDir}/PRP_TelegramStack.png", new Vector3(16f, 1.25f, 3.05f), play);
            SceneDoor(p, "1-01_to_1-02", "MainStreet", "from_1-01", "Спуститься на Главную улицу", $"{EnvDir}/Station/ENV_Station_StairsDown.png", 29f, play);
            Spawn("from_1-02", 27.5f, play);

            Transform story = Group("--- Story");
            Gate("Conductor_PrologueOnly", story, null, F(StoryFlags.PrologueDone), conductor, train);
            Gate("Callahan_UntilMet", story, null, F(StoryFlags.MetCallahan), callahan);

            // 1. Prologue
            CutsceneSequence prologue = Sequence("1_Prologue", story, StoryFlags.PrologueDone,
                Black(true),
                Card("Октябрь 1936 года.\nПоезд на Холлоу-Рок. Ночь, дождь.", 3f),
                Card(AgnesLetterText, 0f),
                FadeIn(2f),
                Anim("ReadLetter"),
                Wait(2.2f),
                Inner("Письмо без обратного адреса. Месяц назад в шахте «Святая Варвара» завалило двадцать три человека."),
                Inner("А вдова пишет, что муж жив. И что он назвал моё имя."),
                Line("Проводник", "Мистер! Холлоу-Рок. Дальше станции не поеду — ни за какие деньги."),
                Line("Холт", "Что не так с Холлоу-Роком?"),
                Line("Проводник", "Ничего, мистер. Просто после темноты здесь лучше не задерживаться."),
                Move(conductor, -1f, 1.8f, true),
                Active(conductor, false),
                Move(train, -18f, 2.2f, false),
                Give(items.Revolver, 1, false),
                Give(items.Ammo, 6, false),
                Give(items.Letter, 1, false),
                Goal("Найти встречающего на перроне."),
                Note("A / D — идти   ·   E — взаимодействовать   ·   Q — выбрать предмет   ·   G — применить", 7f));
            StartTrigger(prologue, null, F(StoryFlags.PrologueDone));

            // Clue on the platform (optional): unsent telegrams.
            CutsceneSequence clue = Sequence("Clue_Telegrams", story, StoryFlags.ClueTelegrams,
                Inner("Телеграммы родным шахтёров. Отправить не успели — или не дали."),
                Inner("Стопка толстая. Компания держит их у себя."),
                Note("Улика: неотправленные телеграммы"));
            Talk(telegrams, "Осмотреть", "", "Неотправленные телеграммы родных шахтёров.").AddBranch(clue, null, null);

            // 2. Meeting on the platform
            CutsceneSequence meeting = Sequence("2_PlatformMeeting", story, StoryFlags.MetCallahan,
                WalkTo(21f),
                Move(callahan, 22.6f, 1.4f, true),
                Face(callahan, 21f),
                Line("Кэллахан", "Холт? Сэмюэл Холт из Чикаго?"),
                Line("Холт", "Смотря кто спрашивает."),
                Line("Кэллахан", "Дьюк Кэллахан, охрана «Уитлок Майнинг». Компания вас ждала. Номер в салуне оплачен."),
                Line("Холт", "А мистер Уитлок?"),
                Line("Кэллахан", "Примет вас утром. Ночью по городу одному лучше не ходить — я провожу."),
                Flag(StoryFlags.EscortToSaloon),
                Goal("Идти за Кэллаханом в салун."),
                Move(callahan, 29f, 1.5f, false),
                WalkTo(27.5f),
                Load("MainStreet", "from_1-01"));
            AreaTrigger(meeting, 20f, 1f, F(StoryFlags.PrologueDone), F(StoryFlags.MetCallahan));

            Transform bounds = Group("--- Collision & Bounds");
            AddBounds(30f, bounds);
            Transform fx = Group("--- FX");
            Inst(p.Rain, "Rain", new Vector3(15f, 9f, 1f), fx);

            EditorSceneManager.SaveScene(scene, StationScene);
        }

        // ------------------------------------------------------------------ 1-02 Main Street

        private static void BuildStreet(Prefabs p, Dictionary<string, RuntimeAnimatorController> ac, Material[] mats, VolumeProfile profile, Items items)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // A Single-mode NewScene unloads assets nothing references any more, which turns the
            // builder's cached ItemSO / Material references into dead objects: reload them by path.
            ReloadAssets(ref items, ref profile);
            mats = BuildMaterials();
            CommonSystems(p, 3.5f, 6f, 54f, profile);

            Transform bg = Group("--- Background");
            Backdrop(mats[0], 30f, bg);
            SpriteObj("Headframe_Distant", $"{EnvDir}/MainStreet/ENV_Headframe_Distant.png", new Vector3(46f, -2f, 32f), bg, 1.6f, new Color(0.55f, 0.6f, 0.68f));
            SetupTimeOfDay(true, mats[0], AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/M_Sky_OvercastDay.mat"));

            Transform geo = Group("--- Geometry");
            Quad("Road_Mud", mats[2], new Vector3(30f, 0f, -0.5f), new Vector3(90f, 0f, 0f), new Vector3(64f, 7f, 1f), geo);
            Quad("Boardwalk", mats[1], new Vector3(30f, 0.01f, 2.4f), new Vector3(90f, 0f, 0f), new Vector3(64f, 1.4f, 1f), geo);

            Transform fac = Group("--- Facades");
            Color tint = new Color(0.82f, 0.83f, 0.88f);
            SpriteObj("FAC_Saloon_6-14", $"{EnvDir}/MainStreet/FAC_Saloon.png", new Vector3(10f, 0f, 3f), fac, 1f, tint);
            SpriteObj("FAC_CompanyOffice_16-24", $"{EnvDir}/MainStreet/FAC_CompanyOffice.png", new Vector3(20f, 0f, 3f), fac, 1f, tint);
            SpriteObj("FAC_SheriffOffice_25-31", $"{EnvDir}/MainStreet/FAC_SheriffOffice.png", new Vector3(28f, 0f, 3f), fac, 1f, tint);
            SpriteObj("FAC_AlleyMouth_33-39", $"{EnvDir}/MainStreet/FAC_AlleyMouth.png", new Vector3(36f, 0f, 3f), fac, 1f, tint);
            SpriteObj("FAC_SawmillFence_40-49", $"{EnvDir}/MainStreet/FAC_SawmillFence.png", new Vector3(44.5f, 0f, 3.2f), fac, 1f, tint);
            SpriteObj("FAC_Chapel_49-56", $"{EnvDir}/MainStreet/FAC_Chapel.png", new Vector3(52.5f, 0f, 3f), fac, 1f, tint);
            SpriteObj("FAC_MineRoadFence_56-60", $"{EnvDir}/MainStreet/FAC_MineRoadFence.png", new Vector3(58f, 0f, 3.2f), fac, 1f, tint);
            PropInst(p, "NoticeBoard_x23", $"{PropDir}/PRP_NoticeBoard.png", new Vector3(23f, 0f, 2.7f), fac);
            GameObject saloonGlow = PointLight("Saloon_Windows_Light", new Vector3(10f, 2.2f, 2.2f), fac, "#E8A84E", 3f, 6f);

            Transform set = Group("--- Set Dressing");
            var lampLights = new List<GameObject> { saloonGlow };
            foreach (float x in new[] { 6f, 31f, 48f })
            {
                GameObject lamp = Inst(p.Lamppost, $"Lamp_x{x:0}", new Vector3(x, 0f, 1.6f), set);
                lampLights.Add(lamp.transform.Find("Light").gameObject);
            }

            PropInst(p, "Barrels_Hide_x41", $"{PropDir}/PRP_Barrels.png", new Vector3(41f, 0f, 1.8f), set);

            Transform fg = Group("--- Foreground (silhouettes)");
            Color sil = new Color(0.12f, 0.12f, 0.14f);
            PropInst(p, "FG_HitchingPost_x4", $"{PropDir}/PRP_HitchingPost.png", new Vector3(4f, -1.0f, -3f), fg, 1.1f, sil);
            PropInst(p, "FG_TelegraphPole_x18", $"{PropDir}/PRP_TelegraphPole.png", new Vector3(18f, -0.6f, -3.6f), fg, 1f, sil);
            PropInst(p, "FG_Barrels_x34", $"{PropDir}/PRP_Barrels.png", new Vector3(34f, -1.0f, -3f), fg, 1.1f, sil);
            PropInst(p, "FG_TelegraphPole_x50", $"{PropDir}/PRP_TelegraphPole.png", new Vector3(50f, -0.6f, -3.6f), fg, 1f, sil);

            Transform play = Group("--- Gameplay");
            SceneDoor(p, "1-02_to_1-01", "Station", "from_1-02", "Подняться на станцию", $"{EnvDir}/MainStreet/ENV_Street_StationSteps.png", 2f, play);
            SceneDoor(p, "1-02_to_1-03", "Saloon", "from_1-02", "Салун «Последняя лампа»", $"{PropDir}/DOOR_Saloon_Closed.png", 10f, play);
            GameObject officeDoor = SceneDoor(p, "1-02_to_1-05", "CompanyOffice", "from_1-02", "Контора «Уитлок Майнинг»", $"{PropDir}/DOOR_CompanyOffice_Closed.png", 20f, play);
            GameObject officeStub = Stub(p, "1-02_to_1-05_night", "1-05", "Контора компании", "Заперто до утра.", $"{PropDir}/DOOR_CompanyOffice_Closed.png", 20f, play);
            Spawn("from_1-01", 3.5f, play);
            Spawn("from_1-03", 10f, play);
            Spawn("from_1-05", 20f, play);

            // Not-yet-built locations keep their closed stubs after the free investigation starts.
            Stub(p, "1-02_to_1-08", "1-08", "Контора шерифа", "Заперто.", $"{PropDir}/DOOR_Sheriff_Closed.png", 28f, play);
            Stub(p, "1-02_to_1-14", "1-14", "Переулок", "Проход перегорожен калиткой.", $"{PropDir}/DOOR_AlleyGate_Closed.png", 36f, play);
            Stub(p, "1-02_to_1-18", "1-18", "Ворота лесопилки", "Ворота на цепи.", $"{PropDir}/DOOR_SawmillGate_Closed.png", 45f, play);
            Stub(p, "1-02_to_1-09", "1-09", "Часовня", "Двери часовни закрыты.", $"{PropDir}/DOOR_Chapel_Closed.png", 52f, play);
            Stub(p, "1-02_to_1-21", "1-21", "Дорога к шахте", "Ворота на замке.", $"{PropDir}/DOOR_MineGate_Closed.png", 58f, play);

            Transform day = Group("State_Day");
            day.SetParent(play, false);
            GameObject miner = Npc(p, ac, "NPC_PasserbyMiner_x13", "PasserbyMiner", new Vector3(13f, 0f, 0.8f), day, "Поговорить", "Не до разговоров, мистер.");
            GameObject woman = Npc(p, ac, "NPC_PasserbyWoman_x15", "PasserbyWoman", new Vector3(15f, 0f, 0.8f), day, "Поговорить", "Вы из Чикаго? Здесь чужих не любят.");
            Transform eveningDay2 = Group("State_Day2Evening (inactive)");
            eveningDay2.SetParent(play, false);
            Npc(p, ac, "NPC_Liam_x38", "Liam", new Vector3(38f, 0f, 0.6f), eveningDay2, "Поговорить", "…");
            eveningDay2.gameObject.SetActive(false);

            GameObject escortCallahan = Npc(p, ac, "NPC_Callahan_Escort", "Callahan", new Vector3(5.2f, 0f, 0.6f), play, "Поговорить", "Салун прямо по улице.");
            Talk(escortCallahan, "Поговорить", "Кэллахан", "Салун прямо по улице, мистер Холт.");

            Transform story = Group("--- Story");
            Gate("NightLights", story, null, F(StoryFlags.TimeDay), lampLights.ToArray());
            Gate("DayPasserby", story, F(StoryFlags.TimeDay), null, miner, woman);
            Gate("OfficeOpenByDay", story, F(StoryFlags.TimeDay), null, officeDoor);
            Gate("OfficeClosedAtNight", story, null, F(StoryFlags.TimeDay), officeStub);
            Gate("CallahanEscort", story, F(StoryFlags.EscortToSaloon), F(StoryFlags.EscortDone), escortCallahan);

            // 2 (cont.). Callahan walks Holt to the saloon.
            CutsceneSequence escort = Sequence("2b_EscortToSaloon", story, StoryFlags.EscortDone,
                Move(escortCallahan, 11f, 1.5f, false),
                WalkTo(9.2f),
                Face(escortCallahan, 9f),
                Line("Кэллахан", "«Последняя лампа». Хозяйку зовут Руфь, она в курсе."),
                Line("Кэллахан", "Утром — в контору компании, каменный дом через дорогу. Мистер Уитлок не любит ждать."),
                Move(escortCallahan, 27f, 1.6f, false),
                Wait(1.2f),
                Goal("Войти в салун «Последняя лампа»."));
            StartTrigger(escort, F(StoryFlags.EscortToSaloon), F(StoryFlags.EscortDone));

            Transform bounds = Group("--- Collision & Bounds");
            AddBounds(60f, bounds);
            Transform fx = Group("--- FX");
            Inst(p.Rain, "Rain_West", new Vector3(15f, 9f, 1f), fx);
            Inst(p.Rain, "Rain_East", new Vector3(45f, 9f, 1f), fx);

            EditorSceneManager.SaveScene(scene, StreetScene);
        }

        // ------------------------------------------------------------------ 1-03 Saloon

        private static void BuildSaloon(Prefabs p, Dictionary<string, RuntimeAnimatorController> ac, Material[] im, VolumeProfile profile, Items items)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // A Single-mode NewScene unloads assets nothing references any more, which turns the
            // builder's cached ItemSO / Material references into dead objects: reload them by path.
            ReloadAssets(ref items, ref profile);
            im = BuildInteriorMaterials();
            CommonSystems(p, 2.5f, 6f, 18f, profile);
            SetupTimeOfDay(false, null, null);

            Transform geo = Group("--- Geometry");
            InteriorShell(24f, im[0], im[3], geo);

            Transform set = Group("--- Set Dressing");
            SpriteObj("BottleShelves_5-15", $"{PropDir}/PRP_BottleShelves.png", new Vector3(10f, 1.2f, 3.5f), set);
            SpriteObj("BarCounter_4-16", $"{PropDir}/PRP_BarCounter.png", new Vector3(10f, 0f, 1.5f), set);
            SpriteObj("Piano_x20", $"{PropDir}/PRP_Piano.png", new Vector3(20.2f, 0f, 3.2f), set);
            SpriteObj("Table_x17", $"{PropDir}/PRP_SaloonTable.png", new Vector3(17.2f, 0f, 1.8f), set);
            SpriteObj("Stairs_21-24", $"{InteriorDir}/ENV_Saloon_Stairs.png", new Vector3(22.4f, 0f, 2.6f), set);
            foreach (float x in new[] { 6f, 10f, 14f, 18f })
            {
                SpriteObj($"HangingLamp_x{x:0}", $"{PropDir}/PRP_HangingLamp.png", new Vector3(x, 3.7f, 1.6f), set);
                PointLight($"Lamp_Light_x{x:0}", new Vector3(x, 3.6f, 1.2f), set, "#F0B45A", 4.5f, 7.5f);
            }

            Transform play = Group("--- Gameplay");
            SceneDoor(p, "1-03_to_1-02", "MainStreet", "from_1-03", "Выйти на улицу", $"{PropDir}/DOOR_Interior.png", 1f, play);
            GameObject stairsDoor = SceneDoor(p, "1-03_to_1-04", "HoltRoom", "from_1-03", "Подняться в номер 3", $"{PropDir}/DOOR_Interior.png", 23f, play);
            stairsDoor.transform.Find("Art").localPosition = new Vector3(0f, 3.4f, 2.4f); // the door at the top of the stairs
            var lockDoor = stairsDoor.GetComponent<SceneTransitionDoor>();
            SetField(lockDoor, "requiredItem", items.RoomKey);
            SetField(lockDoor, "lockedLine", "Номер заперт. Выберите ключ (Q) и примените его у двери (G).");
            SetField(lockDoor, "unlockedLine", "Ключ подошёл. Номер 3 открыт.");
            Spawn("from_1-02", 2.5f, play);
            Spawn("from_1-04", 21.5f, play);

            GameObject ruth = Npc(p, ac, "NPC_Ruth_x10", "Ruth", new Vector3(10f, 0f, 0f), play, "Поговорить", "", 2.2f);
            Npc(p, ac, "NPC_Visitor_x18", "PasserbyMiner", new Vector3(18f, 0f, 0.9f), play, "Поговорить", "Шахта молчит уже месяц.");
            Npc(p, ac, "NPC_Visitor_x19", "PasserbyWoman", new Vector3(19.5f, 0f, 0.9f), play, "Поговорить", "Не мешайте, мистер. Я пью за упокой.");

            Transform story = Group("--- Story");

            // 2 (end). Check-in: Ruth gives the key to room 3.
            CutsceneSequence checkIn = Sequence("2c_CheckIn", story, StoryFlags.SaloonCheckIn,
                WalkTo(8.4f),
                Line("Руфь", "Вы от компании. Кэллахан предупредил."),
                Line("Руфь", "Третий номер, наверху. Компания платит — значит, вопросов я не задаю."),
                Give(items.RoomKey, 1, true),
                Line("Руфь", "Ночью по улице не ходите. И если зазвонит колокол — окна не открывайте."),
                Inner("Тёплое место. Здесь что-то знают — и молчат."),
                Goal("Подняться в номер 3 (лестница справа)."),
                Note("Ключ — в инвентаре: выберите его (Q) и примените у двери (G).", 6f));
            StartTrigger(checkIn, F(StoryFlags.EscortDone), F(StoryFlags.SaloonCheckIn));

            CutsceneSequence night = Sequence("Ruth_Night", story, "",
                Line("Руфь", "Третий номер, наверху. Спокойной ночи, мистер Холт."));
            CutsceneSequence morning = Sequence("3a_RuthMorning", story, StoryFlags.RuthMorning,
                Line("Руфь", "Утро, мистер Холт. Кофе холодный, новости ещё холоднее."),
                Line("Руфь", "Кэллахан заходил на рассвете. Мистер Уитлок ждёт вас в конторе."));
            CutsceneSequence rumor = Sequence("4a_RuthRumor", story, StoryFlags.RuthRumorBarn,
                Line("Руфь", "Раз уж вы спрашиваете людей… Собаки третью ночь лают на сарай за конюшней."),
                Line("Руфь", "Я бы глянула, да кто я такая."),
                Note("Слух: сарай за конюшней (переулок, 1.2)"));

            CutsceneInteractable ruthTalk = Talk(ruth, "Поговорить", "Руфь", "Кофе холодный, мистер Холт. Новости — ещё холоднее.");
            ruthTalk.AddBranch(rumor, F(StoryFlags.FreeInvestigationDay1), null);
            ruthTalk.AddBranch(morning, F(StoryFlags.Day1Morning), F(StoryFlags.OfficeMeeting));
            ruthTalk.AddBranch(night, F(StoryFlags.SaloonCheckIn), F(StoryFlags.Day1Morning));

            AddBounds(24f, Group("--- Collision & Bounds"));
            EditorSceneManager.SaveScene(scene, SaloonScene);
        }

        // ------------------------------------------------------------------ 1-04 Holt's room

        private static void BuildRoom(Prefabs p, Dictionary<string, RuntimeAnimatorController> ac, Material[] im, VolumeProfile profile, Items items)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // A Single-mode NewScene unloads assets nothing references any more, which turns the
            // builder's cached ItemSO / Material references into dead objects: reload them by path.
            ReloadAssets(ref items, ref profile);
            im = BuildInteriorMaterials();
            CommonSystems(p, 2f, 5f, 5f, profile);
            SetupTimeOfDay(false, null, null);

            Transform geo = Group("--- Geometry");
            InteriorShell(10f, im[1], im[3], geo);

            Transform set = Group("--- Set Dressing");
            SpriteObj("Window_x3.5", $"{PropDir}/PRP_Window.png", new Vector3(3.5f, 1.2f, 3.5f), set);
            SpriteObj("Bed_x5", $"{PropDir}/PRP_Bed.png", new Vector3(5f, 0f, 2.4f), set);
            SpriteObj("Table_Letter_x7", $"{PropDir}/PRP_RoomTable.png", new Vector3(7f, 0f, 2.2f), set);
            SpriteObj("Washstand_x8.4", $"{PropDir}/PRP_Washstand.png", new Vector3(8.4f, 0f, 3.1f), set);
            SpriteObj("Suitcase_x9", $"{PropDir}/PRP_Suitcase.png", new Vector3(9.2f, 0f, 1.6f), set);
            PointLight("OilLamp_Light", new Vector3(7.1f, 1.3f, 1.8f), set, "#F2B45A", 4f, 6.5f);

            Transform play = Group("--- Gameplay");
            SceneDoor(p, "1-04_to_1-03", "Saloon", "from_1-04", "Спуститься в салун", $"{PropDir}/DOOR_Interior.png", 1f, play);
            Spawn("from_1-03", 2f, play);
            GameObject bed = InspectSpot("Bed_Sleep_x5", new Vector3(5f, 0f, 0f), play, 1.6f);
            GameObject letter = InspectSpot("Letter_Inspect_x7", new Vector3(7f, 0f, 0f), play);
            // The demo has no combat scenes yet, so the room is where aiming and shooting can be tried.
            new GameObject("CombatZone_Demo").AddComponent<LoneTruth.Combat.CombatZone>().transform.SetParent(play, false);

            Transform story = Group("--- Story");
            CutsceneSequence sleep = Sequence("3_SleepToMorning", story, StoryFlags.Day1Morning,
                Inner("Месяц под землёй. Двадцать три человека. А мёртвые, выходит, пишут письма."),
                FadeOut(1.4f),
                Card("Утро.\nДень первый.", 2.5f),
                Flag(StoryFlags.TimeDay),
                Wait(0.3f),
                FadeIn(1.4f),
                Goal("Явиться в контору компании (Главная улица, каменный дом)."));
            Talk(bed, "Лечь спать", "", "Спать ещё рано.").AddBranch(sleep, F(StoryFlags.SaloonCheckIn), F(StoryFlags.Day1Morning));

            CutsceneSequence reread = Sequence("Letter_Reread", story, "",
                Anim("ReadLetter"),
                Card(AgnesLetterText, 0f));
            Talk(letter, "Перечитать письмо", "", "").AddBranch(reread, null, null);

            CutsceneSequence arrive = Sequence("Room_Arrive", story, "room_first_visit",
                Inner("Железная кровать, умывальник, окно на улицу. Чемодан можно не распаковывать."),
                Goal("Лечь спать."));
            StartTrigger(arrive, F(StoryFlags.SaloonCheckIn), F(StoryFlags.Day1Morning));

            AddBounds(10f, Group("--- Collision & Bounds"));
            EditorSceneManager.SaveScene(scene, RoomScene);
        }

        // ------------------------------------------------------------------ 1-05 Company office

        private static void BuildOffice(Prefabs p, Dictionary<string, RuntimeAnimatorController> ac, Material[] im, VolumeProfile profile, Items items)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // A Single-mode NewScene unloads assets nothing references any more, which turns the
            // builder's cached ItemSO / Material references into dead objects: reload them by path.
            ReloadAssets(ref items, ref profile);
            im = BuildInteriorMaterials();
            CommonSystems(p, 2.5f, 6f, 16f, profile);
            SetupTimeOfDay(false, null, null);

            Transform geo = Group("--- Geometry");
            InteriorShell(22f, im[2], im[3], geo);

            Transform set = Group("--- Set Dressing");
            SpriteObj("ClerkCounter_x4", $"{PropDir}/PRP_ClerkCounter.png", new Vector3(4.3f, 0f, 1.4f), set);
            SpriteObj("GlassPartition_x8", $"{PropDir}/PRP_GlassPartition.png", new Vector3(8.4f, 0f, 2.4f), set);
            SpriteObj("Desk_x13", $"{PropDir}/PRP_Desk.png", new Vector3(13.4f, 0f, 1.6f), set);
            SpriteObj("DeskLamp_x12.6", $"{PropDir}/PRP_DeskLampGreen.png", new Vector3(12.6f, 0.95f, 1.55f), set);
            SpriteObj("Portrait_Iezekiel_x17", $"{PropDir}/PRP_Portrait_Iezekiel.png", new Vector3(17f, 1.3f, 3.5f), set);
            SpriteObj("Flowers_x17", $"{PropDir}/PRP_Flowers.png", new Vector3(17f, 0.75f, 3.3f), set);
            SpriteObj("Safe_x19", $"{PropDir}/PRP_Safe.png", new Vector3(19.4f, 0f, 3.2f), set);
            PointLight("GreenLamp_Light", new Vector3(12.6f, 1.6f, 1.2f), set, "#9BC79A", 4f, 6f);
            PointLight("Ceiling_Light_Reception", new Vector3(4f, 3.8f, 1.2f), set, "#E3C893", 4f, 7f);
            PointLight("Portrait_Light", new Vector3(17f, 3.4f, 2.6f), set, "#E8C890", 3.5f, 5f);

            Transform play = Group("--- Gameplay");
            SceneDoor(p, "1-05_to_1-02", "MainStreet", "from_1-05", "Выйти на улицу", $"{PropDir}/DOOR_Interior.png", 1f, play);
            Spawn("from_1-02", 2.5f, play);
            Stub(p, "1-05_to_1-06", "1-06", "Архив", "Дверь в подвал заперта на висячий замок.", $"{PropDir}/DOOR_Cellar_Closed.png", 7f, play);
            Stub(p, "1-05_to_1-07", "1-07", "Задний двор", "Задняя дверь заперта.", $"{PropDir}/DOOR_BackDoor_Closed.png", 21f, play);

            Npc(p, ac, "NPC_Clerk_x4", "Clerk", new Vector3(4f, 0f, 0f), play, "Поговорить", "Мистер Уитлок вас ждёт. Проходите.", 1.9f);
            GameObject cornelius = Npc(p, ac, "NPC_Cornelius_x14", "Cornelius", new Vector3(14f, 0f, 0f), play, "Поговорить", "", 2.3f);
            Talk(cornelius, "Поговорить", "Корнелиус Уитлок", "Отчёт, мистер Холт. Чем скорее, тем лучше.");
            GameObject portrait = InspectSpot("Clue_Portrait_x17", new Vector3(17f, 0f, 0f), play);

            Transform story = Group("--- Story");

            // 3. Morning: the company office.
            CutsceneSequence meeting = Sequence("3b_OfficeMeeting", story, StoryFlags.OfficeMeeting,
                WalkTo(11.6f),
                Line("Корнелиус Уитлок", "Мистер Холт. Корнелиус Уитлок. Рад, что вы добрались."),
                Line("Корнелиус Уитлок", "Задача простая: подтвердить гибель двадцати трёх человек и вину профсоюзных саботажников."),
                Line("Корнелиус Уитлок", "Страховая выплатит, шахту закроем, и город наконец выдохнет."),
                Line("Холт", "А если саботажников не было?"),
                Line("Корнелиус Уитлок", "Тогда вы их найдёте. Вот аванс — пятьдесят долларов. И пропуск компании."),
                Money(50),
                Give(items.CompanyPass, 1, true),
                Line("Корнелиус Уитлок", "Отчёт — как можно скорее. Чем раньше вы уедете, тем лучше. Для всех."),
                Inner("Он нервничает. И всё время смотрит не на меня — на портрет за моей спиной."),
                Flag(StoryFlags.FreeInvestigationDay1),
                Card("Глава 1 · День 1\nСвободное расследование", 2.5f),
                Goal("Свободное расследование: осмотрите контору, поговорите с людьми в городе."));
            StartTrigger(meeting, F(StoryFlags.Day1Morning), F(StoryFlags.OfficeMeeting));

            // Clue: the portrait with fresh flowers.
            CutsceneSequence portraitClue = Sequence("Clue_Portrait", story, StoryFlags.CluePortrait,
                Inner("Иезекииль Уитлок, основатель. Рама без единой пылинки."),
                Inner("А под ней свежие цветы. Кто-то ухаживает за мёртвым так, будто он ещё здесь."),
                Note("Улика: портрет Иезекииля (свежие цветы)"));
            Talk(portrait, "Осмотреть", "", "Портрет Иезекииля Уитлока. Под рамой свежие цветы.").AddBranch(portraitClue, null, null);

            AddBounds(22f, Group("--- Collision & Bounds"));
            EditorSceneManager.SaveScene(scene, OfficeScene);
        }
    }
}
