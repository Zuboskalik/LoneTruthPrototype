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

        /// <summary>Set by «I'm leaving»: the sequence then takes Holt back to the alley.</summary>
        private const string JesseLeaveFlag = "jesse_leave";

        /// <summary>First contact: Jesse aims; Holt names Agnes — or backs off.</summary>
        private static DialogueSO JesseContactDialogue() =>
            JesseContact("Jesse_FirstContact", "Стой! Ещё шаг — и выстрелю.");

        /// <summary>The same choice once Holt has put the revolver away at Jesse's demand.</summary>
        private static DialogueSO JesseHolsteredDialogue() =>
            JesseContact("Jesse_Holstered", "Вот так. Медленно… А теперь говори: кто ты такой и чего тебе надо?");

        private static DialogueSO JesseContact(string asset, string opening)
        {
            var leave = DChoice("Ладно, ладно. Ухожу.");
            leave.setFlags = new[] { JesseLeaveFlag };
            return DialogueAsset(asset, "aim",
                DNode("aim", "Джесси Коул", opening, "",
                    DChoice("Спокойно. Я не от компании. Меня позвала Агнес Бирн.", "agnes"),
                    leave),
                // Naming Agnes lowers the shotgun (FlagAnimatorBool on Jesse).
                new DialogueSO.Node
                {
                    id = "agnes", speaker = "Джесси Коул", text = "Агнес?.. Жена Томаса.", next = "name",
                    setFlags = new[] { StoryFlags.JesseCalm }
                },
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
        private static DialogueSO JesseDealDialogue(Items items) => JesseDeal(items, "Jesse_Deal", "Ну что, мистер? Бинт и виски?");

        /// <summary>The deal right after the first contact: the same answers, a different opening.</summary>
        private static DialogueSO JesseDealAfterContact(Items items) => JesseDeal(items, "Jesse_DealFirst", "Так что скажешь, мистер?");

        private static DialogueSO JesseDeal(Items items, string asset, string opening)
        {
            var hope = DChoice("Держи бинт. А отец Мэтью спрячет тебя в ризнице.", "hope");
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

            return DialogueAsset(asset, "ask",
                // The wound comes first and the shard-giving Despair answer last but one, so a hasty
                // E examines the wound instead of taking the map.
                DNode("ask", "Джесси Коул", opening, "",
                    wound, hope, truth, despair, DChoice("Пока ничего. Я вернусь.")),
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

        /// <summary>Set once Holt has given Jesse a bandage after the Truth outcome.</summary>
        private const string JesseBandagedFlag = "jesse_bandaged";

        /// <summary>
        /// After the Truth outcome Holt has the map and the testimony but has not helped Jesse.
        /// With a bandage on him he can still give it: +1 Hope (giving up his own resource), once.
        /// </summary>
        private static DialogueSO JesseAfterTruthDialogue(Items items)
        {
            var give = DChoice("Дать бинт: «Держи. Перевяжи рану, пока не загноилась».", "thanks");
            give.requiredItem = items.Bandage;
            give.hideWhenLocked = true;
            give.takeItem = items.Bandage;
            give.givesShard = true;
            give.shard = ShardType.Hope;
            give.shards = 1;
            give.setFlags = new[] { JesseBandagedFlag };

            return DialogueAsset("Jesse_AfterTruth", "ask",
                DNode("ask", "Джесси Коул", "Карту ты получил, мистер. Чего ещё?", "",
                    give, DChoice("Ничего. Держись.")),
                DNode("thanks", "Джесси Коул", "Бинт?.. Спасибо, мистер. Я этого не забуду."));
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
                new SpeakerPortraitsSO.Entry { speaker = "Корнелиус Уитлок", portrait = Sheet("Cornelius", "Cornelius") },
                new SpeakerPortraitsSO.Entry { speaker = "Секретарь", portrait = Sheet("Clerk", "Clerk") },
                new SpeakerPortraitsSO.Entry { speaker = "Шериф Долан", portrait = Sheet("Dolan", "Dolan") },
                new SpeakerPortraitsSO.Entry { speaker = "Отец Мэтью", portrait = Sheet("Matthew", "Matthew") },
                new SpeakerPortraitsSO.Entry { speaker = "Агнес Бирн", portrait = Sheet("Agnes", "Agnes") },
                new SpeakerPortraitsSO.Entry { speaker = "Джесси Коул", portrait = Sheet("Jesse", "Jesse") }
            });
            EditorUtility.SetDirty(asset);
            return asset;
        }
    }
}
