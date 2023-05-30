using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NaughtyAttributes;
using CameraShake;
using UnityEngine.VFX;

public class Arm : MonoBehaviour
{
    #region References Decleration
    [Foldout("References")]
    [SerializeField] TextMeshProUGUI _debugStateText;
    [Foldout("References")]
    [SerializeField] protected Image[] _crossHairs;
    [Foldout("References")]
    [SerializeField] protected GameObject _crossHairFull;
    [Foldout("References")]
    [SerializeField] protected Image[] _glowingCrossHairs;
    [Foldout("References")]
    [SerializeField] private AnimationCurve _glowCurve;
    [Foldout("References")]
    [SerializeField] private float _glowSpeed = 8.0f;
    [Foldout("References")]
    [SerializeField] protected GameObject _ui;
    [Foldout("References")]
    [SerializeField] protected Transform _cameraTransform;
    [Foldout("References")]
    public Animator Animator;
    [Foldout("References")]
    [SerializeField] protected VisualEffect[] _castFx;
    [Foldout("References")]
    [SerializeField] protected VisualEffect[] _precastFx;

    [Foldout("Other")]
    [SerializeField] private float _smoothness = 10;
    [Foldout("Other")]
    [SerializeField] private float _mouseSwayAmountX = -.2f;
    [Foldout("Other")]
    [SerializeField] private float _mouseSwayAmountY = -.1f;
    [Foldout("Other")]
    [SerializeField] private float _controllerSwayAmountX = -5f;
    [Foldout("Other")]
    [SerializeField] private float _controllerSwayAmountY = -3f;


    PlayerInputMap _inputs;
    protected ArmFSM _fsm;
    private bool _isGlowing = false;
    private float _glowT = 0.0f;
    #endregion

    #region Stats Decleration
    [SerializeField] protected KickShake.Params _castShake;
    [Foldout("Stats")]
    [SerializeField] protected float _cooldown;
    protected float _cooldownT;
    #endregion 

    protected virtual void Awake()
    {
        _fsm = GetComponent<ArmFSM>();
        _inputs = new PlayerInputMap();
        _inputs.Actions.Skill.started += _ => PressSong();
        _inputs.Actions.Interact.started += _ => PressCancelSong();
        _inputs.Actions.Skill.canceled += _ => ReleaseSong();
    }

    void Start()
    {

    }

    void Update()
    {
        //State update
        if (_fsm.CurrentState != null)
        {
            _fsm.CurrentState.StateUpdate();
        }

        if (_isGlowing)
            Glow();

        Debugging();
    }

    #region Input presses
    protected virtual void PressCancelSong()
    {
        bool canCancel = _fsm.CurrentState.Name == ArmStateList.PREVIS;
        if (canCancel)
            CancelSong();
    }

    protected virtual void PressSong()
    {
        bool canSong = _fsm.CurrentState.Name == ArmStateList.IDLE;
        if (canSong)
        {
            _fsm.ChangeState(ArmStateList.PREVIS);
        }
    }

    protected virtual void ReleaseSong()
    {
        bool canRelease = _fsm.CurrentState.Name == ArmStateList.PREVIS;
        if (canRelease)
        {
            _fsm.ChangeState(ArmStateList.ACTIVE);
        }
    }
    #endregion

    protected virtual void CancelSong()
    {
        _fsm.ChangeState(ArmStateList.IDLE);
    }

    public virtual void StartPrevis()
    {
        this.Animator.CrossFade("preview", 0.1f, 0);
    }

    public virtual void UpdatePrevis()
    {

    }

    public virtual void StopPrevis()
    {

    }

    public virtual void StartActive()
    {
        if (_crossHairFull) _crossHairFull.SetActive(false);
        StopGlowing();
    }

    public virtual void UpdateActive()
    {

    }

    public virtual void StopActive()
    {

    }

    public virtual void StartRecovery()
    {
        _cooldownT = _cooldown;
    }

    public void UpdateCooldown()
    {
        _cooldownT -= Time.deltaTime;
        if (_crossHairs.Length > 0)
            foreach (Image crosshair in _crossHairs)
                crosshair.fillAmount = Mathf.Lerp(0, 1, Mathf.InverseLerp(_cooldown, 0, _cooldownT));

        if (_cooldownT <= 0f)
        {
            _fsm.ChangeState(ArmStateList.IDLE);
        }
    }

    public virtual void StartIdle()
    {
        Animator.CrossFade("idle", 0, 0);
        Refilled();
    }

    public virtual void CheckLookedAt()
    {

    }

    protected void Refilled()
    {
        if (_crossHairFull)
            _crossHairFull.SetActive(true);
        StartGlowingBriefly();
        if (_inputs.Actions.Skill.IsPressed() && !_inputs.Actions.Interact.IsPressed())
            PressSong();
        SoundManager.Instance.PlaySound("event:/SFX_Controller/Chants/ChantReady", 1f, gameObject);
    }

    public void ForceRefill()
    {
        if (_fsm.CurrentState.Name == ArmStateList.RECOVERY)
            _cooldownT = 0.0f;
    }

    protected void StartGlowingBriefly()
    {
        _isGlowing = true;
        _glowT = 0.0f;
    }

    protected void Glow()
    {
        _glowT += Time.deltaTime * _glowSpeed;
        float alpha = _glowCurve.Evaluate(_glowT);
        foreach (Image image in _glowingCrossHairs)
            image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
    }

    protected void StopGlowing()
    {
        _isGlowing = false;
        if (_glowingCrossHairs.Length > 0)
            foreach (Image image in _glowingCrossHairs)
                image.color = new Color(image.color.r, image.color.g, image.color.b, 0.0f);
    }

    #region Debugging
    void Debugging()
    {
#if UNITY_EDITOR
        DebugDisplayArmState();
#endif
    }

    public void DebugDisplayArmState()
    {
        if (_debugStateText && _fsm.CurrentState != null)
            _debugStateText.text = ("Arm state: " + _fsm.CurrentState.Name);
    }
    #endregion

    #region Swaying
    public void Sway()
    {
        float x = _inputs.Camera.Rotate.ReadValue<Vector2>().x * _mouseSwayAmountX * Time.deltaTime;
        float y = _inputs.Camera.Rotate.ReadValue<Vector2>().y * _mouseSwayAmountY * Time.deltaTime;
        x += _inputs.Camera.RotateX.ReadValue<float>() * _controllerSwayAmountX * Time.deltaTime;
        y += _inputs.Camera.RotateY.ReadValue<float>() * _controllerSwayAmountY * Time.deltaTime;

        Quaternion rotationX = Quaternion.AngleAxis(-y, Vector3.right);
        Quaternion rotationY = Quaternion.AngleAxis(x, Vector3.up);

        Quaternion targetRot = rotationX * rotationY;

        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, _smoothness * Time.deltaTime);
    }

    public void StopSwaying()
    {
        transform.localRotation = Quaternion.identity;
    }
    #endregion

    #region Enable Disable Inputs
    void OnEnable()
    {
        _ui.SetActive(true);
        _inputs.Enable();
    }

    void OnDisable()
    {
        _ui.SetActive(false);
        _inputs.Disable();
    }
    #endregion
}