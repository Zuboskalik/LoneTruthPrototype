using System;
using System.Collections;
using System.Collections.Generic;
using LoneTruth.Core;
using LoneTruth.Interaction;
using LoneTruth.Inventory;
using LoneTruth.Investigation;
using LoneTruth.Player;
using LoneTruth.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LoneTruth.Story
{
    public enum CutsceneStepType
    {
        Wait,
        SetBlack,        // screen instantly black (boolValue) or clear
        FadeOut,
        FadeIn,
        Card,            // full-screen text; duration ≤ 0 waits for input
        Line,            // dialogue line; empty speaker = Holt's inner voice
        MoveActor,       // NPC/prop to x at speed; wait=false runs in parallel
        MovePlayer,      // player walks to x with the walk animation
        TeleportPlayer,
        FaceActor,       // turn actor's sprite towards x
        SetActive,
        SetFlag,
        GiveItem,        // boolValue = announce with a toast
        GiveMoney,
        Objective,
        Toast,
        PlayerAnimation, // animator trigger on the player (key)
        LoadScene,       // boolValue = fade back in after loading
        Shard,           // key = Hope / Despair / Truth, amount
        Keystone,        // key = key truth id (+1 Truth, once)
        QuestOutcome     // key = quest id, text = outcome id (sets the outcome flag, grants its shards)
    }

    [Serializable]
    public class CutsceneStep
    {
        public CutsceneStepType type;
        public string speaker;
        [TextArea(2, 8)] public string text;
        public Transform actor;
        public GameObject target;
        public float x;
        public float duration = 1f;
        public float speed = 1.6f;
        public bool wait = true;
        public bool boolValue = true;
        public string key;
        public ItemSO item;
        public int amount = 1;
        public string scene;
        public string spawnId;
    }

    /// <summary>
    /// A data-driven cutscene: a list of steps edited in the Inspector (or filled by the
    /// chapter builder). While it plays the GameMaster is in Cutscene, so the player, the
    /// inventory and interactions ignore input.
    /// </summary>
    public class CutsceneSequence : MonoBehaviour
    {
        [Tooltip("Set when the sequence finishes; triggers skip sequences whose flag is set.")]
        public string doneFlag;
        public List<CutsceneStep> steps = new List<CutsceneStep>();

        public bool IsPlaying { get; private set; }
        public static bool AnyPlaying { get; private set; }

        public bool IsDone => !string.IsNullOrEmpty(doneFlag) && StoryState.Has(doneFlag);

        public void Play()
        {
            if (!IsPlaying && !AnyPlaying)
            {
                StartCoroutine(Run());
            }
        }

        private IEnumerator Run()
        {
            IsPlaying = true;
            AnyPlaying = true;
            SetCutsceneMode(true);

            foreach (CutsceneStep step in steps)
            {
                if (step.type == CutsceneStepType.LoadScene)
                {
                    // This object dies with the scene, so the load (and the fade back in) runs on
                    // the persistent ScreenFader. Input stays locked until the new scene is up.
                    StoryState.Set(doneFlag);
                    IsPlaying = false;
                    AnyPlaying = false;
                    SpawnPoint.PendingSpawnId = step.spawnId;
                    ScreenFader fader = ScreenFader.Instance;
                    if (fader != null)
                    {
                        fader.StartCoroutine(LoadRoutine(step.scene, step.boolValue));
                    }
                    else
                    {
                        SceneManager.LoadScene(step.scene);
                        SetCutsceneMode(false);
                    }

                    yield break;
                }

                yield return Execute(step);
            }

            Finish();
        }

        private void Finish()
        {
            StoryState.Set(doneFlag);
            PlayerController pc = Player();
            if (pc != null)
            {
                pc.ScriptedInput = 0f;
            }

            SetCutsceneMode(false);
            IsPlaying = false;
            AnyPlaying = false;
        }

        private void OnDestroy()
        {
            if (IsPlaying)
            {
                AnyPlaying = false;
            }
        }

        private static void SetCutsceneMode(bool on)
        {
            if (GameMaster.Instance != null)
            {
                GameMaster.Instance.SetState(on ? GameState.Cutscene : GameState.Exploration);
            }

            if (GameUI.Instance != null)
            {
                GameUI.Instance.SetHudVisible(!on);
            }
        }

        private static PlayerController Player()
        {
            GameObject go = PersistentPlayer.Current;
            return go != null ? go.GetComponent<PlayerController>() : null;
        }

        private IEnumerator Execute(CutsceneStep s)
        {
            GameUI ui = GameUI.Instance;
            ScreenFader fader = ScreenFader.Instance;

            switch (s.type)
            {
                case CutsceneStepType.Wait:
                    yield return new WaitForSeconds(s.duration);
                    break;

                case CutsceneStepType.SetBlack:
                    if (fader != null) fader.SetAlphaImmediate(s.boolValue ? 1f : 0f);
                    break;

                case CutsceneStepType.FadeOut:
                    if (fader != null) yield return fader.FadeTo(1f, s.duration);
                    break;

                case CutsceneStepType.FadeIn:
                    if (fader != null) yield return fader.FadeTo(0f, s.duration);
                    break;

                case CutsceneStepType.Card:
                    if (ui != null) yield return ui.ShowCard(s.text, s.duration);
                    break;

                case CutsceneStepType.Line:
                    CutsceneActor.SetTalking(s.speaker, true);
                    if (ui != null) yield return ui.ShowLine(s.speaker, s.text);
                    CutsceneActor.SetTalking(s.speaker, false);
                    break;

                case CutsceneStepType.MoveActor:
                    if (s.actor != null)
                    {
                        if (s.wait) yield return MoveActor(s.actor, s.x, s.speed);
                        else StartCoroutine(MoveActor(s.actor, s.x, s.speed));
                    }

                    break;

                case CutsceneStepType.MovePlayer:
                    if (s.wait) yield return MovePlayer(s.x);
                    else StartCoroutine(MovePlayer(s.x));
                    break;

                case CutsceneStepType.TeleportPlayer:
                    TeleportPlayer(s.x);
                    break;

                case CutsceneStepType.FaceActor:
                    if (s.actor != null) Face(s.actor, s.x - s.actor.position.x);
                    break;

                case CutsceneStepType.SetActive:
                    if (s.target != null) s.target.SetActive(s.boolValue);
                    break;

                case CutsceneStepType.SetFlag:
                    StoryState.Set(s.key, s.boolValue);
                    break;

                case CutsceneStepType.GiveItem:
                    GiveItem(s.item, s.amount, s.boolValue);
                    break;

                case CutsceneStepType.GiveMoney:
                    StoryState.AddInt(StoryState.MoneyKey, s.amount);
                    if (ui != null && s.boolValue) ui.Toast(Loc.Format("Получено: ${0}", s.amount));
                    break;

                case CutsceneStepType.Objective:
                    if (ui != null) ui.SetObjective(s.text);
                    break;

                case CutsceneStepType.Shard:
                    if (Enum.TryParse(s.key, out ShardType shard)) ShardManager.AddShard(shard, Mathf.Max(1, s.amount));
                    else Debug.LogWarning($"[{name}] Unknown shard type '{s.key}'.");
                    break;

                case CutsceneStepType.Keystone:
                    ShardManager.AddKeystone(s.key);
                    break;

                case CutsceneStepType.QuestOutcome:
                    Notebook.CompleteQuest(s.key, s.text);
                    break;

                case CutsceneStepType.Toast:
                    if (ui != null) ui.Toast(s.text, s.duration > 0f ? s.duration : 3.5f);
                    break;

                case CutsceneStepType.PlayerAnimation:
                    PlayerController pc = Player();
                    if (pc != null) pc.PlayAnimationTrigger(s.key);
                    break;
            }
        }

        private static void GiveItem(ItemSO item, int amount, bool announce)
        {
            GameObject player = PersistentPlayer.Current;
            InventoryManager inv = player != null ? player.GetComponent<InventoryManager>() : null;
            if (item == null || inv == null)
            {
                return;
            }

            inv.AddItem(item, Mathf.Max(1, amount));
            if (announce && GameUI.Instance != null)
            {
                GameUI.Instance.Toast(amount > 1
                    ? Loc.Format("Получено: {0} ×{1}", item.DisplayName, amount)
                    : Loc.Format("Получено: {0}", item.DisplayName));
            }
        }

        private static IEnumerator MoveActor(Transform actor, float x, float speed)
        {
            Face(actor, x - actor.position.x);
            Animator animator = actor.GetComponentInChildren<Animator>();
            SetSpeed(animator, 1f);
            while (Mathf.Abs(actor.position.x - x) > 0.02f)
            {
                Vector3 p = actor.position;
                p.x = Mathf.MoveTowards(p.x, x, speed * Time.deltaTime);
                actor.position = p;
                yield return null;
            }

            SetSpeed(animator, 0f);
        }

        private static IEnumerator MovePlayer(float x)
        {
            PlayerController pc = Player();
            if (pc == null)
            {
                yield break;
            }

            float timeout = 15f;
            while (timeout > 0f && Mathf.Abs(pc.transform.position.x - x) > 0.12f)
            {
                pc.ScriptedInput = Mathf.Sign(x - pc.transform.position.x);
                timeout -= Time.deltaTime;
                yield return null;
            }

            pc.ScriptedInput = 0f;
            yield return new WaitForFixedUpdate();
        }

        private static void TeleportPlayer(float x)
        {
            PlayerController pc = Player();
            if (pc == null)
            {
                return;
            }

            Vector3 p = pc.transform.position;
            p.x = x;
            Rigidbody rb = pc.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = p;
                rb.linearVelocity = Vector3.zero;
            }

            pc.transform.position = p;
        }

        private static void Face(Transform actor, float dx)
        {
            if (Mathf.Abs(dx) < 0.01f)
            {
                return;
            }

            SpriteRenderer sr = actor.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.flipX = dx < 0f; // character art faces right
            }
        }

        private static void SetSpeed(Animator animator, float value)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.name == "Speed" && p.type == AnimatorControllerParameterType.Float)
                {
                    animator.SetFloat("Speed", value);
                    return;
                }
            }
        }

        /// <param name="fadeIn">false keeps the screen black for the next scene's opening card.</param>
        private static IEnumerator LoadRoutine(string scene, bool fadeIn)
        {
            ScreenFader fader = ScreenFader.Instance;
            if (fader.Alpha < 0.99f)
            {
                yield return fader.FadeTo(1f, 0.6f);
            }

            SceneManager.LoadScene(scene);
            yield return null;
            yield return null;

            if (!AnyPlaying)
            {
                SetCutsceneMode(false);
            }

            if (fadeIn)
            {
                yield return fader.FadeTo(0f, 0.6f);
            }
        }
    }
}
