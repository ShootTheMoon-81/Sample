using DG.Tweening;
using UnityEngine;

public class BattleModeTransitionContoller : MonoBehaviour
{
    [Header("Transition Out Directing")]
    [SerializeField]
    private GameObject _rootOutDirecting;

    [Header("Transition In Directing")]
    [SerializeField]
    private GameObject _rootInDirecting;

    public float OutDirectingTime { private set; get; }
    public float InDirectingTime { private set; get; }

    private void Awake()
    {
        var doTweenAnimations = _rootOutDirecting.GetComponentsInChildren<DOTweenAnimation>(true);
        foreach (var doTweenAnimation in doTweenAnimations)
        {
            if (doTweenAnimation.loops == -1)
            {
                continue;
            }

            float directingTime = doTweenAnimation.duration + doTweenAnimation.delay;
            if (directingTime > OutDirectingTime)
                OutDirectingTime = directingTime;
        }
        
        doTweenAnimations = _rootInDirecting.GetComponentsInChildren<DOTweenAnimation>(true);
        foreach (var doTweenAnimation in doTweenAnimations)
        {
            if (doTweenAnimation.loops == -1)
            {
                continue;
            }

            float directingTime = doTweenAnimation.duration + doTweenAnimation.delay;
            if (directingTime > InDirectingTime)
                InDirectingTime = directingTime;
        }
    }

    private void OnDisable()
    {
        _rootOutDirecting.SetActive(false);
        _rootInDirecting.SetActive(false);

        OutDirectingTime = 0.0f;
        InDirectingTime = 0.0f;
    }

    public void SetOutDirecting()
    {
        _rootOutDirecting.SetActive(true);
    }
    
    public void SetInDirecting()
    {
        _rootInDirecting.SetActive(true);
    }
}