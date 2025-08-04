using GlobalEnums;
using UnityEngine;

public static class GameObjectExtensions
{
    public static GameObject FindByName(this GameObject gameObject, string name)
    {
        if (name == null)
            return null;

        Transform transform = gameObject.transform;

        for (int i = 0; i < transform.childCount; ++i)
        {
            GameObject childObject = transform.GetChild(i).gameObject;

            if (childObject.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                return childObject;
        }

        return null;
    }

    public static GameObject FindByNameRecursively(this GameObject gameObject, string name , bool onlyActive = false)
    {
        if (name == null)
            return null;

        Transform transform = gameObject.transform;

        for (int i = 0; i < transform.childCount; ++i)
        {
            GameObject childObject = transform.GetChild(i).gameObject;

            //활성화된 오브젝트만 찾기 옵션이 켜져있을 경우
            bool skip = (onlyActive) ? childObject.activeInHierarchy == false : false;

            if (!skip && childObject.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                return childObject;

            GameObject findRecursive = childObject.FindByNameRecursively(name,onlyActive);

            //skip = (onlyActive) ? findRecursive.activeInHierarchy == false : false;

            if (findRecursive != null)
                return findRecursive;
        }

        return null;
    }

    public static T GetOrAddComponent<T>(this GameObject target) where T : Component
    {
        if (target == null)
        {
            DebugHelper.LogError($"target이 존재하지 않습니다.");
            return null;
        }

        T component = target.GetComponent<T>();

        if (component != null)
            return component;
        else
            return target.AddComponent<T>();
    }

    public static T GetOrAddComponentInChildren<T>(this GameObject target) where T : Component
    {
        T component = target.GetComponentInChildren<T>();

        if (component != null)
            return component;
        else
            return target.AddComponent<T>();
    }

    public static void RemoveComponent<T>(this GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();

        if (component != null)
            Object.Destroy(component);
    }

    public static void SetLayerRecursively(this GameObject gameObject, string layerName)
    {
        gameObject.SetLayerRecursively(LayerMask.NameToLayer(layerName));
    }

    public static void SetLayerRecursively(this GameObject gameObject, int layer)
    {
        if (gameObject == null)
        {
            DebugHelper.LogError($"gameObject가 존재하지 않습니다.");
            return;
        }

        Transform transform = gameObject.transform;

        gameObject.layer = layer;

        for (int i = 0; i < transform.childCount; ++i)
            transform.GetChild(i).gameObject.SetLayerRecursively(layer);
    }
}