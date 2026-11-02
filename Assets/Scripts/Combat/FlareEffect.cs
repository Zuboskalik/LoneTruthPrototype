using System.Collections.Generic;
using UnityEngine;

namespace LoneTruth.Combat
{
    /// <summary>
    /// A lit flare (design doc 3.4): a red flash blinds enemies within 5 m for 4 s, then it burns
    /// for 20 s, lighting the dark and keeping the Hollow back. A consecrated flare also burns
    /// the Hollow caught in the flash for 30 damage. Lives in the scene it was lit in.
    /// </summary>
    public class FlareEffect : MonoBehaviour
    {
        public const float BlindRadius = 5f;
        public const float BlindSeconds = 4f;
        public const float BurnSeconds = 20f;
        public const float RepelRadius = 4f;
        public const int ConsecratedDamage = 30;

        private static readonly List<FlareEffect> Burning = new List<FlareEffect>();

        private Light flareLight;
        private float age;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Burning.Clear();
        }

        public static void Light(Vector3 position, bool consecrated)
        {
            var go = new GameObject(consecrated ? "Flare_Consecrated" : "Flare");
            go.transform.position = new Vector3(position.x, 0.25f, position.z - 0.2f);
            go.AddComponent<FlareEffect>().Setup(consecrated);
            EnemyController.BlindAround(position, BlindRadius, BlindSeconds, consecrated ? ConsecratedDamage : 0);
            EnemyController.MakeNoise(position, BlindRadius * 2f);
        }

        public static bool IsBurningNear(float x, float radius)
        {
            foreach (FlareEffect f in Burning)
            {
                if (Mathf.Abs(f.transform.position.x - x) <= radius)
                {
                    return true;
                }
            }

            return false;
        }

        public static float NearestX(float x)
        {
            float best = float.MaxValue, nearest = x;
            foreach (FlareEffect f in Burning)
            {
                float d = Mathf.Abs(f.transform.position.x - x);
                if (d < best)
                {
                    best = d;
                    nearest = f.transform.position.x;
                }
            }

            return nearest;
        }

        private void Setup(bool consecrated)
        {
            flareLight = gameObject.AddComponent<Light>();
            flareLight.type = LightType.Point;
            flareLight.color = consecrated ? new Color(1f, 0.75f, 0.45f) : new Color(1f, 0.25f, 0.18f);
            flareLight.range = 11f;

            var spark = new GameObject("Spark").AddComponent<SpriteRenderer>();
            spark.transform.SetParent(transform, false);
            spark.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 32f);
            spark.color = flareLight.color;
        }

        private void OnEnable()
        {
            Burning.Add(this);
        }

        private void OnDisable()
        {
            Burning.Remove(this);
        }

        private void Update()
        {
            age += Time.deltaTime;
            float flash = Mathf.Clamp01(1f - age / 0.4f);
            float flicker = 0.85f + 0.15f * Mathf.PerlinNoise(age * 9f, 0f);
            float dying = Mathf.Clamp01((BurnSeconds - age) / 2f);
            flareLight.intensity = (5f + flash * 18f) * flicker * dying;
            if (age >= BurnSeconds)
            {
                Destroy(gameObject);
            }
        }
    }
}
