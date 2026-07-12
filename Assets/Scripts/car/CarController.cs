using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Splines;


[RequireComponent(typeof(CarStateMachine))]
[RequireComponent(typeof(CarStats))]
public class CarController : MonoBehaviour {

    #region variables 
    private CarStateMachine stateMachine;

    [Header("forces")]
    [Range(1,10)]public float downForceFactor = 2;
    [Tooltip("this will increase the sideways velocity while on high speed , more stability when turning!")]
    [Range(0, 0.001f)] public float angularVelocityFactor = 0.0002f;
    [Range(0, 0.001f)] public float linearVelocityFactor = 0.0002f;

    //private 
    public bool usingNitrus = false;
    public float _downForce ;
    
    #endregion

    #region main
    void Start() {
        stateMachine = GetComponent<CarStateMachine>();
        InitializePowerupsUi();
    }

    // update for the current input system 
    void Update() {
    
        //if (Input.GetKeyDown(KeyCode.Tab)) stateMachine.cameraindexPos = stateMachine.cameraindexPos < 2 ? stateMachine.cameraindexPos + 1 : 0;
        if (Input.GetKeyUp(KeyCode.E)) CyclePowerupIndex();
        if (Input.GetKeyUp(KeyCode.F)) UsePowerUp();

    }

    void FixedUpdate() {
        HandleEeffects();
        stateMachine.KPH = stateMachine.rigidbody.linearVelocity.magnitude * 3.6f;

        UpdateSpline();
        Physics();

    }

    public void Physics() {

        //this is world axis
        //stateMachine.rigidbody.AddForce(transform.up * (downforceMultiplier * stateMachine.KPH));

        //this should be local axis
        _downForce = stateMachine.KPH * downForceFactor;
        stateMachine.rigidbody.AddForce(-stateMachine.rigidbody.transform.up * _downForce , ForceMode.Force);
        //stateMachine.rigidbody.angularDamping = Mathf.Lerp(stateMachine.rigidbody.angularDamping, stateMachine.KPH * stateMachine.CarStats.angularVelocityFactor, Time.deltaTime * 3);
        //stateMachine.rigidbody.linearDamping = Mathf.Lerp(stateMachine.rigidbody.linearDamping, stateMachine.KPH * stateMachine.CarStats.linearVelocityFactor, Time.deltaTime * 3);
        stateMachine.rigidbody.angularDamping = stateMachine.KPH * angularVelocityFactor;
        stateMachine.rigidbody.linearDamping = stateMachine.KPH * linearVelocityFactor;

    }


    // camera effects on shift press
    public void HandleEeffects() {
        if (Camera.main == null || stateMachine.CarStats == null) return;

        if (  stateMachine.isShiftPressed || usingNitrus) {
            stateMachine.boostNm = stateMachine.CarStats.boostPowerNM;
            Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView,stateMachine.CarStats.inistalFow + stateMachine.CarStats.fowDisplaceAmount, stateMachine.CarStats.fowDisplaceLerpSpeed * Time.deltaTime);
            SetExhaust(true);
        } else {
            stateMachine.boostNm = 0;
            Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView,stateMachine.CarStats.inistalFow, stateMachine.CarStats.fowDisplaceLerpSpeed * Time.deltaTime);
            SetExhaust(false);
        }
    }

    void CyclePowerupIndex() {
        if (stateMachine.selectedPowerupIndex < 2) stateMachine.selectedPowerupIndex += 1;
        else stateMachine.selectedPowerupIndex = 0;
        UpdatePowerupsUi();
    }

    void UsePowerUp() {
        for (int i = 0; i < stateMachine.powerups.Count; i++) {
            if (stateMachine.powerups[i].index == stateMachine.selectedPowerupIndex) {
                //Powerup _powerUp = stateMachine.powerups[i];
                switch (stateMachine.powerups[i].type) {
                    case PowerupType.nitrus: UseNitrus(); break;
                    case PowerupType.rocket: UseRocket(); break;
                    case PowerupType.shield: UseShield(); break;
                }
                stateMachine.powerups.RemoveAt(i);
                break;
            }
        }
        UpdatePowerupsUi();
    }

    void OnSwitch(InputValue value) => stateMachine.cameraindexPos = stateMachine.cameraindexPos < 2 ? stateMachine.cameraindexPos + 1 : 0;

    

    #endregion

    #region powerups
    private void UseNitrus() {
        StartCoroutine(NitrusCoroutine());
    }

    private IEnumerator NitrusCoroutine() {
        usingNitrus = true;
        yield return new WaitForSeconds(5f);
        usingNitrus = false;
    }

    private void UseRocket() {
        print("using rocket powerup");
    }

    private void UseShield() {
        print("using shield powerup");
    }

    #endregion

    #region spline
    private int closestSplineIndex;
    private float minDistance;
    private float3  nearestPoint,localPos;
    private float  dist, t;
    private Spline  spline;


    void UpdateSpline() {
        // Todo : this needs to be pointed automatically
        if( stateMachine.splineContainer == null)return;
        //Convert racer position to the Spline Container's local space
        localPos = stateMachine.splineContainer.transform.InverseTransformPoint(transform.position);

        // this is the distance from the object to the nearest point 
        minDistance = float.MaxValue;

        // method to loop thru all the spline objects inside a spline container 
        for (int i = 0; i < stateMachine.splineContainer.Splines.Count; i++) {
            spline = stateMachine.splineContainer.Splines[i];

            // Get nearest point on this specific spline
            SplineUtility.GetNearestPoint(spline, localPos, out nearestPoint, out t);
            dist = math.distance(localPos, nearestPoint);

            if (dist < minDistance) {
                minDistance = dist;
                closestSplineIndex = i;
                //nearestPointT = t;
            }
        }

        stateMachine.splinePositionFloat = t;




    }

    #endregion

    #region bus
    public void SetExhaust(bool _exhaust) {
        if (_exhaust) {
            foreach (var item in stateMachine.nitrus) {
                if (!item.isPlaying) {
                    item.Play();
                }
            }
        } else {
            foreach (var item in stateMachine.nitrus) {
                item.Stop();
            }
        }
    }
    #endregion

    #region ui

    public void InitializePowerupsUi() {
        // this will just initialise the place holders for the ui components for the powerups nothings else .
        // not using this count cur players will always have a set amount of powerupds to hold ,
        //var PowerupTypeCount = Enum.GetNames(typeof(PowerupType)).Length;

        float spaceBetween = 120;

        for (int i = 0; i < 3; i++) {
            GameObject newObj = Instantiate(stateMachine.reusablePowerupsUiContainer, stateMachine.powerupsUiContainer.transform);
            RectTransform newRect = newObj.GetComponent<RectTransform>();
            newRect.localPosition = new Vector3((i * spaceBetween) - spaceBetween, newRect.localPosition.y, newRect.localPosition.z);
            stateMachine.reusablePowerupsUiObjects.Add(newRect);
        }

        UpdatePowerupsUi();
    }

    void UpdatePowerupsUi() {

        for (int i = 0; i < stateMachine.reusablePowerupsUiObjects.Count; i++) {

            // this will overlay a ui image to indicate the selected powerup
            stateMachine.reusablePowerupsUiObjects[i].GetComponent<Image>().color = new Color(1f, 1f, 1f, i == stateMachine.selectedPowerupIndex ? 1f : 0f);

            for (int j = 0; j < stateMachine.powerups.Count; j++) {
                if (stateMachine.powerups[j].index == i) {
                    Powerup _powerUp = stateMachine.powerups[j];

                    // main enable powerup ui image function !
                    stateMachine.reusablePowerupsUiObjects[i].GetChild(0).transform.localScale = _powerUp.type == PowerupType.rocket ? new Vector3(1, 1, 1) : new Vector3(0, 0, 0);
                    stateMachine.reusablePowerupsUiObjects[i].GetChild(1).transform.localScale = _powerUp.type == PowerupType.nitrus ? new Vector3(1, 1, 1) : new Vector3(0, 0, 0);
                    stateMachine.reusablePowerupsUiObjects[i].GetChild(2).transform.localScale = _powerUp.type == PowerupType.shield ? new Vector3(1, 1, 1) : new Vector3(0, 0, 0);

                    break;
                } else {
                    // setting for when the powerup is used , so it disable the used one !
                    stateMachine.reusablePowerupsUiObjects[i].GetChild(0).transform.localScale = new Vector3(0, 0, 0);
                    stateMachine.reusablePowerupsUiObjects[i].GetChild(1).transform.localScale = new Vector3(0, 0, 0);
                    stateMachine.reusablePowerupsUiObjects[i].GetChild(2).transform.localScale = new Vector3(0, 0, 0);
                }
            }

            // setting for cases when there is no powerups !
            if (stateMachine.powerups.Count == 0) {
                stateMachine.reusablePowerupsUiObjects[i].GetChild(0).transform.localScale = new Vector3(0, 0, 0);
                stateMachine.reusablePowerupsUiObjects[i].GetChild(1).transform.localScale = new Vector3(0, 0, 0);
                stateMachine.reusablePowerupsUiObjects[i].GetChild(2).transform.localScale = new Vector3(0, 0, 0);
            }

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
        if(!CompareTag("Player"))return;
        float pos = GuiYPos;
        GUI.Label(new Rect(GuiXPos, pos, GuiCellWidth, GuiCellHeight), "KPH: " + stateMachine.KPH.ToString("0"), customStyle);
        pos += GuiYSpace;
        //GUI.Label(new Rect(GuiXPos, pos, GuiCellWidth, GuiCellHeight), "selected powerup: " + stateMachine.powerups[stateMachine.selectedPowerupIndex].ToString());
        //pos += GuiYSpace;
    }
    #endregion

}

[System.Serializable]
public enum WheelType {
    front, rear
}
