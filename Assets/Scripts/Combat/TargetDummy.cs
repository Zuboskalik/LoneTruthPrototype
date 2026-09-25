using System.Collections;
using UnityEngine;

namespace LoneTruth.Combat
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class TargetDummy : MonoBehaviour
    {
        [Header("Hit Flash")]
        [SerializeField] private Color hitFlashColor = Color.red;
        [SerializeField] private float hitFlashDuration = 0.15f;

        [Header("Destruction")]
        [SerializeField] private GameObject crumbleParticlePrefab;
        [SerializeField] private Animator animator;
        [SerializeField] private string destroyTriggerName = "Destroy";
        [SerializeField] private float destroyDelay = 0f;

        private Health health;
        private SpriteRenderer spriteRenderer;
        private Color originalColor;
        private Coroutine flashRoutine;

        private void Awake()
        {
            health = GetComponent<Health>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            originalColor = spriteRenderer.color;

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            health.OnDamaged += HandleDamaged;
            health.OnDeath += HandleDeath;
        }

        private void OnDestroy()
        {
            health.OnDamaged -= HandleDamaged;
            health.OnDeath -= HandleDeath;
        }

        public void TakeDamage(int damage)
        {
            health.TakeDamage(damage);
        }

        private void HandleDamaged(int damage, int currentHealth)
        {
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }

            flashRoutine = StartCoroutine(HitFlashRoutine());
        }

        private IEnumerator HitFlashRoutine()
        {
            spriteRenderer.color = hitFlashColor;
            yield return new WaitForSeconds(hitFlashDuration);
            spriteRenderer.color = originalColor;
            flashRoutine = null;
        }

        private void HandleDeath()
        {
            if (crumbleParticlePrefab != null)
            {
                Instantiate(crumbleParticlePrefab, transform.position, transform.rotation);
            }

            if (animator != null && !string.IsNullOrEmpty(destroyTriggerName))
            {
                animator.SetTrigger(destroyTriggerName);
            }

            Destroy(gameObject, destroyDelay);
        }
    }
}
