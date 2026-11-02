using System.Collections;
using LoneTruth.Core;
using LoneTruth.Inventory;
using LoneTruth.Player;
using LoneTruth.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LoneTruth.Combat
{
    /// <summary>
    /// Holt's death rolls the game back to the last save (or the last rest, if he has not saved
    /// since). So a fight cannot lock the story, he always gets back enough rounds for one.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class PlayerDeath : MonoBehaviour
    {
        private const string DeathCard = "Темнота. Угольная пыль на зубах.";
        private const int MinRounds = 6;

        [SerializeField] private Color hitColor = new Color(1f, 0.25f, 0.2f);

        private Health health;
        private SpriteRenderer sprite;
        private Coroutine flashRoutine;

        private void Awake()
        {
            health = GetComponent<Health>();
            sprite = GetComponentInChildren<SpriteRenderer>();
            health.OnDamaged += HandleDamaged;
            health.OnDeath += HandleDeath;
        }

        private void OnDestroy()
        {
            health.OnDamaged -= HandleDamaged;
            health.OnDeath -= HandleDeath;
        }

        private void HandleDamaged(int damage, int remaining)
        {
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }

            flashRoutine = StartCoroutine(Flash());
        }

        private IEnumerator Flash()
        {
            sprite.color = hitColor;
            yield return new WaitForSeconds(0.18f);
            sprite.color = Color.white;
            flashRoutine = null;
        }

        private void HandleDeath()
        {
            StartCoroutine(RollBack());
        }

        private IEnumerator RollBack()
        {
            GameMaster gm = GameMaster.Instance;
            gm?.SetState(GameState.Cutscene);
            GetComponent<PlayerController>().ScriptedInput = 0f;
            ScreenFader fader = ScreenFader.Instance;
            if (fader != null)
            {
                yield return fader.FadeTo(1f, 0.8f);
            }

            GameUI ui = GameUI.Instance;
            ui.SetHudVisible(false);
            yield return ui.ShowCard(DeathCard, 2f);

            SaveData data = SaveSystem.LastCheckpoint ?? SaveSystem.Latest();
            if (data != null)
            {
                yield return SaveMenuUI.Instance.Restore(data);
            }
            else
            {
                // No save and no rest yet (only when a scene is started directly in the editor).
                health.SetCurrent(health.MaxHealth);
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                yield return null;
                ui.SetHudVisible(true);
                if (fader != null)
                {
                    yield return fader.FadeTo(0f, 0.8f);
                }

                gm?.SetState(GameState.Exploration);
            }

            TopUpRounds();
        }

        private void TopUpRounds()
        {
            var inventory = GetComponent<InventoryManager>();
            var weapon = GetComponent<WeaponController>();
            if (inventory == null || weapon == null || weapon.AmmoItem == null)
            {
                return;
            }

            int rounds = weapon.LoadedAmmo + inventory.CountItem(weapon.AmmoItem);
            if (rounds < MinRounds)
            {
                inventory.AddItem(weapon.AmmoItem, MinRounds - rounds);
            }
        }
    }
}
