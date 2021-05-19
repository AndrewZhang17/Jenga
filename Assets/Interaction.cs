using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;

[RequireComponent(typeof(ARRaycastManager))]
public class Interaction : MonoBehaviour
{
    public GameController gameController;
    
    private ARRaycastManager _arRaycastManager;

    static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private bool started = false;


    void Awake() 
    {
        _arRaycastManager = GetComponent<ARRaycastManager>();
    }

    bool TryGetTouchPosition(out Vector3 inputPosition)
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                //When a touch has first been detected, change the message and record the starting position
                case TouchPhase.Began:
                    // Record initial touch position.
                    inputPosition = touch.position;
                    return true;
                case TouchPhase.Moved:
                    break;
                case TouchPhase.Ended:
                    break;
                case TouchPhase.Stationary:
                    break;
            }
        }
        inputPosition = default;
        return false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!started) {
            Vector3 inputPosition;

            var touchResult = TryGetTouchPosition(out inputPosition);
            if (touchResult && _arRaycastManager.Raycast(inputPosition, hits, TrackableType.PlaneWithinPolygon)) {
                gameController.SpawnTower(hits[0].pose.position);
                started = true;
            }
        }
        
    }
}
