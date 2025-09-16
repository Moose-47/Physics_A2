using UnityEngine;

public class Waypoint : MonoBehaviour
{
    public Vector2 GetRandomPointInside()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null) return transform.position;

        Vector2 size = box.size;
        Vector2 center = (Vector2)transform.position + box.offset;

        float x = Random.Range(center.x - size.x / 2f, center.x + size.x / 2f);
        float y = Random.Range(center.y - size.y / 2f, center.y + size.y / 2f);

        return new Vector2(x, y);
    }
}

