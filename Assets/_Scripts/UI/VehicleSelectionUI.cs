using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Handles vehicle selection only. Each vehicle button assigns its sprite immediately as the player's selection.
/// Design panels are no longer used.
/// </summary>
public class VehicleSelectionUI : MonoBehaviour
{
    [Header("Vehicle Options")]
    public Button[] vehicleOptions;     // Array of buttons for choosing vehicle type
    public Button confirmVehicleButton; // Button that confirms vehicle selection
    public GameObject vehicleSelectionPanel; // Panel where vehicle buttons are displayed

    [Header("Main Menu Button")]
    public Button mainMenuButton;       // Button to return to main menu

    private Sprite selectedSprite;      // Stores the sprite of the selected vehicle

    private void OnEnable()
    {
        // Attach listeners to each vehicle button
        for (int i = 0; i < vehicleOptions.Length; i++)
        {
            int index = i; // Capture correct index
            vehicleOptions[i].onClick.AddListener(() => OnVehicleSelected(index));
        }

        // Attach confirm listener
        confirmVehicleButton.onClick.AddListener(OnVehicleConfirmed);

        // Ensure vehicle selection panel is active
        vehicleSelectionPanel.SetActive(true);

        // Attach main menu button listener
        mainMenuButton.onClick.AddListener(BackToMainMenu);
    }

    private void OnDisable()
    {
        // Remove listeners safely using null checks
        if (vehicleOptions != null)
        {
            for (int i = 0; i < vehicleOptions.Length; i++)
            {
                if (vehicleOptions[i] != null)
                    vehicleOptions[i].onClick.RemoveAllListeners();
            }
        }

        if (confirmVehicleButton != null)
            confirmVehicleButton.onClick.RemoveAllListeners();

        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveAllListeners();
    }

    private void OnVehicleSelected(int index)
    {
        // Grab the sprite from the button's Image component
        Image img = vehicleOptions[index].GetComponent<Image>();
        if (img != null)
            selectedSprite = img.sprite;

        // Optional: give visual feedback on selection
        Debug.Log($"Vehicle {index} selected: {selectedSprite.name}");
    }

    private void OnVehicleConfirmed()
    {
        if (selectedSprite != null)
        {
            // Send selection to your singleton or vehicle manager
            VehicleSelection.Instance.SelectVehicle(selectedSprite);
            VehicleSelection.Instance.ConfirmSelection();

            Debug.Log($"Vehicle confirmed: {selectedSprite.name}");
        }
        else
        {
            Debug.LogWarning("No vehicle selected!");
        }
    }

    private void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}