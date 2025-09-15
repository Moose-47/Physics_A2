using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VehicleSelectionUI : MonoBehaviour
{
    [Header("Vehicle Options")]
    public Button[] vehicleOptions;             //Array of buttons for choosing vehicle type
    public Button confirmVehicleButton;         //Button that confirms vehicle selection
    public GameObject vehicleSelectionPanel;    //The panel where the vehicle options are displayed

    [Header("Design Panels")]
    public GameObject[] designPanels;           //Array of panels, one for each different vehicle option
    public Button confirmDesignButton;          //Button to confirm chosen design (sprite)

    [Header("Additional Buttons")]
    public Button mainMenuButton;               //Button to return to main menu from vehicle selection
    public Button backButton;                   //Button to return to vehicle selection from design selection

    private int currentVehicleIndex = -1;       //Keeps track of which vechiels is currently selected
    private Sprite selectedSprite;              //Stores the final chosen sprite (design) to give to VehicleSelection
    private Button[] currentDesignButtons;      //Stores the 5 design buttons of the currently active design panel


    private void OnEnable()
    {
        //Attach listeners to each vehicle button
        //These buttons will call OnVehicleSelecte when clicked
        for (int i = 0; i < vehicleOptions.Length; i++)
        {
            int index = i; //Need to 'capture' the correct index for each button
            vehicleOptions[i].onClick.AddListener(() => OnVehicleSelected(index));
        }

        //Attach confirm listeners
        confirmVehicleButton.onClick.AddListener(OnVehicleConfirmed);
        confirmDesignButton.onClick.AddListener(OnDesignConfirmed);

        //Show vehicle selection panel by default
        vehicleSelectionPanel.SetActive(true);

        //Hide all design panels by default
        foreach (var panel in designPanels)
            panel.SetActive(false);

        //Attach listeners to main menu and back buttons
        mainMenuButton.onClick.AddListener(BackToMainMenu);
        backButton.onClick.AddListener(BackToVehicleSelect);
    }

    /// <summary>
    /// Removing all listeners from all buttons to avoid any potential memory leaks
    /// </summary>
    private void OnDisable()
    {
        for (int i = 0; i < vehicleOptions.Length;i++)
        {
            vehicleOptions[i].onClick.RemoveAllListeners();
        }

        if (currentDesignButtons != null) //Null check in case player returns to main menu before selecting a vehicle
        {
            for (int i = 0; i < currentDesignButtons.Length; i++)
            {
                currentDesignButtons[i].onClick.RemoveAllListeners();
            }
        }

        confirmVehicleButton.onClick.RemoveAllListeners();
        confirmDesignButton.onClick.RemoveAllListeners();
        mainMenuButton.onClick.RemoveAllListeners();
        backButton.onClick.RemoveAllListeners();
    }

    private void OnVehicleSelected(int index)
    {
        currentVehicleIndex = index; //Save which vehicle index the player picked (0, 1, or 2)
    }

    private void OnVehicleConfirmed()
    {
        if (currentVehicleIndex == -1) return; //Do nothing if the player hasn't selected a vehicle yet

        //Hide selection panel
        vehicleSelectionPanel.SetActive(false);

        //Hide all design panels
        foreach (var panel in designPanels)
            panel.SetActive(false);

        //Show the design panel that matches the selected vehicle
        designPanels[currentVehicleIndex].SetActive(true);

        //Get all design buttons from the panel
        currentDesignButtons = designPanels[currentVehicleIndex].GetComponentsInChildren<Button>();

        //Ensuring no old listeners are left over
        foreach (var btn in currentDesignButtons)
            btn.onClick.RemoveAllListeners();

        //Add listener to each design button
        //Each button when clicked calls OnDesignSelected by its index
        for (int i = 0; i < currentDesignButtons.Length;i++)
        {
            int index = i;
            currentDesignButtons[i].onClick.AddListener(() => OnDesignSelected(index));
        }
    }

    private void OnDesignSelected(int index)
    {
        //Get the image component on the button
        Image img = currentDesignButtons[index].GetComponent<Image>();
        //Save the sprite from the image component
        if (img != null)
            selectedSprite = img.sprite;
    }

    private void OnDesignConfirmed()
    {
        //Only continue if the player has selected a design
        if (selectedSprite != null)
        {
            VehicleSelection.Instance.SelectVehicle(selectedSprite);
            VehicleSelection.Instance.ConfirmSelection();
        }
    }

    private void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private void BackToVehicleSelect()
    {
        //Hide all design panels
        foreach (var panel in designPanels)
            panel.SetActive(false);

        //Re-show the vehicle selection panel
        vehicleSelectionPanel.SetActive(true);
    }
}
