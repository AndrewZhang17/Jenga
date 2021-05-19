using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public GameObject PlanePrefab;
    public GameObject BlockPrefab;
    public int NumLayers;
    public float MaxLateralError;
    public float MaxVerticalError;

    private float blockWidth;
    private float blockHeight;

    // Start is called before the first frame update
    void Start()
    {
        
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SpawnTower(Vector3 pos) 
    {
        Vector3 dims = BlockPrefab.GetComponent<Renderer>().bounds.size;
        blockWidth = dims.z;
        blockHeight = dims.y;
        float curHeight = 0;
        bool rotate = false;

        Instantiate(PlanePrefab, pos, Quaternion.identity);

        for (int i = 0; i < NumLayers; i++) {
            for (int j = -1; j < 2; j++) {
                float verticalError = Random.Range(0f, MaxVerticalError);
                float lateralError = Random.Range(-MaxLateralError / 2, MaxLateralError / 2);
                Instantiate(BlockPrefab, pos + new Vector3(rotate ? (j * blockWidth) + lateralError : 0, curHeight + verticalError, rotate ? 0 : (j * blockWidth) + lateralError), Quaternion.Euler(0, rotate ? 90 : 0, 0));
            }
            curHeight += blockHeight + MaxVerticalError;
            rotate = !rotate;
        }
    }
}
