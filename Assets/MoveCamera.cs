using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    public float speed;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKey(KeyCode.RightArrow))
        {
            transform.RotateAround(Vector3.zero, Vector3.up, -speed * 15 * Time.deltaTime);
        }
        if(Input.GetKey(KeyCode.LeftArrow))
        {
            transform.RotateAround(Vector3.zero, Vector3.up, speed * 15 * Time.deltaTime);
        }
        if(Input.GetKey(KeyCode.DownArrow))
        {
            transform.Translate(new Vector3(0,-speed * Time.deltaTime,0));
        }
        if(Input.GetKey(KeyCode.UpArrow))
        {
            transform.Translate(new Vector3(0,speed * Time.deltaTime,0));
        }
    }
}
