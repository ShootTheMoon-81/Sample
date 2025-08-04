using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace SoundSource
{
    [RequireComponent(typeof(SoundSourceData))]
    public class SoundSourcePlayer : MonoBehaviour
    {
        [SerializeField]
        private bool _isPlayingOnEnable;

        [SerializeField]
        private float _delay;

        private SoundSourceData _soundSourceData;

        private CancellationTokenSource _cancellationTokenSource;

        private void Awake()
        {
            _soundSourceData = GetComponent<SoundSourceData>();
        }

        private void OnEnable()
        {
            if (_isPlayingOnEnable)
            {
                PlayAsync().Forget();
            }
        }

        private void OnDisable()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        public void Play()
        {
            PlayAsync().Forget();
        }

        private async UniTask PlayAsync()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            if (_delay > 0.0f)
            {
                await UniTask.Delay((int)(_delay * 1000), cancellationToken: _cancellationTokenSource.Token);
            }
            else
            {
                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, _cancellationTokenSource.Token);
            }

            _soundSourceData.Play();

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
        
        public void Stop()
        {
            _soundSourceData.Stop();
        }

        public void Clear()
        {
            SoundManager.Instance.Clear();
        }
    }
}