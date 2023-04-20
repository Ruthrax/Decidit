using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class Synergies : LocalManager<Synergies>
{
    public enum Chants
    {
        ARAGON,
        MUSE,
        EYLAU
    }

    [Foldout("Malades")]
    public List<EnemyHealth> Hospital;
    [Foldout("Fugue -> Malades")]
    [SerializeField]
    Pooler _fugueMaladeShotsPooler;

    [Foldout("Cimetière")]
    [SerializeField] Transform _eylauArea;
    [Foldout("Muse -> Cimetière")]
    [SerializeField] Pooler _explosionVfxPooler;
    [Foldout("Fugue -> Cimetière")]
    [SerializeField] Pooler _blackHolePooler;

    public void Synergize(SynergyProjectile bullet, Transform collider)
    {
        SoundManager.Instance.PlaySound("event:/SFX_Controller/UniversalSound", 1f, collider.gameObject);
        // SoundManager.Instance.PlaySound("event:/SFX_Controller/Shoots/MuseMalade/Impact", 1f, gameObject);

        Chants bulletChant = bullet.Chant;
        Chants colliderChant = collider.GetComponent<SynergyTrigger>().Chant;

        switch (bulletChant)
        {
            case Chants.ARAGON:
                switch (colliderChant)
                {
                    case Chants.ARAGON:
                        //Nothing!
                        break;
                    case Chants.MUSE:
                        FugueOnMalade(bullet.transform.position);
                        break;
                    case Chants.EYLAU:
                        Vector3 position = bullet.transform.position;
                        position.y += bullet.Direction.normalized.y * 8;
                        FugueOnCimetiere(position);
                        break;
                }
                break;

            case Chants.MUSE:
                switch (colliderChant)
                {
                    case Chants.ARAGON:
                        MuseOnAragon();
                        break;
                    case Chants.MUSE:
                        //Nothing!
                        break;
                    case Chants.EYLAU:
                        MuseOnCimetiere(bullet.transform.position.y);
                        bullet.Explode(bullet.transform.forward);
                        break;
                }
                break;

            case Chants.EYLAU:
                switch (colliderChant)
                {
                    case Chants.ARAGON:
                        EylauOnAragon();
                        break;
                    case Chants.MUSE:
                        EylauOnMalade();
                        break;
                    case Chants.EYLAU:
                        //Nothing!
                        break;
                }
                break;
        }
    }

    #region Muse -> Nuage
    public void MuseOnAragon()
    {
        Debug.Log("projectile transformé en projo acide");
    }
    #endregion

    #region Eylau -> Nuage
    public void EylauOnAragon()
    {
        Debug.Log("Projectile pas chargé devient chargé");
    }
    #endregion

    #region Fugue -> Malade
    public void FugueOnMalade(Vector3 position)
    {
        FugueMaladeShot shot = _fugueMaladeShotsPooler.Get() as FugueMaladeShot;
        shot.Setup(Hospital, position);
    }
    #endregion

    #region Eylau -> Malade
    public void EylauOnMalade()
    {
        foreach (EnemyHealth enemy in Hospital)
        {
            enemy.TakeDamage(0.5f);
            //TODO JT enemy.Slow();
        }
    }
    #endregion

    #region Fugue -> Cimetière
    public void FugueOnCimetiere(Vector3 position)
    {
        Debug.Log("Trou noir, voir avec jt pour attirer les ennemis au centre du cimetière");
        Vector3 initialPos = new Vector3(_eylauArea.position.x, position.y, _eylauArea.position.z);
        SpawnBlackHole(initialPos);
    }

    private void SpawnBlackHole(Vector3 position)
    {
        BlackHole blackHole = _blackHolePooler.Get() as BlackHole;
        if (blackHole)
        {
            blackHole.transform.position = position;
            blackHole.Setup();
        }
    }
    #endregion

    #region Muse -> Cimetière
    public void MuseOnCimetiere(float y)
    {
        Vector3 initialPos = new Vector3(_eylauArea.position.x, y, _eylauArea.position.z);
        for (int i = 1; i < 10; i++)
        {
            Vector2 circle = (Random.insideUnitCircle).normalized * Random.Range(2.0f, 6.0f);
            Vector3 position = new Vector3(circle.x, 0.0f, circle.y);

            //twice to fill the cimetiere
            SpawnAnExplosion(initialPos + position, i);
            SpawnAnExplosion(initialPos - position, i);
        }
    }

    private void SpawnAnExplosion(Vector3 pos, int i)
    {

        MuseEylauExplosions exp = _explosionVfxPooler.Get().GetComponent<MuseEylauExplosions>();
        exp.Setup(pos + Vector3.up * i * 2, i / 30.0f);

        if (i == 1)
            SoundManager.Instance.PlaySound("event:/SFX_Controller/Synergies/MuseOnEyleau/Sound", 1f, exp.gameObject);

        MuseEylauExplosions exp2 = _explosionVfxPooler.Get().GetComponent<MuseEylauExplosions>();
        exp2.Setup(pos + Vector3.down * i * 2, i / 30.0f);
    }
    #endregion
}
