using System.Collections;
using UnityEngine;

public class WeatherSystem : MonoBehaviour
{
    public float dayDuration = 125f; 
    [Range(0, 1)] public float currentTime = 0f; 

    public float dayBrightness = 1.2f;
    public float nightBrightness = 0.3f;
    public float rainDarknessFactor = 0.6f;

    public AudioSource dayAmbience;
    public AudioSource nightAmbience;
    public float ambienceMaxVol = 0.3f; 

    public GameObject rainObject;
    public Light sunLight;
    public AudioSource rainAudio;
    public AudioSource windAudio;

    public float minSunnyTime = 20f;
    public float maxSunnyTime = 40f;
    public float minRainTime = 15f;
    public float maxRainTime = 25f;

    private bool isRaining = false;
    private float currentLakeMultiplier = 1f;

    private void Start()
    {
        if (rainObject != null) rainObject.SetActive(false);
        StartCoroutine(WeatherCycle());
    }

    private void Update()
    {
        currentTime += Time.deltaTime / dayDuration;
        if (currentTime >= 1f) currentTime = 0f;

        RenderSettings.skybox.SetFloat("_DayNightPos", currentTime);

        float dayIntensity = (Mathf.Sin((currentTime - 0.25f) * Mathf.PI * 2) + 1) / 2;

        float targetBaseIntensity = Mathf.Lerp(nightBrightness, dayBrightness, dayIntensity);
        if (isRaining) targetBaseIntensity *= rainDarknessFactor;

        if (sunLight != null)
            sunLight.intensity = Mathf.Lerp(sunLight.intensity, targetBaseIntensity, Time.deltaTime);

        if (dayAmbience != null && nightAmbience != null)
        {
            float targetLakeMult = isRaining ? 0f : 1f;

            currentLakeMultiplier = Mathf.Lerp(currentLakeMultiplier, targetLakeMult, Time.deltaTime * 0.5f);

            dayAmbience.volume = (dayIntensity * ambienceMaxVol) * currentLakeMultiplier;
            nightAmbience.volume = ((1f - dayIntensity) * ambienceMaxVol) * currentLakeMultiplier;
        }

        UpdateWeatherSounds();
    }

    void UpdateWeatherSounds()
    {
        if (rainAudio != null)
            rainAudio.volume = Mathf.Lerp(rainAudio.volume, isRaining ? 0.8f : 0f, Time.deltaTime);

        if (windAudio != null)
            windAudio.volume = Mathf.Lerp(windAudio.volume, isRaining ? 0.4f : 0.01f, Time.deltaTime);
    }

    IEnumerator WeatherCycle()
    {
        while (true)
        {
            isRaining = false;
            if (rainObject != null) rainObject.SetActive(false);
            yield return new WaitForSeconds(Random.Range(minSunnyTime, maxSunnyTime));

            isRaining = true;
            if (rainObject != null) rainObject.SetActive(true);
            yield return new WaitForSeconds(Random.Range(minRainTime, maxRainTime));
        }
    }
}