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

    private float threshold = 0.1f;

    // Start is called before the first frame update
    async void Start()
    {
        // Get game controller
        gameController = FindObjectOfType<GameController>();

        // Get firebase database 
        database = FirebaseDatabase.DefaultInstance;

        // Get block path
        blockPath = GameController.DB_PATH + "/" + layer + "-" + (rowPosition + 1);

        Debug.Log("Block Created! Layer: " + layer + ", Row Position: " + rowPosition + ", Path: " + blockPath);

        // Save initial position to database
        await FirebaseUpdate();

        // Listen for database events
        database.GetReference(blockPath).ValueChanged += HandleValueChanged;

        // Save current position
        oldPos = transform.position;
    }

    // Update is called once per frame
    async void Update()
    {
        if (gameController.isPlayerTurn() && Vector3.Distance(transform.position, oldPos) >= threshold) {
            await FirebaseUpdate();
            oldPos = transform.position;
        }
    }

    public void EnableGravity(bool enable) {
        this.GetComponent<Rigidbody>().useGravity = enable;
    }

    public void SetBlockParams(int layer, int rowPosition) {
        this.layer = layer;
        this.rowPosition = rowPosition;
    }

    public async System.Threading.Tasks.Task FirebaseUpdate() {
        // Set position
        string positionJson = JsonUtility.ToJson(transform.position);
        await database.GetReference(blockPath + "/position").SetRawJsonValueAsync(positionJson);

        // Set rotation
        string rotationJson = JsonUtility.ToJson(transform.rotation);
        await database.GetReference(blockPath + "/rotation").SetRawJsonValueAsync(rotationJson);
    }

    void HandleValueChanged(object sender, ValueChangedEventArgs args) {
        if (!gameController.isPlayerTurn()) {
            // Get position and rotation dictionaries
            foreach (KeyValuePair<string, object> obj in args.Snapshot.Value as Dictionary<string, object>) {
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
                } else if (obj.Key == "rotation") {
                    Quaternion rotation = new Quaternion(0, 0, 0, 0);

                    foreach (KeyValuePair<string, object> pos in obj.Value as Dictionary<string, object>) {
                        float value = System.Convert.ToSingle(pos.Value);

                        rotation = new Quaternion(
                            pos.Key == "x" ? value : rotation.x,
                            pos.Key == "y" ? value : rotation.y,
                            pos.Key == "z" ? value : rotation.z,
                            pos.Key == "w" ? value : rotation.w
                        );
                    }

                    transform.rotation = rotation;
                }
            }
        }
    }
}
