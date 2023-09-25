using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Movement : MonoBehaviour
{
    [SerializeField] private CharacterController characterController;
    [SerializeField] private float speed = 10f;
    [SerializeField] private float mouseSense = 500f;
    [SerializeField] private Transform playerBody;
    float xRot = 0f;
    float yRot = 0f;

    void Start(){
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update(){
        float inputX = Input.GetAxis("HorizontalSide");
        float inputY = Input.GetAxis("HorizontalFront");
        float inputZ = Input.GetAxis("Vertical");
        
        float mouseX = Input.GetAxis("Mouse X") * mouseSense * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSense * Time.deltaTime;

        xRot -= mouseY;
        xRot = Mathf.Clamp(xRot, -90f, 90f);
        yRot += mouseX;
        playerBody.rotation = Quaternion.Euler(xRot, yRot, 0f);

        Movemt(inputX, inputY, inputZ);
    }

    void Movemt(float x, float y, float z){
        Vector3 move = x * transform.right + y * transform.forward + z * transform.up;
        characterController.Move(move * speed * Time.deltaTime);
    }
}
