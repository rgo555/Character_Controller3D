using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IsometricMovement : MonoBehaviour
{
    CharacterController controller;
    Animator anim;

    public float speed = 8;
    
    float gravity = -9.81f;
    Vector3 playerVelocity;

    public LayerMask groundLayer;

    // Start is called before the first frame update
    void Awake()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        //Gravedad
        Vector3 move = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

        float velZ = Vector3.Dot(move.normalized, transform.forward);
        float velX = Vector3.Dot(move.normalized, transform.right);

        anim.SetFloat("VelZ", velZ);
        anim.SetFloat("Velx", velX);

        controller.Move(move.normalized * speed * Time.deltaTime);

         if(!controller.isGrounded && playerVelocity.y <0)
        {
            playerVelocity.y = 0;
        }

        if(!controller.isGrounded)
        {
            playerVelocity.y += gravity;
        }

        controller.Move(playerVelocity * Time.deltaTime);

        //rotation con raycast
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if(Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
        {
            Vector3 direction = hit.point - transform.position;
            direction.y = 0;
            transform.forward = direction;
        }
    }
}
