using Sirenix.OdinInspector;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace SoundSource
{
    public class SoundSourceData : MonoBehaviour
    {
        [HideInInspector]
        [SerializeField]
        private string _soundTableGroupId;
        
        [ValueDropdown("CalcSoundTableGroupId")]
        [BoxGroup("Group ID", Order = 0)]
        [PropertyOrder(-1)]
        [ShowInInspector]
        public string SoundTableGroupId
        {
            get => _soundTableGroupId;
            set => _soundTableGroupId = value;
        }
        
#if UNITY_EDITOR
        private static IEnumerable CalcSoundTableGroupId => Data.DataTable.SoundDataTable
            .Where(data => data.SoundChannelType is
                SoundChannelType.BattleSfx or SoundChannelType.UiInteraction or SoundChannelType.UiDirection or SoundChannelType.Voice or SoundChannelType.Bgm)
            .Select(data => new ValueDropdownItem($"{data.SoundChannelType}/{data.SoundGroupID}", data.SoundGroupID));
#endif

        public void Play()
        {
            if (string.IsNullOrEmpty(_soundTableGroupId))
            {
#if UNITY_EDITOR
                DebugHelper.LogError("Can't find SoundTable Group ID");
                return;
#endif
            }
            
            SoundManager.Instance.Play(_soundTableGroupId);
        }

        public void Stop()
        {
            if (string.IsNullOrEmpty(_soundTableGroupId))
            {
#if UNITY_EDITOR
                DebugHelper.LogError("Can't find SoundTable Group ID");
                return;
#endif
            }
            
            SoundManager.Instance.Stop(_soundTableGroupId);
        }

        public void Clear()
        {
            SoundManager.Instance.Clear();
        }
    }
}