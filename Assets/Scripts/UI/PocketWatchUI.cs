using System.Collections;
using System.Collections.Generic;
using LoneTruth.Core;
using LoneTruth.Investigation;
using UnityEngine;
using UnityEngine.UI;

namespace LoneTruth.UI
{
    /// <summary>
    /// The father's pocket watch in the corner of the HUD (design doc, section 4). It stopped at
    /// 4:17; every shard Holt gains sets a coloured fragment into the cracked glass with a short
    /// chime, so the mosaic shows where the story leans. No numbers here — those are in the notebook.
    /// Also shows a short "notebook updated" note above the watch.
    /// </summary>
    [RequireComponent(typeof(GameUI))]
    public class PocketWatchUI : MonoBehaviour
    {
        private const int FragmentCount = 24;

        // Fragments fill the glass in a scattered order, like cracks spreading.
        private static readonly int[] SlotOrder =
        {
            7, 19, 2, 13, 22, 9, 4, 16, 0, 11, 20, 6, 14, 1, 18, 10, 3, 23, 8, 15, 5, 21, 12, 17
        };

        [SerializeField] private float size = 150f;
        [SerializeField] private Color rimColor = new Color(0.45f, 0.36f, 0.22f);
        [SerializeField] private Color faceColor = new Color(0.13f, 0.125f, 0.12f);
        [SerializeField] private Color inkColor = new Color(0.86f, 0.81f, 0.69f);
        [SerializeField] private Color hopeColor = new Color(0.96f, 0.66f, 0.2f, 0.85f);
        [SerializeField] private Color despairColor = new Color(0.45f, 0.17f, 0.55f, 0.9f);
        [SerializeField] private Color truthColor = new Color(0.86f, 0.94f, 1f, 0.85f);
        [SerializeField] private float notebookNoteSeconds = 4f;

        private static Sprite circle;

        private RectTransform root;
        private readonly List<Image> fragments = new List<Image>();
        private int shown;
        private Text notebookNote;
        private Coroutine noteRoutine;
        private AudioSource audioSource;
        private AudioClip chime;
        private bool visible = true;

        private void Start()
        {
            Build();
            Refresh(false);
        }

        private void OnEnable()
        {
            ShardManager.ShardGained += OnShardGained;
            Notebook.Updated += OnNotebookUpdated;
            Loc.Changed += Relocalize;
        }

        private void OnDisable()
        {
            ShardManager.ShardGained -= OnShardGained;
            Notebook.Updated -= OnNotebookUpdated;
            Loc.Changed -= Relocalize;
        }

        public void SetVisible(bool value)
        {
            visible = value;
            if (root != null)
            {
                root.gameObject.SetActive(value);
            }
        }

        private void OnShardGained(ShardType type, int total)
        {
            Refresh(true);
        }

        private void OnNotebookUpdated()
        {
            if (notebookNote == null)
            {
                return;
            }

            if (noteRoutine != null)
            {
                StopCoroutine(noteRoutine);
            }

            noteRoutine = StartCoroutine(ShowNote());
        }

        private void Relocalize()
        {
            if (notebookNote != null)
            {
                notebookNote.text = Loc.T(NoteText);
            }
        }

        private const string NoteText = "Блокнот обновлён · Tab";

        /// <summary>Shows a fragment for every shard gained so far; new ones arrive with a chime.</summary>
        private void Refresh(bool animate)
        {
            if (fragments.Count == 0)
            {
                return;
            }

            int total = Mathf.Min(ShardManager.Total, FragmentCount);
            for (int i = shown; i < total; i++)
            {
                Image fragment = fragments[SlotOrder[i]];
                fragment.color = ColorOf(ShardManager.GetInOrder(i));
                fragment.gameObject.SetActive(true);
                if (animate && isActiveAndEnabled)
                {
                    StartCoroutine(SetFragment(fragment));
                }
            }

            if (animate && total > shown && audioSource != null)
            {
                audioSource.PlayOneShot(chime, 0.6f);
            }

            shown = total;
        }

        private Color ColorOf(ShardType type)
        {
            switch (type)
            {
                case ShardType.Hope: return hopeColor;
                case ShardType.Despair: return despairColor;
                default: return truthColor;
            }
        }

        private IEnumerator SetFragment(Image fragment)
        {
            Color target = fragment.color;
            float t = 0f;
            while (t < 0.6f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / 0.6f);
                Color c = target;
                c.a = target.a * k;
                fragment.color = c;
                // A small jolt of the whole watch as the piece clicks in.
                root.localScale = Vector3.one * (1f + 0.12f * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }

            fragment.color = target;
            root.localScale = Vector3.one;
        }

        private IEnumerator ShowNote()
        {
            notebookNote.text = Loc.T(NoteText);
            notebookNote.gameObject.SetActive(true);
            for (float t = 0f; t < notebookNoteSeconds + 1f; t += Time.unscaledDeltaTime)
            {
                float alpha = t < 0.3f ? t / 0.3f : t > notebookNoteSeconds ? 1f - (t - notebookNoteSeconds) : 1f;
                Color c = notebookNote.color;
                c.a = Mathf.Clamp01(alpha);
                notebookNote.color = c;
                yield return null;
            }

            notebookNote.gameObject.SetActive(false);
            noteRoutine = null;
        }

        // ---------------------------------------------------------------- building

        private void Build()
        {
            GameUI ui = GetComponent<GameUI>();
            var go = new GameObject("PocketWatch", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            root = (RectTransform)go.transform;
            GameUI.Place(root, new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-48 - size / 2, 40 + size / 2), new Vector2(size, size));

            Disc("Crown", root, new Vector2(0, size * 0.54f), size * 0.16f, rimColor);
            Disc("Rim", root, Vector2.zero, size, rimColor);
            Disc("Face", root, Vector2.zero, size * 0.88f, faceColor);

            // Hour marks.
            for (int h = 0; h < 12; h++)
            {
                float a = h * 30f * Mathf.Deg2Rad;
                float r = size * 0.36f;
                RectTransform mark = Bar("Mark", root, new Vector2(Mathf.Sin(a) * r, Mathf.Cos(a) * r),
                    new Vector2(h % 3 == 0 ? 4f : 2.5f, h % 3 == 0 ? 12f : 7f), inkColor);
                mark.localEulerAngles = new Vector3(0, 0, -h * 30f);
            }

            // Shard fragments: wedges of the glass, hidden until earned.
            var glass = new GameObject("Fragments", typeof(RectTransform));
            glass.transform.SetParent(root, false);
            GameUI.Place((RectTransform)glass.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.84f, size * 0.84f));
            for (int i = 0; i < FragmentCount; i++)
            {
                Image wedge = Disc($"Fragment_{i}", glass.transform, Vector2.zero, size * 0.84f, Color.white);
                wedge.type = Image.Type.Filled;
                wedge.fillMethod = Image.FillMethod.Radial360;
                wedge.fillOrigin = (int)Image.Origin360.Top;
                wedge.fillClockwise = true;
                wedge.fillAmount = 1f / FragmentCount;
                wedge.rectTransform.localEulerAngles = new Vector3(0, 0, -i * 360f / FragmentCount);
                wedge.gameObject.SetActive(false);
                fragments.Add(wedge);
            }

            // Hands stopped at 4:17 — the hour of the 1906 collapse.
            Hand("HourHand", (4f + 17f / 60f) * 30f, size * 0.24f, 5f);
            Hand("MinuteHand", 17f * 6f, size * 0.36f, 3f);
            Disc("Pin", root, Vector2.zero, 9f, rimColor);

            // Cracks across the glass and a faint sheen.
            var crack = new Color(1f, 1f, 1f, 0.32f);
            Vector2 impact = new Vector2(size * 0.14f, size * 0.12f);
            foreach (var (angle, length) in new[] { (20f, 0.24f), (95f, 0.22f), (160f, 0.4f), (215f, 0.3f), (290f, 0.38f), (330f, 0.2f) })
            {
                float a = angle * Mathf.Deg2Rad;
                float l = size * length;
                RectTransform line = Bar("Crack", root, impact + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * l / 2f, new Vector2(1.5f, l), crack);
                line.localEulerAngles = new Vector3(0, 0, -angle);
            }

            Disc("Sheen", root, new Vector2(-size * 0.12f, size * 0.14f), size * 0.4f, new Color(1f, 1f, 1f, 0.05f));

            // Under the watch root, so it hides with the HUD (cutscenes, the open notebook).
            notebookNote = ui.MakeText("NotebookNote", root, 22, inkColor, TextAnchor.LowerRight);
            GameUI.Place(notebookNote.rectTransform, new Vector2(1, 1), new Vector2(1, 0), new Vector2(0, 14), new Vector2(600, 32));
            notebookNote.gameObject.SetActive(false);

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            chime = MakeChime();

            root.gameObject.SetActive(visible);
        }

        private void Hand(string name, float degrees, float length, float width)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(root, false);
            var img = go.GetComponent<Image>();
            img.color = inkColor;
            img.raycastTarget = false;
            RectTransform rt = img.rectTransform;
            GameUI.Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(width, length));
            rt.localEulerAngles = new Vector3(0, 0, -degrees);
        }

        private static Image Disc(string name, Transform parent, Vector2 pos, float diameter, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = Circle();
            img.color = color;
            img.raycastTarget = false;
            GameUI.Place(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(diameter, diameter));
            return img;
        }

        private static RectTransform Bar(string name, Transform parent, Vector2 pos, Vector2 barSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            GameUI.Place(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, barSize);
            return img.rectTransform;
        }

        /// <summary>A soft-edged white disc, generated once.</summary>
        private static Sprite Circle()
        {
            if (circle != null)
            {
                return circle;
            }

            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            float r = n / 2f - 1f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f));
                    byte a = (byte)(Mathf.Clamp01(r - d + 0.5f) * 255);
                    px[y * n + x] = new Color32(255, 255, 255, a);
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            circle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            return circle;
        }

        /// <summary>A short glassy chime: two decaying partials.</summary>
        private static AudioClip MakeChime()
        {
            const int rate = 44100;
            int samples = (int)(rate * 0.9f);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Exp(-t * 6f) * Mathf.Clamp01(t * 400f);
                data[i] = env * (0.55f * Mathf.Sin(2f * Mathf.PI * 1568f * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * 2349f * t)
                                 + 0.15f * Mathf.Sin(2f * Mathf.PI * 3136f * t));
            }

            AudioClip clip = AudioClip.Create("ShardChime", samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
