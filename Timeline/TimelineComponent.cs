using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using Cinemachine;
using UniRx;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using CriTimeline.Mana;
using Managers;

public interface ITimelinePlay
{
    void Play(BaseActor actor, Vector3 offset, Action callback = null);

    void Play(Action pauseCallBack, Action finishCallBack, bool isSoulCapture);
}

public class TimelineComponent : MonoBehaviour, ITimelinePlay
{
    protected IngameGameView _gameView;
    protected PlayableDirector _playableDirector;
    protected BaseActor timelineActor;

    protected CinemachineVirtualCamera Cam;
    protected GameObject cameraTarget;

    protected AnimationTrack camAnimTrack;

    protected CriManaMovieControllerForUI _movieController;

    private void Awake()
    {
        _playableDirector = GetComponent<PlayableDirector>();
        if (_playableDirector != null)
        {
            _playableDirector.stopped += OnStopDirector;
            _playableDirector.paused += OnPauseDirector;
        }

        //Cam = CameraManager.Instance.GetCamera(CameraManager.CameraType.BattleAction);
        //activeTarget = CameraManager.Instance.targetBattleActive;
    }

    public virtual void Initialize(IngameGameView v)
    {
        _gameView = v;
    }

    protected void InitTrack(CameraManager.CameraType type)
    {
        CinemachineVirtualCamera cinemachineCam = CameraManager.Instance.GetCamera(type);

        if (_playableDirector != null && _playableDirector.playableAsset != null)
        {
            TimelineAsset timeline = _playableDirector.playableAsset as TimelineAsset;
            if (timeline != null)
            {
                foreach (var track in timeline.GetOutputTracks())
                {
                    switch (track)
                    {
                        case AnimationTrack:
                            {
                                //_pd.SetGenericBinding(track, timelineCamAnimator);
                            }
                            break;
                        case CameraZoomTrack:
                            {
                                _playableDirector.SetGenericBinding(track, cinemachineCam);
                            }
                            break;
                        case ScreenFadeTrack:
                            {
                                if (track.name.Contains("Front"))
                                    _playableDirector.SetGenericBinding(track, CameraManager.Instance.GetBlackPanelController().GetBlackPanel(true));
                                else if (track.name.Contains("Back"))
                                    _playableDirector.SetGenericBinding(track, CameraManager.Instance.GetBlackPanelController().GetBlackPanel());
                                else
                                    _playableDirector.SetGenericBinding(track, CameraManager.Instance.GetBlackPanelController().GetBlackPanel());
                            }
                            break;
                        case CameraShakeTrack:
                            {
                                _playableDirector.SetGenericBinding(track, cinemachineCam);
                            }
                            break;
                        case ChromaticAberrationTrack:
                            {
                                _playableDirector.SetGenericBinding(track, GlobalVolumeRoot.Instance.GlobalVolume);

                                GlobalVolumeRoot.Instance.GlobalVolumeChromaticAberration(true);

                            }
                            break;
                        case LensDistortionTrack:
                            {
                                _playableDirector.SetGenericBinding(track, GlobalVolumeRoot.Instance.GlobalVolume);

                                GlobalVolumeRoot.Instance.GlobalVolumeLensDistortion(true);
                            }
                            break;
                        case ZoomBlurTrack:
                            {
                                _playableDirector.SetGenericBinding(track, GlobalVolumeRoot.Instance.GlobalVolume);

                                GlobalVolumeRoot.Instance.GlobalVolumeZoomBlur(true);
                            }
                            break;
                        case ActiveCutScenePlayTrack:
                            {
                                _playableDirector.SetGenericBinding(track, this);
                            }
                            break;
                        case CriManaTrack:
                            {
                                _movieController = gameObject.GetOrAddComponent<CriManaMovieControllerForUI>();
                                if (_movieController)
                                {
                                    _movieController.player.statusChangeCallback = EndCallback;

                                    //movieController.renderMode = CriManaMovieMaterial.RenderMode.Always;
                                    _movieController.restartOnEnable = true;
                                    _movieController.target = _gameView.CutSceneImage;
                                    _movieController.player.SetSpeed(TimeScaleManager.Instance.CurrGameSpeed);

                                    _playableDirector.SetGenericBinding(track, _movieController);
                                }
                            }
                            break;
                    }
                }

                foreach (var g in timeline.GetRootTracks())
                {
                    if (g is not GroupTrack)
                        continue;

                    // Group 이름이 Camera 인 경우 
                    if (string.Equals(g.name, "Camera") == false)
                        continue;

                    foreach (var track in g.GetChildTracks())
                    {
                        if (track is AnimationTrack)
                        {
                            GameObject targetObj = CameraManager.Instance.GetTargetObject(type);
                            Animator animator = targetObj.GetComponent<Animator>();
                            _playableDirector.SetGenericBinding(track, animator);
                        }
                    }
                }
            }
        }
    }

    private void EndCallback(CriMana.Player.Status status)
    {
        switch (status)
        {
            case CriMana.Player.Status.Prep:
                {
                    //if (_movieController.player.movieInfo != null)
                    //{
                    //    RawImageAspectFitter fitter = sceneImage.gameObject.GetOrAddComponent<RawImageAspectFitter>();
                    //    fitter.AdjustAspect(_movieController.player.movieInfo.width, _movieController.player.movieInfo.height);
                    //}
                }
                break;
            case CriMana.Player.Status.StopProcessing:
                {
                    _gameView.CutSceneImage.enabled = false;
                }
                break;
        }
    }

    public virtual void Play(BaseActor actor, Vector3 offset, Action callback = null) { }

    public virtual void Play(Action pauseCallBack, Action finishCallBack, bool isSoulCapture) { }

    public virtual void PlayActiveCutScene(string path) { }

    public virtual void StopActiveCutScene() { }

    protected virtual void OnStopDirector(PlayableDirector pd) { }

    protected virtual void OnPauseDirector(PlayableDirector pd) { }

}
