using UnityEngine;
using System.Collections.Generic;

///<summary>
///Singleton that holds race results between scenes.
///Stores a list of tuples (car sprite, finish time) for display.
///</summary>
public class RaceResultsHolder : MonoBehaviour
{
    //Singleton instance
    public static RaceResultsHolder Instance { get; private set; }

    //Results stored as (sprite, finishTime) tuples
    private List<(Sprite sprite, float finishTime)> raceResults = new List<(Sprite sprite, float finishTime)>();

    private void Awake()
    {
        //Ensure only one instance exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); //Keep this object alive across scenes
    }

    ///<summary>
    ///Set race results from LapTracker
    ///</summary>
    ///<param name="results">List of tuples (sprite, finishTime)</param>
    public void SetResults(List<(Sprite sprite, float finishTime)> results)
    {
        raceResults = new List<(Sprite sprite, float finishTime)>(results);
    }

    ///<summary>
    ///Get the full list of race results
    ///</summary>
    ///<returns>List of tuples (sprite, finishTime)</returns>
    public List<(Sprite sprite, float finishTime)> GetResults()
    {
        return new List<(Sprite sprite, float finishTime)>(raceResults);
    }

    ///<summary>
    ///Clear results if needed (optional)
    ///</summary>
    public void ClearResults()
    {
        raceResults.Clear();
    }
}
