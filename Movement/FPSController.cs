using UnityEngine;

public enum MoveState { Idle, Walking, Running, Jumping, Falling};

[RequireComponent(typeof(CharacterController))]

public class FPSController : MonoBehaviour {
    [Header("General")]
    [SerializeField] private MoveState m_moveState = MoveState.Idle;
    public bool playerControl = true;

    [Header("Movement")]
    [SerializeField] [Range(0f, 1f)] private float m_moveBackwardFactor = .8f;
    [SerializeField] [Range(0f, 1f)] private float m_moveSideFactor = .8f;
    [SerializeField] private float m_antiBumpFactor = .75f;
    private CharacterController m_characterController;
    private bool m_grounded = true;
    private float m_speed;
    private Vector2 m_input = Vector2.zero;
    private Vector3 m_moveDirection = Vector3.zero;
    private Vector3 m_moveVelocity = Vector3.zero;
    private float m_stepCycle; 
    private float m_nextStep; 
    private CollisionFlags m_collisionFlags;

    [Header("Walking")]
    [SerializeField] private float m_walkSpeed = 5f;
    [SerializeField] private float m_stepInterval = 5f; 

    [Header("Running")]
    [SerializeField] private float m_runSpeed = 8f;
    [SerializeField] [Range(0f, 1f)] private float m_runstepLenghten = .7f;

    [Header("Jumping")]
    [SerializeField] private float m_jumpVerticalSpeed = 7f;
    [SerializeField] private float m_jumpHorizontalSpeed = 2f;
    [SerializeField] private float m_framesGroundedBetweenJumps = 1;
    private float m_jumpFrameCounter;

    [Header("Falling")]
    [SerializeField] private float m_fallingDamageThreshold = 10f;
    private float m_fallStartLevel;
    private bool m_falling = false;

    [Header("Camera")]
    [SerializeField] private FPSMouseLook m_mouseLook;
    private Camera m_fpsCamera;

    [Header("Physics")]
    [SerializeField] private float m_gravity = 20f;
    [SerializeField] private float m_pushPower = .1f;

    

    // ---------- Properties ---------- 

    public float Speed
    {
        get { return m_speed; }
        set { m_speed = Mathf.Clamp(value, 0, m_runSpeed); }
    }
    public MoveState CurrentMoveState
    {
        get { return m_moveState; }
    }

    private void Awake()
    {
        m_characterController = GetComponent<CharacterController>();
        m_fpsCamera = Camera.main; 
    }

	private void Start ()
    {
        m_mouseLook.Init(transform, m_fpsCamera.transform);
        m_speed = m_walkSpeed;
        m_jumpFrameCounter = m_framesGroundedBetweenJumps;
        m_stepCycle = m_nextStep = 0f;
    }
	
	private void Update ()
    {
        if (!playerControl)
            return;
        m_mouseLook.LookRotation();

        if (m_grounded)
        {
            if (m_falling)
            {
                m_falling = false;
            }
            if (Input.GetKey(KeyCode.LeftShift))
            {
                m_speed = m_runSpeed;
            }
            else
            {
                m_speed = m_walkSpeed;
            }
        }
        else
        {
            if (!m_falling)
            {
                m_falling = true;
                m_fallStartLevel = transform.position.y;
            }
        }
        MovePosition();
        UpdateMoveState();
    }

    private void MovePosition()
    {

        m_input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (m_input.y < 0)
            m_input.y *= m_moveBackwardFactor;
        if (m_input.x != 0)
            m_input.x *= m_moveSideFactor;
        if (m_input.sqrMagnitude > 1)
            m_input.Normalize();

        if (m_grounded)
        {
            m_moveDirection = transform.forward * m_input.y + transform.right * m_input.x;
            RaycastHit hitInfo;
            Physics.SphereCast(transform.position + m_characterController.center, m_characterController.radius, Vector3.down, out hitInfo,
                               m_characterController.height / 2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            m_moveDirection = Vector3.ProjectOnPlane(m_moveDirection, hitInfo.normal).normalized;
            m_moveDirection.y = -m_antiBumpFactor;
            m_moveVelocity = m_moveDirection * m_speed;
            if (!Input.GetButton("Jump"))
                m_jumpFrameCounter++;
            else if (m_jumpFrameCounter >= m_framesGroundedBetweenJumps)
            {
                m_moveVelocity.y = m_jumpVerticalSpeed;
                m_moveVelocity += m_moveDirection * m_jumpHorizontalSpeed;
                m_jumpFrameCounter = 0;

            }
        }
        m_moveVelocity.y -= m_gravity * Time.deltaTime; 
        m_collisionFlags = m_characterController.Move(m_moveVelocity * Time.deltaTime);
        m_grounded = (m_collisionFlags & CollisionFlags.Below) != 0;
    }

    //  ---------- Move State ---------- 

    private void UpdateMoveState()
    {
        if (!m_grounded)
        {
            if (m_characterController.velocity.y < 0)
                m_moveState = MoveState.Falling;
            else
                m_moveState = MoveState.Jumping;
            return;
        }

        Vector2 moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        

        if (moveInput.sqrMagnitude == 0)
        {
            m_moveState = MoveState.Idle;
            return;
        }

        if (m_speed == m_runSpeed)
            m_moveState = MoveState.Running;
        else if (m_speed == m_walkSpeed)
            m_moveState = MoveState.Walking;
    }
}
