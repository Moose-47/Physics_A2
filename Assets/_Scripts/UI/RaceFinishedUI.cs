using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RaceFinishedUI : MonoBehaviour
{
    public Transform resultsParent;
    public GameObject resultEntryPrefab;

    private void Start()
    {
        List<(Sprite sprite, float finishTime)> results = RaceResultsHolder.Instance.GetResults();

        foreach (var entry in results)
        {
            GameObject go = Instantiate(resultEntryPrefab, resultsParent);
            Image carImage = go.transform.Find("CarImage").GetComponent<Image>();
            TMP_Text timeText = go.transform.Find("TimeText").GetComponent<TMP_Text>();

            carImage.sprite = entry.sprite;
            timeText.text = entry.finishTime.ToString("F2"); //F2 rounds the float to 2 decimal places so 12.4552 -> 12.46
        }
    }
}
