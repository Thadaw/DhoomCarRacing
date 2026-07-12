using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI speedText;
    private PhotonCarController carController;

    void Start()
    {
        if (carController == null)
        {
            carController = FindFirstObjectByType<PhotonCarController>();
        }
    }

    void Update()
    {
        if (speedText == null || carController == null) return;

        speedText.text = $"{carController.CarSpeed():0} KM/H";
    }

    public void BindCarController(PhotonCarController ctrl)
    {
        carController = ctrl;
    }
}