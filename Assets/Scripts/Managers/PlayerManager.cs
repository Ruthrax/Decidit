using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using NaughtyAttributes;

public class PlayerManager : LocalManager<PlayerManager>
{
    //Slow down time
    float _slowMoT;
    float _slowMoInitialT;
    float _timeSpeed;
    [Foldout("Debugging")]
    [SerializeField] TextMeshProUGUI _timescaleDebugUi;

    //Controller Rumble
    float _rumbleT;
    float _rumbleInitialT;
    float _rumbleLowFreqIntensity;
    float _rumbleHighFreqIntensity;
    bool _isRumbling;

    //Display framerate
    [Foldout("Debugging")]
    [SerializeField] GameObject _DebuggingCanvas;
    [Foldout("Debugging")]
    [SerializeField] TextMeshProUGUI _fps;
    float _delayFramerateCalculation;

    //Lock framerate
    bool _isLockedAt60;

    //Pausing
    private bool _canPause = true;
    float _timescaleBeforePausing;
    bool _isPaused;
    [Foldout("Menu")]
    // [SerializeField] GameObject _menu;

    //Death variables
    bool _isDying;
    float _dieT;
    [Foldout("Death")]
    [SerializeField] float _deathDuration = 1f;

    //Victory variables

    //StopGame stuff
    public List<GameObject> Guns;
    public List<GameObject> Arms;

    PlayerInputMap _inputs;

    protected override void Awake()
    {
        base.Awake();
        _inputs = new PlayerInputMap();
        _inputs.Debugging.DisplayFramerate.started += _ => DisplayFramerate();
        _inputs.MenuNavigation.Pause.started += _ => PressPause();
        _inputs.Debugging.Lock.started += _ => LockFramerate();
        _inputs.MenuNavigation.anyButton.started += _ => SwitchToController();
        _inputs.MenuNavigation.moveMouse.started += _ => SwitchToMouse();
    }

    private void Start()
    {
        _isPaused = false;
        MenuManager.Instance.StopMenuing();
        _isLockedAt60 = false;
    }

    private void Update()
    {
        SlowMo();
        Rumble();

        if (_isDying)
            Die();

        if (_fps.enabled)
            UpdateFramerate();
    }

    private void LockFramerate()
    {
        if (_isPaused)
            return;
        if (_isLockedAt60)
        {
            _isLockedAt60 = false;
            Application.targetFrameRate = 0;
        }
        else
        {
            _isLockedAt60 = true;
            Application.targetFrameRate = 60;
        }
    }

    private void DisplayTimeScale()
    {
        if (_timescaleDebugUi)
            _timescaleDebugUi.text = ("TimeScale: " + Time.timeScale.ToString("F3"));
    }

    #region Equipping
    public void Skill4()
    {
        foreach (GameObject arm in Arms)
            arm.SetActive(false);
        Arms[3].SetActive(true);
    }
    public void Skill3()
    {
        foreach (GameObject arm in Arms)
            arm.SetActive(false);
        Arms[2].SetActive(true);
    }
    public void Skill2()
    {
        foreach (GameObject arm in Arms)
            arm.SetActive(false);
        Arms[1].SetActive(true);
    }
    public void Skill1()
    {
        foreach (GameObject arm in Arms)
            arm.SetActive(false);
        Arms[0].SetActive(true);
    }
    public void Gun4()
    {
        foreach (GameObject gun in Guns)
            gun.SetActive(false);
        Guns[3].SetActive(true);
        PlaceHolderSoundManager.Instance.PlayWeaponEquip();
    }
    public void Gun3()
    {
        foreach (GameObject gun in Guns)
            gun.SetActive(false);
        Guns[2].SetActive(true);
        PlaceHolderSoundManager.Instance.PlayWeaponEquip();
    }
    public void Gun2()
    {
        foreach (GameObject gun in Guns)
            gun.SetActive(false);
        Guns[1].SetActive(true);
        PlaceHolderSoundManager.Instance.PlayWeaponEquip();
    }
    public void Gun1()
    {
        foreach (GameObject gun in Guns)
            gun.SetActive(false);
        Guns[0].SetActive(true);
        PlaceHolderSoundManager.Instance.PlayWeaponEquip();
    }
    #endregion

    #region Ingame menus
    private void PressPause()
    {
        if (!_canPause)
            return;

        if (_isPaused)
            Unpause();
        else
            Pause();
    }

    public void Pause()
    {
        _isPaused = true;
        // _menu.SetActive(true);

        MenuManager.Instance.StartMenuing();
        MenuManager.Instance.OpenMain();
        StopGame();
    }

    public void Unpause()
    {
        _isPaused = false;
        // _menu.SetActive(false);

        MenuManager.Instance.StopMenuing();
        ResumeGame();
    }

    private void StopGame()
    {
        //timescale
        _timescaleBeforePausing = Time.timeScale;
        Time.timeScale = 0f;
    }

    private void ResumeGame()
    {
        if (_timescaleBeforePausing != 0)
            Time.timeScale = _timescaleBeforePausing;
        else
            Time.timeScale = 1;
    }

    public void StartDying()
    {
        if (_isDying)
            return;

        _isDying = true;
        _dieT = 0f;
        StopRumbling();
        StopSlowMo();
        _canPause = false;
    }

    private void Die()
    {
        _dieT += Time.unscaledDeltaTime;

        Time.timeScale = Mathf.Lerp(0, 1, Mathf.InverseLerp(_deathDuration, 0, _dieT));
        StartRumbling(1, 1, _dieT);

        if (_dieT >= _deathDuration)
            Dead();
    }

    private void Dead()
    {
        _isDying = false;
        // _menu.SetActive(true);
        MenuManager.Instance.StartMenuing();
        MenuManager.Instance.OpenDeath();
        StopGame();
    }

    public void OnPlayerWin()
    {
        // _menu.SetActive(true);
        _canPause = false;
        MenuManager.Instance.StartMenuing();
        MenuManager.Instance.OpenWin();
        StopGame();
    }
    #endregion

    #region Framerate Displayer
    private void DisplayFramerate()
    {
        _fps.enabled = !_fps.enabled;
    }

    private void UpdateFramerate()
    {
        if (_delayFramerateCalculation <= 0)
        {
            _fps.text = (1 / Time.unscaledDeltaTime).ToString("F1");
            _delayFramerateCalculation = 0.05f;
        }
        else
            _delayFramerateCalculation -= Time.deltaTime;
    }
    #endregion

    #region Slow mo
    public void StartSlowMo(float speed, float duration)
    {
        if (duration > _slowMoT)
        {
            _slowMoInitialT = duration;
            _slowMoT = duration;
        }
        if (speed < _timeSpeed)
            _timeSpeed = speed;
    }

    private void SlowMo()
    {
        if (_slowMoT > 0 || !_isPaused)
        {
            Time.timeScale = Mathf.Lerp(_timeSpeed, 1, Mathf.InverseLerp(_slowMoInitialT, 0, _slowMoT));
            DisplayTimeScale();

            _slowMoT -= Time.unscaledDeltaTime;
            if (_slowMoT < 0)
                StopSlowMo();
        }
    }

    private void StopSlowMo()
    {
        Time.timeScale = 1;
        _slowMoT = 0;
        DisplayTimeScale();
    }
    #endregion

    #region Rumble
    public void StartRumbling(float lowFreqStrength, float highFreqStrength, float duration)
    {
        if (duration > _rumbleT)
        {
            _rumbleInitialT = duration;
            _rumbleT = duration;
        }

        if (lowFreqStrength > _rumbleLowFreqIntensity)
        {
            _rumbleLowFreqIntensity = lowFreqStrength;
        }
        if (highFreqStrength > _rumbleHighFreqIntensity)
        {
            _rumbleHighFreqIntensity = highFreqStrength;
        }


        if (Gamepad.current != null)
            Gamepad.current.ResumeHaptics();
        _isRumbling = true;
    }

    private void Rumble()
    {
        if (_isRumbling)
        {
            float low = _rumbleLowFreqIntensity * Mathf.InverseLerp(0, _rumbleInitialT, _rumbleT);
            float high = _rumbleHighFreqIntensity * Mathf.InverseLerp(0, _rumbleInitialT, _rumbleT);
            if (Gamepad.current != null)
                Gamepad.current.SetMotorSpeeds(low, high);

            _rumbleT -= Time.deltaTime;
            if (_rumbleT <= 0.1f)
                StopRumbling();
        }
    }

    public void StopRumbling()
    {
        if (Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(0, 0);
            Gamepad.current.PauseHaptics();
        }
        _rumbleHighFreqIntensity = 0f;
        _rumbleLowFreqIntensity = 0f;
        _rumbleInitialT = 0f;
        _rumbleT = 0;
        _isRumbling = false;
    }
    #endregion

    #region Change current device
    private void SwitchToMouse()
    {
        if (MenuManager.Instance.CurrentDevice == MenuManager.Devices.Mouse)
            return;
        MenuManager.Instance.CurrentDevice = MenuManager.Devices.Mouse;
    }

    private void SwitchToController()
    {
        if (MenuManager.Instance.CurrentDevice == MenuManager.Devices.Controller)
            return;
        MenuManager.Instance.CurrentDevice = MenuManager.Devices.Controller;
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
