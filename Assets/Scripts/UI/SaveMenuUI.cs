using System;
using System.Collections;
using System.Collections.Generic;
using LoneTruth.Combat;
using LoneTruth.Core;
using LoneTruth.Interaction;
using LoneTruth.Inventory;
using LoneTruth.Player;
using LoneTruth.Story;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LoneTruth.UI
{
    /// <summary>
    /// The save window: picking a slot at the bed, «New game / Continue» at the start of the game,
    /// and restoring a save (from Continue, or after Holt dies). W / S — pick, E — confirm, Esc — back.
    /// </summary>
    [RequireComponent(typeof(GameUI))]
    public class SaveMenuUI : MonoBehaviour
    {
        public static SaveMenuUI Instance { get; private set; }

        private const string SavedSpawn = "from_1-03"; // Holt comes back by his room's door
        private const string TransientFlag = StoryFlags.WeaponDrawn;
        private const int MaxRows = 4;
        private const int DeleteRequest = -2; // Choose reports «delete slot n» as DeleteRequest - n

        /// <summary>Tests: answer the next menu with this row (-1 = wait for a key).</summary>
        public static int AutoPick = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            AutoPick = -1;
        }

        [SerializeField] private ItemSO[] itemCatalog;
        [SerializeField] private Color panelColor = new Color(0.04f, 0.045f, 0.055f, 0.96f);
        [SerializeField] private Color titleColor = new Color(0.78f, 0.57f, 0.18f);
        [SerializeField] private Color textColor = new Color(0.93f, 0.9f, 0.85f);
        [SerializeField] private Color mutedColor = new Color(0.55f, 0.53f, 0.49f);

        private GameUI ui;
        private GameObject panel;
        private Text titleText;
        private Text hintText;
        private readonly List<Text> rows = new List<Text>();

        public void SetCatalog(ItemSO[] items)
        {
            itemCatalog = items;
        }

        private void Awake()
        {
            if (GameUI.Instance != null && GameUI.Instance.gameObject != gameObject)
            {
                return;
            }

            Instance = this;
            ui = GetComponent<GameUI>();
        }

        private void Start()
        {
            if (Instance == this)
            {
                Build();
            }
        }

        public bool IsReady => panel != null;

        private void Update()
        {
            // The clock runs with the game: not under the start menu, not while the notebook pauses it.
            if (!CutsceneTrigger.HoldSceneStart)
            {
                SaveSystem.PlaySeconds += Time.deltaTime;
            }
        }

        // ---------------------------------------------------------------- flows

        /// <summary>The bed's «Save the game»: pick one of three slots (a filled one is overwritten).</summary>
        public IEnumerator SaveToSlot()
        {
            while (true)
            {
                int picked = -1;
                yield return ChooseSlot("Сохранить игру", false, i => picked = i);
                if (picked < 0)
                {
                    yield break;
                }

                if (SaveSystem.Read(picked) != null)
                {
                    // «No» is the safe default: a hasty E does not lose an old save.
                    int answer = -1;
                    yield return Choose("Перезаписать?", new[] { "Да", "Нет" }, new[] { true, true }, true, i => answer = i, 1);
                    if (answer != 0)
                    {
                        continue;
                    }
                }

                SaveSystem.Write(picked, Capture());
                GameUI.Instance.Toast("Игра сохранена.");
                yield break;
            }
        }

        /// <summary>The start of the game. Calls back with true when the prologue should run.</summary>
        public IEnumerator StartMenu(Action<bool> startNewGame)
        {
            while (true)
            {
                int choice = -1;
                // All saves may have been deleted from the slot list below.
                yield return Choose("Lone Truth", new[] { "Новая игра", "Продолжить" }, new[] { true, SaveSystem.AnySaves() }, false, i => choice = i);
                if (choice == 0)
                {
                    startNewGame(true);
                    yield break;
                }

                int slot = -1;
                yield return ChooseSlot("Продолжить", true, i => slot = i);
                SaveData data = slot >= 0 ? SaveSystem.Read(slot) : null;
                if (data != null)
                {
                    startNewGame(false);
                    yield return Restore(data);
                    yield break;
                }
            }
        }

        /// <summary>Puts the whole game back into the saved state and opens its location.</summary>
        public IEnumerator Restore(SaveData data)
        {
            ScreenFader fader = ScreenFader.Instance;
            GameMaster gm = GameMaster.Instance;
            gm?.SetState(GameState.Cutscene);
            if (fader != null)
            {
                if (fader.Alpha < 0.99f)
                {
                    yield return fader.FadeTo(1f, 0.6f);
                }
            }

            CutsceneSequence.AbortAll();
            // Flag triggers of the scene being left must not react to the restored flags.
            CutsceneTrigger.HoldSceneStart = true;
            Apply(data);
            CutsceneTrigger.HoldSceneStart = false;
            SaveSystem.SetCheckpoint(data);
            SaveSystem.PlaySeconds = data.played;
            SpawnPoint.PendingSpawnId = data.spawn;
            SceneManager.LoadScene(data.scene);
            yield return null;
            yield return null;
            GameUI.Instance.SetHudVisible(true);
            if (fader != null)
            {
                yield return fader.FadeTo(0f, 0.8f);
            }

            gm?.SetState(GameState.Exploration);
        }

        /// <summary>The current state, ready to be written to a slot or kept as the death checkpoint.</summary>
        public SaveData Capture()
        {
            var flags = new List<string>();
            var ints = new List<string>();
            StoryState.Export(flags, ints);
            flags.Remove(TransientFlag);

            GameObject player = PersistentPlayer.Current;
            var items = new List<string>();
            foreach (InventorySlot slot in player.GetComponent<InventoryManager>().Slots)
            {
                if (!slot.IsEmpty)
                {
                    items.Add(slot.item.Id + "*" + slot.quantity);
                }
            }

            return new SaveData
            {
                scene = SceneManager.GetActiveScene().name,
                spawn = SavedSpawn,
                goal = GameUI.Instance.RawObjective,
                time = DateTime.UtcNow.Ticks,
                played = Mathf.RoundToInt(SaveSystem.PlaySeconds),
                hp = player.GetComponent<Health>().CurrentHealth,
                rounds = player.GetComponent<WeaponController>().LoadedAmmo,
                flags = flags.ToArray(),
                ints = ints.ToArray(),
                items = items.ToArray()
            };
        }

        private void Apply(SaveData data)
        {
            StoryState.Import(data.flags, data.ints);
            GameObject player = PersistentPlayer.Current;
            var slots = new List<InventorySlot>();
            foreach (string entry in data.items ?? new string[0])
            {
                int star = entry.LastIndexOf('*');
                ItemSO item = FindItem(entry.Substring(0, star));
                if (item != null)
                {
                    slots.Add(new InventorySlot(item, int.Parse(entry.Substring(star + 1))));
                }
            }

            player.GetComponent<InventoryManager>().RestoreSlots(slots);
            player.GetComponent<PlayerDeath>().RefreshHopeBonus();
            player.GetComponent<Health>().SetCurrent(Mathf.Max(1, data.hp));
            GetComponent<PocketWatchUI>().Resync();
            player.GetComponent<WeaponController>().RestoreState(data.rounds);
            player.GetComponent<PlayerController>().ScriptedInput = 0f;
            GameUI.Instance.SetObjective(data.goal);
        }

        private ItemSO FindItem(string id)
        {
            foreach (ItemSO item in itemCatalog)
            {
                if (item != null && item.Id == id)
                {
                    return item;
                }
            }

            Debug.LogWarning($"[Save] Unknown item '{id}'.");
            return null;
        }

        // ---------------------------------------------------------------- menus

        /// <summary>The slot list. X / Delete on a filled slot deletes it after asking; picked gets -1 on Esc.</summary>
        private IEnumerator ChooseSlot(string title, bool filledOnly, Action<int> picked)
        {
            int cursor = -1;
            while (true)
            {
                var labels = new string[SaveSystem.SlotCount];
                var enabled = new bool[SaveSystem.SlotCount];
                bool any = false;
                for (int i = 0; i < SaveSystem.SlotCount; i++)
                {
                    SaveData data = SaveSystem.Read(i);
                    labels[i] = Loc.Format("Слот {0}", i + 1) + "   ·   " + (data != null ? SaveSystem.Describe(data) : Loc.T("пусто"));
                    enabled[i] = !filledOnly || data != null;
                    any |= enabled[i];
                }

                if (!any)
                {
                    picked(-1); // the last save was just deleted
                    yield break;
                }

                int result = -1;
                yield return Choose(title, labels, enabled, true, r => result = r, cursor >= 0 && enabled[cursor] ? cursor : -1, true);
                if (result > DeleteRequest)
                {
                    picked(result);
                    yield break;
                }

                cursor = DeleteRequest - result;
                int answer = -1;
                yield return Choose("Удалить сохранение?", new[] { "Да", "Нет" }, new[] { true, true }, true, i => answer = i, 1);
                if (answer == 0)
                {
                    SaveSystem.Delete(cursor);
                }
            }
        }

        /// <param name="labels">Row texts; each is translated (ready-made labels pass through unchanged).</param>
        private IEnumerator Choose(string title, string[] labels, bool[] enabled, bool canCancel, Action<int> picked, int preselected = -1, bool canDelete = false)
        {
            int selected = preselected >= 0 ? preselected : Array.IndexOf(enabled, true);
            panel.SetActive(true);
            Render(title, labels, enabled, selected, canCancel, canDelete);
            yield return null; // the key that opened the menu must not answer it

            int result = -1;
            while (true)
            {
                if (AutoPick != -1)
                {
                    result = AutoPick;
                    AutoPick = -1;
                    break;
                }

                int move = 0;
                if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) || Input.mouseScrollDelta.y < -0.1f) move = 1;
                if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.mouseScrollDelta.y > 0.1f) move = -1;
                if (move != 0)
                {
                    int next = selected;
                    do
                    {
                        next = (next + move + labels.Length) % labels.Length;
                    }
                    while (!enabled[next]);
                    selected = next;
                    Render(title, labels, enabled, selected, canCancel, canDelete);
                }

                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                {
                    result = selected;
                    break;
                }

                if (canDelete && (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Delete)) && SaveSystem.Read(selected) != null)
                {
                    result = DeleteRequest - selected;
                    break;
                }

                if (canCancel && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab)))
                {
                    break;
                }

                yield return null;
            }

            panel.SetActive(false);
            yield return null; // don't let the closing key reach what comes next
            picked(result);
        }

        private void Render(string title, string[] labels, bool[] enabled, int selected, bool canCancel, bool canDelete)
        {
            titleText.text = Loc.T(title);
            for (int i = 0; i < rows.Count; i++)
            {
                Text row = rows[i];
                if (i >= labels.Length)
                {
                    row.text = "";
                    continue;
                }

                bool isSelected = i == selected;
                row.text = $"{(isSelected ? "›" : " ")}  {Loc.T(labels[i])}";
                row.color = !enabled[i] ? mutedColor : isSelected ? titleColor : textColor;
                row.fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal;
            }

            hintText.text = Loc.T(canDelete ? "W / S — выбрать   ·   E — подтвердить   ·   X — удалить   ·   Esc — назад"
                : canCancel ? "W / S — выбрать   ·   E — подтвердить   ·   Esc — назад"
                : "W / S — выбрать   ·   E — подтвердить");
        }

        private void Build()
        {
            panel = new GameObject("SavePanel", typeof(RectTransform), typeof(Image), typeof(Canvas));
            panel.transform.SetParent(transform, false);
            // Above the screen fader: the menu shows over the black screen.
            var canvas = panel.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 150;
            var bg = panel.GetComponent<Image>();
            bg.color = panelColor;
            bg.raycastTarget = false;
            GameUI.Place((RectTransform)panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(860, 360));

            titleText = ui.MakeText("Title", panel.transform, 30, titleColor, TextAnchor.UpperLeft);
            titleText.fontStyle = FontStyle.Bold;
            GameUI.Place(titleText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -30), new Vector2(780, 40));
            for (int i = 0; i < MaxRows; i++)
            {
                Text row = ui.MakeText($"Row_{i}", panel.transform, 26, textColor, TextAnchor.MiddleLeft);
                GameUI.Place(row.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -100 - i * 46), new Vector2(780, 42));
                rows.Add(row);
            }

            hintText = ui.MakeText("Hint", panel.transform, 18, mutedColor, TextAnchor.LowerRight);
            GameUI.Place(hintText.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 16), new Vector2(780, 26));
            panel.SetActive(false);
        }
    }
}
