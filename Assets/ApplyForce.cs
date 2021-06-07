using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplyForce : MonoBehaviour
{
    private Rigidbody rb;
    private bool active = false;

    private GameController gameController;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Get game controller
        gameController = FindObjectOfType<GameController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (active && Input.GetMouseButton(0) && gameController.isPlayerTurn()) {
            var worldMousePosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 4));

            var direction = worldMousePosition - transform.position;
            direction.y = 0;

            rb.AddForce((direction).normalized * 5 * Time.deltaTime);
        }
    }

    void OnMouseDown()
    {
        active = true;
    }

    void OnMouseUp()
    {
        active = false;
    }
}
