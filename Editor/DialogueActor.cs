using Cysharp.Threading.Tasks;
using Data;
using Dialogue.Components;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Dialogue
{
    public class DialogueActor : DialogueObject
    {
        private string _currentActorId;

        public string CurrentActorId => _currentActorId;

        public DialogueActorCharacter CurrentActor => _currentActorId != null ? Get<DialogueActorCharacter>(_currentActorId) : null;
        
        // public Dictionary<string, GameObject> LoadedActors { get; } = new(StringComparer.OrdinalIgnoreCase);
        //
        // public string CurrentActorId { get; set; }
        // public GameObject CurrentActor => string.IsNullOrEmpty(CurrentActorId) ? null : LoadedActors[CurrentActorId];
        //
        // public string PreviousActorId { get; set; }
        // public GameObject PreviousActor => string.IsNullOrEmpty(PreviousActorId) ? null : LoadedActors[PreviousActorId];
        //
        public async UniTask Load(string actorId)
        {
            var actorData = DataTable.DialogueActorDataTable[actorId];
            var actor = await Add<DialogueActorCharacter>(actorId, actorData.PrefabPath);
            
            if (actor is null)
            {
                DebugHelper.LogError($"Can't find actor resource: {actorData.PrefabPath}");
                return;
            }
            
            actor.Init();
        }

        public void Change(string actorId)
        {
            _currentActorId = actorId;
            var actor = CurrentActor;
            actor.transform.SetParent(transform, false);
            actor.transform.localPosition = Vector3.zero;
            actor.transform.localScale = Vector3.one;
            actor.gameObject.SetActive(true);
        }

        public override void Release()
        {
            base.Release();

            _currentActorId = null;
        }
        //     foreach (var actor in LoadedActors.Values)
        //     {
        //         Addressables.ReleaseInstance(actor);
        //     }
        //     
        //     LoadedActors.Clear();
        //
        //     PreviousActorId = CurrentActorId = string.Empty;
        //     
        //     base.Release();
        // }
    }
}