using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;


public class ControlsMenu : MonoBehaviour
{

    public Button continuebtn;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        continuebtn.onClick.AddListener(() => SceneManager.LoadScene("RaceScene"));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
