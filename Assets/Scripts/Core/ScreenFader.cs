using System;
using System.Collections;
using UnityEngine;

namespace LoneTruth.Core
{
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenFader : MonoBehaviour
    {
        public static ScreenFader Instance { get; private set; }

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeOutDuration = 0.4f;
        [SerializeField] private float holdDuration = 0.15f;
        [SerializeField] private float fadeInDuration = 0.4f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            SetAlpha(0f);
        }

        public void FadeAndExecute(Action onPeak, Action onComplete = null)
        {
            StartCoroutine(FadeRoutine(onPeak, onComplete));
        }

        private IEnumerator FadeRoutine(Action onPeak, Action onComplete)
        {
            yield return Fade(0f, 1f, fadeOutDuration);

            onPeak?.Invoke();

            if (holdDuration > 0f)
            {
                yield return new WaitForSeconds(holdDuration);
            }

            yield return Fade(1f, 0f, fadeInDuration);

            onComplete?.Invoke();
        }

        public float Alpha => canvasGroup != null ? canvasGroup.alpha : 0f;

        public void SetAlphaImmediate(float alpha)
        {
            SetAlpha(alpha);
        }

        /// <summary>Fade from the current alpha to the target (used by cutscenes).</summary>
        public IEnumerator FadeTo(float target, float duration)
        {
            yield return Fade(Alpha, target, duration);
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                SetAlpha(to);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetAlpha(Mathf.Lerp(from, to, elapsed / duration));
                yield return null;
            }

            SetAlpha(to);
        }

        private void SetAlpha(float alpha)
        {
            canvasGroup.alpha = alpha;
            canvasGroup.blocksRaycasts = alpha > 0.99f;
            canvasGroup.interactable = false;
        }
    }
}
