using System.Collections.Generic;
using System.Linq;
using System.Text;
using LoneTruth.Core;
using LoneTruth.Investigation;
using UnityEngine;
using UnityEngine.UI;

namespace LoneTruth.UI
{
    /// <summary>
    /// The detective's notebook (Tab): clues, people, quests and the chapter map, plus the shard
    /// counts — the only place numbers are shown. Clues can be marked and compared; the right set
    /// gives a conclusion (DeductionSO). Opens only during exploration and pauses the game.
    /// </summary>
    [RequireComponent(typeof(GameUI))]
    public class NotebookUI : MonoBehaviour
    {
        private enum Section { Clues, People, Quests, Map }

        private static readonly string[] SectionTitles = { "Улики", "Люди", "Квесты", "Карта" };

        [SerializeField] private ChapterNotebookSO chapter;
        [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
        [SerializeField] private Color paperColor = new Color(0.84f, 0.8f, 0.7f);
        [SerializeField] private Color inkColor = new Color(0.16f, 0.14f, 0.12f);
        [SerializeField] private Color fadedInkColor = new Color(0.16f, 0.14f, 0.12f, 0.62f);
        [SerializeField] private Color accentColor = new Color(0.55f, 0.2f, 0.12f);
        [SerializeField] private int visibleRows = 14;

        public static bool IsOpen { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsOpen = false;
        }

        private GameUI ui;
        private GameObject root;
        private Text chapterText;
        private Text shardsText;
        private readonly List<Text> tabTexts = new List<Text>();
        private readonly List<Text> rows = new List<Text>();
        private GameObject listPanel;
        private GameObject detailPanel;
        private Text detailTitle;
        private Text detailSubtitle;
        private Text detailBody;
        private Image portrait;
        private Text messageText;
        private Text hintText;
        private RectTransform mapPanel;
        private readonly List<GameObject> mapItems = new List<GameObject>();

        private Section section;
        private int selected;
        private int scroll;
        private readonly HashSet<ClueSO> marked = new HashSet<ClueSO>();
        private string message = "";
        private float savedTimeScale = 1f;

        public void SetChapter(ChapterNotebookSO notebookChapter)
        {
            chapter = notebookChapter;
            Notebook.SetChapter(chapter);
        }

        private void Awake()
        {
            ui = GetComponent<GameUI>();
            Notebook.SetChapter(chapter);
        }

        private void Start()
        {
            Build();
            root.SetActive(false);
        }

        private void OnEnable()
        {
            Loc.Changed += OnLanguageChanged;
        }

        private void OnDisable()
        {
            Loc.Changed -= OnLanguageChanged;
            if (IsOpen)
            {
                Close();
            }
        }

        private void OnLanguageChanged()
        {
            if (IsOpen) Render();
        }

        // ---------------------------------------------------------------- input

        private void Update()
        {
            if (root == null)
            {
                return;
            }

            GameMaster gm = GameMaster.Instance;
            if (!IsOpen)
            {
                bool exploring = gm == null || gm.IsInState(GameState.Exploration);
                if (exploring && Input.GetKeyDown(toggleKey))
                {
                    Open();
                }

                return;
            }

            if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }

            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) SwitchSection(1);
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) SwitchSection(-1);
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) || Input.mouseScrollDelta.y < -0.1f) Move(1);
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.mouseScrollDelta.y > 0.1f) Move(-1);
            if (section == Section.Clues && Input.GetKeyDown(KeyCode.Space)) ToggleMark();
            if (section == Section.Clues && Input.GetKeyDown(KeyCode.F)) Compare();

            if (Input.GetMouseButtonDown(0))
            {
                for (int i = 0; i < tabTexts.Count; i++)
                {
                    if (RectTransformUtility.RectangleContainsScreenPoint(tabTexts[i].rectTransform, Input.mousePosition, null))
                    {
                        SwitchSection(i - (int)section);
                    }
                }
            }
        }

        public void Open()
        {
            if (IsOpen || root == null)
            {
                return;
            }

            IsOpen = true;
            if (GameMaster.Instance != null)
            {
                GameMaster.Instance.SetState(GameState.Notebook);
            }

            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            ui.SetHudVisible(false);
            message = "";
            selected = 0;
            scroll = 0;
            root.SetActive(true);
            Render();
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            Time.timeScale = savedTimeScale;
            if (GameMaster.Instance != null && GameMaster.Instance.IsInState(GameState.Notebook))
            {
                GameMaster.Instance.SetState(GameState.Exploration);
            }

            ui.SetHudVisible(true);
            root.SetActive(false);
        }

        private void SwitchSection(int delta)
        {
            if (delta == 0)
            {
                return;
            }

            int count = SectionTitles.Length;
            section = (Section)(((int)section + delta + count) % count);
            selected = 0;
            scroll = 0;
            message = "";
            Render();
        }

        private void Move(int delta)
        {
            int count = Entries().Count;
            if (count == 0)
            {
                return;
            }

            selected = Mathf.Clamp(selected + delta, 0, count - 1);
            if (selected < scroll) scroll = selected;
            if (selected >= scroll + visibleRows) scroll = selected - visibleRows + 1;
            Render();
        }

        private void ToggleMark()
        {
            List<object> entries = Entries();
            if (selected < entries.Count && entries[selected] is ClueSO clue)
            {
                if (!marked.Remove(clue))
                {
                    marked.Add(clue);
                }

                message = "";
                Render();
            }
        }

        private void Compare()
        {
            if (marked.Count < 2)
            {
                message = Loc.T("Отметьте пробелом хотя бы две улики, чтобы сопоставить их.");
                Render();
                return;
            }

            DeductionSO deduction = Notebook.Compare(marked, out bool isNew);
            if (deduction == null)
            {
                message = Loc.T("Не складывается. Чего-то не хватает — или это не связано.");
            }
            else
            {
                message = (isNew ? Loc.T("Вывод") : Loc.T("Уже записано")) + ": " + Loc.T(deduction.Title);
                marked.Clear();
                // Jump to the conclusion at the end of the list.
                selected = Entries().IndexOf(deduction);
                if (selected >= scroll + visibleRows) scroll = selected - visibleRows + 1;
            }

            Render();
        }

        // ---------------------------------------------------------------- content

        private List<object> Entries()
        {
            switch (section)
            {
                case Section.Clues:
                    return Notebook.KnownClues().Cast<object>().Concat(Notebook.ConcludedDeductions()).ToList();
                case Section.People:
                    return Notebook.KnownPeople().Cast<object>().ToList();
                case Section.Quests:
                    return Notebook.VisibleQuests().Cast<object>().ToList();
                default:
                    return new List<object>();
            }
        }

        private void Render()
        {
            chapterText.text = chapter != null ? Loc.T(chapter.ChapterTitle) : "";
            shardsText.text = string.Format("{0} {1}   ·   {2} {3}   ·   {4} {5}   ·   {6} {7}/{8}",
                Loc.T("Надежда"), ShardManager.Get(ShardType.Hope),
                Loc.T("Отчаяние"), ShardManager.Get(ShardType.Despair),
                Loc.T("Истина"), ShardManager.Get(ShardType.Truth),
                Loc.T("Ключевые истины"), ShardManager.KeystoneCount, ShardManager.KeystonesRequired);

            for (int i = 0; i < tabTexts.Count; i++)
            {
                bool active = i == (int)section;
                tabTexts[i].text = Loc.T(SectionTitles[i]).ToUpperInvariant();
                tabTexts[i].color = active ? accentColor : fadedInkColor;
                tabTexts[i].fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
            }

            bool map = section == Section.Map;
            listPanel.SetActive(!map);
            detailPanel.SetActive(!map);
            mapPanel.gameObject.SetActive(map);
            hintText.text = Loc.T(section == Section.Clues
                ? "A / D — раздел   ·   W / S — запись   ·   Пробел — отметить улику   ·   F — сопоставить   ·   Tab — закрыть"
                : "A / D — раздел   ·   W / S — запись   ·   Tab — закрыть");

            if (map)
            {
                RenderMap();
                return;
            }

            List<object> entries = Entries();
            selected = Mathf.Clamp(selected, 0, Mathf.Max(0, entries.Count - 1));
            for (int r = 0; r < rows.Count; r++)
            {
                int i = scroll + r;
                Text row = rows[r];
                if (i >= entries.Count)
                {
                    row.text = r == 0 && entries.Count == 0 ? Loc.T(EmptyText()) : "";
                    row.color = fadedInkColor;
                    row.fontStyle = FontStyle.Italic;
                    continue;
                }

                bool isSelected = i == selected;
                row.text = (isSelected ? "›  " : "    ") + EntryLabel(entries[i]);
                row.color = isSelected ? accentColor : inkColor;
                row.fontStyle = entries[i] is DeductionSO ? FontStyle.Italic : isSelected ? FontStyle.Bold : FontStyle.Normal;
            }

            RenderDetails(entries.Count > 0 ? entries[selected] : null);
            messageText.text = message;
        }

        private string EmptyText()
        {
            switch (section)
            {
                case Section.Clues: return "Улик пока нет.";
                case Section.People: return "Пока ни с кем не знаком.";
                default: return "Записей пока нет.";
            }
        }

        private string EntryLabel(object entry)
        {
            switch (entry)
            {
                case ClueSO clue:
                    return (marked.Contains(clue) ? "• " : "") + Loc.T(clue.Title);
                case DeductionSO deduction:
                    return Loc.T("Вывод") + ": " + Loc.T(deduction.Title);
                case PersonSO person:
                    return Loc.T(person.DisplayName);
                case QuestSO quest:
                    string status = quest.Status == QuestStatus.Done ? "  — " + Loc.T("завершён")
                        : quest.Status == QuestStatus.Failed ? "  — " + Loc.T("провален") : "";
                    return Loc.T(quest.Title) + status;
                default:
                    return "";
            }
        }

        private void RenderDetails(object entry)
        {
            portrait.gameObject.SetActive(false);
            var body = new StringBuilder();
            switch (entry)
            {
                case ClueSO clue:
                    detailTitle.text = Loc.T(clue.Title);
                    detailSubtitle.text = Loc.T(clue.Place);
                    body.Append(Loc.T(clue.Description));
                    if (marked.Contains(clue))
                    {
                        body.Append("\n\n").Append(Loc.T("Отмечено для сопоставления."));
                    }

                    break;

                case DeductionSO deduction:
                    detailTitle.text = Loc.T(deduction.Title);
                    detailSubtitle.text = Loc.T("Вывод из улик") + ": " +
                                          string.Join(", ", deduction.Clues.Select(c => Loc.T(c.Title)));
                    body.Append(Loc.T(deduction.Conclusion));
                    break;

                case PersonSO person:
                    detailTitle.text = Loc.T(person.DisplayName);
                    detailSubtitle.text = Loc.T(person.Role);
                    body.Append(Loc.T(person.Description));
                    foreach (PersonSO.Note note in person.Notes)
                    {
                        if (StoryState.Has(note.flag))
                        {
                            body.Append("\n\n— ").Append(Loc.T(note.text));
                        }
                    }

                    if (person.Portrait != null)
                    {
                        portrait.sprite = person.Portrait;
                        portrait.gameObject.SetActive(true);
                    }

                    break;

                case QuestSO quest:
                    detailTitle.text = Loc.T(quest.Title);
                    detailSubtitle.text = Loc.T(quest.IsMainQuest ? "Основное расследование" : "Побочное дело");
                    foreach (QuestSO.Stage stage in quest.ReachedStages())
                    {
                        if (body.Length > 0) body.Append("\n\n");
                        body.Append("— ").Append(Loc.T(stage.text));
                    }

                    QuestSO.Outcome outcome = quest.ChosenOutcome;
                    if (outcome != null)
                    {
                        body.Append("\n\n").Append(Loc.T("Итог")).Append(": ").Append(Loc.T(outcome.text));
                    }

                    break;

                default:
                    detailTitle.text = "";
                    detailSubtitle.text = "";
                    break;
            }

            detailBody.text = body.ToString();
        }

        private void RenderMap()
        {
            foreach (GameObject item in mapItems)
            {
                Destroy(item);
            }

            mapItems.Clear();
            if (chapter == null)
            {
                return;
            }

            Vector2 area = mapPanel.rect.size;
            Vector2 Pos(ChapterNotebookSO.MapLocation l) => new Vector2((l.position.x - 0.5f) * area.x, (l.position.y - 0.5f) * area.y);
            var byId = chapter.Map.ToDictionary(l => l.id);

            // Links first, so the boxes sit on top.
            foreach (ChapterNotebookSO.MapLocation l in chapter.Map)
            {
                if (string.IsNullOrEmpty(l.linkedTo) || !byId.TryGetValue(l.linkedTo, out var other))
                {
                    continue;
                }

                Vector2 a = Pos(l), b = Pos(other);
                var link = new GameObject("Link", typeof(RectTransform), typeof(Image));
                link.transform.SetParent(mapPanel, false);
                var img = link.GetComponent<Image>();
                img.color = fadedInkColor;
                img.raycastTarget = false;
                GameUI.Place(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), (a + b) / 2f, new Vector2(3f, (b - a).magnitude));
                img.rectTransform.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg - 90f);
                mapItems.Add(link);
            }

            string here = Notebook.CurrentScene;
            foreach (ChapterNotebookSO.MapLocation l in chapter.Map)
            {
                bool visited = Notebook.IsVisited(l.scene);
                bool current = !string.IsNullOrEmpty(l.scene) && l.scene == here;
                var box = new GameObject("Place_" + l.id, typeof(RectTransform), typeof(Image));
                box.transform.SetParent(mapPanel, false);
                var img = box.GetComponent<Image>();
                img.color = current ? accentColor : visited ? inkColor : new Color(inkColor.r, inkColor.g, inkColor.b, 0.18f);
                img.raycastTarget = false;
                GameUI.Place(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Pos(l), new Vector2(18f, 18f));
                mapItems.Add(box);

                // A scrap of paper behind each label, so links never run through the text.
                var backing = new GameObject("LabelBack_" + l.id, typeof(RectTransform), typeof(Image));
                backing.transform.SetParent(mapPanel, false);
                var back = backing.GetComponent<Image>();
                back.color = paperColor;
                back.raycastTarget = false;
                mapItems.Add(backing);

                Text label = Ink("Label_" + l.id, mapPanel, 20, visited || current ? inkColor : fadedInkColor, TextAnchor.UpperCenter);
                label.text = Loc.T(l.title) + (current ? "\n" + Loc.T("(вы здесь)") : "");
                label.fontStyle = current ? FontStyle.Bold : FontStyle.Normal;
                if (current) label.color = accentColor;
                GameUI.Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), Pos(l) + new Vector2(0, -14f), new Vector2(220f, 60f));
                mapItems.Add(label.gameObject);
                GameUI.Place(back.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), Pos(l) + new Vector2(0, -12f),
                    new Vector2(Mathf.Min(220f, label.preferredWidth) + 12f, label.preferredHeight + 4f));
            }

            Text legend = Ink("Legend", mapPanel, 18, fadedInkColor, TextAnchor.LowerLeft);
            legend.text = Loc.T("Тёмные — где я уже был. Блёклые — ещё не заходил.");
            GameUI.Place(legend.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, -6), new Vector2(900, 28));
            mapItems.Add(legend.gameObject);
        }

        // ---------------------------------------------------------------- building

        private void Build()
        {
            root = new GameObject("Notebook", typeof(RectTransform), typeof(Canvas));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 90; // above the HUD, below cards (120) and the fader (100)
            var rt = (RectTransform)root.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Panel("Dim", root.transform, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.72f), stretch: true);
            RectTransform page = Panel("Page", root.transform, Vector2.zero, new Vector2(1560, 900), paperColor);
            Panel("Spine", page, new Vector2(-780 + 34, 0), new Vector2(6, 900), new Color(0.55f, 0.2f, 0.12f, 0.35f));

            chapterText = Ink("Chapter", page, 30, inkColor, TextAnchor.UpperLeft);
            chapterText.fontStyle = FontStyle.Bold;
            GameUI.Place(chapterText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(70, -34), new Vector2(700, 40));

            shardsText = Ink("Shards", page, 20, inkColor, TextAnchor.UpperRight);
            GameUI.Place(shardsText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-50, -42), new Vector2(900, 30));

            for (int i = 0; i < SectionTitles.Length; i++)
            {
                Text tab = Ink("Tab_" + i, page, 24, fadedInkColor, TextAnchor.MiddleLeft);
                GameUI.Place(tab.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(70 + i * 190, -92), new Vector2(180, 36));
                tabTexts.Add(tab);
            }

            Panel("Rule", page, new Vector2(0, 900 / 2f - 124), new Vector2(1440, 2), new Color(inkColor.r, inkColor.g, inkColor.b, 0.35f));

            listPanel = new GameObject("List", typeof(RectTransform));
            listPanel.transform.SetParent(page, false);
            GameUI.Place((RectTransform)listPanel.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(70, -146), new Vector2(560, 640));
            for (int r = 0; r < visibleRows; r++)
            {
                Text row = Ink("Row_" + r, listPanel.transform, 24, inkColor, TextAnchor.MiddleLeft);
                row.horizontalOverflow = HorizontalWrapMode.Overflow;
                GameUI.Place(row.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -r * 44), new Vector2(560, 40));
                rows.Add(row);
            }

            detailPanel = new GameObject("Details", typeof(RectTransform));
            detailPanel.transform.SetParent(page, false);
            GameUI.Place((RectTransform)detailPanel.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(680, -146), new Vector2(810, 660));
            detailTitle = Ink("Title", detailPanel.transform, 30, inkColor, TextAnchor.UpperLeft);
            detailTitle.fontStyle = FontStyle.Bold;
            GameUI.Place(detailTitle.rectTransform, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(600, 40));
            detailSubtitle = Ink("Subtitle", detailPanel.transform, 20, fadedInkColor, TextAnchor.UpperLeft);
            detailSubtitle.fontStyle = FontStyle.Italic;
            GameUI.Place(detailSubtitle.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -44), new Vector2(600, 56));
            detailBody = Ink("Body", detailPanel.transform, 24, inkColor, TextAnchor.UpperLeft);
            detailBody.lineSpacing = 1.15f;
            GameUI.Place(detailBody.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -108), new Vector2(600, 500));

            var portraitGo = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portraitGo.transform.SetParent(detailPanel.transform, false);
            portrait = portraitGo.GetComponent<Image>();
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            GameUI.Place(portrait.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(190, 300));

            messageText = Ink("Message", page, 22, accentColor, TextAnchor.MiddleLeft);
            messageText.fontStyle = FontStyle.Italic;
            GameUI.Place(messageText.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(680, 70), new Vector2(810, 34));

            hintText = Ink("Hint", page, 18, fadedInkColor, TextAnchor.MiddleLeft);
            GameUI.Place(hintText.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(70, 26), new Vector2(1420, 28));

            var mapGo = new GameObject("Map", typeof(RectTransform));
            mapGo.transform.SetParent(page, false);
            mapPanel = (RectTransform)mapGo.transform;
            GameUI.Place(mapPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(1380, 620));
        }

        private RectTransform Panel(string name, Transform parent, Vector2 pos, Vector2 panelSize, Color color, bool stretch = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            if (stretch)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
            else
            {
                GameUI.Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, panelSize);
            }

            return rt;
        }

        /// <summary>Ink on paper: the game font without the HUD's drop shadow.</summary>
        private Text Ink(string name, Transform parent, int fontSize, Color color, TextAnchor anchor)
        {
            Text text = ui.MakeText(name, parent, fontSize, color, anchor);
            Shadow shadow = text.GetComponent<Shadow>();
            if (shadow != null)
            {
                Destroy(shadow);
            }

            return text;
        }
    }
}
