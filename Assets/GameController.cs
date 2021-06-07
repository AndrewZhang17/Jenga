using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;

public class GameController : MonoBehaviour
{
    public GameObject BlockPrefab;
    public int NumLayers;
    public float MaxLateralError;
    public float MaxVerticalError;

    private float blockWidth;
    private float blockHeight;

    public const string DB_PATH = "test";
    private FirebaseDatabase database;

    private int count1 = 0;
    private int count2 = 0;

    public int playerNum = 0;
    public int currentPlayerTurn;

    // Start is called before the first frame update
    void Start()
    {
        // Get firebase database 
        database = FirebaseDatabase.DefaultInstance;

        Vector3 dims = BlockPrefab.GetComponent<Renderer>().bounds.size;
        blockWidth = dims.z;
        blockHeight = dims.y;
        float curHeight = 0;
        bool rotate = false;

        for (int i = 0; i < NumLayers; i++) {
            for (int j = -1; j < 2; j++) {
                float verticalError = Random.Range(0f, MaxVerticalError);
                float lateralError = Random.Range(-MaxLateralError / 2, MaxLateralError / 2);
                GameObject newBlock = Instantiate(BlockPrefab, new Vector3(rotate ? (j * blockWidth) + lateralError : 0, curHeight + verticalError, rotate ? 0 : (j * blockWidth) + lateralError), Quaternion.Euler(0, rotate ? 90 : 0, 0));
                newBlock.GetComponent<Block>().SetBlockParams(i, j);
            }
            curHeight += blockHeight + MaxVerticalError;
            rotate = !rotate;
        }

        // Listen for database events
        database.GetReference(DB_PATH + "/turn").ValueChanged += HandleTurnChanged;

        // // Get current turn (not needed because of HandleTurnChanged)
        // var turn = await database.GetReference(DB_PATH + "/turn").GetValueAsync();
        // Debug.Log("Turn: " + turn.Value);
    }

    public bool isPlayerTurn() {
        return playerNum == currentPlayerTurn;
    }

    void HandleTurnChanged(object sender, ValueChangedEventArgs args) {
        currentPlayerTurn = int.Parse(args.Snapshot.Value.ToString());
        Debug.Log("New turn: " + currentPlayerTurn + ", " + (currentPlayerTurn == playerNum ? "Enabling" : "Disabling") + " Gravity");

        // Get all blocks and change their gravity
        foreach (Block block in FindObjectsOfType<Block>()) {
            block.EnableGravity(currentPlayerTurn == playerNum);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q)) {
            count1++;

            database.GetReference(DB_PATH + "/count1").SetRawJsonValueAsync(count1.ToString());
        }

        if (Input.GetKeyDown(KeyCode.W)) {
            count2++;

            database.GetReference(DB_PATH + "/count2").SetRawJsonValueAsync(count2.ToString());
        }

        if (Input.GetKeyDown("1")) {
            playerNum = 1;
            Debug.Log("Set player number to " + playerNum);
        }

        if (Input.GetKeyDown("2")) {
            playerNum = 2;
            Debug.Log("Set player number to " + playerNum);
        }
    }
}
