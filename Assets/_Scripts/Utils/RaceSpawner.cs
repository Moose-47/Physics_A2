using System.Collections.Generic;
using UnityEngine;

public class RaceSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Transform[] spawnPoints;  //Array of positions where racers can spawn
    public GameObject playerPrefab;  //Prefab for the player car
    public GameObject aiPrefab;      //Prefab for AI cars
    public Sprite[] aiSprites;       //Array of sprites to randomly assign to AI cars

    //List to keep track of which spawn points are still available
    private List<int> availableSpawnIndices = new List<int>();

    private void Awake()
    {
        GameObject music = GameObject.Find("BGM");
        Destroy(music);
    }
    private void Start()
    {
        //Ensure that we have exactly 9 spawn points (or more)
        if (spawnPoints.Length < 9)
        {
            Debug.LogError("There needs to be 8 starting positions");
            return; //Stop execution if not enough spawn points
        }

        //Call function to spawn all racers at start of the race
        SpawnRacers();
    }

    ///<summary>
    ///Spawns the player and AI cars at random, non-overlapping spawn points.
    ///</summary>
    void SpawnRacers()
    {
        //Clear the list of available spawn points in case we call this multiple times
        availableSpawnIndices.Clear();

        //Fill the list with all spawn point indices (0, 1, 2, ..., spawnPoints.Length-1)
        for (int i = 0; i < spawnPoints.Length; i++)
            availableSpawnIndices.Add(i);

        //--- Spawn the Player ---
        int playerIndex = PickRandomSpawnIndex(); //Pick a random index and remove it from available
        //Instantiate creates a new instance of a prefab at a position and rotation
        GameObject player = Instantiate(playerPrefab, spawnPoints[playerIndex].position, spawnPoints[playerIndex].rotation);
        //Get the sprite renderer inside the player and set it to the selected sprite from VehicleSelection
        if (VehicleSelection.Instance != null)
        {
            PlayerController playerCar = player.GetComponent<PlayerController>();
            if (playerCar != null)
                playerCar.Initialize(VehicleSelection.Instance.GetSelectedSprite());
        }
        //--- Spawn AI Cars ---
        int aiCount = spawnPoints.Length - 1; //Remaining spawn points are for AI
        for (int i = 0; i < aiCount; i++)
        {
            int aiIndex = PickRandomSpawnIndex(); //Pick a random spawn point for AI

            //Instantiate the AI car prefab
            GameObject aiInstance = Instantiate(aiPrefab, spawnPoints[aiIndex].position, spawnPoints[aiIndex].rotation);

            //--- Randomly assign a sprite to the AI car ---
            AiCarController ai = aiInstance.GetComponent<AiCarController>();
            if (ai != null && aiSprites.Length > 0)
            {
                ai.Initialize(aiSprites[Random.Range(0, aiSprites.Length)]);
            }
            //Random.Range(0, array.Length) picks a random index from the array
            //This gives AI variety in appearance/colors
        }
    }

    ///<summary>
    ///Picks a random index from availableSpawnIndices and removes it from the list
    ///to ensure no two cars spawn at the same point.
    ///</summary>
    int PickRandomSpawnIndex()
    {
        //Randomly pick an index in the availableSpawnIndices list
        int randomListIndex = Random.Range(0, availableSpawnIndices.Count);

        //Get the actual spawn point index corresponding to that list index
        int spawnIndex = availableSpawnIndices[randomListIndex];

        //Remove it from the list so no other car can use it
        availableSpawnIndices.RemoveAt(randomListIndex);

        //Return the chosen spawn point index
        return spawnIndex;
    }
}
