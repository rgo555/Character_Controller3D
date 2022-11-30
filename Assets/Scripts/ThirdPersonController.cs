using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonController : MonoBehaviour
{
    private CharacterController controller;
    private Animator anim;
    public Transform cam;
    public Transform LookAtTransform;

    //Cosas para el Ragdoll
    private Rigidbody[] ragdollBodies; 
    private SphereCollider[] sphereColliders;
    private CapsuleCollider[] capsuleColliders;
    private bool isRagdoll = false; 

    //variables para controlar velocidad, altura de salto y gravedad

    public float speed = 5;
    public float jumpHeight = 1;
    public float gravity = -9.81f;
    [SerializeField]private float pushStrength = 4f;
    [Header("Fisicas")]

    //variables para el ground sensor
    [Header("Sensor Suelo")]
    public bool isGrounded;
    public Transform groundSensor;
    public float sensorRadius = 0.1f;
    public LayerMask ground;
    private Vector3 playerVelocity;

    //variables para rotacion del personaje
    private float turnSmoothVelocity;
    public float turnSmoothTime = 0.1f;

    //variables para el movimiento del raton con virtual camera
    public Cinemachine.AxisState xAxis;
    public Cinemachine.AxisState yAxis;

    //Variable para coger objetos
    public GameObject objectToPick;
    [SerializeField]private GameObject pickedObject;
    [SerializeField]Transform interactionZone;

    public GameObject[] cameras;
    
    public LayerMask raylayer;
    // Start is called before the first frame update
    void Start()
    {
        //Asignamos el character controller a su variable
        controller = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>();

        //Con esto podemos esconder el icono del raton para que no moleste
        Cursor.lockState = CursorLockMode.Locked;

        ragdollBodies = GetComponentsInChildren<Rigidbody>();
        sphereColliders = GetComponentsInChildren<SphereCollider>();
        capsuleColliders = GetComponentsInChildren<CapsuleCollider>(); 

        foreach (Rigidbody body in ragdollBodies)
        {
            body.isKinematic = true;
        }

        foreach (SphereCollider sphere in sphereColliders)
        {
            sphere.enabled = false;
        }

        foreach (CapsuleCollider capsule in capsuleColliders)
        {
            capsule.enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Llamamos la funcion de movimiento
        //Movement();
        MovementTPS();
        //MovementTPS2();
        
        if(!isRagdoll)
        {
            MovementTPS();
            Jump();
            PickObjects();
        }

        //Lamamaos la funcion de salto
        Jump();
        PickObjects();

        RaycastHit hit;
        if(Physics.Raycast(transform.position, transform.forward, out hit, 20f, raylayer))
        {
            Vector3 hitPosition = hit.point;
            float hitDistance = hit.distance;
            string hitName = hit.transform.name;
            //Animator hitAnimator = hit.transform.GameObject.GetComponent<Animator>();
            //hit.transform.GameObject.GetComponent<ScripRandom>().FuncionRandom();
            Debug.DrawRay(transform.position, transform.forward * 20f, Color.green);
            Debug.Log("posicion impacto: " + hitPosition + "distancia impacto: " + hitDistance + " nombre objeto: " + hitName);
        }
        else
        {
            Debug.DrawRay(transform.position, transform.forward * 20f, Color.red);
        }
        if(Input.GetButtonDown("Fire1"))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit2;
            if(Physics.Raycast(ray, out hit2))
            {
                Debug.Log(hit2.point);
                transform.position = new Vector3(hit2.point.x, transform.position.y, hit2.point.z);
            }
        }
    }
#region FuncionesDeMovimiento
    void Movement()
    {
        //Creamos un Vector3 y en los ejes X y Z le asignamos los inputs de movimiento
        Vector3 move = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

        if(move != Vector3.zero)
        {
            //Creamos una variable float para almacenar la posicion a la que queremos que mire el personaje
            //Usamos la funcion Atan2 para calcular el angulo al que tendra que mirar nuestro personaje
            //lo multiplicamos por Rad2Deg para que nos de el valor en grados y le sumamos la rotacion de la camara en Y para que segund donde mire la camara afecte a la rotacion
            float targetAngle = Mathf.Atan2(move.x, move.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            //Usamos un SmoothDamp para que nos haga una transicion entre el angulo actual y al que queremos llegar
            //de esta forma no nos rotara de golpe al personaje
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            //le aplicamos la rotacion al personaje
            transform.rotation = Quaternion.Euler(0, angle, 0);

            //Creamos otro Vector3 el cual multiplicaremos el angulo al que queremos que mire el personaje por un vector hacia delante
            //para que el personaje camine en la direccion correcta a la que mira
            Vector3 moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            //Funcion del character controller a la que le pasamos el Vector que habiamos creado y lo multiplicamos por la velocidad para movernos
            controller.Move(moveDirection.normalized * speed * Time.deltaTime);
        }
    }

    //Movimiento TPS con Freelook camera
    void MovementTPS()
    {
        float z = Input.GetAxisRaw("Vertical");
        anim.SetFloat("VelZ", z);
        float x = Input.GetAxisRaw("Horizontal");
        anim.SetFloat("VelX", x);

        //Creamos un Vector3 y en los ejes X y Z le asignamos los inputs de movimiento
        Vector3 move = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

        if(move != Vector3.zero)
        {
            //Creamos una variable float para almacenar la posicion a la que queremos que mire el personaje
            //Usamos la funcion Atan2 para calcular el angulo al que tendra que mirar nuestro personaje
            //lo multiplicamos por Rad2Deg para que nos de el valor en grados y le sumamos la rotacion de la camara en Y para que segund donde mire la camara afecte a la rotacion
            float targetAngle = Mathf.Atan2(move.x, move.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            //Usamos un SmoothDamp para que nos haga una transicion entre el angulo actual y el de la camara
            //de esta forma no nos rotara de golpe al personaje
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, cam.eulerAngles.y, ref turnSmoothVelocity, turnSmoothTime);
            //le aplicamos la rotacion al personaje
            transform.rotation = Quaternion.Euler(0, angle, 0);

            //Creamos otro Vector3 el cual multiplicaremos el angulo al que queremos que mire el personaje por un vector hacia delante
            //para que el personaje camine en la direccion correcta a la que mira
            Vector3 moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            //Funcion del character controller a la que le pasamos el Vector que habiamos creado y lo multiplicamos por la velocidad para movernos
            controller.Move(moveDirection.normalized * speed * Time.deltaTime);
        }
    }

    //Movimiento TPS con virtaul camera
    void MovementTPS2()
    {
        //Creamos un Vector3 y en los ejes X y Z le asignamos los inputs de movimiento
        Vector3 move = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

        //Actualizamos los inputs del raton
        xAxis.Update(Time.deltaTime);
        yAxis.Update(Time.deltaTime);

        //Hacemos rotar al personaje en el eje Y dependiendo del valor X del raton
        transform.rotation = Quaternion.Euler(0, xAxis.Value, 0);
        //Hacemos rotar la camara en el eje X dependiendo del valor del raton en el eje Y
        LookAtTransform.eulerAngles = new Vector3(yAxis.Value, xAxis.Value, LookAtTransform.eulerAngles.z);
        //LookAtTransform.rotation = Quaternion.Euler(yAxis.Value, xAxis.Value, LookAtTransform.eulerAngles.z);

        //Si pulsamos el boton de apuntar activamos y desactivamos las camaras correspondientes
        if(Input.GetButton("Fire2"))
        {
            cameras[0].SetActive(false);
            cameras[1].SetActive(true);
        }
        else
        {
            cameras[0].SetActive(true);
            cameras[1].SetActive(false);
        }


        if(move != Vector3.zero)
        {
            //Creamos una variable float para almacenar la posicion a la que queremos que mire el personaje
            //Usamos la funcion Atan2 para calcular el angulo al que tendra que mirar nuestro personaje
            //lo multiplicamos por Rad2Deg para que nos de el valor en grados y le sumamos la rotacion de la camara en Y para que segund donde mire la camara afecte a la rotacion
            float targetAngle = Mathf.Atan2(move.x, move.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            //Usamos un SmoothDamp para que nos haga una transicion entre el angulo actual y el de la camara
            //de esta forma no nos rotara de golpe al personaje
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, cam.eulerAngles.y, ref turnSmoothVelocity, turnSmoothTime);
            
            //Creamos otro Vector3 el cual multiplicaremos el angulo al que queremos que mire el personaje por un vector hacia delante
            //para que el personaje camine en la direccion correcta a la que mira
            Vector3 moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            //Funcion del character controller a la que le pasamos el Vector que habiamos creado y lo multiplicamos por la velocidad para movernos
            controller.Move(moveDirection.normalized * speed * Time.deltaTime);
        }
    }
#endregion

#region FuncionDeSalto
    //Funcion de salto y gravedad
    void Jump()
    {
        //Le asignamos a la boleana isGrounded su valor dependiendo del CheckSpher
        //CheckSphere crea una esfera pasandole la poscion, radio y layer con la que queremos que interactue
        //si la esfera entra en contacto con la capa que le digamos convertira nuestra boleana en true y si no entra en contacto en false
        //isGrounded = Physics.CheckSphere(groundSensor.position, sensorRadius, ground);

        //isGrounded = Physics.Raycast(groundSensor.position, Vector3.down, sensorRadius, ground);
        if(Physics.Raycast(groundSensor.position, Vector3.down, sensorRadius, ground))
        {
            isGrounded = true;
            Debug.DrawRay(groundSensor.position, Vector3.down * sensorRadius, Color.green);
        }
        else
        {
            isGrounded = false;
            Debug.DrawRay(groundSensor.position, Vector3.down * sensorRadius, Color.red);
        }

        anim.SetBool("Jump", !isGrounded);

        //Si estamos en el suelo y playervelocity es menor que 0 hacemos que le vuelva a poner el valor a 0
        //esto es para evitar que siga aplicando fuerza de gravedad cuando estemos en el suelo y evitar comportamientos extraños
        if(isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = 0;
        }

        //si estamos en el suelo y pulasamos el imput de salto hacemos que salte el personaje
        if(isGrounded && Input.GetButtonDown("Jump"))
        {
            //Formula para hacer que los saltos sean de una altura concreta
            //la altura depende del valor de jumpHeight 
            //Si jumpHeigt es 1 saltara 1 metro de alto
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2 * gravity); 
        }

        //a playervelocity.y le iremos sumando el valor de la gravedad
        playerVelocity.y += gravity * Time.deltaTime;
        //como playervelocity en el eje Y es un valor negativo esto nos empuja al personaje hacia abajo
        //asi le aplicaremos la gravedad
        controller.Move(playerVelocity * Time.deltaTime);
    }
#endregion
     
    void OnDrawGizmos() 

        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, transform.forward * 20);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(groundSensor.position, sensorRadius);
        }

#region FuncionCoger
    void PickObjects()
    {
        if(Input.GetKeyDown(KeyCode.E))
        {
            if(objectToPick != null && pickedObject == null && objectToPick.gameObject.GetComponent<PickableObject>().isPickable == true)
            {
                pickedObject = objectToPick;
                pickedObject.GetComponent<PickableObject>().isPickable = false;
                pickedObject.transform.SetParent(interactionZone);
                pickedObject.transform.position = interactionZone.position; 
                pickedObject.GetComponent<Rigidbody>().useGravity = false;
                pickedObject.GetComponent<Rigidbody>().isKinematic = true; 
            }
            else if(pickedObject != null)
            {
                pickedObject.GetComponent<PickableObject>().isPickable = true;
                pickedObject.transform.SetParent(null);
                pickedObject.GetComponent<Rigidbody>().useGravity = true;
                pickedObject.GetComponent<Rigidbody>().isKinematic = false; 
                pickedObject = null;
            }
        }
    }
#endregion

    private void OnControllerColliderHit(ControllerColliderHit hit) 
    {
        if(hit.gameObject.tag == "Empujable")
        {
            Rigidbody body = hit.collider.attachedRigidbody;

            if(body == null || body.isKinematic)
            {
                return;
            }

            Vector3 pushDir = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
            body.velocity = pushDir * pushStrength / body.mass;
        }
    }

    void Ragdolls()
    {
        if(Input.GetKeyDown(KeyCode.F))
            {
            foreach (Rigidbody body in ragdollBodies)
            {
                body.isKinematic = false;
            }

            foreach (SphereCollider sphere in sphereColliders)
            {
                sphere.enabled = true;
            }

            foreach (CapsuleCollider capsule in capsuleColliders)
            {
                capsule.enabled = true;
            }

            controller.enabled = false;
            anim.enabled = false;
        }
    }

}
