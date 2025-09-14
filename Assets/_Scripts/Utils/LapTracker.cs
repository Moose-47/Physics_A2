using System;
using System.Runtime.CompilerServices;
using Unity.Android.Types;
using UnityEngine;
using TMPro;

public class LapTracker : MonoBehaviour
{

    public int totalLaps = 3;

    public Transform[] checkpoints;
    public Transform finishLine;
    public TMP_Text lapText;
    
    
    private int currentCheckpointIndex = 0;
    private int currentLap = 0;
    

    public int CurrentLap => currentLap;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateLapUI();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // --- Checkpoints ---
        if (currentCheckpointIndex < checkpoints.Length && other.transform == checkpoints[currentCheckpointIndex])
        {
            currentCheckpointIndex++;
            Debug.Log("✅ Cleared checkpoint" + currentCheckpointIndex);
        }

        // --- Finish Line ---
        if (other.transform == finishLine)
        {
            if (currentCheckpointIndex == checkpoints.Length)
            {
                currentLap++;
                currentCheckpointIndex = 0; // reset checkpoints
                UpdateLapUI();

                Debug.Log("🏁 Lap " + currentLap + " complete!");

                if (currentLap >= totalLaps)
                {
                    Debug.Log("🎉 Race finished!");
                    lapText.text = "Race Finished!";
                }
            }
            else
            {
                Debug.Log("⚠️ Lap not counted — missed checkpoints!");
            }
        }
    }




    private void UpdateLapUI()
    {
        if (lapText != null)
        {
            lapText.text = "Lap" + Mathf.Clamp(currentLap + 1, 1, totalLaps) + " / " + totalLaps;
        }
    }
}
