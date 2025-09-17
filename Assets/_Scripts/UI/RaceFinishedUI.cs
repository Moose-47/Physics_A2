using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RaceFinishedUI : MonoBehaviour
{
    [Header("UI Text Elements (element 0 = 1st place")]
    public TMP_Text[] resultsText;
    public Image[] carImages;
    public Button returnToMain;

    private void Start()
    {
        List<(Sprite sprite, float finishTime)> results = RaceResultsHolder.Instance.GetResults();

        for (int i = 0; i < resultsText.Length; i++)
        {
            if (i < results.Count)
            {
                var entry = results[i];
                string timeText = entry.finishTime == 0f ? "DNF" : entry.finishTime.ToString("F2");
                resultsText[i].text = $"{timeText}";
                carImages[i].sprite = entry.sprite;
            }
            else
            {
                resultsText[i].text = "";
                carImages[i].sprite = null;
            }
        }
        returnToMain.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));
    }

    private void OnDisable()
    {
        returnToMain.onClick.RemoveAllListeners();
    }
}
