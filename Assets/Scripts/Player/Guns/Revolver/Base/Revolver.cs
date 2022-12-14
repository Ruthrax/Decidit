using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NaughtyAttributes;
using UnityEngine.VFX;

public class Revolver : MonoBehaviour
{
    #region References Decleration
    [Header("References")]
    [SerializeField] private TextMeshProUGUI _ammoCountText;
    [SerializeField] protected Transform _canonPosition;
    [SerializeField] protected VisualEffect _muzzleFlash;
    private PlayerInputMap _inputs;
    private RevolverFSM _fsm;
    protected Transform _camera;

    #endregion

    #region Stats Decleration
    [Header("Stats")]
    [SerializeField] protected LayerMask _mask;
    [SerializeField] private float _recoilTime = .3f;
    [SerializeField] private float _reloadMaxTime = 1f;
    [SerializeField] private float _reloadMinTime = 0.9f;
    [SerializeField] private int _maxAmmo = 6;
    [SerializeField] protected float _shootShakeIntensity;
    [SerializeField] protected float _shootShakeDuration;

    protected Vector3 _currentlyAimedAt;
    private float _recoilT;
    private float _reloadT;
    private int _ammo;
    private bool _reloadBuffered;
    #endregion

    void Awake()
    {
        _fsm = GetComponent<RevolverFSM>();
        _camera = Camera.main.transform;
        _inputs = new PlayerInputMap();
        _inputs.Actions.Shoot.started += _ => PressShoot();
        _inputs.Actions.Reload.started += _ => PressReload();
    }

    void Start()
    {
        Reloaded();
    }

    void Update()
    {
        //State update
        if (_fsm.currentState != null)
            _fsm.currentState.StateUpdate();



        //Debugging();
    }

    //     #region Debugging
    //     void Debugging()
    //     {
    // #if UNITY_EDITOR
    //         DebugDisplayGunState();
    // #endif
    //     }

    //     public void DebugDisplayGunState()
    //     {
    //         // if (_debugStateText && _fsm.currentState != null)
    //         //     _debugStateText.text = ("Revolver state: " + _fsm.currentState.Name);
    //     }
    //     #endregion

    #region Aiming
    public void CheckLookedAt()
    {
        if (Physics.Raycast(_camera.position, _camera.forward, out RaycastHit hit, 9999999999f, _mask))
            _currentlyAimedAt = hit.point;
        else
            _currentlyAimedAt = _camera.forward * 9999999999f;
    }
    #endregion

    #region Shooting
    private void PressShoot()
    {
        if ((_fsm.currentState.Name == RevolverStateList.IDLE || _fsm.currentState.Name == RevolverStateList.RELOADING) && _ammo > 0)
            _fsm.ChangeState(RevolverStateList.SHOOTING);
    }

    public void CheckBuffer()
    {
        if (_inputs.Actions.Shoot.IsPressed())
            PressShoot();
        if (_reloadBuffered)
            PressReload();

        _reloadBuffered = false;
    }

    public virtual void Shoot()
    {

    }

    public void StartRecoil()
    {
        _recoilT = _recoilTime;
    }

    public void Recoiling()
    {
        _recoilT -= Time.deltaTime;
        if (_recoilT <= 0)
        {
            if (_ammo > 0)
                _fsm.ChangeState(RevolverStateList.IDLE);
            else
                _fsm.ChangeState(RevolverStateList.RELOADING);
        }
    }
    #endregion

    #region Ammo
    public void LowerAmmoCount()
    {
        _ammo--;
        DisplayAmmo();
    }

    private void PressReload()
    {
        if (_fsm.currentState.Name == RevolverStateList.IDLE && _ammo < _maxAmmo)
            _fsm.ChangeState(RevolverStateList.RELOADING);
        else if (_fsm.currentState.Name == RevolverStateList.SHOOTING && _ammo < _maxAmmo)
            _reloadBuffered = true;
    }

    public void StartReload()
    {
        _reloadT = _reloadMaxTime;
        PlaceHolderSoundManager.Instance.PlayReload();
    }

    public void Reloading()
    {
        _reloadT -= Time.deltaTime;
        if (_reloadT <= _reloadMaxTime - _reloadMinTime && _ammo < _maxAmmo)
        {
            Reloaded();
        }
        if (_reloadT <= 0)
        {
            _fsm.ChangeState(RevolverStateList.IDLE);
        }
    }

    private void Reloaded()
    {
        _ammo = _maxAmmo;
        PlaceHolderSoundManager.Instance.PlayReloaded();
        DisplayAmmo();
    }

    private void DisplayAmmo()
    {
        if (_ammoCountText)
            _ammoCountText.text = _ammo.ToString() + "/" + _maxAmmo.ToString();
        else
            Debug.LogError("AmmoCount UI Text unassigned.");
    }
    #endregion

    #region Enable Disable Inputs
    void OnEnable()
    {
        Reloaded();
        _inputs.Enable();
    }

    void OnDisable()
    {
        _inputs.Disable();
    }
    #endregion
}