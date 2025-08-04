using Sirenix.OdinInspector;
#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DialogueActorMoveCurveData : ScriptableObject, ISerializationCallbackReceiver
{
    [SerializeField]
    private List<SerializableKeyValuePair> _list;
    
    private Dictionary<string, MoveCurve> _moveCurves = new(StringComparer.OrdinalIgnoreCase);

    public MoveCurve this[string key] => _moveCurves.TryGetValue(key, out var result) ? result : default;

    private bool IsValidKey(string key)
    {
        return string.IsNullOrEmpty(key) == false && _list.Count(pair => string.Equals(pair.key, key, StringComparison.OrdinalIgnoreCase)) <= 1;
    }

    // private SerializableKeyValuePair CheckDuplicateKey(SerializableKeyValuePair value, GUIContent label, Func<GUIContent, bool> callNextDrawer)
    // {
    //     _list.Contains()
    // }

    void ISerializationCallbackReceiver.OnBeforeSerialize()
    {
    }

    void ISerializationCallbackReceiver.OnAfterDeserialize()
    {
        _moveCurves ??= new(StringComparer.OrdinalIgnoreCase);
        _moveCurves.Clear();

        foreach (var pair in _list)
        {
            _moveCurves.Add(pair.key, pair.value);
        }
    }

    [Serializable]
    public struct MoveCurve
    {
        [HorizontalGroup("Curves")]
        [BoxGroup("Curves/X")]
        [HideLabel]
        public AnimationCurve x;
        
        [HorizontalGroup("Curves")]
        [BoxGroup("Curves/Y")]
        [HideLabel]
        public AnimationCurve y;
    }

    [Serializable]
    private struct SerializableKeyValuePair
    {
        [HideLabel]
        [HorizontalGroup("Pair", width: 150)]
        [BoxGroup("Pair/Key")]
        [GUIColor("@$root.IsValidKey($value) ? Color.white : Color.red")]
        public string key;
        
        [HideLabel]
        [HorizontalGroup("Pair")]
        public MoveCurve value;
    }
}