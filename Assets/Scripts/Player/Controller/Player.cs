using System;
using UnityEngine;
using TMPro;

public class Player : LocalManager<Player>
{
    #region References
    [Header("References")]
    [SerializeField] TextMeshProUGUI _debugInputVelocityText;
    [SerializeField] TextMeshProUGUI _debugGlobalVelocityText;
    [SerializeField] TextMeshProUGUI _debugGravityText;
    [SerializeField] TextMeshProUGUI _debugSlopeText;
    [SerializeField] TextMeshProUGUI _debugStateText;
    [SerializeField] TextMeshProUGUI _debugSpeedText;
    [SerializeField] Transform _head;
    [SerializeField] CharacterController _charaCon;
    PlayerInputMap _inputs;
    PlayerFSM _fsm;
    PlayerHealth _playerHealth;
    #endregion

    #region Camera rotation variables
    private float _cameraTargetYRotation;
    private float _cameraTargetXRotation;

    [Header("Camera Settings")]
    //Mouse
    [Range(0.1f, 100)][SerializeField] private float _mouseSensitivityX = 10f;
    [Range(0.1f, 100)][SerializeField] private float _mouseSensitivityY = 10f;
    [SerializeField] private bool _mouseXInverted = false;
    [SerializeField] private bool _mouseYInverted = false;

    //Joysticks
    [Range(1f, 100)][SerializeField] private float _stickSensitivityX = 15f;
    [Range(1f, 100)][SerializeField] private float _stickSensitivityY = 15f;
    [SerializeField] private bool _controllerCameraXInverted = false;
    [SerializeField] private bool _controllerCameraYInverted = false;

    int _mouseXInvertedValue;
    int _mouseYInvertedValue;
    int _controllerCameraXInvertedValue;
    int _controllerCameraYInvertedValue;

    //General
    [SerializeField] float _cameraSmoothness = 100000;
    #endregion

    #region Jumping, Falling and Ground Detection variables
    [Header("Jumping Settings")]
    [Range(0, 50)][SerializeField] private float _jumpStrength = 14f;
    [Range(0, 10)][SerializeField] private float _airborneDrag = 3f;
    [Range(0, 10)][SerializeField] private float _jumpingDrag = 3f;
    [Range(0, 90)][SerializeField] private float _maxCeilingAngle = 30f;

    private const float _gravity = 9.81f;
    private float _currentlyAppliedGravity;
    private Vector3 _steepSlopesMovement;

    private RaycastHit _groundHit;
    private RaycastHit _ceilingHit;
    private const float _groundSpherecastLength = .65f; // _charaCon.height/2 - _charaCon.radius
    private const float _ceilingRaycastLength = 1f; // _charaCon.height/2 + 0.1f margin to mitigate skin width
    private const float _groundSpherecastRadius = .35f; // _charaCon.radius + 0.1f margin to mitigate skin width
    private const float _ceilingSpherecastRadius = .25f; // _charaCon.radius
    private bool _justJumped;
    private float _jumpCooldown;
    private float _coyoteTime;
    private const float _coyoteMaxTime = 0.3f;

    #endregion

    #region Movement Variables
    [Header("Movement Settings")]
    [Range(0, 20)]
    [SerializeField]
    private float _baseSpeed = 9f;
    [Range(0, 1)]
    [SerializeField]
    private float _movementAccelerationSpeed = 0.0666f; //Approx 4 frames
    [Range(0, 1)]
    [SerializeField]
    private float _movementDecelerationSpeed = 0.0666f; //Approx 4 frames
    [Range(0, 100)]
    [SerializeField]
    private float _slideForceOnSlopes = 1f;
    [Range(0, 100)]
    [Tooltip("if lower than movement speed, you will accelerate when airborne")]
    [SerializeField]
    private float _airborneFriction = 9f;
    [Range(0, 100)]
    [SerializeField]
    private float _groundedFriction = 50f;
    // ADD LATER [Range(0, 100)][SerializeField] private float _slidingFriction = 5f;
    private float _currentFriction;

    private float _currentSpeed;
    private Vector3 _movementInputs; // X is Left-Right and Z is Backward-Forward
    private Vector3 _lastFrameMovementInputs;
    private Vector3 _globalMomentum;
    private float _movementAcceleration;
    private bool _isPressingADirection;

    private Vector3 _finalMovement;
    #endregion

    protected override void Awake()
    {
        base.Awake();
        _fsm = GetComponent<PlayerFSM>();
        _playerHealth = GetComponent<PlayerHealth>();
        _inputs = new PlayerInputMap();
        _inputs.FirstPersonCamera.Jump.started += _ => PressJump();
    }

    private void Start()
    {
        transform.rotation = Quaternion.identity;
        _movementInputs = Vector2.zero;
        _globalMomentum = Vector3.zero;
        _currentSpeed = _baseSpeed;
        _currentlyAppliedGravity = 0;
        _movementAcceleration = 0;
        SetCameraInvert();
    }

    //TODO // Slopes behaviours
    //TODO Walljump?
    //? when airborne, Raycast towards inputDirection and, if wall, and if Vector3.Dot(inputdirection, wall.normal) ~= -1, wallride
    //? jump again when wallriding to walljump => add jumpStrength to gravity; reset momentum; and add wall's normal to momentum

    private void Update() //Things that are StateMachine-unrelated
    {
        //jump cooldown
        if (_justJumped)
        {
            _jumpCooldown -= Time.deltaTime;
            if (_jumpCooldown <= 0) _justJumped = false;
        }

        //Camera
        MoveCameraWithRightStick();
        MoveCameraWithMouse();

        //Ground Spherecast and Storage of its values
        Physics.SphereCast(transform.position, _groundSpherecastRadius, -transform.up, out _groundHit, _groundSpherecastLength);
        //Ceiling Spherecast and Storage of its values
        Physics.SphereCast(transform.position, _ceilingSpherecastRadius, transform.up, out _ceilingHit, _ceilingRaycastLength);

        //Update Movement Vectors
        UpdateMovement();
        HandleMovementAcceleration();
        UpdateGlobalMomentum();

        //State update
        if (_fsm.currentState != null)
            _fsm.currentState.StateUpdate();

        //Final Movement Formula //I got lost with the deltatime stuff but i swear it works perfectly
        _finalMovement = (_globalMomentum * Time.deltaTime) + (_movementInputs) + (Vector3.up * _currentlyAppliedGravity * Time.deltaTime) + (_steepSlopesMovement * Time.deltaTime);

        //Debug Values on screen
        UpdateDebugTexts();
    }

    void LateUpdate()
    {
        //Apply Movement
        ApplyMovementsToCharacter(_finalMovement);
    }

    #region Debugs
    private void UpdateDebugTexts()
    {
#if UNITY_EDITOR
        if (_debugStateText)
            _debugStateText.text = _fsm.currentState.Name;

        if (_debugInputVelocityText)
            _debugInputVelocityText.text =
            ("Input Velocity:\nx= " + (_movementInputs.x / Time.deltaTime).ToString("F1") +
            "\nz= " + (_movementInputs.z / Time.deltaTime).ToString("F1") +
            "\n\n Acceleration:\n  " + _movementAcceleration.ToString("F1"));

        if (_debugGlobalVelocityText)
            _debugGlobalVelocityText.text =
            ("Momentum Velocity:\nx= " + (_globalMomentum.x).ToString("F1") +
            "\ny= " + (_globalMomentum.y).ToString("F1") +
            "\nz= " + (_globalMomentum.z).ToString("F1"));

        if (_debugGravityText)
            _debugGravityText.text = ("Gravity:\n   " + _currentlyAppliedGravity.ToString("F1"));

        if (_debugSlopeText)
            _debugSlopeText.text = ("Slope Velocity:\n   " + _steepSlopesMovement.ToString("F1"));

        if (_debugSpeedText)
            _debugSpeedText.text = ("Total Speed:\n   " + (_finalMovement.magnitude / Time.deltaTime).ToString("F3"));
#endif
    }

    void OnDrawGizmos()
    {
#if UNITY_EDITOR
        //ground cast
        Gizmos.color = Color.blue;
        Debug.DrawLine(transform.position, transform.position - transform.up * (_groundSpherecastLength), Color.blue, 0f, false);
        RaycastHit debugGroundcast;
        if (Physics.SphereCast(transform.position, _groundSpherecastRadius, -transform.up, out debugGroundcast, _groundSpherecastLength)) // should be the same as CheckGround()'s
            Gizmos.DrawWireSphere(new Vector3(transform.position.x, (debugGroundcast.point.y + _groundSpherecastRadius), transform.position.z), (_groundSpherecastRadius));

        //Direction Vector
        Debug.DrawLine(transform.position, transform.position + _finalMovement / Time.deltaTime * 0.3f, Color.red, 0f);

        //Ceiling Cast
        Debug.DrawLine(transform.position, transform.position + (transform.up * (_ceilingRaycastLength)), Color.cyan, 0f);
#endif
    }
    #endregion

    #region Camera Functions
    private void SetCameraInvert()
    {
        if (_mouseXInverted) _mouseXInvertedValue = -1;
        else _mouseXInvertedValue = 1;

        if (_mouseYInverted) _mouseYInvertedValue = -1;
        else _mouseYInvertedValue = 1;

        if (_controllerCameraXInverted) _controllerCameraXInvertedValue = -1;
        else _controllerCameraXInvertedValue = 1;

        if (_controllerCameraYInverted) _controllerCameraYInvertedValue = -1;
        else _controllerCameraYInvertedValue = 1;
    }

    private void MoveCameraWithMouse()
    {
        Vector2 mouseMovement = _inputs.FirstPersonCamera.Rotate.ReadValue<Vector2>()/* * Time.deltaTime */;
        _cameraTargetYRotation = Mathf.Repeat(_cameraTargetYRotation, 360);
        _cameraTargetYRotation += mouseMovement.x * _mouseSensitivityX * 0.01f * _mouseXInvertedValue;
        _cameraTargetXRotation -= mouseMovement.y * _mouseSensitivityY * 0.01f * _mouseYInvertedValue;

        _cameraTargetXRotation = Mathf.Clamp(_cameraTargetXRotation, -85, 85);

        var targetRotation = Quaternion.Euler(Vector3.up * _cameraTargetYRotation) * Quaternion.Euler(Vector3.right * _cameraTargetXRotation);

        _head.rotation = Quaternion.Lerp(_head.rotation, targetRotation, _cameraSmoothness * Time.deltaTime);
    }

    private void MoveCameraWithRightStick()
    {
        float RStickMovementX = _inputs.FirstPersonCamera.RotateX.ReadValue<float>() * Time.deltaTime;
        float RStickMovementY = _inputs.FirstPersonCamera.RotateY.ReadValue<float>() * Time.deltaTime;
        _cameraTargetYRotation = Mathf.Repeat(_cameraTargetYRotation, 360);
        _cameraTargetYRotation += RStickMovementX * _stickSensitivityX * 10 * _controllerCameraXInvertedValue;
        _cameraTargetXRotation -= RStickMovementY * _stickSensitivityY * 10 * _controllerCameraYInvertedValue;

        _cameraTargetXRotation = Mathf.Clamp(_cameraTargetXRotation, -85, 85f);

        var targetRotation = Quaternion.Euler(Vector3.up * _cameraTargetYRotation) * Quaternion.Euler(Vector3.right * _cameraTargetXRotation);

        _head.rotation = Quaternion.Lerp(_head.rotation, targetRotation, _cameraSmoothness * Time.deltaTime);
    }
    #endregion

    #region Ground Detection Functions

    public void CheckForGround()
    {
        if (_groundHit.transform != null)
        {
            if (Vector3.Angle(transform.up, _groundHit.normal) < _charaCon.slopeLimit && !_justJumped)
                _fsm.ChangeState(PlayerStatesList.GROUNDED);
        }
    }

    public void CheckForSteepSlope()
    {
        if (_groundHit.transform != null)
        {
            if (Vector3.Angle(transform.up, _groundHit.normal) > _charaCon.slopeLimit)
                _fsm.ChangeState(PlayerStatesList.FALLINGDOWNSLOPE);
        }
    }

    public void CheckForNoGround()
    {
        if (_groundHit.transform == null)
            _fsm.ChangeState(PlayerStatesList.AIRBORNE);
    }

    public void Land()
    {
        //Reset many values when landing
        _currentFriction = _groundedFriction;
        _currentlyAppliedGravity = 0;
        _globalMomentum.y = 0;
        _coyoteTime = _coyoteMaxTime;

        //Jump immediately if player is pressing jump
        if (_inputs.FirstPersonCamera.Jump.IsPressed()) PressJump();
    }

    public void StartFalling()
    {
        _currentFriction = _airborneFriction;
        if (_fsm.previousState.Name != PlayerStatesList.FALLINGDOWNSLOPE)
            _currentlyAppliedGravity *= 0.8f;
    }

    #endregion

    #region Jumping and Falling Functions
    private void PressJump()
    {
        bool jumpCondition;
        jumpCondition = _fsm.currentState.Name == PlayerStatesList.GROUNDED;
        jumpCondition = jumpCondition || (_fsm.currentState.Name == PlayerStatesList.AIRBORNE && _coyoteTime > 0f);
        jumpCondition = jumpCondition || (_fsm.currentState.Name == PlayerStatesList.FALLINGDOWNSLOPE && _coyoteTime > 0f);
        if (jumpCondition)
            _fsm.ChangeState(PlayerStatesList.JUMPING);
    }

    public void StartJumping()
    {
        _currentlyAppliedGravity = _jumpStrength;
        _justJumped = true;
        _coyoteTime = -1f;
        _currentFriction = _airborneFriction;
        _jumpCooldown = 0.1f; //min time allowed between two jumps, to avoid mashing jump up slopes and so we
                              //dont check for a ground before the character actually jumps.
    }

    public void CoyoteTimeCooldown()
    {
        if (_coyoteTime > 0f)
            _coyoteTime -= Time.deltaTime;
    }

    public void ResetSlopeMovement()
    {
        _steepSlopesMovement = Vector3.zero;
    }

    public void CheckForCeiling()
    {
        if (_ceilingHit.transform != null)
        {
            //ceiling is pretty horizontal -> bonk
            if (Vector3.Angle(Vector3.down, _ceilingHit.normal) < _maxCeilingAngle)
                _currentlyAppliedGravity = 0f;

            // ceiling is slopey -> slide along
            else
                _fsm.ChangeState(PlayerStatesList.JUMPINGUPSLOPE);
        }
    }

    public void CheckForNoCeiling()
    {
        if (_ceilingHit.transform == null)
            _fsm.ChangeState(PlayerStatesList.AIRBORNE);
    }

    public void JumpSlideUpSlope()
    {
        // Create slope variables
        Vector3 temp = Vector3.Cross(_ceilingHit.normal, Vector3.up);
        Vector3 slopeDir = Vector3.Cross(temp, _ceilingHit.normal);

        _steepSlopesMovement = (slopeDir + Vector3.up) * _currentlyAppliedGravity * -(Vector3.Dot(_movementInputs.normalized, slopeDir) - 1);

        // Nullify movement input towards wall
        Vector3 ceilingHorizontalDir = new Vector3(slopeDir.x, 0, slopeDir.z).normalized;
        float movementDot = Vector3.Dot(ceilingHorizontalDir, _movementInputs.normalized);
        if (movementDot < 0)
            _movementInputs += (ceilingHorizontalDir * _currentSpeed * -movementDot * Time.deltaTime);
    }

    public void ApplyJumpingGravity()
    {
        _currentlyAppliedGravity -= _gravity * _jumpingDrag * Time.deltaTime;
        if (_currentlyAppliedGravity <= 0)
            _fsm.ChangeState(PlayerStatesList.AIRBORNE);
    }

    public void ApplyAirborneGravity()
    {
        _currentlyAppliedGravity -= _gravity * _airborneDrag * Time.deltaTime;
    }

    public void FallDownSlope()
    {
        // Create slope variables
        Vector3 temp = Vector3.Cross(_groundHit.normal, Vector3.down);
        Vector3 slopeDir = Vector3.Cross(temp, _groundHit.normal);

        //Add gravity in the right direction
        _currentlyAppliedGravity -= _gravity * _airborneDrag * Time.deltaTime;
        _steepSlopesMovement = (slopeDir + Vector3.down) * -_currentlyAppliedGravity * -(Vector3.Dot(_movementInputs.normalized, slopeDir) - 1);

        // Nullify movement input towards wall
        Vector3 slopeHorizontalDir = new Vector3(slopeDir.x, 0, slopeDir.z).normalized;
        float movementDot = Vector3.Dot(slopeHorizontalDir, _movementInputs.normalized);
        if (movementDot < 0)
            _movementInputs += (slopeHorizontalDir * _currentSpeed * -movementDot * Time.deltaTime);
    }

    #endregion

    #region Shmovement Functions

    private Vector3 MakeDirectionCameraRelative(Vector2 inputDirection)
    {
        Vector3 forward = _head.forward;
        Vector3 right = _head.right;
        forward.y = 0;
        right.y = 0;
        forward = forward.normalized;
        right = right.normalized;

        Vector3 rightRelative = inputDirection.x * right;
        Vector3 forwardRelative = inputDirection.y * forward;

        return (forwardRelative + rightRelative);
    }

    private void UpdateMovement()
    {
        //Register new movement input
        Vector3 _newMovementInputs = MakeDirectionCameraRelative(_inputs.FirstPersonCamera.Move.ReadValue<Vector2>());

        //only use new movement input if it is not null
        _isPressingADirection = _newMovementInputs.x != 0 || _newMovementInputs.z != 0;
        if (_isPressingADirection)
            _movementInputs = _newMovementInputs;
        //else, we use last frame's movement input again
        else
            _movementInputs = _lastFrameMovementInputs;

        //Store this frame's input
        _lastFrameMovementInputs = _movementInputs;

        _movementInputs *= Time.deltaTime * _currentSpeed;
    }

    private void HandleMovementAcceleration()
    {
        if (!_isPressingADirection)
            _movementAcceleration = Mathf.Clamp01(_movementAcceleration -= Time.deltaTime / _movementDecelerationSpeed);

        else
            _movementAcceleration = Mathf.Clamp01(_movementAcceleration += Time.deltaTime / _movementAccelerationSpeed);

        _movementInputs *= _movementAcceleration;
    }

    private void UpdateGlobalMomentum()
    {
        //Add Input Vector to Momentum
        _globalMomentum += _movementInputs;

        //Store last frame's direction inside a variable
        var lastFrameXVelocitySign = Mathf.Sign(_globalMomentum.x);
        var lastFrameZVelocitySign = Mathf.Sign(_globalMomentum.z);

        _globalMomentum = _globalMomentum.normalized * (_globalMomentum.magnitude - _currentFriction * Time.deltaTime);

        //If last frame's direction was opposite, snap to 0
        if (Mathf.Sign(_globalMomentum.x) != lastFrameXVelocitySign) _globalMomentum.x = 0;
        if (Mathf.Sign(_globalMomentum.z) != lastFrameZVelocitySign) _globalMomentum.z = 0;
    }

    private void ApplyMovementsToCharacter(Vector3 direction)
    {
        //Move along slopes
        if (_fsm.currentState.Name == PlayerStatesList.GROUNDED || _fsm.currentState.Name == PlayerStatesList.SLIDING)
        {
            if (Vector3.Angle(transform.up, _groundHit.normal) < _charaCon.slopeLimit)
                direction = (direction - (Vector3.Dot(direction, _groundHit.normal)) * _groundHit.normal);
        }

        //Actually move
        _charaCon.Move(direction);
    }

    #endregion

    #region Enable Disable Inputs
    void OnEnable()
    {
        _inputs.Enable();
    }

    void OnDisable()
    {
        _inputs.Disable();
    }
    #endregion
}