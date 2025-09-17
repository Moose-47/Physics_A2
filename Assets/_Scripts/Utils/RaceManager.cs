using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using TMPro;

///<summary>
///Tracks player and AI laps, checkpoints, and calculates AI predicted finish times
///for displaying a results screen similar to Mario Kart.
///</summary>
public class RaceManager : MonoBehaviour
{
    [Header("Lap Settings")]
    public int totalLaps = 3; //Total laps required to finish race

    [Header("Checkpoints")]
    public Transform[] checkpoints; //Waypoints that player must pass in order
    public Transform finishLine;    //Finish line transform

    [Header("Racers")]
    public PlayerController player; //Reference to player
    public List<AiCarController> aiCars = new List<AiCarController>(); //AI cars list

    private int currentCheckpointIndex = 0; //Tracks which checkpoint player is expected to hit next
    private int currentLap = 0;             //Tracks player's current lap
    private bool playerFinished = false;    //Flag indicating if player finished race

    private float raceTimer = 0f; //Global race timer (all laps)

    public TMP_Text lapText;
    public TMP_Text timerText;

    //Dictionary storing actual finish times of AI cars. Value <0 means AI hasn't finished yet.
    private Dictionary<AiCarController, float> aiFinishTimes = new Dictionary<AiCarController, float>();

    public int CurrentLap => currentLap; // Allows other scripts to read the player's current lap

    ///<summary>
    ///Automatically finds all AI cars in the scene when this object is enabled
    ///and initializes their finish times.
    ///</summary>
    private void OnEnable()
    {
        player = FindAnyObjectByType<PlayerController>();
        AiCarController[] foundAiCars = FindObjectsByType<AiCarController>(FindObjectsSortMode.None);
        aiCars = new List<AiCarController>(foundAiCars);

        //Initialize the AI finish times dictionary
        foreach (var ai in aiCars)
        {
            aiFinishTimes[ai] = -1f; //-1 means AI hasn't finished
        }

        // Enable AI movement and start their lap timer
        foreach (var ai in foundAiCars)
        {
            ai.canMove = true;
            ai.StartLap(raceTimer);
        }
        player.canMove = true;
    }

    ///<summary>
    ///Updates global race timer while player has not finished
    ///</summary>
    private void Update()
    {
        if (!playerFinished)
        {
            raceTimer += Time.deltaTime;
            timerText.text = $"TIME: {raceTimer.ToString("F2")}";
        }
    }

    ///<summary>
    ///Called by checkpoints when the player collides with them.
    ///Only increments if the player is at the correct checkpoint in sequence.
    ///</summary>
    ///<param name="index">Index of the checkpoint that was hit</param>
    ///<param name="other">Collider of the object that triggered the checkpoint</param>
    public void PlayerHitCheckpoint(int index, Collider2D other)
    {
        //Only allow the player to progress checkpoints
        if (other.CompareTag("Player") && index == currentCheckpointIndex)
        {
            currentCheckpointIndex++;
        }
    }

    ///<summary>
    ///Called when any racer (player or AI) crosses the finish line trigger.
    ///Updates laps, records lap times for AI, and records finish times if race is completed.
    ///</summary>
    ///<param name="other">Collider of the object that triggered the finish line</param>
    public void HitFinish(Collider2D other)
    {
        // ---------- PLAYER FINISH LOGIC ----------
        if (other.CompareTag("Player") && !playerFinished)
        {
            // Only count the lap if player has hit all checkpoints
            if (currentCheckpointIndex == checkpoints.Length)
            {
                currentLap++;             // Increment player's lap
                if (lapText != null)
                {
                    lapText.text = "Lap: " + Mathf.Clamp(currentLap + 1, 1, totalLaps) + " / " + totalLaps;
                }
                currentCheckpointIndex = 0; // Reset checkpoints for next lap

                // If the player completed all laps, finish the race
                if (currentLap >= totalLaps)
                {
                    playerFinished = true;         // Mark player as finished
                    CalculateAiPredictedFinishTimes(); // Calculate AI predicted times and go to results
                }
            }
        }
        // ---------- AI FINISH LOGIC ----------
        else if (other.CompareTag("Ai"))
        {
            AiCarController ai = other.GetComponent<AiCarController>();
            if (ai != null)
            {
                // Record the AI's lap time if this is a valid lap
                if (ai.CurrentLap >= 0)
                {
                    ai.EndLap(raceTimer); // Store the lap time internally in the AI
                }

                ai.CurrentLap++; // Increment AI lap counter

                // Record the AI's overall finish time if they've completed all laps
                if (ai.CurrentLap >= totalLaps && aiFinishTimes[ai] < 0f)
                {
                    aiFinishTimes[ai] = raceTimer; // Store the finish time
                }
            }
        }
    }

    /// <summary>
    /// Calculates predicted finish times for AI racers that haven't finished yet.
    /// Uses the AI's average lap time to estimate total finish time.
    /// </summary>
    private void CalculateAiPredictedFinishTimes()
    {
        // Dictionary storing finish or predicted times for display
        Dictionary<AiCarController, float> predictedAiTimes = new Dictionary<AiCarController, float>();

        foreach (var ai in aiCars)
        {
            if (aiFinishTimes[ai] >= 0f)
            {
                // AI has finished the race, use actual finish time
                predictedAiTimes[ai] = aiFinishTimes[ai];
            }
            else if (ai.lapTimes.Count > 0)
            {
                // AI has completed at least 1 lap, estimate finish time based on average lap
                predictedAiTimes[ai] = ai.AverageLapTime() * totalLaps;
            }
            else
            {
                // AI hasn't completed a single lap yet, mark as DNF (Did Not Finish)
                predictedAiTimes[ai] = 0f;
            }
        }

        // ---------- Prepare results for the UI ----------
        List<(Sprite sprite, float finishTime)> results = new List<(Sprite sprite, float finishTime)>();

        // Add player
        Sprite playerSprite = player.GetComponent<SpriteRenderer>().sprite;
        results.Add((playerSprite, raceTimer));

        // Add all AI cars
        foreach (var kvp in predictedAiTimes)
        {
            Sprite aiSprite = kvp.Key.GetComponent<SpriteRenderer>().sprite;
            results.Add((aiSprite, kvp.Value));
        }

        // Sort results by finish time (lowest time first = first place)
        results.Sort((a, b) => a.finishTime.CompareTo(b.finishTime));

        // Send the sorted results to the RaceResultsHolder singleton
        RaceResultsHolder.Instance.SetResults(results);

        // Load the race finished scene
        SceneManager.LoadScene("RaceFinished");
    }
}