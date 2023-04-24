using System.Collections.Specialized;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class DungeonGenerator : LocalManager<DungeonGenerator>
{
    [SerializeField] int _seed;
    [SerializeField] bool _randomizeSeed;
    //[SerializeField] float _dungeonRotation;
    [SerializeField] int[] _difficultyPerRoom;
    private int _numberOfRooms;
    [SerializeField] int _firstPowerupAfterRoom;
    [SerializeField] int _secondPowerupAfterRoom;

    [SerializeField] RoomSetup _starterRoom;
    [SerializeField] RoomSetup _finalRoom;
    // public List<RoomSetup> RoomSets;

    [SerializeField] List<int> _scenesEasy;
    [SerializeField] List<int> _scenesMedium;
    [SerializeField] List<int> _scenesHard;
    // SceneManager.LoadScene(ScenesHard[Random.Range(0, ScenesHard.Count)], LoadSceneMode.Additive);

    private List<List<Room>> _usableRooms;
    public List<RoomSetup> Corridors;
    public List<RoomSetup> CorridorsSpell;

    public int TotalRooms { get { return _actualRooms.Count - 1; } }

    List<Room> _actualRooms;
    public int CurrentRoom;

    //Progression
    [System.NonSerialized] public bool ChoseASkill;
    [System.NonSerialized] public bool ChoseAGun;

    [SerializeField] List<Room> _roomsToBuild;

    protected override void Awake()
    {
        base.Awake();
        _actualRooms = new List<Room>();
    }

    void Update()
    {
    }

    void Start()
    {
        _numberOfRooms = _difficultyPerRoom.Length;
        ResetDungeon();
        StartCoroutine(SetUsableRooms());

        ChoseAGun = false;
        ChoseASkill = false;
    }

    IEnumerator SetUsableRooms()
    {
        if (transform.childCount > 0)
        {
            ResetDungeon();
            ClearDungeon();
        }

        //* basically a new instance of the rooms setup so we can delete a room once it has been instanced in order to avoid repeats.
        _usableRooms = new List<List<Room>>();
        _usableRooms.Add(new List<Room>());
        for (int i = 0; i < _scenesEasy.Count; i++)
        {
            SceneManager.LoadScene(_scenesEasy[i], LoadSceneMode.Additive);
            Debug.Log("1");
            yield return null;
            GameObject thatScenesRoot = GameObject.Find((_scenesEasy[i].ToString()));
            Room thatScenesRoom = thatScenesRoot.GetComponent<Room>();
            _usableRooms[0].Add(thatScenesRoom);
        }

        // _usableRooms.Add(new List<Room>());
        // for (int i = 0; i < _scenesMedium.Count; i++)
        // {
        //     SceneManager.LoadScene(_scenesMedium[i], LoadSceneMode.Additive);
        //     Room thatScenesRoom = GameObject.Find(_scenesMedium[i].ToString()).GetComponent<Room>();
        //     _usableRooms[1].Add(thatScenesRoom);
        // }

        // _usableRooms.Add(new List<Room>());
        // for (int i = 0; i < _scenesHard.Count; i++)
        // {
        //     SceneManager.LoadScene(_scenesHard[i], LoadSceneMode.Additive);
        //     Room thatScenesRoom = GameObject.Find(_scenesHard[i].ToString()).GetComponent<Room>();
        //     _usableRooms[2].Add(thatScenesRoom);
        // }

        //Prefab
        // for (int i = 0; i < RoomSets.Count; i++)
        // {
        //     _usableRooms.Add(new List<Room>());
        //     foreach (Room room in RoomSets[i].rooms)
        //     {
        //         _usableRooms[i].Add(room);
        //     }
        // }

        Generate();
        yield return null;
    }

    public void Generate()
    {


        //* randomizing seed
        if (_randomizeSeed)
            _seed = Random.Range(0, 1000);
        Random.InitState(_seed);

        _roomsToBuild = new List<Room>(_numberOfRooms + 2 + (_numberOfRooms + 1));
        _roomsToBuild.Add(_starterRoom.Get());

        int stackCount = Mathf.RoundToInt(_numberOfRooms / 3f);
        for (int i = 0; i < _numberOfRooms; i++)
        {
            SelectCorridor(i);

            //* passe a la difficulte suivante
            if (stackCount <= 0)
            {
                stackCount = Mathf.RoundToInt(_numberOfRooms / 3f);
            }

            //* ajoute une salle avec une difficulte predefinie et la retire du Set pour ne pas retomber dessus.
            AddRandomRoom(_difficultyPerRoom[i]);

            stackCount--;
        }

        _roomsToBuild.Add(Corridors[Random.Range(0, Corridors.Count)].Get());
        _roomsToBuild.Add(_finalRoom.Get());
        Build();
    }

    void SelectCorridor(int indexRoom)
    {
        switch (indexRoom)
        {
            case 1:
                _roomsToBuild.Add(CorridorsSpell[Random.Range(0, CorridorsSpell.Count)].Get());
                break;

            case 3:
                _roomsToBuild.Add(CorridorsSpell[Random.Range(0, CorridorsSpell.Count)].Get());
                break;

            default:
                _roomsToBuild.Add(Corridors[Random.Range(0, Corridors.Count)].Get());
                break;
        }
    }

    private void AddRandomRoom(int difficulty)
    {
        int roomToAddIndex = Random.Range(0, _usableRooms[difficulty].Count);
        if (_usableRooms[difficulty].Count <= 0)
        {
            string roomSetDifficulty = "";
            switch (difficulty)
            {
                case 0:
                    roomSetDifficulty = "EASY";
                    break;
                case 1:
                    roomSetDifficulty = "MEDIUM";
                    break;
                case 2:
                    roomSetDifficulty = "HARD";
                    break;
            }
            Debug.LogError("Not enough room in RoomSet [" + roomSetDifficulty + "]");
        }

        Room roomToAdd = _usableRooms[difficulty][roomToAddIndex];
        _roomsToBuild.Add(roomToAdd);
        _usableRooms[difficulty].RemoveAt(roomToAddIndex);
    }

    private void ResetDungeon()
    {
        //transform.DetachChildren();
        _roomsToBuild.Clear();
        _actualRooms.Clear();
    }

    public void ClearDungeon()
    {
        while (transform.childCount > 0)
            DestroyImmediate(transform.GetChild(0).gameObject);
    }

    private void Build()
    {
        Transform lastDoor = null;
        foreach (Room room in _roomsToBuild)
        {
            //* Spawn room
            Room roomInstance = Instantiate(room, Vector3.zero, Quaternion.identity, transform);
            roomInstance.gameObject.SetActive(true);

            //* set rotation
            if (lastDoor != null)
            {
                roomInstance.transform.rotation = lastDoor.rotation;
                roomInstance.transform.Rotate(0, 180, 0);
            }
            else
                roomInstance.transform.rotation = Quaternion.identity;

            //* set position
            if (lastDoor != null)
            {
                Vector3 roomPosition = lastDoor.position + (roomInstance.transform.position - roomInstance.Entry.transform.position);
                roomInstance.transform.position = roomPosition;
            }

            //* Disable Room and its enemies
            lastDoor = roomInstance.Exit.transform;
            roomInstance.EnableEnemies(false);
            roomInstance.gameObject.SetActive(false);

            _actualRooms.Add(roomInstance);
        }

        EnableFirstRooms();
    }

    private void EnableFirstRooms()
    {
        _actualRooms[0].gameObject.SetActive(true);
        _actualRooms[0].Exit.OpenDoor();
        _actualRooms[1].gameObject.SetActive(true);
        _actualRooms[1].Entry.OpenDoor();
    }

    public Room GetRoom(int i = 0)
    {
        return _actualRooms[Mathf.Clamp(CurrentRoom + i, 0, TotalRooms)];
    }

    public void SetCurrentRoom(Room room)
    {
        CurrentRoom = _actualRooms.IndexOf(room);
    }

    public int GetRoomIndex(Room room)
    {
        return _actualRooms.IndexOf(room);
    }

    /*    public void Endgame()
        {
            Debug.Log("EndGame");
            TimerManager.instance.SaveTimer();
            MenuManager.Instance.OpenWin();
        }*/

    /// <summary>
    /// get seed all rooms
    /// </summary>
    /// <returns></returns>
    public List<Room> GetRooms()
    {
        return _roomsToBuild;
    }

    public int GetSeed()
    {
        return _seed;
    }
}
