using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI speedText;
    private CarController carController;

    void Start()
    {
        if (carController == null)
        {
            carController = FindFirstObjectByType<CarController>();
        }
    }

    void Update()
    {
        if (speedText == null || carController == null) return;

        speedText.text = $"{carController.CarSpeed():0} KM/H";
    }

    public void BindCarController(CarController ctrl)
    {
        carController = ctrl;
    }
}