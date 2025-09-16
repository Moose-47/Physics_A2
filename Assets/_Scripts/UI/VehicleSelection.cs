using UnityEngine;
using UnityEngine.SceneManagement;

///<summary>
///Singleton that persists across scenes.
///Stores the player's chosen vehicle sprite so that when the RaceScene loads,
///the RaceSpawner can apply the correct appearance to the Player prefab.
///</summary>
public class VehicleSelection : MonoBehaviour
{
    public static VehicleSelection Instance;

    private Sprite selectedSprite;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    public void SelectVehicle(Sprite sprite)
    {
        selectedSprite = sprite;
    }

    public void ConfirmSelection()
    {
        SceneManager.LoadScene("Controls");
    }

    public Sprite GetSelectedSprite()
    {
        return selectedSprite;
    }
}
