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
            Debug.Log("Player cleared checkpoint " + index);
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
                Debug.Log("Player completed lap " + currentLap);

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
                ai.CurrentLap++;
                if (ai.CurrentLap >= totalLaps && aiFinishTimes[ai] < 0f)
                {
                    aiFinishTimes[ai] = raceTimer;
                    Debug.Log(ai.name + " finished race at " + raceTimer + " seconds");
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
                //AI has already finished, use recorded finish time
                predictedAiTimes[ai] = aiFinishTimes[ai];
            }
            else
            {
                //AI hasn't finished, calculate predicted finish time
                float remainingDistance = ComputeRemainingDistance(ai);
                float avgSpeed = Mathf.Max(ai.CurrentForwardSpeed, 0.1f); //Avoid divide by zero
                float predictedTime = raceTimer + remainingDistance / avgSpeed;

                predictedAiTimes[ai] = predictedTime;
            }
        }
            Debug.Log("----- AI Finish Times (Final / Predicted) -----");
            foreach (var kvp in predictedAiTimes)
            {
                string status = aiFinishTimes[kvp.Key] >= 0f ? "FINISHED" : "PREDICTED";
                Debug.Log($"{kvp.Key.name}: {kvp.Value:F2} seconds ({status})");
            }
            Debug.Log("-----------------------------------------------");

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

    ///<summary>
    ///Computes remaining distance for AI to finish race
    ///</summary>
    private float ComputeRemainingDistance(AiCarController ai)
    {
        float distance = 0f;
        int lapsRemaining = totalLaps - ai.CurrentLap;

        Transform[] waypoints = ai.waypoints;
        int currentIndex = ai.currentIndex;

        if (waypoints == null || waypoints.Length < 2)
            return 0f;

        //Distance from AI's current position to next waypoint
        distance += Vector2.Distance(ai.transform.position, waypoints[currentIndex].position);

        //Distance through remaining waypoints in current lap
        for (int i = currentIndex; i < waypoints.Length - 1; i++)
        {
            distance += Vector2.Distance(waypoints[i].position, waypoints[i + 1].position);
        }

        //Add full lap distance for remaining laps
        float lapDistance = 0f;
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            lapDistance += Vector2.Distance(waypoints[i].position, waypoints[i + 1].position);
        }

        distance += lapDistance * (lapsRemaining - 1);

        return distance;
    }
}
