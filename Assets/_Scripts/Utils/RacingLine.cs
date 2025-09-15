using UnityEngine;

///<summary>
///Holds a sequence of waypoints that define the racing track path.
/// 
///On Awake, the script automatically gathers all of its children into the 
///<see cref="waypoints"/> array. These waypoints are then used by the 
///<see cref="AiCarController"/> to navigate the track, calculate look-ahead targets, 
///adjust speed for corners, and handle recovery when off track.
///</summary>
public class RacingLine : MonoBehaviour
{
    public Transform[] waypoints;

    private void Awake()
    {
        //Stores all children of this object as waypoints in order
        waypoints = new Transform[transform.childCount];
        for (int i = 0; i < waypoints.Length; i++) 
            waypoints[i] = transform.GetChild(i);
    }
}
