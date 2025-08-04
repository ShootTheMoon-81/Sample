using Cysharp.Threading.Tasks;
using Utils;

namespace GameModes.Transitions
{
    public class ScreenFadeTransition : Transition
    {
        public override UniTask Out() => ScreenFader.FadeOut(0.3f);
        
        public override UniTask In() => ScreenFader.FadeIn(0.3f);
    }
}