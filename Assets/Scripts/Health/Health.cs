using System;
using UnityEngine;
using UnityEngine.UI;
using NaughtyAttributes;

public class Health : MonoBehaviour
{
    [Header("References")]
    [SerializeField] protected Image _hpUi;
    [SerializeField] protected Image _probHpUi;

    [Header("Stats")]
    [Range(1, 300)][SerializeField] protected float _maxHp = 100;
    [Range(0, 3)][SerializeField] float _probationMaxStartup = 1;
    [Range(0.1f, 60)][SerializeField] float _probationSpeed = 15;

    protected float _hp;
    protected float _probHp;
    protected bool _hasProbation;
    protected float _probationStartup;


    protected virtual void Start()
    {
        _hp = _maxHp;
        _probHp = _hp;
        _hasProbation = false;
        DisplayHealth();
        DisplayProbHealth();
    }

    protected virtual void Update()
    {
        if (_hasProbation)
            UpdateProbHealth();
    }

    public virtual void TakeDamage(int damage = 5)
    {
        _hp -= damage;
        DisplayHealth();

        if (_hp <= 0)
        {
            _hp = 0f;
            Death();
            return;
        }

        StartProbHealth();
    }

    protected virtual void DisplayHealth()
    {
        if (_hpUi)
        {
            _hpUi.fillAmount = Mathf.InverseLerp(0, _maxHp, _hp);
        }
    }

    protected void StartProbHealth()
    {
        _probationStartup = _probationMaxStartup;
        _hasProbation = true;
    }

    private void UpdateProbHealth()
    {
        if (_probationStartup > 0)
        {
            _probationStartup -= Time.deltaTime;
            return;
        }

        else
        {
            _probHp -= _probationSpeed * Time.deltaTime;
            if (_probHp < _hp)
            {
                _hasProbation = false;
                _probHp = _hp;
            }
            DisplayProbHealth();
        }
    }

    private void DisplayProbHealth()
    {
        if (_probHpUi)
        {
            _probHpUi.fillAmount = Mathf.InverseLerp(0, _maxHp, _probHp);
        }
    }

    private void Death()
    {

    }
}
