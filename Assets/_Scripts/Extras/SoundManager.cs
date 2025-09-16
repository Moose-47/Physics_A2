using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public GameObject CarAccel;
    

    public void CreateSound(GameObject sound)
    {
        Debug.Log(sound.name);
        Instantiate(sound);
    }
}
