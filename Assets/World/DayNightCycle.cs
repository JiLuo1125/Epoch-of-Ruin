using UnityEngine;
using UnityEngine.Rendering;

public class DayNightCycle : MonoBehaviour
{
    [Header("Lights")]
    [SerializeField] private Light sunLight;
    [SerializeField] private Light moonLight;

    [Header("Time")]
    [SerializeField] private bool autoRun = true;
    [SerializeField] private float cycleDurationSeconds = 120f;

    [Range(0f, 1f)]
    [SerializeField] private float timeOfDay = 0.35f;

    [Header("Sun Rotation")]
    [SerializeField] private float sunYaw = -30f;

    [Header("Light Intensity")]
    [SerializeField] private float daySunIntensity = 1.25f;
    [SerializeField] private float sunsetSunIntensity = 0.65f;
    [SerializeField] private float nightSunIntensity = 0f;
    [SerializeField] private float nightMoonIntensity = 0.22f;

    [Header("Light Colors")]
    [SerializeField] private Color daySunColor = new Color(1f, 0.96f, 0.82f);
    [SerializeField] private Color sunsetSunColor = new Color(1f, 0.45f, 0.2f);
    [SerializeField] private Color nightSunColor = new Color(0.08f, 0.12f, 0.25f);
    [SerializeField] private Color moonColor = new Color(0.45f, 0.55f, 1f);

    [Header("Ambient")]
    [SerializeField] private Color dayAmbientColor = new Color(0.62f, 0.68f, 0.78f);
    [SerializeField] private Color sunsetAmbientColor = new Color(0.42f, 0.25f, 0.2f);
    [SerializeField] private Color nightAmbientColor = new Color(0.035f, 0.05f, 0.1f);

    [Header("Fog")]
    [SerializeField] private bool controlFog = true;
    [SerializeField] private Color dayFogColor = new Color(0.72f, 0.82f, 0.9f);
    [SerializeField] private Color sunsetFogColor = new Color(0.75f, 0.42f, 0.28f);
    [SerializeField] private Color nightFogColor = new Color(0.025f, 0.035f, 0.08f);
    [SerializeField] private float dayFogDensity = 0.006f;
    [SerializeField] private float nightFogDensity = 0.018f;

    [Header("Skybox")]
    [SerializeField] private bool controlSkybox = true;
    [SerializeField] private Color daySkyTint = new Color(0.72f, 0.82f, 1f);
    [SerializeField] private Color sunsetSkyTint = new Color(0.9f, 0.45f, 0.28f);
    [SerializeField] private Color nightSkyTint = new Color(0.015f, 0.02f, 0.05f);
    [SerializeField] private float daySkyExposure = 1f;
    [SerializeField] private float nightSkyExposure = 0.08f;

    public float TimeOfDay => timeOfDay;

    private void Awake()
    {
        if (sunLight != null)
            RenderSettings.sun = sunLight;

        RenderSettings.ambientMode = AmbientMode.Flat;

        if (controlFog)
            RenderSettings.fog = true;

        UpdateLighting();
    }

    private void Update()
    {
        if (!autoRun)
            return;

        if (cycleDurationSeconds <= 0.1f)
            return;

        timeOfDay += Time.deltaTime / cycleDurationSeconds;

        if (timeOfDay > 1f)
            timeOfDay -= 1f;

        UpdateLighting();
    }

    private void UpdateLighting()
    {
        float sunAngle = timeOfDay * 360f - 90f;

        if (sunLight != null)
            sunLight.transform.rotation = Quaternion.Euler(sunAngle, sunYaw, 0f);

        if (moonLight != null)
            moonLight.transform.rotation = Quaternion.Euler(sunAngle + 180f, sunYaw, 0f);

        float sunHeight = 0f;

        if (sunLight != null)
            sunHeight = -sunLight.transform.forward.y;

        float dayAmount = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.05f, 0.45f, sunHeight));
        float nightAmount = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.25f, 0.15f, sunHeight));
        float sunsetAmount = GetSunsetAmount(sunHeight);

        Color sunColor = Color.Lerp(nightSunColor, daySunColor, dayAmount);
        sunColor = Color.Lerp(sunColor, sunsetSunColor, sunsetAmount);

        Color ambientColor = Color.Lerp(nightAmbientColor, dayAmbientColor, dayAmount);
        ambientColor = Color.Lerp(ambientColor, sunsetAmbientColor, sunsetAmount);

        Color fogColor = Color.Lerp(nightFogColor, dayFogColor, dayAmount);
        fogColor = Color.Lerp(fogColor, sunsetFogColor, sunsetAmount);

        float sunIntensity = Mathf.Lerp(nightSunIntensity, daySunIntensity, dayAmount);
        sunIntensity = Mathf.Lerp(sunIntensity, sunsetSunIntensity, sunsetAmount);

        if (sunLight != null)
        {
            sunLight.color = sunColor;
            sunLight.intensity = sunIntensity;
        }

        if (moonLight != null)
        {
            moonLight.color = moonColor;
            moonLight.intensity = nightMoonIntensity * nightAmount;
        }

        RenderSettings.ambientLight = ambientColor;

        if (controlFog)
        {
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = Mathf.Lerp(nightFogDensity, dayFogDensity, dayAmount);
        }

        UpdateSkybox(dayAmount, sunsetAmount);
    }

    private void UpdateSkybox(float dayAmount, float sunsetAmount)
    {
        if (!controlSkybox || RenderSettings.skybox == null)
            return;

        Material skybox = RenderSettings.skybox;
        Color skyTint = Color.Lerp(nightSkyTint, daySkyTint, dayAmount);
        skyTint = Color.Lerp(skyTint, sunsetSkyTint, sunsetAmount);
        float exposure = Mathf.Lerp(nightSkyExposure, daySkyExposure, dayAmount);

        if (skybox.HasProperty("_Tint"))
            skybox.SetColor("_Tint", skyTint);

        if (skybox.HasProperty("_SkyTint"))
            skybox.SetColor("_SkyTint", skyTint);

        if (skybox.HasProperty("_Exposure"))
            skybox.SetFloat("_Exposure", exposure);

        if (skybox.HasProperty("_Brightness"))
            skybox.SetFloat("_Brightness", exposure);
    }

    private float GetSunsetAmount(float sunHeight)
    {
        float nearHorizon = 1f - Mathf.Abs(sunHeight - 0.05f) / 0.22f;
        return Mathf.Clamp01(nearHorizon);
    }
}
