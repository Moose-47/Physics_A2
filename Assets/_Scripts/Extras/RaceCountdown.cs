using System.Collections;
using TMPro;
using UnityEngine;

public class RaceCountdown : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text countdownText;

    [Header("Race Manager")]
    public GameObject raceManager;

    [Header("Settings")]
    public int countdownFrom = 5;

    [Header("Sound Effects")]
    public AudioClip countdownSfx;
    public AudioClip startSfx;

    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
         if (raceManager != null)
            raceManager.SetActive(false);

        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        int count = countdownFrom;
        audioSource.PlayOneShot(countdownSfx);
        while (count > 0)
        {
            if (countdownText != null)
                countdownText.text = count.ToString();

            yield return new WaitForSeconds(1f);
            audioSource.PlayOneShot(countdownSfx);
            count--;
        }

        if (countdownText != null)
        {
            audioSource.PlayOneShot(startSfx);
            countdownText.text = "START!";
        }

        if (raceManager != null)
            raceManager.SetActive(true);

        yield return new WaitForSeconds(1f);
        if (countdownText != null)
            countdownText.text = "";
    }
}
