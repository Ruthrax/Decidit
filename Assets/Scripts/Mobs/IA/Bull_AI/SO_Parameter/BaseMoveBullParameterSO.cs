using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Bull_BaseMoveParameter", menuName = "Bull/BaseMoveParameter")]
public class BaseMoveBullParameterSO : ScriptableObject
{
    [Header("Speed Rotation Parameter")]
    [SerializeField] public float speedRot;
    [SerializeField] public float maxSpeedRot;
    [SerializeField] public float smoothRot;

    [Header("Speed Movement Parameter")]
    public float baseSpeed;
    public float stopSpeed;
    public float delayBeforeJump;
    public float maxDelayBeforeJump;

    [Header("Distance Player Change State")]
    public float distActiveRush;
    public float distActiveAttack;
}