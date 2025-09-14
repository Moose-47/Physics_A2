using UnityEngine;

public class RacingLine : MonoBehaviour
{
    public Transform[] waypoints;

    private void Awake()
    {
        waypoints = new Transform[transform.childCount];
        for (int i = 0; i < waypoints.Length; i++) 
            waypoints[i] = transform.GetChild(i);
    }
}
