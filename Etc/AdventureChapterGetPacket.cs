using MessageSystem;
using UI.Messages;
using Users;

namespace Network.Packets.Game.Adventure
{
    public class AdventureChapterGetPacket : Packet<AdventureChapterGetReq, AdventureChapterGetAns>
    {
        public override string Endpoint => "game/adventure/chapter/get";
        
        public AdventureChapterGetPacket(string chapterId)
        {
            Request.ChapterId = chapterId;
        }

        protected override void OnCompleted(AdventureChapterGetAns answer)
        {
            if (answer.ErrCode == 0)
            {
                if (answer.AdventureChapter == null)
                {
                    return;
                }

                User.My.AdventureModeInfo.SetAdventureChapter(answer.AdventureChapter);
                
                MessageService.Instance.Publish(AdventureChapterGetEvent.Create(answer.AdventureChapter.Adventures));
            }
            else
            {
#if UNITY_EDITOR
                DebugHelper.Log($"Error Code: {answer.ErrCode}");
                DebugHelper.Log($"Error Message: {answer.ErrMsg}");
#endif
            }
        }
    }
}
