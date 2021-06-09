using System.Collections;
using System.Collections.Generic;
using System.Runtime;
using UnityEngine;
using Firebase.Database;

public class Block : MonoBehaviour
{
    private int layer;

    private int rowPosition;

    private string blockPath;

    private GameController gameController;

    private FirebaseDatabase database;

    private Vector3 oldPos;

    private float threshold = 0.001f;

    private bool isNewPiece = false;

    // Start is called before the first frame update
    async void Start()
    {
        // Get game controller
        gameController = FindObjectOfType<GameController>();

        // Get firebase database 
        database = gameController.database;

        // Get block path
        blockPath = GameController.DB_PATH + "/" + layer + "-" + (rowPosition + 1);

        // Listen for database events
        database.GetReference(blockPath).ValueChanged += HandleValueChanged;

        // Save current position
        oldPos = transform.position;

        // Save position to Firebase if it's part of a new tower
        if (isNewPiece) {
            await SetBlockPositionFirebase();
        }

        Debug.Log("Block Created! Layer: " + layer + ", Row Position: " + rowPosition + ", Path: " + blockPath);
    }

    // Update is called once per frame
    async void Update()
    {
        if (gameController.isPlayerTurn() && Vector3.Distance(transform.position, oldPos) >= threshold) {
            await SetBlockPositionFirebase();
            oldPos = transform.position;
        }
    }

    public void DeleteListener() {
        // Remove database listeners
        database.GetReference(blockPath).ValueChanged -= HandleValueChanged;
    }

    public void EnableGravity(bool enable) {
        this.GetComponent<BoxCollider>().enabled = enable;
        this.GetComponent<Rigidbody>().useGravity = enable;
    }

    public void SetBlockParams(int layer, int rowPosition, bool isNewPiece = false) {
        this.layer = layer;
        this.rowPosition = rowPosition;
        this.isNewPiece = isNewPiece;
    }

    public async System.Threading.Tasks.Task SetBlockPositionFirebase() {
        // Set position
        string positionJson = JsonUtility.ToJson(transform.position);
        await database.GetReference(blockPath + "/position").SetRawJsonValueAsync(positionJson);

        // Set rotation
        string rotationJson = JsonUtility.ToJson(transform.rotation.eulerAngles);
        await database.GetReference(blockPath + "/rotation").SetRawJsonValueAsync(rotationJson);
    }

    void HandleValueChanged(object sender, ValueChangedEventArgs args) {
        if (!gameController.isPlayerTurn()) {
            SetBlockPosition(args.Snapshot.Value);
        }
    }

    public void SetBlockPosition(object positionData) {
        // Get position and rotation dictionaries
        foreach (KeyValuePair<string, object> obj in positionData as Dictionary<string, object>) {
            if (obj.Key == "position") {
                Vector3 position = new Vector3(0, 0, 0);

                foreach (KeyValuePair<string, object> pos in obj.Value as Dictionary<string, object>) {
                    float value = System.Convert.ToSingle(pos.Value);

                    position = new Vector3(
                        pos.Key == "x" ? value : position.x,
                        pos.Key == "y" ? value : position.y,
                        pos.Key == "z" ? value : position.z
                    );
                }

                transform.position = position;
                oldPos = position;
            } else if (obj.Key == "rotation") {
                Vector3 rotation = new Vector3(0, 0, 0);

                foreach (KeyValuePair<string, object> rot in obj.Value as Dictionary<string, object>) {
                    float value = System.Convert.ToSingle(rot.Value);

                    // rotation = new Quaternion(
                    rotation = new Vector3(
                        rot.Key == "x" ? value : rotation.x,
                        rot.Key == "y" ? value : rotation.y,
                        rot.Key == "z" ? value : rotation.z
                    );
                }

                transform.rotation = Quaternion.Euler(rotation);
            }
        }
    }
}
