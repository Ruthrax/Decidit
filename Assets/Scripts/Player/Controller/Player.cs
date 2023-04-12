using System;
using UnityEngine;
using TMPro;
using NaughtyAttributes;
using CameraShake;

public class Player : LocalManager<Player>
{
    #region References
    [Foldout("Debug References")]
    [SerializeField] TextMeshProUGUI _debugInputVelocityText;
    [Foldout("Debug References")]
    [SerializeField] TextMeshProUGUI _debugGlobalVelocityText;
    [Foldout("Debug References")]
    [SerializeField] TextMeshProUGUI _debugGravityText;
    [Foldout("Debug References")]
    [SerializeField] TextMeshProUGUI _debugSlopeText;
    [Foldout("Debug References")]
    [SerializeField] TextMeshProUGUI _debugStateText;
    [Foldout("Debug References")]
    [SerializeField] TextMeshProUGUI _debugSpeedText;
    [Foldout("Debug References")]
    [SerializeField] TextMeshProUGUI _debugCoyoteText;
    [Foldout("Debug References")]
    [SerializeField] TextMeshProUGUI _debugWallText;
    [Foldout("References")]
    public Transform Head;
    [Foldout("References")]
    public CharacterController CharaCon;
    [Foldout("References")]
    [SerializeField] LayerMask _collisionMask;
    [Foldout("References")]
    public PlayerHealth PlayerHealth;
    PlayerInputMap _inputs;
    PlayerFSM _fsm;
    PlayerHealth _playerHealth;
    #endregion

    #region Camera rotation variables
    private float _cameraTargetYRotation;
    private float _cameraTargetXRotation;
    float _shakeT;
    float _shakeInitialT;
    float _shakeIntensity;
    bool _up = true;
    Vector3 _initialHeadPos = new Vector3(0, 0.4f, 0);

    //Mouse
    [Foldout("Camera Mouse Settings")]
    [Range(0.1f, 100)][SerializeField] private float _mouseSensitivityX = 10f;
    [Foldout("Camera Mouse Settings")]
    [Range(0.1f, 100)][SerializeField] private float _mouseSensitivityY = 10f;
    [Foldout("Camera Mouse Settings")]
    [SerializeField] private bool _mouseXInverted = false;
    [Foldout("Camera Mouse Settings")]
    [SerializeField] private bool _mouseYInverted = false;

    //Joysticks
    [Foldout("Camera Stick Settings")]
    [Range(1f, 100)][SerializeField] private float _stickSensitivityX = 15f;
    [Foldout("Camera Stick Settings")]
    [Range(1f, 100)][SerializeField] private float _stickSensitivityY = 15f;
    [Foldout("Camera Stick Settings")]
    [SerializeField] private bool _controllerCameraXInverted = false;
    [Foldout("Camera Stick Settings")]
    [SerializeField] private bool _controllerCameraYInverted = false;

    private bool _isRightSticking;
    private float _rStickAcceleration;
    [Foldout("Camera Stick Settings")]
    [SerializeField] private float _rStickDecelerationSpeed = 0.04f;
    [Foldout("Camera Stick Settings")]
    [SerializeField] private float _rStickAccelerationSpeed = 0.3f;

    int _mouseXInvertedValue;
    int _mouseYInvertedValue;
    int _controllerCameraXInvertedValue;
    int _controllerCameraYInvertedValue;

    //General
    [SerializeField] float _cameraSmoothness = 100000;
    #endregion

    #region Jumping, Falling and Ground Detection variables
    [Foldout("Jumping Settings")]
    [Range(0, 100)][SerializeField] private float _baseJumpStrength = 14f;
    private float _currentJumpStrength;
    [Foldout("Jumping Settings")]
    [Range(0, 50)][SerializeField] private float _airborneDrag = 3f;
    [Foldout("Jumping Settings")]
    [Range(0, 50)][SerializeField] private float _jumpingDrag = 3f;
    [Foldout("Jumping Settings")]
    [Range(0, 50)][SerializeField] private float _wallrideDrag = 3f;
    [Foldout("Jumping Settings")]
    [Range(0, 90)][SerializeField] private float _maxCeilingAngle = 30f;
    [Foldout("Jumping Settings")]
    [SerializeField] private bool _canCancelJump;

    private const float _gravity = 9.81f;
    public float CurrentlyAppliedGravity;
    private Vector3 _steepSlopesMovement;

    private RaycastHit _groundHit;
    private RaycastHit _ceilingHit;
    private const float _groundSpherecastLength = .75f; // _charaCon.height/2 - _charaCon.radius
    private const float _ceilingRaycastLength = 1f; // _charaCon.height/2 + 0.1f margin to mitigate skin width
    private const float _groundSpherecastRadius = .25f; // _charaCon.radius + 0.1f margin to mitigate skin width
    private const float _ceilingSpherecastRadius = .25f; // _charaCon.radius
    private bool _justJumped;
    private float _jumpCooldown;
    private float _coyoteTime;
    private const float _coyoteMaxTime = 0.3f;
    [Foldout("Eylau Settings")]
    private float _eylauBuffFactor = 1.5f;

    #endregion

    #region Movement Variables
    [Foldout("Movement Settings")]
    [Range(0, 20)]
    [SerializeField]
    private float _baseSpeed = 9f;
    [Foldout("Movement Settings")]
    [Range(0, 20)]
    [SerializeField]
    private float _movementAccelerationSpeed;
    [Foldout("Movement Settings")]
    [Range(0, 20)]
    [SerializeField]
    private float _movementDecelerationSpeed;
    [Foldout("Movement Settings")]
    [Range(0, 20)]
    [SerializeField]
    private float _airMovementAccelerationSpeed;
    [Foldout("Movement Settings")]
    [Range(0, 20)]
    [SerializeField]
    private float _airMovementDecelerationSpeed;
    [Foldout("Movement Settings")]
    [SerializeField]
    private AnimationCurve _accelerationSpeed;
    [Foldout("Movement Settings")]
    [Range(0, 100)]
    [Tooltip("if lower than movement speed, you will accelerate when airborne")]
    [SerializeField]
    private float _airborneFriction = 9f;
    [Foldout("Movement Settings")]
    [Range(0, 100)]
    [SerializeField]
    private float _groundedFriction = 50f;
    [Foldout("Movement Settings")]
    [Range(0, 1)]
    [Tooltip("how much you can influence momentum with inputs")]
    [SerializeField]
    private float _directionnalInfluence = 0.4f;
    [Foldout("Movement Settings")]
    [Range(1, 80)]
    [Tooltip("Maximum Momentum: You can't DI after that threshold")]
    [SerializeField] private int _maximumDiMomentum = 50;

    // ADD LATER [Range(0, 100)][SerializeField] private float _slidingFriction = 5f;
    private float _currentFriction;

    private float _currentSpeed;
    private bool _canMove;
    Vector3 _rawInputs;
    private Vector3 _movementInputs; // X is Left-Right and Z is Backward-Forward
    private Vector3 _lastFrameMovementInputs;
    private Vector3 _globalMomentum;
    private float _movementAcceleration;
    private bool _isPressingADirection;
    private float _movementAccelerationT;
    #endregion

    #region Wallriding and walljumping
    [Foldout("Walling")]
    [Range(0.0f, -30.0f)]
    [SerializeField] public float WallRidingGravity;
    [Foldout("Walling")]
    [Range(0.0f, 50.0f)]
    [SerializeField] private float _wallJumpForce;
    [Foldout("Walling")]
    [Range(0, 3)]
    [SerializeField] private int _maxNumberOfWalljumps;
    private int _currentNumberOfWalljumps;
    private RaycastHit _currentWall;
    private RaycastHit _previousWall;
    private const float _wallCoyoteMaxTime = 0.2f;
    private float _wallCoyoteTime;

    public Vector3 FinalMovement { get; private set; }
    #endregion

    protected override void Awake()
    {
        base.Awake();
        _fsm = GetComponent<PlayerFSM>();
        _playerHealth = GetComponent<PlayerHealth>();
        _inputs = new PlayerInputMap();
        _inputs.Movement.Jump.started += _ => PressJump();
        _inputs.Movement.Jump.canceled += _ => StopJumping();
    }

    private void Start()
    {
        ForceRotation(transform);
        _movementInputs = Vector2.zero;
        _globalMomentum = Vector3.zero;
        _currentSpeed = _baseSpeed;
        _currentJumpStrength = _baseJumpStrength;
        CurrentlyAppliedGravity = 0;
        _movementAcceleration = 0;
        _canMove = true;
        _movementAccelerationT = 0.0f;
        SetCameraInvert();
        //StopShake();
    }

    private void Update()
    {
        //jump cooldown
        if (_justJumped)
        {
            _jumpCooldown -= Time.deltaTime;
            if (_jumpCooldown <= 0) _justJumped = false;
        }

        //Camera
        MoveCameraWithRightStick();
        // HandleRStickAcceleration();
        MoveCameraWithMouse();
        //Shake();

        //Ground Spherecast and Storage of its values
        Physics.SphereCast(transform.position, _groundSpherecastRadius, -transform.up, out _groundHit, _groundSpherecastLength, _collisionMask);
        //Ceiling Spherecast and Storage of its values
        Physics.SphereCast(transform.position, _ceilingSpherecastRadius, transform.up, out _ceilingHit, _ceilingRaycastLength, _collisionMask);

        //Update Movement Vectors
        UpdateMovement();
        HandleMovementAcceleration();
        UpdateGlobalMomentum();

        //State update
        if (_fsm.CurrentState != null)
            _fsm.CurrentState.StateUpdate();

        //Final Movement Formula //I got lost with the deltatime stuff but i swear it works perfectly
        FinalMovement = (_globalMomentum * Time.deltaTime)
        + (_movementInputs * Mathf.InverseLerp(_maximumDiMomentum, 0.0f, _globalMomentum.magnitude))
        + (Vector3.up * CurrentlyAppliedGravity * Time.deltaTime)
        + (_steepSlopesMovement * Time.deltaTime);

        //Debug Values on screen
        UpdateDebugTexts();
    }

    private void LateUpdate()
    {
        //Apply Movement
        ApplyMovementsToCharacter(FinalMovement);
    }

    #region Debugs
    private void UpdateDebugTexts()
    {
#if UNITY_EDITOR
        if (_debugStateText)
            _debugStateText.text = ("Character state: " + _fsm.CurrentState.Name);

        if (_debugInputVelocityText)
            _debugInputVelocityText.text =
            ("Input Velocity:\nx= " + (_movementInputs.x / Time.deltaTime).ToString("F6") +
            "\nz= " + (_movementInputs.z / Time.deltaTime).ToString("F6") +
            "\n\n Acceleration:\n  " + _movementAcceleration.ToString("F1") +
            "\nAccelerationT:\n  " + _movementAccelerationT.ToString("F1"));

        if (_debugGlobalVelocityText)
            _debugGlobalVelocityText.text =
            ("Momentum Velocity: " + _globalMomentum.magnitude + "\nx= " + (_globalMomentum.x).ToString("F1") +
            "\ny= " + (_globalMomentum.y).ToString("F1") +
            "\nz= " + (_globalMomentum.z).ToString("F1"));

        if (_debugGravityText)
            _debugGravityText.text = ("Gravity:\n   " + CurrentlyAppliedGravity.ToString("F1"));

        if (_debugSlopeText)
            _debugSlopeText.text = ("Slope Velocity:\n   " + _steepSlopesMovement.ToString("F1"));

        if (_debugSpeedText)
            _debugSpeedText.text = ("Total Speed:\n   " + (FinalMovement.magnitude / Time.deltaTime).ToString("F3"));

        if (_debugCoyoteText)
            _debugCoyoteText.text = ("Coyote Time: " + _coyoteTime + "\n" + "Wall Coyote Time: " + _wallCoyoteTime);
        if (_debugWallText)
            _debugWallText.text = ("Found a wall to ride: " + (_currentWall.transform != null)
            + "\n" + "Remembers a wall to ride: " + (_previousWall.transform != null));
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
        Debug.DrawLine(transform.position, transform.position + FinalMovement / Time.deltaTime * 0.3f, Color.red, 0f);

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
        Vector2 mouseMovement = _inputs.Camera.Rotate.ReadValue<Vector2>()/* * Time.deltaTime */;
        // _cameraTargetYRotation = Mathf.Repeat(_cameraTargetYRotation, 360);
        _cameraTargetYRotation += mouseMovement.x * _mouseSensitivityX * 0.01f * _mouseXInvertedValue * Time.timeScale;
        _cameraTargetXRotation -= mouseMovement.y * _mouseSensitivityY * 0.01f * _mouseYInvertedValue * Time.timeScale;

        _cameraTargetXRotation = Mathf.Clamp(_cameraTargetXRotation, -89.5f, 89.5f);

        var targetRotation = Quaternion.Euler(Vector3.up * _cameraTargetYRotation) * Quaternion.Euler(Vector3.right * _cameraTargetXRotation);

        Head.rotation = Quaternion.Slerp(Head.rotation, targetRotation, Time.timeScale);
    }

    private void MoveCameraWithRightStick()
    {
        float RStickMovementX = _inputs.Camera.RotateX.ReadValue<float>() * Time.deltaTime;
        float RStickMovementY = _inputs.Camera.RotateY.ReadValue<float>() * Time.deltaTime;
        _isRightSticking = (RStickMovementX != 0.0f || RStickMovementY != 0.0f);

        HandleRStickAcceleration();

        _cameraTargetYRotation = Mathf.Repeat(_cameraTargetYRotation, 360);
        _cameraTargetYRotation += RStickMovementX * _stickSensitivityX * 10 * _controllerCameraXInvertedValue * _rStickAcceleration;
        _cameraTargetXRotation -= RStickMovementY * _stickSensitivityY * 10 * _controllerCameraYInvertedValue * _rStickAcceleration;

        _cameraTargetXRotation = Mathf.Clamp(_cameraTargetXRotation, -89.5f, 89.5f);


        var targetRotation = Quaternion.Euler(Vector3.up * _cameraTargetYRotation) * Quaternion.Euler(Vector3.right * _cameraTargetXRotation);

        Head.rotation = Quaternion.Slerp(Head.rotation, targetRotation, _cameraSmoothness * Time.deltaTime);
    }

    private void HandleRStickAcceleration()
    {
        if (!_isRightSticking)
            _rStickAcceleration = Mathf.Clamp01(_rStickAcceleration -= Time.deltaTime / _rStickDecelerationSpeed);

        else
            _rStickAcceleration = Mathf.Clamp01(_rStickAcceleration += Time.deltaTime / _rStickAccelerationSpeed);
    }

    public void ForceRotation(Transform obj)
    {
        _cameraTargetXRotation = obj.eulerAngles.x;
        _cameraTargetYRotation = obj.eulerAngles.y;
    }

    public void StartKickShake(KickShake.Params shParams, Vector3 pos)
    {
        PlayerManager.Instance.StartRumbling
        (
            shParams.strength.eulerAngles.magnitude + shParams.strength.position.magnitude,
            shParams.strength.eulerAngles.magnitude + shParams.strength.position.magnitude,
            shParams.releaseTime
        );

        CameraShaker.Shake(new KickShake(shParams, pos, false));
    }

    public void StartPerlinShake(PerlinShake.Params shParams, Vector3 pos)
    {
        PlayerManager.Instance.StartRumbling
        (
            shParams.strength.eulerAngles.magnitude + shParams.strength.position.magnitude,
            shParams.strength.eulerAngles.magnitude + shParams.strength.position.magnitude,
            shParams.envelope.decay
        );

        CameraShaker.Shake(new PerlinShake(shParams, 1, pos, false));
    }

    public void StartBounceShake(BounceShake.Params shParams, Vector3 pos)
    {
        PlayerManager.Instance.StartRumbling
        (
            shParams.positionStrength + shParams.rotationStrength,
            shParams.positionStrength + shParams.rotationStrength,
            shParams.numBounces * 0.01f
        );

        CameraShaker.Shake(new BounceShake(shParams, pos));
    }

    //Deprecated
    // private void Shake()
    // {
    //     if (_shakeT > 0)
    //     {
    //         float shakeIntensity = _shakeIntensity * Mathf.InverseLerp(0, _shakeInitialT, _shakeT);

    //         //old shake, in all directions
    //         //Head.localPosition = _initialHeadPos + new Vector3(UnityEngine.Random.Range(-1, 1), UnityEngine.Random.Range(-1, 1), 0).normalized * shakeIntensity;

    //         //new shake, only vertical
    //         if (_up)
    //         {
    //             Head.localPosition = _initialHeadPos + Vector3.up * shakeIntensity;
    //             _up = false;
    //         }
    //         else
    //         {
    //             Head.localPosition = _initialHeadPos + Vector3.down * shakeIntensity;
    //             _up = true;
    //         }
    //         _shakeT -= Time.deltaTime;
    //         if (_shakeT < 0)
    //             StopShake();
    //     }
    // }

    // public void StopShake()
    // {
    //     _shakeIntensity = 0;
    //     _shakeT = 0;
    //     Head.localPosition = _initialHeadPos;
    // }
    #endregion

    #region Ground Detection Functions

    public void CheckForGround()
    {
        if (_groundHit.transform != null)
        {
            if (Vector3.Angle(transform.up, _groundHit.normal) < CharaCon.slopeLimit && !_justJumped)
                _fsm.ChangeState(PlayerStatesList.GROUNDED);
        }
    }

    public void CheckForSteepSlope()
    {
        if (_groundHit.transform != null)
        {
            if (Vector3.Angle(transform.up, _groundHit.normal) > CharaCon.slopeLimit)
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
        SoundManager.Instance.PlaySound("event:/SFX_Controller/CharactersNoises/Landing", 1f, gameObject);
        //Reset many values when landing
        _currentFriction = _groundedFriction;
        CurrentlyAppliedGravity = 0;
        _globalMomentum.y = 0;
        _coyoteTime = _coyoteMaxTime;
        _currentNumberOfWalljumps = _maxNumberOfWalljumps;
        ResetWalls();

        //Jump immediately if player is pressing jump
        if (_inputs.Movement.Jump.IsPressed()) PressJump();
    }

    public void StartFalling()
    {
        _currentFriction = _airborneFriction;
        if (_fsm.PreviousState.Name != PlayerStatesList.FALLINGDOWNSLOPE)
            CurrentlyAppliedGravity *= 0.8f;
    }

    #endregion

    #region Jumping and Falling Functions
    private void PressJump()
    {
        bool isWallriding = _fsm.CurrentState.Name == PlayerStatesList.WALLRIDING;

        bool _isJumpingNextToWall = _fsm.CurrentState.Name == PlayerStatesList.JUMPING;
        _isJumpingNextToWall = _isJumpingNextToWall || _fsm.CurrentState.Name == PlayerStatesList.JUMPINGUPSLOPE;
        _isJumpingNextToWall = _isJumpingNextToWall && _currentWall.transform != null;
        if (_isJumpingNextToWall)
            _isJumpingNextToWall = _isJumpingNextToWall && Vector3.Dot(_currentWall.normal, _movementInputs) < 0.5f;

        if ((isWallriding || _isJumpingNextToWall || _wallCoyoteTime > 0.0f) && _currentNumberOfWalljumps > 0)
            _fsm.ChangeState(PlayerStatesList.WALLJUMPING);


        bool jumpCondition = _fsm.CurrentState.Name == PlayerStatesList.GROUNDED;
        jumpCondition = jumpCondition || (_fsm.CurrentState.Name == PlayerStatesList.AIRBORNE && _coyoteTime > 0f);
        jumpCondition = jumpCondition || (_fsm.CurrentState.Name == PlayerStatesList.FALLINGDOWNSLOPE && _coyoteTime > 0f);

        if (jumpCondition)
            _fsm.ChangeState(PlayerStatesList.JUMPING);
    }

    public void StartJumping()
    {
        SoundManager.Instance.PlaySound("event:/SFX_Controller/CharactersNoises/Jump", 1f, gameObject);
        CurrentlyAppliedGravity = _currentJumpStrength;
        _justJumped = true;
        _coyoteTime = -1f;
        _currentFriction = _airborneFriction;
        _jumpCooldown = 0.1f; //min time allowed between two jumps, to avoid mashing jump up slopes and so we
                              //dont check for a ground before the character actually jumps.
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
                CurrentlyAppliedGravity = 0f;

            // ceiling is slopey -> slide along
            else if (_fsm.CurrentState.Name != PlayerStatesList.JUMPINGUPSLOPE)
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

        _steepSlopesMovement = (slopeDir + Vector3.up) * CurrentlyAppliedGravity;

        // Nullify movement input towards wall
        Vector3 ceilingHorizontalDir = new Vector3(slopeDir.x, 0, slopeDir.z).normalized;
        float movementDot = Vector3.Dot(ceilingHorizontalDir, _movementInputs.normalized);
        if (movementDot < 0)
            _movementInputs += (ceilingHorizontalDir * _currentSpeed * -movementDot * Time.deltaTime);
    }

    public void ApplyJumpingGravity()
    {
        CurrentlyAppliedGravity -= _gravity * _jumpingDrag * Time.deltaTime;
        if (CurrentlyAppliedGravity <= 0)
            _fsm.ChangeState(PlayerStatesList.AIRBORNE);
    }

    private void StopJumping()
    {
        if (_canCancelJump && (_fsm.CurrentState.Name == PlayerStatesList.JUMPING || _fsm.CurrentState.Name == PlayerStatesList.JUMPINGUPSLOPE))
        {
            if (CurrentlyAppliedGravity > 4.0f)
                CurrentlyAppliedGravity = 4.0f;
        }
    }

    public void ApplyAirborneGravity()
    {
        CurrentlyAppliedGravity -= _gravity * _airborneDrag * Time.deltaTime;
    }

    public void ApplyWallridingGravity()
    {
        CurrentlyAppliedGravity -= _gravity * _wallrideDrag * Time.deltaTime;
    }

    public void FallDownSlope()
    {
        // Create slope variables
        Vector3 temp = Vector3.Cross(_groundHit.normal, Vector3.down);
        Vector3 slopeDir = Vector3.Cross(temp, _groundHit.normal);

        //Add gravity in the right direction
        CurrentlyAppliedGravity -= _gravity * _airborneDrag * Time.deltaTime;
        _steepSlopesMovement = (slopeDir + Vector3.down) * -CurrentlyAppliedGravity;

        // Nullify movement input towards wall
        Vector3 slopeHorizontalDir = new Vector3(slopeDir.x, 0, slopeDir.z).normalized;
        float movementDot = Vector3.Dot(slopeHorizontalDir, _movementInputs.normalized);
        if (movementDot < 0)
            _movementInputs += (slopeHorizontalDir * _currentSpeed * -movementDot * Time.deltaTime);
    }

    #endregion

    #region Movement Functions

    public void EylauMovementBuff()
    {
        SoundManager.Instance.PlaySound("event:/SFX_Controller/Chants/CimetièreEyleau/BuffStart", 1f, gameObject);
        _currentSpeed = _baseSpeed * _eylauBuffFactor;
        _currentJumpStrength = _baseJumpStrength * _eylauBuffFactor;
    }

    public void ResetEylauMovementBuff()
    {
        //TODO ART le son se produit quand on lance le sort
        SoundManager.Instance.PlaySound("event:/SFX_Controller/Chants/CimetièreEyleau/BuffEnd", 1f, gameObject);
        _currentSpeed = _baseSpeed;
        _currentJumpStrength = _baseJumpStrength;
    }

    private Vector3 MakeDirectionCameraRelative(Vector2 inputDirection)
    {
        Vector3 forward = Head.forward;
        Vector3 right = Head.right;
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
        _rawInputs = MakeDirectionCameraRelative(_inputs.Movement.Move.ReadValue<Vector2>());

        //only use new movement input if it is not null
        _isPressingADirection = _rawInputs.x != 0 || _rawInputs.z != 0;
        if (_isPressingADirection)
            _movementInputs = _rawInputs;
        //else, we use last frame's movement input again
        else
            _movementInputs = _lastFrameMovementInputs;

        if (_fsm.CurrentState.Name != PlayerStatesList.GROUNDED)
        {
            float dot = Vector3.Dot(_lastFrameMovementInputs, _movementInputs);
            _movementAccelerationT -= (1 - Mathf.InverseLerp(-1, 1, (dot)));
            //     _movementInputs = Vector3.Lerp(_lastFrameMovementInputs, _movementInputs, (1 - Mathf.InverseLerp(-1, 1, (dot)) * Time.deltaTime));
        }

        //Store this frame's input
        _lastFrameMovementInputs = _movementInputs;

        _movementInputs *= Time.deltaTime * _currentSpeed;
    }

    private void HandleMovementAcceleration()
    {
        if (_fsm.CurrentState.Name == PlayerStatesList.GROUNDED)
        {
            if (!_isPressingADirection)
                _movementAcceleration = _accelerationSpeed.Evaluate(_movementAccelerationT -= (Time.deltaTime * _movementDecelerationSpeed));
            else
                _movementAcceleration = _accelerationSpeed.Evaluate(_movementAccelerationT += (Time.deltaTime * _movementAccelerationSpeed));
        }
        else
        {
            if (!_isPressingADirection)
                _movementAcceleration = _accelerationSpeed.Evaluate(_movementAccelerationT -= (Time.deltaTime * _airMovementDecelerationSpeed));
            else
                _movementAcceleration = _accelerationSpeed.Evaluate(_movementAccelerationT += (Time.deltaTime * _airMovementAccelerationSpeed));
        }

        _movementAccelerationT = Mathf.Clamp01(_movementAccelerationT);

        _movementInputs *= _movementAcceleration;
    }

    public void AddMomentum(Vector3 blow)
    {
        if (_fsm.CurrentState.Name == PlayerStatesList.JUMPING || _fsm.CurrentState.Name == PlayerStatesList.JUMPINGUPSLOPE)
        {
            _fsm.ChangeState(PlayerStatesList.JUMPING);
        }
        else
        {
            KillMomentum();
            _fsm.ChangeState(PlayerStatesList.AIRBORNE);
        }
        FinalMovement += blow.normalized;
        _globalMomentum += blow;
    }

    private void UpdateGlobalMomentum()
    {
        //Add Input Vector to Momentum
        _globalMomentum += _movementInputs * _directionnalInfluence;

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
        if (_fsm.CurrentState.Name == PlayerStatesList.GROUNDED || _fsm.CurrentState.Name == PlayerStatesList.SLIDING)
        {
            if (Vector3.Angle(transform.up, _groundHit.normal) < CharaCon.slopeLimit)
                direction = (direction - (Vector3.Dot(direction, _groundHit.normal)) * _groundHit.normal);
        }

        //Actually move
        if (CharaCon.enabled)
            CharaCon.Move(direction);
    }

    public void KillMomentum()
    {
        _globalMomentum = Vector3.zero;
        CurrentlyAppliedGravity = 0f;
        _steepSlopesMovement = Vector3.zero;
        _movementAcceleration = 0.0f;
    }

    public void AllowMovement(bool boolean)
    {
        _canMove = boolean;
    }
    #endregion

    #region Wallriding and Walljumping

    public void CheckWall()
    {
        //Wallride raycast and Storage of its values
        Physics.Raycast(transform.position, _rawInputs, out RaycastHit hit, .4f, _collisionMask);

        if (hit.transform == null)
        {
            if (_wallCoyoteTime > 0.0f && _previousWall.transform == null)
            {
                _previousWall = _currentWall;
                _currentWall = hit;
            }
        }
        else
            _currentWall = hit;
    }

    public void ResetWalls()
    {
        _currentWall = new RaycastHit();
        _previousWall = new RaycastHit();
        _wallCoyoteTime = -1.0f;
    }

    public void CheckForWallride()
    {
        if (_currentWall.transform != null && Vector3.Dot(_currentWall.normal, _movementInputs.normalized) < -0.5f)
            _fsm.ChangeState(PlayerStatesList.WALLRIDING);
    }

    public void CheckForJumpingWallride()
    {
        if (_currentWall.transform != null && Vector3.Dot(_currentWall.normal, _movementInputs.normalized) < -0.5f)
            ResetWallCoyoteTime();
    }

    public void CheckForNoWallRide()
    {
        if (_currentWall.transform != null)
        {
            if (Vector3.Dot(_currentWall.normal, _movementInputs.normalized) > -0.5f)
                _fsm.ChangeState(PlayerStatesList.AIRBORNE);
        }
        else
            _fsm.ChangeState(PlayerStatesList.AIRBORNE);
    }

    public void StartWalljumping()
    {
        if (_currentWall.transform != null)
            AddMomentum(_currentWall.normal * _wallJumpForce);
        else if (_previousWall.transform != null)
            AddMomentum(_previousWall.normal * _wallJumpForce);
        else
            Debug.LogWarning("ERROR: Walljumped without a wall???");

        _currentNumberOfWalljumps--;
        ResetWalls();
        _fsm.ChangeState(PlayerStatesList.JUMPING);
    }

    public void ResetWallCoyoteTime()
    {
        _wallCoyoteTime = _wallCoyoteMaxTime;
    }

    public void WallCoyoteTimeCooldown()
    {
        if (_wallCoyoteTime > 0f)
            _wallCoyoteTime -= Time.deltaTime;
        else if (_wallCoyoteTime > -1.0f)
            ResetWalls();
    }
    #endregion

    #region Enable Disable
    void OnEnable()
    {
        _inputs.Enable();

        //Buffer jump is jump was pressed when enabled
        if (_inputs.Movement.Jump.IsPressed()) PressJump();
    }

    void OnDisable()
    {
        _inputs.Disable();
        _fsm.ChangeState(PlayerStatesList.AIRBORNE);
    }
    #endregion
}