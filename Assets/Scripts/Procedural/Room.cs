using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using System.Linq;

public class Room : MonoBehaviour
{
    [Header("References")]
    [SerializeField] List<EnemyHealth> _enemiesList;
    [SerializeField] List<Door> _doors;
    public Door Entry => _doors[0];
    public Door Exit => _doors[1];

    [Header("Settings")]
    [SerializeField] private bool _isCorridor = false;

    public int CurrentEnemiesInRoom;


    void Awake()
    {
        // this.transform.parent = DungeonGenerator.Instance.transform;
    }

    // [Button]
    public void CountEnemies()
    {
        _enemiesList.Clear();
        foreach (EnemyHealth enemy in GetComponentsInChildren<EnemyHealth>(includeInactive: true))
        {
            _enemiesList.Add(enemy);
            enemy.Room = this;
        }
    }

    // [Button]
    public void FindDoors()
    {
        _doors.Clear();
        foreach (Door door in GetComponentsInChildren<Door>())
        {
            _doors.Add(door);
            door.ThisDoorsRoom = this;
        }
    }

    public void EnableEnemies(bool b)
    {
        CountEnemies();
        CurrentEnemiesInRoom = _enemiesList.Count;
        SoundManager.Instance.PlaySound("event:/SFX_Environement/StartFight", 1f, gameObject);
        SoundManager.Instance.FightingSound();
        foreach (EnemyHealth enemyHealth in _enemiesList)
        {
            if (enemyHealth == null)
            {
                Debug.LogError("La room [" + this.gameObject.name + "] n'a pas d'ennemi assigné");
            }
            enemyHealth.gameObject.SetActive(b);
            if (enemyHealth.isActiveAndEnabled)
            {
                enemyHealth.SetDissolve();
                enemyHealth.StartCoroutine("DissolveInverse");
            }
        }
    }

    public void EnterRoom()
    {
        DungeonGenerator.Instance.SetCurrentRoom(this);
        DungeonGenerator.Instance.GetRoom(-1).gameObject.SetActive(false);
        this.Entry.CloseDoor();
        Killplane.Instance.MoveSpawnPointTo(this.Entry.transform.position + this.Entry.transform.forward * 4.0f + Vector3.up * 2);

        if (_isCorridor)
        {
            DungeonGenerator.Instance.GetRoom(1).gameObject.SetActive(true);
            DungeonGenerator.Instance.GetRoom(2).gameObject.SetActive(true);
            TimerManager.Instance.isInCorridor = true;
        }

        else
        {
            PlayerManager.Instance.RechargeEverything();
            SoundManager.Instance.PlaySound("event:/SFX_Environement/StartFight", 1f, gameObject);
            this.EnableEnemies(true);
            TimerManager.Instance.isInCorridor = false;
        }
    }

    public void ExitRoom()
    {
        if (DungeonGenerator.Instance.GetRoomIndex(this) != DungeonGenerator.Instance.CurrentRoom)
            return;

        if (DungeonGenerator.Instance.GetRoomIndex(this) >= DungeonGenerator.Instance.TotalRooms)
        {
            PlayerManager.Instance.OnPlayerWin();
        }

        if (_isCorridor)
        {
            this.Exit.OpenDoor();
            DungeonGenerator.Instance.GetRoom(1).Entry.OpenDoor();
        }

        else
        {
            //Nothing
        }

        this.Exit.HasBeenTriggered = true;
    }

    public void CheckForEnemies()
    {
        Debug.Log(CurrentEnemiesInRoom + " left in " + gameObject.name);
        if (CurrentEnemiesInRoom <= 0)
            FinishRoom();
    }

    private void FinishRoom()
    {
        //Feedbacks
        SoundManager.Instance.PlaySound("event:/SFX_Environement/SlowMo", 1f, gameObject);
        // PlayerHealth.Instance.TrueHeal(1);
        SoundManager.Instance.ClearedSound();
        PlayerManager.Instance.StartSlowMo(0.01f, 2f);
        PlayerManager.Instance.StartFlash(1.0f,1);

        //Progress in dungeon
        this.Exit.OpenDoor();
        if (DungeonGenerator.Instance != null)
            DungeonGenerator.Instance.GetRoom(1).Entry.OpenDoor();

        // Break Timer
        TimerManager.Instance.isInCorridor = true;
    }
}
