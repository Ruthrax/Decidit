using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class MuseArm : Arm
{
    [Foldout("References")]
    [SerializeField] Pooler _pooler;

    [Foldout("Stats")]
    [SerializeField] protected float _launchShakeIntensity;
    [Foldout("Stats")]
    [SerializeField] protected float _launchShakeDuration;

    [Foldout("References")]
    [SerializeField]
    Transform _canonPosition;
    [Foldout("References")]
    [SerializeField]
    LayerMask _mask;

    Vector3 _currentlyAimedAt;

    protected override void PressSong()
    {
        base.PressSong();
        base.ReleaseSong();
    }
    protected override void ReleaseSong()
    {

    }

    public override void StartIdle()
    {
        Refilled();
    }

    public override void CheckLookedAt()
    {
        if (Physics.Raycast(_cameraTransform.position, _cameraTransform.forward, out RaycastHit hit, 10000f, _mask))
            _currentlyAimedAt = hit.point;
        else
            _currentlyAimedAt = _cameraTransform.forward * 10000f;
    }

    public override void StartActive()
    {
        _crossHairOutline.enabled = false;
        PooledObject shot = _pooler.Get();
        shot.transform.rotation = transform.rotation;
        shot.GetComponent<Projectile>().Setup(_canonPosition.position, (_currentlyAimedAt - _canonPosition.position).normalized, _cameraTransform.forward);


        Player.Instance.StartShake(_launchShakeIntensity, _launchShakeDuration);
        PlaceHolderSoundManager.Instance.PlayMuseRocketLaunch();
        //_muzzleFlash.PlayAll();
    }
}
