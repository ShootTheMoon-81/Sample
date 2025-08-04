using System;

public class Singleton<T> where T : class, new()
{                                                                      
    private static T instance = null;
    public static T Instance
    {
        get
        {
            if (instance == null)
                instance = new T();

            return instance;
        }
    }

    public static bool IsInstanceExists
    {
        get
        {
            return (instance != null);
        }
    }
}