using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GameManager : MonoBehaviour {

    public CarStateMachine[] carStateMachines;  // not using a list since playercount doesnt change ever !
    public float[] positions;

    public CarStateMachine PlayerStateMachine;

    public Transform[] aiStartPositions;

    public GameObject[] aiCars;



    void Start() {

        SpawnAiRacers();
        GetObjects();

    }


    void FixedUpdate() {
        for (int i = 0; i < carStateMachines.Length; i++) {
            positions[i] = carStateMachines[i].splinePositionFloat;
        }
    }

    #region startup

    void GetObjects() {
        // get all car state machines
        carStateMachines = Object.FindObjectsByType<CarStateMachine>(FindObjectsSortMode.None);
        if (carStateMachines.Length == 0) {
            Debug.LogError("No cars found");
            return;
        }
        positions = new float[carStateMachines.Length];
        foreach (var item in carStateMachines) {
            if (item.tag == "Player") {
                PlayerStateMachine = item;
            }
        }
    }

    void SpawnAiRacers() {

        for (int i = 0; i < aiStartPositions.Length; i++) {
            Instantiate(aiCars[Random.Range(0, aiCars.Length)], aiStartPositions[i].position, Quaternion.identity);
        }

    }




    #endregion


    #region gui
    [Header("gui")]
    public float GuiXPos = 0;
    public float GuiYPos = 0;
    public float GuiYSpace = 1;
    public GUIStyle customStyle = new();
    public float GuiCellWidth = 200;
    public float GuiCellHeight = 20;

    void OnGUI() {
        // if(!CompareTag("Player"))return;
        float pos = GuiYPos;

        // foreach (var item in positions) {
        //     GUI.Label(new Rect(GuiXPos, pos, GuiCellWidth, GuiCellHeight), "position: " + item.ToString("0.00"), customStyle);
        //     pos += GuiYSpace;
        // }

        var newArr = positions.OrderBy(x => x).ToArray();
        var playerPositionIndex = 0;

        for (int i = 0; i < newArr.Length; i++) {
            if (PlayerStateMachine.splinePositionFloat > newArr[i]) {
                playerPositionIndex = i;
                // GUI.Label(new Rect(GuiXPos, pos, GuiCellWidth, GuiCellHeight), "Player Position: " + i + "from " + positions.Length, customStyle);
                // pos += GuiYSpace;
            }
        }
        GUI.Label(new Rect(GuiXPos, pos, GuiCellWidth, GuiCellHeight), "Player Position: " + (positions.Length - playerPositionIndex) + "from " + positions.Length, customStyle);
        pos += GuiYSpace;


    }




    //GUI.Label(new Rect(GuiXPos, pos, GuiCellWidth, GuiCellHeight), "selected powerup: " + stateMachine.powerups[stateMachine.selectedPowerupIndex].ToString());
    //pos += GuiYSpace;

    #endregion

}
