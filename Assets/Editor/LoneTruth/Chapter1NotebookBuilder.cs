using System.Collections.Generic;
using LoneTruth.Core;
using LoneTruth.Investigation;
using UnityEditor;
using UnityEngine;

namespace LoneTruth.EditorTools
{
    /// <summary>
    /// Chapter 1 notebook data (design doc 3.2, 4, 8): clues, people, quests with their shard
    /// outcomes, the key truth «Десятина» and the chapter map. Entries for things the demo does
    /// not reach yet (archive, chapel, side quests) are prepared and stay hidden until their
    /// flags are set.
    /// </summary>
    public static partial class Chapter1LocationBuilder
    {
        private const string NotebookDir = "Assets/ScriptableObjects/Notebook/Chapter1";
        private const string NotebookPath = NotebookDir + "/Notebook_Chapter1.asset";

        private static T NotebookAsset<T>(string name) where T : ScriptableObject
        {
            string path = $"{NotebookDir}/{name}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static ClueSO Clue(string name, string flag, string title, string place, string description)
        {
            var clue = NotebookAsset<ClueSO>("Clue_" + name);
            clue.Configure(flag, title, place, description);
            return clue;
        }

        private static ChapterNotebookSO BuildNotebook()
        {
            // ---------------------------------------------------------------- clues
            var telegrams = Clue("UnsentTelegrams", StoryFlags.ClueTelegrams, "Неотправленные телеграммы", "Станция, телеграф",
                "Стопка телеграмм родным погибших шахтёров. Отправить не успели — или не дали. Компания держит их у себя.");
            var portraitClue = Clue("Portrait", StoryFlags.CluePortrait, "Портрет Иезекииля Уитлока", "Контора «Уитлок Майнинг»",
                "Основатель компании. Рама без единой пылинки, а под ней свежие цветы. Кто-то ухаживает за мёртвым так, будто он ещё здесь.");
            var report = Clue("CollapseReport", "clue_collapse_report", "Отчёт шерифа об обвале", "Контора шерифа",
                "Подписан шерифом Доланом. Долан проговорился: «Взрывали в 4:17. Взрывали, я сказал? Обрушилось». Часы отца остановились в то же время.");
            var ledger = Clue("Ledger", "clue_ledger", "Гроссбух добычи серебра", "Архив компании",
                "Перед каждым обвалом — 1882, 1906, 1936 — жила иссякает. На полях пометка «Д. уплачена» и время: 4:17. Через месяц после обвалов 1882 и 1906 годов находят новую богатую жилу.");
            var deathBook = Clue("DeathBook", "clue_death_book", "Приходская книга смертей", "Часовня Святой Варвары",
                "12, 17 и 23 погибших — в 1882, 1906 и 1936 годах. Тела не найдены ни разу.");
            var shell = Clue("Shell45", "clue_shell_45", "Гильза .45", "Задний двор конторы",
                "Лежала в грязи у заднего крыльца конторы компании. Калибр .45 — как кольт Кэллахана.");
            var token = Clue("Token1882", "clue_token_1882", "Жетон шахтёра 1882 года", "Кладбище",
                "Остался от Пустого, который рассыпался угольной пылью. Шахтёр, погибший полвека назад.");

            // ---------------------------------------------------------------- key truth
            var tithe = NotebookAsset<DeductionSO>("Deduction_Tithe");
            tithe.Configure("Десятина",
                "Обвалы — плата людьми за новую жилу. Каждые 24–30 лет, в 4:17, компания отдаёт шахте людей, и серебро возвращается. В 1936-м новой жилы нет.",
                "KEY_TITHE", "tithe", ledger, deathBook);

            // ---------------------------------------------------------------- people
            var agnes = NotebookAsset<PersonSO>("Person_Agnes");
            agnes.Configure("Агнес Бирн", "", "Вдова шахтёра Томаса Бирна",
                "Написала мне в Чикаго: «Томас жив. Он пишет мне по ночам. В последнем письме он назвал ваше имя».", null);

            var callahan = NotebookAsset<PersonSO>("Person_Callahan");
            callahan.Configure("Дьюк Кэллахан", StoryFlags.MetCallahan, "Начальник охраны «Уитлок Майнинг»",
                "Встретил меня на перроне: компания меня ждала. Проводил до салуна. На поясе кольт .45.",
                NpcSheet("Callahan") != null ? SheetFrame(CallahanSheet, "Idle_01") : null);
            callahan.AddNote("clue_shell_45", "У заднего крыльца конторы — гильза .45. Его калибр.");

            var ruth = NotebookAsset<PersonSO>("Person_Ruth");
            ruth.Configure("Руфь Маккензи", StoryFlags.SaloonCheckIn, "Хозяйка салуна «Последняя лампа»",
                "Сдала мне третий номер: компания платит. Знает всё, что говорят в городе.",
                NpcSheet("Ruth") != null ? SheetFrame(RuthSheet, "Stand_05") : null);
            ruth.AddNote(StoryFlags.RuthRumorBarn, "Говорит, собаки третью ночь лают на сарай за конюшней.");

            var cornelius = NotebookAsset<PersonSO>("Person_Cornelius");
            cornelius.Configure("Корнелиус Уитлок", StoryFlags.OfficeMeeting, "Директор «Уитлок Майнинг», внук основателя",
                "Нанял меня подтвердить гибель 23 человек и вину профсоюзных саботажников — ради страховки. Хочет, чтобы я уехал как можно скорее.",
                S($"{CharDir}/CH_Cornelius_Ref.png"));
            cornelius.AddNote(StoryFlags.CluePortrait, "Нервничает и всё время смотрит на портрет деда у меня за спиной.");

            // ---------------------------------------------------------------- quests
            var main = NotebookAsset<QuestSO>("Quest_Main");
            main.Configure("ch1", "Холлоу-Рок", true, "");
            main.AddStage("", "Найти на перроне человека, который меня встречает.");
            main.AddStage(StoryFlags.MetCallahan, "Кэллахан проводит меня до салуна «Последняя лампа». Номер оплачен компанией.");
            main.AddStage(StoryFlags.SaloonCheckIn, "Переночевать в номере над салуном.");
            main.AddStage(StoryFlags.Day1Morning, "Утром явиться в контору «Уитлок Майнинг» на Главной улице.");
            main.AddStage(StoryFlags.FreeInvestigationDay1,
                "Корнелиус хочет, чтобы я подтвердил гибель 23 человек. Узнать, что на самом деле случилось в шахте: поговорить с людьми, осмотреть город.");

            // Story choices (2 shards each) — reached later in the chapter.
            var signature = NotebookAsset<QuestSO>("Quest_Signature");
            signature.Configure("ch1_signature", "Цена подписи", true, "");
            signature.AddStage("choice_signature", "Корнелиус положил на стол $500 и 23 бланка актов о смерти: подписать и уехать сегодня же.");
            signature.AddOutcome("refuse", ShardType.Hope, 2, "", "Отказался: «Мёртвых я подписываю, когда вижу тела».");
            signature.AddOutcome("sign", ShardType.Despair, 2, "", "Взял деньги и подписал.");

            var trigger = NotebookAsset<QuestSO>("Quest_CallahanTrigger");
            trigger.Configure("ch1_callahan", "Курок Кэллахана", true, "");
            trigger.AddStage("choice_callahan", "Раненый Кэллахан лежит среди ящиков с динамитом.");
            trigger.AddOutcome("spare", ShardType.Hope, 2, "", "Пощадил Кэллахана и отдал шерифу.");
            trigger.AddOutcome("leave", ShardType.Despair, 2, "", "Оставил его у горящего шнура.");
            trigger.AddOutcome("truth", ShardType.Truth, 2, "KEY_TITHE",
                "«Кто отдаёт приказы Уитлоку?» Кэллахан признался: Корнелиус получает письма из шахты, написанные рукой покойного деда.");

            // Side quests (1 shard each).
            var letters = NotebookAsset<QuestSO>("Quest_1_1_Letters");
            letters.Configure("1_1", "Письма из-под земли", false, "agnes_door_closed");
            letters.AddStage("q1_1_started", "Агнес показала семь писем, написанных рукой Томаса, с угольной пылью на сгибах. Они появляются на её подоконнике каждую ночь.");
            letters.AddStage("q1_1_postman", "Письма приносит Лиам: они вылетают с тягой из вентиляционной скважины на окраине.");
            letters.AddOutcome("hope", ShardType.Hope, 1, "", "Сказал Агнес, что Томас, скорее всего, жив, и пообещал вернуть его.");
            letters.AddOutcome("despair", ShardType.Despair, 1, "", "Продал сведения о вентиляции Корнелиусу, а Агнес сказал, что письма — жестокая шутка мальчишки.");
            letters.AddOutcome("truth", ShardType.Truth, 1, "",
                "Почерк тот же, но в новых письмах есть слова, которых Томас не знал. Его рукой кто-то водит.");

            var barn = NotebookAsset<QuestSO>("Quest_1_2_Barn");
            barn.Configure("1_2", "Раненый в сарае", false, "");
            barn.AddStage(StoryFlags.RuthRumorBarn, "Руфь говорит, что собаки третью ночь лают на сарай за конюшней. Сарай — в переулке за домом Агнес.");
            barn.AddStage("q1_2_found_jesse", "В сарае прячется раненый шахтёр Джесси Коул. Просит бинт и виски, обещает отдать карту нижних горизонтов.");
            barn.AddOutcome("hope", ShardType.Hope, 1, "", "Отдал свой бинт; отец Мэтью спрячет Джесси в ризнице.");
            barn.AddOutcome("despair", ShardType.Despair, 1, "", "Забрал карту и ушёл.");
            barn.AddOutcome("truth", ShardType.Truth, 1, "clue_shell_45",
                "«Тебя не камнем задело. Кто стрелял?» Джесси видел, как люди компании закладывали заряды за час до обвала. Кэллахан стрелял ему вслед.");

            var bell = NotebookAsset<QuestSO>("Quest_1_3_Bell");
            bell.Configure("1_3", "Ночной колокол", false, "");
            bell.AddStage("q1_3_started", "Каждую ночь в 4:17 звонит колокол часовни, хотя колокольня заперта. Отец Мэтью просит выяснить причину — и явно чего-то боится.");
            bell.AddOutcome("hope", ShardType.Hope, 1, "", "Простил отца Мэтью и уговорил помогать людям.");
            bell.AddOutcome("despair", ShardType.Despair, 1, "", "Шантажировал отца Мэтью книгой и пожертвованиями.");
            bell.AddOutcome("truth", ShardType.Truth, 1, "clue_token_1882",
                "«Чьи гробы пустые?» Среди погибших 1906 года — Эдгар Холт. Мой отец.");

            // ---------------------------------------------------------------- map
            var map = new List<ChapterNotebookSO.MapLocation>
            {
                Place("1-01", "Станция", StationScene, 0.06f, 0.88f, "1-02"),
                Place("1-02", "Главная улица", StreetScene, 0.30f, 0.62f, ""),
                Place("1-03", "Салун «Последняя лампа»", SaloonScene, 0.12f, 0.36f, "1-02"),
                Place("1-04", "Номер Холта", RoomScene, 0.12f, 0.10f, "1-03"),
                Place("1-05", "Контора компании", OfficeScene, 0.34f, 0.30f, "1-02"),
                Place("1-06", "Архив", "", 0.34f, 0.06f, "1-05"),
                Place("1-08", "Контора шерифа", "", 0.54f, 0.36f, "1-02"),
                Place("1-09", "Часовня Святой Варвары", "", 0.52f, 0.88f, "1-02"),
                Place("1-10", "Кладбище", "", 0.76f, 0.88f, "1-09"),
                Place("1-14", "Переулок и дом Агнес", "", 0.74f, 0.36f, "1-02"),
                Place("1-15", "Окраина и вентиляционная скважина", "", 0.90f, 0.12f, "1-14"),
                Place("1-18", "Лесопилка и склад взрывчатки", "", 0.72f, 0.62f, "1-02"),
                Place("1-21", "Ворота шахты", "", 0.93f, 0.62f, "1-18")
            };

            var chapter = AssetDatabase.LoadAssetAtPath<ChapterNotebookSO>(NotebookPath);
            if (chapter == null)
            {
                chapter = ScriptableObject.CreateInstance<ChapterNotebookSO>();
                AssetDatabase.CreateAsset(chapter, NotebookPath);
            }

            chapter.Configure("Глава 1 · Холлоу-Рок",
                new List<ClueSO> { telegrams, portraitClue, report, ledger, deathBook, shell, token },
                new List<PersonSO> { agnes, callahan, ruth, cornelius },
                new List<QuestSO> { main, signature, trigger, letters, barn, bell },
                new List<DeductionSO> { tithe },
                map);
            EditorUtility.SetDirty(chapter);
            AssetDatabase.SaveAssets();
            return chapter;
        }

        private static ChapterNotebookSO.MapLocation Place(string id, string title, string scenePath, float x, float y, string linkedTo)
        {
            return new ChapterNotebookSO.MapLocation
            {
                id = id,
                title = title,
                scene = string.IsNullOrEmpty(scenePath) ? "" : System.IO.Path.GetFileNameWithoutExtension(scenePath),
                position = new Vector2(x, y),
                linkedTo = linkedTo
            };
        }
    }
}
