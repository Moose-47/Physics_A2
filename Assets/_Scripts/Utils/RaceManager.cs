using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

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

    //Dictionary storing actual finish times of AI cars. Value <0 means AI hasn't finished yet.
    private Dictionary<AiCarController, float> aiFinishTimes = new Dictionary<AiCarController, float>();

    public int CurrentLap => currentLap;

    ///<summary>
    ///Automatically finds all AI cars in the scene when this object is enabled
    ///and initializes their finish times.
    ///</summary>
    private void OnEnable()
    {
        player = FindAnyObjectByType<PlayerController>();
        AiCarController[] foundAiCars = FindObjectsByType<AiCarController>(FindObjectsSortMode.None);
        aiCars = new List<AiCarController>(foundAiCars);

        foreach (var ai in aiCars)
        {
            aiFinishTimes[ai] = -1f; //-1 means AI hasn't finished
        }

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
            raceTimer += Time.deltaTime;
    }

    public void PlayerHitCheckpoint(int index, Collider2D other)
    {
        if (other.CompareTag("Player") && index == currentCheckpointIndex)
        {
            currentCheckpointIndex++;
        }
    }

    public void HitFinish(Collider2D other)
    {
        if (other.CompareTag("Player") && !playerFinished)
        {
            if (currentCheckpointIndex == checkpoints.Length)
            {
                currentLap++;
                currentCheckpointIndex = 0;

                if (currentLap >= totalLaps)
                {
                    if (!playerFinished)
                    {
                        playerFinished = true;
                        CalculateAiPredictedFinishTimes();
                    }
                }
            }
        }
        else if (other.CompareTag("Ai"))
        {
            AiCarController ai = other.GetComponent<AiCarController>();
            if (ai != null)
            {
                if (ai.CurrentLap >= 0)
                {
                    // End current lap and store lap time
                    ai.EndLap(raceTimer);
                }

                // Increment lap count
                ai.CurrentLap++;

                // Record finish time immediately if done
                if (ai.CurrentLap >= totalLaps && aiFinishTimes[ai] < 0f)
                {
                    aiFinishTimes[ai] = raceTimer;
                }
            }
        }
    }

    ///<summary>
    ///Calculates predicted finish times for AI cars that haven't finished yet.
    ///Uses remaining distance and current forward speed for estimation.
    ///</summary>
    private void CalculateAiPredictedFinishTimes()
    {
        Dictionary<AiCarController, float> predictedAiTimes = new Dictionary<AiCarController, float>();

        foreach (var ai in aiCars)
        {
            if (aiFinishTimes[ai] >= 0f)
            {
                // AI has finished
                predictedAiTimes[ai] = aiFinishTimes[ai];
            }
            else if (ai.lapTimes.Count > 0)
            {
                // AI has completed at least one lap, predict finish
                predictedAiTimes[ai] = ai.AverageLapTime() * totalLaps;
            }
            else
            {
                //AI hasn't completed a single lap yet set time to 0 DNF
                predictedAiTimes[ai] = 0f; 
            }
        }
        //---------- Prepare results list using sprites ----------
        //List of tuples (sprite, finishTime)
        List<(Sprite sprite, float finishTime)> results = new List<(Sprite sprite, float finishTime)>();

        //Add player
        Sprite playerSprite = player.GetComponent<SpriteRenderer>().sprite;
        results.Add((playerSprite, raceTimer));

        //Add AI cars
        foreach (var kvp in predictedAiTimes)
        {
            //kvp.Key = AiCarController reference
            //kvp.Value = predicted finish time
            Sprite aiSprite = kvp.Key.GetComponent<SpriteRenderer>().sprite;
            results.Add((aiSprite, kvp.Value));
        }

        //Sort by finish time ascending (first place first)
        results.Sort((a, b) => a.finishTime.CompareTo(b.finishTime));

        //Send results to a singleton holder to display in the next scene
        RaceResultsHolder.Instance.SetResults(results);

        //Load results scene
        SceneManager.LoadScene("RaceFinished");
    }
}
