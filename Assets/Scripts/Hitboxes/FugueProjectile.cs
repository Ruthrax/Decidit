using System.Collections;
using System.Collections.Generic;

using NaughtyAttributes;
using UnityEngine;

public class FugueProjectile : SynergyProjectile
{
    [SerializeField] protected bool _centered;
    [SerializeField] ProjectileOscillator[] _objects;
    [SerializeField] Vector3[] _directions;

    public void Setup(Vector3 position, Vector3 direction, Vector3 cameraDirection, float damage)
    {
        base.Setup(position, direction, cameraDirection);
        transform.rotation = Camera.main.transform.parent.rotation;
        this.Damage = damage;
        SetupOscillatingTrails();
    }

    protected override void Bounce(RaycastHit hit)
    {
        this.Damage += 0.5f;
        base.Bounce(hit);
    }

    void SetupOscillatingTrails()
    {
        for (int i = 0; i < _objects.Length; i++)
        {
            Vector3 direction = transform.right * _directions[i].x + transform.up * _directions[i].y + transform.forward * _directions[i].z;
            _objects[i].Setup(direction.normalized, _centered);
        }
    }
}