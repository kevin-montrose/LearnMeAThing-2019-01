using LearnMeAThing.Assets;
using LearnMeAThing.Managers;

namespace LearnMeAThing.Tests
{
    class _AnimationManager : IAnimationManager
    {
        private readonly (AnimationNames Name, AnimationTemplate Template)[] Responses;
        public _AnimationManager(params (AnimationNames Name, AnimationTemplate Template)[] responses)
        {
            Responses = responses;
        }

        public AnimationTemplate Get(AnimationNames name)
        {
            if (Responses == null) return default;

            foreach(var item in Responses)
            {
                if (item.Name == name) return item.Template;
            }

            return default;
        }
    }
}
