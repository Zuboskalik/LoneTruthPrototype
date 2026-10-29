using System.Collections.Generic;
using LoneTruth.Core;
using LoneTruth.Dialogue;
using LoneTruth.Investigation;
using UnityEditor;
using UnityEngine;

namespace LoneTruth.EditorTools
{
    /// <summary>
    /// Chapter 1 dialogues with answers (DialogueSO) and the speaker portraits for the dialogue
    /// panel. Answers follow the design doc: side-quest outcomes give one shard, the Truth answer
    /// appears only when its clue is in the notebook.
    /// </summary>
    public static partial class Chapter1LocationBuilder
    {
        private const string DialogueDir = "Assets/ScriptableObjects/Dialogue/Chapter1";
        private const string PortraitsPath = "Assets/ScriptableObjects/Dialogue/SpeakerPortraits.asset";
        private const string ShopDir = "Assets/ScriptableObjects/Shops";

        private static DialogueSO.Node DNode(string id, string speaker, string text, string next = "", params DialogueSO.Choice[] choices)
        {
            return new DialogueSO.Node { id = id, speaker = speaker, text = text, next = next, choices = new List<DialogueSO.Choice>(choices) };
        }

        private static DialogueSO.Choice DChoice(string text, string next = "")
        {
            return new DialogueSO.Choice { text = text, next = next };
        }

        private static DialogueSO DialogueAsset(string name, string start, params DialogueSO.Node[] nodes)
        {
            string path = $"{DialogueDir}/{name}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<DialogueSO>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<DialogueSO>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.Configure(start, new List<DialogueSO.Node>(nodes));
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static LoneTruth.Story.CutsceneStep Talking(DialogueSO dialogue) =>
            new LoneTruth.Story.CutsceneStep { type = LoneTruth.Story.CutsceneStepType.Dialogue, dialogue = dialogue };

        // ------------------------------------------------------------------ 1-16 Jesse (quest 1.2)

        /// <summary>First contact: Jesse aims; Holt names Agnes — or backs off.</summary>
        private static DialogueSO JesseContactDialogue()
        {
            return DialogueAsset("Jesse_FirstContact", "aim",
                DNode("aim", "Джесси Коул", "Стой! Ещё шаг — и выстрелю.", "",
                    DChoice("Спокойно. Я не от компании. Меня позвала Агнес Бирн.", "agnes"),
                    DChoice("Ладно, ладно. Ухожу.")),
                DNode("agnes", "Джесси Коул", "Агнес?.. Жена Томаса.", "name"),
                DNode("name", "Джесси Коул", "Джесси Коул. Я выбрался оттуда до обвала. А потом меня зацепило.", "deal"),
                new DialogueSO.Node
                {
                    id = "deal", speaker = "Джесси Коул",
                    text = "Мне нужен бинт и виски. Вывезите меня из города тайком — отдам карту нижних горизонтов.",
                    next = "wound", setFlags = new[] { StoryFlags.Quest12FoundJesse }
                },
                DNode("wound", "", "Рана в боку, повязка насквозь. Долго он здесь не протянет."));
        }

        /// <summary>The choice of quest 1.2 (design doc 8.4): Hope, Despair, or Truth with the .45 casing.</summary>
        private static DialogueSO JesseDealDialogue(Items items)
        {
            var hope = DChoice("Держи мой бинт. Отец Мэтью спрячет тебя в ризнице.", "hope");
            hope.requiredItem = items.Bandage;
            hope.lockedHint = "нужен бинт";
            hope.takeItem = items.Bandage;
            hope.questId = "1_2";
            hope.outcomeId = "hope";
            hope.setFlags = new[] { "jesse_in_vestry", "map_via_liam" };

            var despair = DChoice("Давай карту. Дальше сам.", "despair");
            despair.questId = "1_2";
            despair.outcomeId = "despair";
            despair.giveItem = items.LowerLevelsMap;
            despair.setFlags = new[] { "jesse_left_alone" };

            // Truth needs both the .45 casing and a look at the wound (design doc 8.4).
            var wound = DChoice("Дай взгляну на рану.", "wound");
            wound.forbiddenFlags = new[] { "jesse_wound_examined" };
            wound.hideWhenLocked = true;

            var truth = DChoice("Тебя не камнем задело. Кто стрелял?", "truth");
            truth.presentClue = "clue_shell_45";
            truth.requiredFlags = new[] { "jesse_wound_examined" };
            truth.hideWhenLocked = true;
            truth.questId = "1_2";
            truth.outcomeId = "truth";
            truth.giveItem = items.LowerLevelsMap;
            truth.setFlags = new[] { "jesse_in_vestry", "clue_jesse_testimony" };

            return DialogueAsset("Jesse_Deal", "ask",
                DNode("ask", "Джесси Коул", "Ну что, мистер? Бинт и виски?", "",
                    hope, despair, wound, truth, DChoice("Пока ничего. Я вернусь.")),
                new DialogueSO.Node
                {
                    id = "wound", speaker = "", next = "ask", setFlags = new[] { "jesse_wound_examined" },
                    text = "Края ровные, внутри засел металл. Это не камень — это пуля."
                },
                DNode("hope", "Джесси Коул", "Спасибо, мистер. Карту завтра принесёт Лиам."),
                DNode("despair", "Джесси Коул", "Держи. …Будь ты проклят, сыщик.", "despair_inner"),
                DNode("despair_inner", "", "Карта у меня. А Джесси останется здесь один."),
                DNode("truth", "Джесси Коул", "Пуля. За час до обвала люди компании закладывали заряды — Грейди с ними. Я видел. А Кэллахан стрелял мне вслед.", "truth_end"),
                DNode("truth_end", "Джесси Коул", "Я всё напишу, мистер. И карту отдам."));
        }

        // ------------------------------------------------------------------ 1-03 Ruth (trade)

        private static LoneTruth.Shop.ShopSO RuthShop(Items items)
        {
            string path = $"{ShopDir}/Shop_Ruth.asset";
            var shop = AssetDatabase.LoadAssetAtPath<LoneTruth.Shop.ShopSO>(path);
            if (shop == null)
            {
                shop = ScriptableObject.CreateInstance<LoneTruth.Shop.ShopSO>();
                AssetDatabase.CreateAsset(shop, path);
            }

            // Goods from the maps (1-03): rounds, bandages, whiskey. The flare and the crowbar ($15)
            // join when the Hollows and the air-shaft grate exist. Prices other than the crowbar's
            // are not in the design doc.
            shop.Configure("ruth", "«Последняя лампа» — лавка Руфи", new List<LoneTruth.Shop.ShopSO.Entry>
            {
                new LoneTruth.Shop.ShopSO.Entry { item = items.Ammo, amount = 6, price = 6 },
                new LoneTruth.Shop.ShopSO.Entry { item = items.Bandage, amount = 1, price = 8 },
                new LoneTruth.Shop.ShopSO.Entry { item = items.Whiskey, amount = 1, price = 10 }
            });
            EditorUtility.SetDirty(shop);
            return shop;
        }

        private static DialogueSO RuthDialogue(Items items)
        {
            LoneTruth.Shop.ShopSO shop = RuthShop(items);
            DialogueSO.Choice Trade(string next)
            {
                var c = DChoice("Покажи, что продаёшь.", next);
                c.openShop = shop;
                return c;
            }

            return DialogueAsset("Ruth_Talk", "ask",
                DNode("ask", "Руфь", "Чего вам, мистер Холт?", "",
                    Trade("again"), DChoice("Ничего. Бывай.")),
                DNode("again", "Руфь", "Что-нибудь ещё?", "",
                    Trade("again"), DChoice("Нет. Спасибо, Руфь.")));
        }

        // ------------------------------------------------------------------ portraits

        private static SpeakerPortraitsSO BuildSpeakerPortraits()
        {
            var asset = AssetDatabase.LoadAssetAtPath<SpeakerPortraitsSO>(PortraitsPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<SpeakerPortraitsSO>();
                AssetDatabase.CreateAsset(asset, PortraitsPath);
            }

            Sprite Placeholder(string key) => S($"{CharDir}/CH_{key}_Ref.png");
            Sprite Sheet(string key, string fallbackKey)
            {
                CharacterSheet sheet = NpcSheet(key);
                return sheet != null ? SheetFrame(sheet, NpcIdleFrame(key)) : Placeholder(fallbackKey);
            }

            asset.Configure(new List<SpeakerPortraitsSO.Entry>
            {
                new SpeakerPortraitsSO.Entry { speaker = "Холт", portrait = HasHoltSheet ? HoltFrame("Holt_Idle_00") : Placeholder("Holt") },
                new SpeakerPortraitsSO.Entry { speaker = "Кэллахан", portrait = Sheet("Callahan", "Callahan") },
                new SpeakerPortraitsSO.Entry { speaker = "Руфь", portrait = Sheet("Ruth", "Ruth") },
                new SpeakerPortraitsSO.Entry { speaker = "Проводник", portrait = Sheet("Conductor", "Conductor") },
                new SpeakerPortraitsSO.Entry { speaker = "Корнелиус Уитлок", portrait = Placeholder("Cornelius") },
                new SpeakerPortraitsSO.Entry { speaker = "Шериф Долан", portrait = Placeholder("Dolan") },
                new SpeakerPortraitsSO.Entry { speaker = "Отец Мэтью", portrait = Placeholder("Matthew") },
                new SpeakerPortraitsSO.Entry { speaker = "Агнес Бирн", portrait = Placeholder("Agnes") },
                new SpeakerPortraitsSO.Entry { speaker = "Джесси Коул", portrait = Placeholder("Jesse") }
            });
            EditorUtility.SetDirty(asset);
            return asset;
        }
    }
}
