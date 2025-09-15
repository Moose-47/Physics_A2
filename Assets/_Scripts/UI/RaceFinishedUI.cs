using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RaceFinishedUI : MonoBehaviour
{
    [Header("UI Text Elements (element 0 = 1st place")]
    public TMP_Text[] resultsText;
    public Image[] carImages;

    private void Start()
    {
        List<(Sprite sprite, float finishTime)> results = RaceResultsHolder.Instance.GetResults();

        for (int i = 0; i < resultsText.Length; i++)
        {
            if (i < results.Count)
            {
                var entry = results[i];
                resultsText[i].text = $"{i + 1}. {entry.finishTime:F2}"; //F2 rounds to two decimal places.
                carImages[i].sprite = entry.sprite;
            }
            else
            {
                resultsText[i].text = "";
                carImages[i].sprite = null;
            }
        }
    }
}
