using UnityEngine;
using System.Collections;

public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour {

    // 싱글톤 클래스                                                                      
    private static T singleton = null;
    public static T Instance
    {
        get
        {
            if (singleton == null)
            {
                singleton = FindObjectOfType(typeof(T)) as T;

                if (null != singleton && Application.isPlaying == true)
                    singleton.transform.parent = MonoSingletonGroup.transform;
            }

            if (singleton == null)
            {
                GameObject obj = new GameObject(typeof(T).ToString());
                singleton = obj.AddComponent(typeof(T)) as T;
                singleton.transform.parent = MonoSingletonGroup.transform;
            }
            return singleton;
        }
    }

    public static bool IsInstanceExists
    {
        get
        {
            return (singleton != null);
        }
    }
    //    

    public virtual void OnDestroy()
    {
        singleton = null;
    }
}

public class MonoSingletonDontDestroyed<T> : MonoBehaviour where T : MonoBehaviour
{

    // 싱글톤 클래스                                                                      
    private static T singleton = null;
    public static T Instance
    {
        get
        {
            if (singleton == null)
            {
                singleton = FindObjectOfType(typeof(T)) as T;

                if (null != singleton)
                    singleton.transform.parent = MonoSingletonGroup.transform;
            }

            if (singleton == null)
            {
                GameObject obj = new GameObject(typeof(T).ToString());
                singleton = obj.AddComponent(typeof(T)) as T;
                singleton.transform.parent = MonoSingletonGroup.transform;
            }
            return singleton;
        }
    }

    public static bool IsInstanceExists
    {
        get
        {
            return (singleton != null);
        }
    }
    //    

    public virtual void OnDestroy()
    {
        singleton = null;
    }
}

public class MonoStaticUI<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T singleton = null;
    public static T Instance
    {
        get
        {
            return singleton;
        }
    }

    protected virtual void Awake()
    {
        if (null != singleton)
        {
            GameObject.Destroy(singleton.gameObject);
            singleton = null;
        }

        singleton = gameObject.GetComponent<T>();
    }

    protected virtual void OnDestroy()
    {
        singleton = null;
    }
}

public class MonoSingletonGroup
{
    static private GameObject m_group = null;
    static private string m_name { get { return "_MonoSingletonGroup"; } }

    static public GameObject gameObject
    {
        get
        {
            if (null == m_group)
            {
                m_group = new GameObject(m_name);
                m_group.isStatic = true;
                GameObject.DontDestroyOnLoad(m_group);
            }

            return m_group;
        }
    }

    static public Transform transform
    {
        get
        {
            return gameObject.transform;
        }
    }

    static public void DestroyImmediate()
    {
        GameObject.DestroyImmediate(m_group);
        m_group = null;
    }
}