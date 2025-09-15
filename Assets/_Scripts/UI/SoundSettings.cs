using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SoundSettings : MonoBehaviour
{
    public AudioMixer mixer;

    public Slider masterSlider;
    public TMP_Text masterTxt;

    public Slider musicSlider;
    public TMP_Text musicTxt;

    public Slider sfxSlider;
    public TMP_Text sfxTxt;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Start()
    {
        LoadVolumeSettings();

        setupSliderInfo(masterSlider, masterTxt, "master");
        setupSliderInfo(musicSlider, musicTxt, "music");
        setupSliderInfo(sfxSlider, sfxTxt, "sfx");
    }

    void OnSliderValueChanged(float value, Slider slider, TMP_Text sliderText, string parameterName)
    {
        value = (value == 0.0f) ? -80.0f : 20.0f * Mathf.Log10(slider.value);
        sliderText.text = (value == -80.0f) ? "0%" : $"{(int)(slider.value * 100)}%";
        mixer.SetFloat(parameterName, value);
        PlayerPrefs.SetFloat(parameterName, slider.value);
    }
    void setupSliderInfo(Slider slider, TMP_Text sliderText, string parameterName)
    {
        slider.onValueChanged.AddListener((value) => OnSliderValueChanged(value, slider, sliderText, parameterName));
        OnSliderValueChanged(slider.value, slider, sliderText, parameterName);
    }

    void LoadVolumeSettings()
    {
        float defaultVolume = 0.5f;

        float masterVol = PlayerPrefs.HasKey("master") ? PlayerPrefs.GetFloat("master") : defaultVolume;
        float musicVol = PlayerPrefs.HasKey("music") ? PlayerPrefs.GetFloat("music") : defaultVolume;
        float sfxVol = PlayerPrefs.HasKey("sfx") ? PlayerPrefs.GetFloat("sfx") : defaultVolume;

        masterSlider.value = masterVol;
        musicSlider.value = musicVol;
        sfxSlider.value = sfxVol;

        mixer.SetFloat("master", (masterVol == 0.0f) ? -80.0f : 20.0f * Mathf.Log10(masterVol));
        mixer.SetFloat("music", (musicVol == 0.0f) ? -80.0f : 20.0f * Mathf.Log10(musicVol));
        mixer.SetFloat("sfx", (sfxVol == 0.0f) ? -80.0f : 20.0f * Mathf.Log10(sfxVol));
    }
}
