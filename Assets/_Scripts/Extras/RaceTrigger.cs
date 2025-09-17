using UnityEngine;
using System.Collections.Generic;

public class RaceTrigger : MonoBehaviour
{
    public enum TriggerType { Checkpoint, Finish }
    public TriggerType triggerType;
    public int checkpointIndex; // Only used if it's a checkpoint

    // Track which objects are currently inside this trigger
    private HashSet<Collider2D> objectsInside = new HashSet<Collider2D>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (objectsInside.Contains(other)) return; // already inside, ignore
        objectsInside.Add(other);

        RaceManager rm = FindAnyObjectByType<RaceManager>();
        if (rm == null) return;

        if (triggerType == TriggerType.Checkpoint)
            rm.PlayerHitCheckpoint(checkpointIndex, other);
        else if (triggerType == TriggerType.Finish)
            rm.HitFinish(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (objectsInside.Contains(other))
            objectsInside.Remove(other);
    }
}