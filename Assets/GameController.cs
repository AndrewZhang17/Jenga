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

    private GameObject turnDisplay;

    public const string DB_PATH = "test";
    public FirebaseDatabase database;

    public int playerNum = 0;
    public int currentPlayerTurn;

    // Start is called before the first frame update
    void Start()
    {
        // Get firebase database 
        database = FirebaseDatabase.DefaultInstance;

        // Get turn display
        turnDisplay = GameObject.Find("TurnDisplay");

        // Listen for database events
        database.GetReference(DB_PATH + "/turn").ValueChanged += HandleTurnChanged;
    }

    private async void BuildExistingTower() {
        // Remove all existing blocks
        foreach (Block block in FindObjectsOfType<Block>()) {
            block.DeleteListener();
            Destroy(block.gameObject);
        }

        for (int layer = 0; layer < NumLayers; layer++) {
            for (int rowPosition = -1; rowPosition < 2; rowPosition++) {
                // Get this block's position and rotation
                var blockData = await database.GetReference(DB_PATH + "/" + layer + "-" + (rowPosition + 1)).GetValueAsync();
                
                // Create new block
                GameObject newBlock = Instantiate(BlockPrefab, new Vector3(100, 100, 100), Quaternion.identity);

                // Set block parameters
                newBlock.GetComponent<Block>().SetBlockParams(layer, rowPosition);
                newBlock.GetComponent<Block>().SetBlockPosition(blockData.Value);
            }
        }
    }

    private void BuildNewTower() {
        // Remove all existing blocks
        foreach (Block block in FindObjectsOfType<Block>()) {
            block.DeleteListener();
            Destroy(block.gameObject);
        }

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
                newBlock.GetComponent<Block>().SetBlockParams(i, j, true);
            }
            curHeight += blockHeight + MaxVerticalError;
            rotate = !rotate;
        }
    }

    private void updateTurnDisplay() {
        string text = "";

        if (playerNum == 0) {
            text = "Select a player";
        } else if (isPlayerTurn()) {
            text = "Player " + currentPlayerTurn + ", it's your turn!";
        } else {
            text = "Player " + currentPlayerTurn + ", it's not your turn.";
        }

        turnDisplay.GetComponent<UnityEngine.UI.Text>().text = text;
    }

    public bool isPlayerTurn() {
        return playerNum == currentPlayerTurn;
    }

    void HandleTurnChanged(object sender, ValueChangedEventArgs args) {
        currentPlayerTurn = int.Parse(args.Snapshot.Value.ToString());
        updateTurnDisplay();
        Debug.Log("New turn: " + currentPlayerTurn + ", " + (isPlayerTurn() ? "Enabling" : "Disabling") + " Gravity");

        // Get all blocks and turn on/off gravity
        foreach (Block block in FindObjectsOfType<Block>()) {
            block.EnableGravity(isPlayerTurn());
        }
    }

    // Update is called once per frame
    async void Update()
    {
        if (Input.GetKeyDown("1")) {
            playerNum = 1;
            Debug.Log("Set player number to " + playerNum + ", " + (isPlayerTurn() ? "Enabling" : "Disabling") + " Gravity");

            // Get all blocks and turn on/off gravity
            foreach (Block block in FindObjectsOfType<Block>()) {
                block.EnableGravity(isPlayerTurn());
            }
        }

        if (Input.GetKeyDown("2")) {
            playerNum = 2;
            Debug.Log("Set player number to " + playerNum + ", " + (isPlayerTurn() ? "Enabling" : "Disabling") + " Gravity");

            // Get all blocks and turn on/off gravity
            foreach (Block block in FindObjectsOfType<Block>()) {
                block.EnableGravity(isPlayerTurn());
            }
        }

        if (Input.GetKeyDown(KeyCode.E) && isPlayerTurn()) {
            // Get all blocks and set their positions
            foreach (Block block in FindObjectsOfType<Block>()) {
                await block.SetBlockPositionFirebase();
            }

            // Update turn number
            await database.GetReference(DB_PATH + "/turn").SetValueAsync((currentPlayerTurn % 2) + 1);
        }

        if (Input.GetKeyDown(KeyCode.N)) {
            BuildNewTower();
        }

        if (Input.GetKeyDown(KeyCode.M)) {
            BuildExistingTower();
        }
    }
}
