using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI speedText;
    private PhotonCarController carController;
    private float findTimer;

    void Start()
    {
        FindCar();
    }

    void Update()
    {
        if (speedText == null) return;

        if (carController == null)
        {
            findTimer -= Time.deltaTime;
            if (findTimer <= 0f)
            {
                FindCar();
                findTimer = 0.5f;
            }
            return;
        }

        speedText.text = $"{carController.CarSpeed():0} KM/H";
    }

    private void FindCar()
    {
        carController = FindFirstObjectByType<PhotonCarController>();
    }

    public void BindCarController(PhotonCarController ctrl)
    {
        carController = ctrl;
    }
}