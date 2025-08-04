using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;

using Cysharp.Threading.Tasks;
using Data;
using UI.Overlay;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class SoundManager : MonoSingletonDontDestroyed<SoundManager>
{
    // public enum SoundType
    // {
    //     BGM,
    //     SE,
    //     TOTAL
    // }
    //
    // AudioSource[] audioSources = new AudioSource[(int)SoundType.TOTAL];
    // Dictionary<string, AudioClip> audioClips = new Dictionary<string, AudioClip>();
    //
    // private GameMusicController gameMusicController;
    // private Coroutine crossFadeCoroutine;
    //
    // private float _backGroundVolume = 0.5f;
    // private float _effectVolume = 0.5f;
    //
    // private void Start()
    // {
    //     string[] soundNames = System.Enum.GetNames(typeof(SoundType)); // "BGM", "SE"
    //     for (int i = 0; i < soundNames.Length - 1; i++)
    //     {
    //         GameObject go = new GameObject { name = soundNames[i] };
    //         audioSources[i] = go.AddComponent<AudioSource>();
    //         go.transform.parent = transform;
    //     }
    //
    //     audioSources[(int)SoundType.BGM].loop = true; // bgm 은 루프     
    //
    //     gameMusicController = gameObject.GetOrAddComponent<GameMusicController>();
    //
    //     BackGroundVolume = PlayerPrefs.GetInt(UISystemOptionPopup.OPTION_VOLUME_KEY, 50) * 0.01f;
    //     EffectVolume = PlayerPrefs.GetInt(UISystemOptionPopup.OPTION_VOLUME_KEY, 50) * 0.01f;
    // }
    //
    // public float BackGroundVolume
    // {
    //     get => _backGroundVolume;
    //     set
    //     {
    //         _backGroundVolume = value;
    //         AudioSource audioSource = audioSources[(int)SoundType.BGM];
    //         if (audioSource != null)
    //             audioSource.volume = value;
    //     }
    // }
    //
    // public float EffectVolume
    // {
    //     get => _effectVolume;
    //     set
    //     {
    //         _effectVolume = value;
    //         AudioSource audioSource = audioSources[(int)SoundType.SE];
    //         if (audioSource != null)
    //             audioSource.volume = value;
    //     }
    // }
    //
    // //public float Voluem(SoundType type)
    // //{
    // //    return type == SoundType.BGM ? BackGroundVolume : EffectVolume;
    // //}
    //
    // public async UniTask LoadAsync(string path,Action<AudioClip> callback)
    // {
    //     string filename = SB.Str("Assets/Bundles/", path, ".ogg");
    //
    //     await AssetManager.Instance.LoadAssetAsync<AudioClip>(filename, (ret)=>
    //     {
    //         callback(ret);
    //     });
    // }
    //
    // public void Clear()
    // {
    //     foreach (AudioSource audioSource in audioSources)
    //     {
    //         audioSource.clip = null;
    //         audioSource.Stop();
    //     }
    //
    //     audioClips.Clear();
    // }
    //
    // public void Play(AudioClip audioClip, SoundType type = SoundType.SE, bool loop = false )
    // {
    //     if (audioClip == null)
    //         return;
    //
    //     if (type == SoundType.BGM) // BGM
    //     {
    //         AudioSource audioSource = audioSources[(int)SoundType.BGM];
    //         if (audioSource.isPlaying)
    //             audioSource.Stop();
    //
    //         audioSource.time = 0;
    //         audioSource.loop = loop;
    //         audioSource.clip = audioClip;
    //         audioSource.Play();
    //     }
    //     else // SE
    //     {
    //         AudioSource audioSource = audioSources[(int)SoundType.SE];
    //         if (audioSource == null)
    //         {
    //             Debug.LogError($"SoundType.SE가 audioSources 내에 존재하지 않습니다.");
    //             return;
    //         }
    //
    //         audioSource?.PlayOneShot(audioClip);
    //     }
    // }
    //
    // public void CrossFadeBGM(string path,float fadeTime = 1.0f,bool loop = false)
    // {
    //     if (audioSources[(int)SoundType.BGM].clip != null && audioSources[(int)SoundType.BGM].clip.name == path)
    //         return;
    //
    //     path = SB.Str("sound_bgm", "/", path);
    //     GetOrAddAudioClip(path, (audioClip) =>
    //     {
    //         PlayCrossFadeBGM(audioClip, fadeTime,loop);
    //     }, SoundType.BGM);
    // }
    //
    // public void PlayCrossFadeBGM(AudioClip audioClip, float fTime , bool loop = false)
    // {
    //     float fadeTime = fTime;
    //     if (audioClip != null)
    //     {
    //         AudioSource prevAudioSource = audioSources[(int)SoundType.BGM];
    //
    //         float startTime = gameMusicController.GetMusicTimeStamp(audioClip.name);
    //
    //         if(crossFadeCoroutine != null)
    //             StopCoroutine(crossFadeCoroutine);
    //
    //         crossFadeCoroutine = StartCoroutine(prevAudioSource.CrossFade(
    //             startTime : (startTime < 0 )? 0 : startTime,
    //             newSound: audioClip,
    //             finalVolume: BackGroundVolume,
    //             fadeTime: fadeTime,(prevAudioSource) =>
    //             {
    //                 gameMusicController.SetMusicTimeStamp(prevAudioSource.clip.name, prevAudioSource.time);
    //             }));
    //     }
    //     else
    //     {
    //         AudioSource prevAudioSource = audioSources[(int)SoundType.BGM];
    //
    //         if (crossFadeCoroutine != null)
    //             StopCoroutine(crossFadeCoroutine);
    //
    //         crossFadeCoroutine = StartCoroutine(prevAudioSource.FadeOut(fadeTime));
    //     }
    // }
    //
    // public void Play(string path, SoundType type = SoundType.SE, bool loop = false)
    // {
    //     path = SB.Str((type == SoundType.BGM) ? "sound_bgm" : "sound", "/", path);
    //     GetOrAddAudioClip(path, (audioClip) =>
    //     {
    //         Play(audioClip, type, loop);
    //     },type);
    // }
    //
    // public void Stop(SoundType type)
    // {
    //     AudioSource audioSource = audioSources[(int)type];
    //     if (audioSource != null && audioSource.isPlaying)
    //         audioSource.Stop();
    // }
    //
    // private void GetOrAddAudioClip(string path, Action<AudioClip> callback = null, SoundType type = SoundType.SE)
    // {
    //
    //     AudioClip audioClip = null;
    //
    //     if (type == SoundType.BGM) // BGM
    //     {
    //         _ = LoadAsync(path, (clip) =>
    //           {
    //               audioClip = clip;
    //
    //               callback?.Invoke(audioClip);
    //           });
    //     }
    //     else // SE
    //     {
    //         if (audioClips.TryGetValue(path, out audioClip) == false)
    //         {
    //             _ = LoadAsync(path, (clip) =>
    //             {
    //                 audioClip = clip;
    //
    //                 audioClips.Add(path, audioClip);
    //
    //                 callback?.Invoke(audioClip);
    //             });
    //         }
    //         else
    //             callback?.Invoke(audioClip);
    //     }
    // }

    #region UI Interaction 큐.
    private List<SoundData> _uiInteractions = new();
    private string _touchSoundName;
    #endregion

    #region 사운드 시스템 개편.
    AudioSource[] audioSources = new AudioSource[(int)SoundChannelType.Max];
    Dictionary<string, AudioClip> audioClips = new();

    private GameMusicController gameMusicController;
    private Coroutine crossFadeCoroutine;

    // private float _backGroundVolume = 0.5f;
    // private float _effectVolume = 0.5f;
    private float _soundVolume = 1.0f;

    private void Awake()
    {
        string[] soundNames = Enum.GetNames(typeof(SoundChannelType)); // "BGM", "SE"
        for (int i = 0; i < soundNames.Length - 1; i++)
        {
            GameObject go = new GameObject { name = soundNames[i] };
            audioSources[i] = go.AddComponent<AudioSource>();
            go.transform.parent = transform;
        }

        audioSources[(int)SoundChannelType.Bgm].loop = true; // bgm 은 루프     

        gameMusicController = gameObject.GetOrAddComponent<GameMusicController>();

        // BackGroundVolume = PlayerPrefs.GetInt(UISystemOptionPopup.OPTION_VOLUME_KEY, 50) * 0.01f;
        // EffectVolume = PlayerPrefs.GetInt(UISystemOptionPopup.OPTION_VOLUME_KEY, 50) * 0.01f;
        SoundVolume = PlayerPrefs.GetInt(UISystemOptionPopup.OPTION_VOLUME_KEY, 50) * 0.01f;
        
        //Instance.UpdateLoopAsync().Forget();
    }

    public float SoundVolume
    {
        get => _soundVolume;
        set
        {
            for (int i = 0; i < audioSources.Length; i++)
            {
                audioSources[i].volume = _soundVolume = value;
            }
        }
    }

    // public float BackGroundVolume
    // {
    //     get => _backGroundVolume;
    //     set
    //     {
    //         _backGroundVolume = value;
    //         AudioSource audioSource = audioSources[(int)SoundChannelType.Bgm];
    //         if (audioSource != null)
    //             audioSource.volume = value;
    //     }
    // }
    //
    // public float EffectVolume
    // {
    //     get => _effectVolume;
    //     set
    //     {
    //         _effectVolume = value;
    //         AudioSource audioSource = audioSources[(int)SoundChannelType.BattleSfx];
    //         if (audioSource != null)
    //             audioSource.volume = value;
    //     }
    // }

    #region UI Interaction 큐.
    // private async UniTaskVoid UpdateLoopAsync()
    // {
    //     while (IsInstanceExists == true)
    //     {
    //         await UniTask.Yield(PlayerLoopTiming.LastUpdate);
    //         PlayUiInteraction();
    //     }
    // }
    //
    // private void PlayUiInteraction()
    // {
    //     for (int i = 0; i < _uiInteractions.Count; i++)
    //     {
    //         if (_uiInteractions[i].SoundGroupID.Contains("Touch", StringComparison.OrdinalIgnoreCase) == false)
    //         {
    //             //_uiInteractions.Remove(_uiInteractions[i]);
    //             //break;
    //
    //             Play(
    //                 SoundDataTable.GetResourcePath(_uiInteractions[i]),
    //                 _uiInteractions[i].SoundChannelType,
    //                 _uiInteractions[i].LoopChk,
    //                 _uiInteractions[i].Volume);
    //         }
    //     }
    //     
    //     _uiInteractions.Clear();
    // }
    #endregion

    #region 외부에서 직접 호출
    public async UniTask PreLoad(string soundTableGroupId, Action finishAction = null)
    {
        SoundData tableData = DataTable.SoundDataTable.GetTableData(soundTableGroupId);
        
        if (tableData != null)
        {
            if (tableData.SoundChannelType != SoundChannelType.Bgm)
            {
                string path = SoundDataTable.GetResourcePath(tableData);

                bool isExist = (await AssetManager.Exists(path));
                if (isExist == false)
                {
                    DebugHelper.LogError($"Sound Load 실패, {path}의 경로가 존재하지 않습니다.");
                    return; 
                }

                if (audioClips.ContainsKey(path) == false)
                {
                    var audioClip = await Addressables.LoadAssetAsync<AudioClip>(path);

                    if (audioClips.TryAdd(path, audioClip) == false)
                    {
                        Addressables.Release(audioClip);
                    }
                    
                    finishAction?.Invoke();
                }
            }
        }
        else
        {
#if UNITY_EDITOR
            DebugHelper.LogError($"사운드 테이블을 찾을 수 없습니다. GroupId : {soundTableGroupId}");
#endif
        }
    }
    
    public float Play(string soundTableGroupId)
    {
        SoundData tableData = DataTable.SoundDataTable.GetTableData(soundTableGroupId);

        if (tableData == null)
        {
#if UNITY_EDITOR
            DebugHelper.LogError($"사운드 테이블을 찾을 수 없습니다. GroupId : {soundTableGroupId}");
#endif
            return 0.0f;
        }

        string path = SoundDataTable.GetResourcePath(tableData);
        
        switch (tableData.SoundChannelType)
        {
            case SoundChannelType.Bgm:
                {
                    CrossFadeBGM(path, 0.3f, tableData.LoopChk, tableData.Volume);
                }
                break;
            case SoundChannelType.UiInteraction:
            case SoundChannelType.None:
            case SoundChannelType.BattleSfx:
            case SoundChannelType.UiDirection:
            case SoundChannelType.Voice:
            case SoundChannelType.Max:
                {
                    Play(path, tableData.SoundChannelType, tableData.LoopChk, tableData.Volume);
                }
                break;
        }

        if (audioClips.ContainsKey(path))
        {
            return audioClips[path]?.length ?? 0.0f;
        }
        else
        {
            return 0.0f;
        }
    }

    public float TouchPlay(string path)
    {
        Play(path, SoundChannelType.UiInteraction, false, 1);

        if (audioClips.ContainsKey(path))
        {
            return audioClips[path]?.length ?? 0.0f;
        }
        else
        {
            return 0.0f;
        }
    }
    
    public void Stop(string soundTableGroupId)
    {
        SoundData tableData = DataTable.SoundDataTable.GetTableData(soundTableGroupId);
        
        if (tableData == null)
        {
#if UNITY_EDITOR
            DebugHelper.LogError($"사운드 테이블을 찾을 수 없습니다. GroupId : {soundTableGroupId}");
#endif
            return;
        }
        
        Stop(tableData.SoundChannelType);
    }

    public void Stop(SoundChannelType type)
    {
        AudioSource audioSource = audioSources[(int)type];
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }
    
    public void Clear()
    {
        foreach (AudioSource audioSource in audioSources)
        {
            audioSource.clip = null;
            audioSource.Stop();
        }

        audioClips.Clear();
    }

    public void Clear(string soundTableGroupId)
    {
        SoundData tableData = DataTable.SoundDataTable.GetTableData(soundTableGroupId);
        
        if (tableData == null)
        {
#if UNITY_EDITOR
            DebugHelper.LogError($"사운드 테이블을 찾을 수 없습니다. GroupId : {soundTableGroupId}");
#endif
            return;
        }
        
        audioSources[(int)tableData.SoundChannelType].Stop();
        audioSources[(int)tableData.SoundChannelType].clip = null;

        if (audioClips.ContainsKey(soundTableGroupId))
        {
            Stop(soundTableGroupId);
            
            Addressables.Release(audioClips[soundTableGroupId]);
            
            audioClips.Remove(soundTableGroupId);
        }
    }
    
    public void Play(AudioClip audioClip, SoundChannelType type = SoundChannelType.BattleSfx, bool loop = false ,float volume = 1.0f)
    {
        if (audioClip == null)
            return;

        if (type == SoundChannelType.Bgm) // BGM
        {
            AudioSource audioSource = audioSources[(int)SoundChannelType.Bgm];
            if (audioSource.isPlaying)
                audioSource.Stop();

            audioSource.time = 0;
            audioSource.loop = loop;
            audioSource.clip = audioClip;
            audioSource.volume = SoundVolume * volume;
            audioSource.Play();
        }
        else // SE
        {
            AudioSource audioSource = audioSources[(int)type];
            if (audioSource == null)
            {
#if UNITY_EDITOR
                Debug.LogError($"SoundType.SE가 audioSources 내에 존재하지 않습니다.");
#endif
                return;
            }

            audioSource.loop = loop;
            audioSource.volume = SoundVolume * volume;

            // FIXME: 임시 로직.
            // Touch 사운드만 AudioSource.Clip에 넣어 Stop 할 수 있게 함. Touch 사운드를 구별하는 방법이 현재는 스트링뿐.
            // 그 외 Sfx는 PlayOneShot.
            if (string.IsNullOrEmpty(_touchSoundName))
            {
                if (audioClip.name.Contains("Touch", StringComparison.OrdinalIgnoreCase))
                {
                    _touchSoundName = audioClip.name;
                }
            }

            if (string.Equals(audioClip.name, _touchSoundName))
            {
                audioSource.clip = audioClip;
                audioSource.Play();
            }
            else
            {
                if (audioSource.isPlaying && string.Equals(audioClip.name, _touchSoundName))
                {
                    audioSource.Stop();
                }
                
                audioSource?.PlayOneShot(audioClip);
            }
        }
    }
    #endregion
    
    private async UniTask LoadAsync(string path, Action<AudioClip> callback)
    {
        //string filename = SB.Str("Assets/Bundles/", path, ".ogg");

        await AssetManager.Instance.LoadAssetAsync<AudioClip>(path, (ret)=>
        {
            callback(ret);
        });
    }

    private void CrossFadeBGM(string path, float fadeTime = 1.0f, bool loop = false, float volume = 1.0f)
    {
        if (audioSources[(int)SoundChannelType.Bgm].clip != null)
        {
            if (path.Contains(audioSources[(int)SoundChannelType.Bgm].clip.name, StringComparison.OrdinalIgnoreCase) &&
                audioSources[(int)SoundChannelType.Bgm].isPlaying)
            {
                return;
            }
        }

        //path = SB.Str("sound_bgm", "/", path);
        GetOrAddAudioClip(path, (audioClip) =>
        {
            PlayCrossFadeBGM(audioClip, fadeTime, loop, volume);
        }, SoundChannelType.Bgm);
    }

    private void PlayCrossFadeBGM(AudioClip audioClip, float fTime , bool loop = false, float volume = 1.0f)
    {
        float fadeTime = fTime;
        
        AudioSource prevAudioSource = audioSources[(int)SoundChannelType.Bgm];

        if (prevAudioSource.clip == null)
        {
            Play(audioClip, SoundChannelType.Bgm, true, volume);
            return;
        }
        
        if (audioClip != null)
        {
            float startTime = gameMusicController.GetMusicTimeStamp(audioClip.name);

            if(crossFadeCoroutine != null)
                StopCoroutine(crossFadeCoroutine);

            if (prevAudioSource.clip == null)
            {
                crossFadeCoroutine = StartCoroutine(prevAudioSource.CrossFade(
                    startTime : (startTime < 0 )? 0 : startTime,
                    newSound: audioClip,
                    finalVolume: SoundVolume * volume,
                    fadeTime: fadeTime,(prevAudioSource) =>
                    {
                        gameMusicController.SetMusicTimeStamp(prevAudioSource.clip.name, prevAudioSource.time);
                    }));
            }
            else
            {
                crossFadeCoroutine = StartCoroutine(prevAudioSource.CrossFade(
                    startTime : (startTime < 0 )? 0 : startTime,
                    newSound: audioClip,
                    finalVolume: SoundVolume * volume,
                    fadeTime: fadeTime,(prevAudioSource) =>
                    {
                        gameMusicController.SetMusicTimeStamp(prevAudioSource.clip.name, prevAudioSource.time);
                    }));
            }
        }
        else
        {
            if (crossFadeCoroutine != null)
                StopCoroutine(crossFadeCoroutine);

            crossFadeCoroutine = StartCoroutine(prevAudioSource.FadeOut(fadeTime));
        }
    }

    private void Play(string path, SoundChannelType type = SoundChannelType.BattleSfx, bool loop = false, float volume = 1.0f)
    {
        //path = SB.Str((type == SoundChannelType.Bgm) ? "sound_bgm" : "sound", "/", path);
        GetOrAddAudioClip(path, (audioClip) =>
        {
            Play(audioClip, type, loop, volume);
        },type);
    }

    private void GetOrAddAudioClip(string path, Action<AudioClip> callback = null, SoundChannelType type = SoundChannelType.BattleSfx)
    {
        AudioClip audioClip = null;

        if (type == SoundChannelType.Bgm) // BGM
        {
            _ = LoadAsync(path, (clip) =>
              {
                  audioClip = clip;

                  callback?.Invoke(audioClip);
              });
        }
        else // SE//
        {
            if (audioClips.TryGetValue(path, out audioClip) == false)
            {
                _ = LoadAsync(path, (clip) =>
                {
                    audioClip = clip;

                    audioClips.Add(path, audioClip);

                    callback?.Invoke(audioClip);
                });
            }
            else
                callback?.Invoke(audioClip);
        }
    }
    #endregion
}

public static class AudioSourceExtensions
{
    public static IEnumerator CrossFade(
        this AudioSource audioSource,
        float startTime,
        AudioClip newSound,
        float finalVolume,
        float fadeTime,Action<AudioSource> onChanged)
    {
        // 페이드 아웃 후에 새로운 BGM 페이드인 
        yield return FadeOut(audioSource, fadeTime, onChanged);
        audioSource.clip = newSound;
        yield return FadeIn(audioSource, startTime,fadeTime, finalVolume);
    }

    public static IEnumerator FadeOut(this AudioSource audioSource, float fadeTime, Action<AudioSource> onChanged = null)
    {
        float startVolume = audioSource.volume;
        while (audioSource.volume > 0)
        {
            audioSource.volume -= startVolume * Time.unscaledDeltaTime / fadeTime;
            yield return null;
        }
        onChanged(audioSource);
        audioSource.Stop();
        audioSource.volume = 0;
        audioSource.time = 0;
    }
    public static IEnumerator FadeIn(this AudioSource audioSource, float startTime, float fadeTime, float finalVolume)
    {
        float startVolume = 0.2f;
        audioSource.time = startTime;
        audioSource.volume = 0;
        audioSource.Play();
        while (audioSource.volume < finalVolume)
        {
            audioSource.volume += startVolume * Time.unscaledDeltaTime / fadeTime;
            yield return null;
        }
        audioSource.volume = finalVolume;
    }
}