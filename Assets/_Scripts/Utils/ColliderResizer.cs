using UnityEngine;

///<summary>
///This script exists for the purpose of resizing the CapsuleCollider2D component
///on the Player and Ai vehicles once instantiated to account for the slight 
///difference in sizes between the 3 different base vehicle sprites
///</summary>
public class ColliderResizer : MonoBehaviour
{
    private CapsuleCollider2D cc;
    private SpriteRenderer sr;

    private void Awake()
    {
        cc = GetComponent<CapsuleCollider2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    public void ResetCollider()
    {
        if (sr.sprite == null || cc == null) return;

        //Get sprite bounds (size in world units)
        Vector2 size = sr.sprite.bounds.size;

        //Assign size to collider
        cc.size = size;

        //Center the collider on the sprite
        cc.offset = Vector2.zero;

        //Force vertical orientation of the CapsuleCollider2D
        cc.direction = CapsuleDirection2D.Vertical;
    }
}
