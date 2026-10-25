using UnityEngine;

namespace LoneTruth.Core
{
    /// <summary>
    /// Switches a scene between its night and day look (ambient, fog, key light, sky) from the
    /// "time_day" story flag. Objects that only exist at night or by day use FlagGate instead.
    /// </summary>
    public class TimeOfDayEnvironment : MonoBehaviour
    {
        [Header("Night")]
        [SerializeField] private Color nightAmbient = new Color(0.12f, 0.14f, 0.18f);
        [SerializeField] private Color nightFog = new Color(0.08f, 0.09f, 0.12f);
        [SerializeField] private float nightFogDensity = 0.018f;
        [SerializeField] private Color nightLightColor = new Color(0.49f, 0.56f, 0.7f);
        [SerializeField] private float nightLightIntensity = 0.25f;
        [SerializeField] private Material nightSky;

        [Header("Day (overcast)")]
        [SerializeField] private Color dayAmbient = new Color(0.36f, 0.38f, 0.41f);
        [SerializeField] private Color dayFog = new Color(0.32f, 0.34f, 0.37f);
        [SerializeField] private float dayFogDensity = 0.012f;
        [SerializeField] private Color dayLightColor = new Color(0.78f, 0.8f, 0.84f);
        [SerializeField] private float dayLightIntensity = 0.75f;
        [SerializeField] private Material daySky;

        [Header("References")]
        [SerializeField] private Light keyLight;
        [SerializeField] private Renderer skyRenderer;
        [SerializeField] private bool useFog = true;

        public void Configure(Light light, Renderer sky, Material night, Material day, bool fog)
        {
            keyLight = light;
            skyRenderer = sky;
            nightSky = night;
            daySky = day;
            useFog = fog;
        }

        /// <summary>Darker, warmer interior look: lamps carry the light, the window only tints it.</summary>
        public void UseInteriorPreset()
        {
            nightAmbient = new Color(0.2f, 0.19f, 0.2f);
            dayAmbient = new Color(0.32f, 0.31f, 0.31f);
            nightLightIntensity = 0.2f;
            dayLightIntensity = 0.5f;
        }

        private void OnEnable()
        {
            StoryState.Changed += Apply;
            Apply();
        }

        private void OnDisable()
        {
            StoryState.Changed -= Apply;
        }

        public void Apply()
        {
            bool day = StoryState.Has(StoryFlags.TimeDay);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = day ? dayAmbient : nightAmbient;
            RenderSettings.fog = useFog;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = day ? dayFog : nightFog;
            RenderSettings.fogDensity = day ? dayFogDensity : nightFogDensity;

            if (keyLight != null)
            {
                keyLight.color = day ? dayLightColor : nightLightColor;
                keyLight.intensity = day ? dayLightIntensity : nightLightIntensity;
            }

            Material sky = day ? daySky : nightSky;
            if (skyRenderer != null && sky != null)
            {
                skyRenderer.sharedMaterial = sky;
            }
        }
    }
}
