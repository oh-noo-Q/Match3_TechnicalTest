using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

//--------------------------------------------------------------------------------------
//----Smart pool script use singleton pattern to cache prefabs with pooling mechanism.
//--------------------------------------------------------------------------------------


/// <summary>
/// -------------------Hose use:
/// SmartPool.Instance.Spawm()      <=>     Instantite()
/// SmartPool.Instance.Despawn()    <=>     Destroy()
/// SmartPool.Instance.Preload()    <=>     Preload some object in game
/// </summary>


public class Pool
{
    int nextId;
    public string type;
    Stack<GameObject> inactive;                 // Stack hold gameobject belong this pool in state inactive
    Transform prefabContrainer;                // Transform contain pools gameobject
    GameObject prefab;                          // Prefabs belong pool

    /// <summary>
    /// Inital pool
    /// </summary>
    /// <param name="prefabs">Prefab belong to pool</param>
    /// <param name="initQuantify">Number gameobject initial</param>
    public Pool(GameObject prefabs, int initQuantify)
    {
        this.prefab = prefabs;
        this.prefabContrainer = new GameObject(prefabs.name + "_pool").transform;
        this.prefabContrainer.SetParent(SmartPool.Instance.transform);

        inactive = new Stack<GameObject>(initQuantify);
    }

    /// <summary>
    /// Instantiate gameobject to scene
    /// If stack don't have any gameobject in state deactive,
    /// we will instantiate new gameobject
    /// Otherwise, we remove one elemnet in stack and active it in game
    /// </summary>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <returns></returns>
    public GameObject Spawn(Vector3 position, Quaternion rotation)
    {
        GameObject obj;

        if (inactive.Count == 0)
        {
            // Instatite if stack empty
            obj = GameObject.Instantiate(prefab, position, rotation);

            if (nextId >= 10)
                obj.name = prefab.name + "_" + (nextId++);
            else
                obj.name = prefab.name + "_0" + (nextId++);

            var poolIdentify = obj.GetComponent<PoolIdentify>();
            if (poolIdentify == null)
                poolIdentify = obj.AddComponent<PoolIdentify>();
            else
                Debug.Log("PoolIdentify exist in object: " + obj.name);
            poolIdentify.pool = this;
            // Set to contrainer
            obj.transform.SetParent(prefabContrainer, false);
        }
        else
        {
            obj = inactive.Pop();

            if (obj == null)
                return Spawn(position, rotation);
        }

        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);

        return obj;
    }

    /// <summary>
    /// Method return gameobject belong to pool
    /// </summary>
    /// <param name="obj">Gameobject will return pool</param>
    public void Despawn(GameObject obj)
    {
        if (obj.activeSelf)
        {
            obj.SetActive(false);
            inactive.Push(obj);
            obj.transform.SetParent(prefabContrainer, false);
        }
    }

    /// <summary>
    /// Method to destroy pool
    /// </summary>
    public void DestroyAll()
    {
        // Return stack
        prefab = null;

        // Clear stack
        inactive.Clear();

        // Destroy child
        for (int i = 0; i < prefabContrainer.childCount; i++)
            MonoBehaviour.Destroy(prefabContrainer.GetChild(i).gameObject);

        // Destroy parent
        Object.Destroy(prefabContrainer.gameObject);

        Resources.UnloadUnusedAssets();
    }

    public void Preload(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Spawn(Vector3.zero, Quaternion.identity);
        }
        for (int i = 0; i < prefabContrainer.childCount; i++)
        {
            Despawn(prefabContrainer.GetChild(i).gameObject);
        }
    }

    /// <summary>
    ///  Chekc pool exist or not when load new level
    /// </summary>
    /// <returns></returns>
    public bool CheckPoolExist()
    {
        return (prefabContrainer) ? true : false;
    }

    /// <summary>
    /// Method return all gameobject to pool
    /// </summary>
    public void ReturnPool()
    {
        for (int i = 0; i < prefabContrainer.childCount; i++)
        {
            if (prefabContrainer.GetChild(i).gameObject.activeSelf)
                Despawn(prefabContrainer.GetChild(i).gameObject);
        }
    }
}


/// <summary>
/// Main class hold pool data
/// </summary>
public class SmartPool : SingletonDontDestroy<SmartPool>
{
    const int DEFAULT_POOL_SIZE = 3;

    private Dictionary<GameObject, Pool> pools = new Dictionary<GameObject, Pool>();

    /// <summary>
    /// Initial dictionary for pool system
    /// </summary>
    /// <param name="prefabs"></param>
    /// <param name="quantify"></param>
    bool Init(GameObject prefabs = null, int quantify = DEFAULT_POOL_SIZE)
    {
        if (Instance.pools == null)
            Instance.pools = new Dictionary<GameObject, Pool>();

        if (prefabs != null && Instance.pools.ContainsKey(prefabs) == false)
        {
            Instance.pools[prefabs] = new Pool(prefabs, quantify);
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    ///  Method to instantiate prefab to scene
    /// </summary>
    /// <param name="prefabs">Objects will spawn</param>
    /// <param name="position">Position for gameoject</param>
    /// <param name="rotation">Rotation for gameobject</param>
    /// <returns></returns>
    public GameObject Spawn(GameObject prefabs, Vector3 position, Quaternion rotation)
    {
        return GetPool(prefabs).Spawn(position, rotation);
        //return GameObject.Instantiate(prefabs, position, rotation);
    }

    public Pool GetPool(GameObject prefabs)
    {
        Init(prefabs);
        return Instance.pools[prefabs];
    }

    public void Preload(GameObject prefabs, int count)
    {
        if (Init(prefabs))
        {
            Instance.pools[prefabs].Preload(count);
        }
    }

    /// <summary>
    /// Method to deactive gameobject
    /// </summary>
    /// <param name="prefabs">Gameobject will deactive</param>
    public void Despawn(GameObject prefabs, bool destroyIfNotPool = false)
    {
        //Destroy(prefabs);
        //return;
        PoolIdentify poolIndent = prefabs.GetComponent<PoolIdentify>();

        if (poolIndent == null && destroyIfNotPool)
        {
            //prefabs.SetActive(false);
            Debug.LogError(prefabs.name + " is not exist pool. It will be destroyed");
            DestroyImmediate(prefabs);
        }
        else
        {
            poolIndent.pool.Despawn(prefabs);
        }
    }

    /// <summary>
    /// Method will make all gameoject belong prefab will deactive
    /// </summary>
    /// <param name="prefab"></param>
    public void ReturnPool(GameObject prefab)
    {
        if (Instance.pools == null && prefab)
            return;

        if (Instance.pools.ContainsKey(prefab))
            Instance.pools[prefab].ReturnPool();
    }
}