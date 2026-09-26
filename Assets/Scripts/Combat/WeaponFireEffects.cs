using System.Collections;
using UnityEngine;

namespace LoneTruth.Combat
{
    public class WeaponFireEffects : MonoBehaviour
    {
        [Header("Tracer")]
        [SerializeField] private float tracerDuration = 0.06f;
        [SerializeField] private float tracerWidth = 0.05f;
        [SerializeField] private Color tracerColor = new Color(1f, 0.9f, 0.4f, 1f);

        [Header("Impact Flash")]
        [SerializeField] private Sprite impactSprite;
        [SerializeField] private float impactFlashDuration = 0.15f;
        [SerializeField] private float impactStartScale = 0.15f;
        [SerializeField] private float impactEndScale = 0.5f;

        private LineRenderer tracerLine;
        private Coroutine tracerRoutine;

        private void Awake()
        {
            EnsureTracerLine();
        }

        private void EnsureTracerLine()
        {
            if (tracerLine != null)
            {
                return;
            }

            GameObject tracerObject = new GameObject("MuzzleTracer");
            tracerObject.transform.SetParent(transform, false);

            tracerLine = tracerObject.AddComponent<LineRenderer>();
            tracerLine.positionCount = 2;
            tracerLine.startWidth = tracerWidth;
            tracerLine.endWidth = tracerWidth * 0.4f;
            tracerLine.numCapVertices = 4;
            tracerLine.sortingLayerName = "Default";
            tracerLine.sortingOrder = 20;
            tracerLine.useWorldSpace = true;
            tracerLine.receiveShadows = false;
            tracerLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            Material material = new Material(shader);
            material.color = tracerColor;
            tracerLine.material = material;
            tracerLine.startColor = tracerColor;
            tracerLine.endColor = tracerColor;
            tracerLine.enabled = false;
        }

        public void PlayTracer(Vector3 from, Vector3 to)
        {
            EnsureTracerLine();
            tracerLine.SetPosition(0, from);
            tracerLine.SetPosition(1, to);
            tracerLine.enabled = true;

            if (tracerRoutine != null)
            {
                StopCoroutine(tracerRoutine);
            }

            tracerRoutine = StartCoroutine(HideTracerAfterDelay());
        }

        private IEnumerator HideTracerAfterDelay()
        {
            yield return new WaitForSeconds(tracerDuration);
            tracerLine.enabled = false;
            tracerRoutine = null;
        }

        public void PlayImpactFlash(Vector3 position)
        {
            if (impactSprite == null)
            {
                return;
            }

            GameObject flashObject = new GameObject("ImpactFlash");
            flashObject.transform.position = position;
            flashObject.transform.localScale = Vector3.one * impactStartScale;

            SpriteRenderer flashRenderer = flashObject.AddComponent<SpriteRenderer>();
            flashRenderer.sprite = impactSprite;
            flashRenderer.sortingOrder = 25;

            StartCoroutine(FlashRoutine(flashObject, flashRenderer));
        }

        private IEnumerator FlashRoutine(GameObject flashObject, SpriteRenderer flashRenderer)
        {
            float elapsed = 0f;
            Color color = flashRenderer.color;

            while (elapsed < impactFlashDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / impactFlashDuration;

                float scale = Mathf.Lerp(impactStartScale, impactEndScale, t);
                flashObject.transform.localScale = Vector3.one * scale;

                color.a = Mathf.Lerp(1f, 0f, t);
                flashRenderer.color = color;

                yield return null;
            }

            Destroy(flashObject);
        }
    }
}
