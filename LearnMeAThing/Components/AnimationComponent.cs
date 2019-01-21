using LearnMeAThing.Assets;
using LearnMeAThing.Managers;

namespace LearnMeAThing.Components
{
    /// <summary>
    /// Represents an animation, will loop through indefintely
    /// </summary>
    sealed class AnimationComponent : AStatefulComponent
    {
        public override ComponentType Type => ComponentType.Animation;
        
        public AnimationNames Name { get; private set; }
        
        public uint TickCounter { get; private set; }
        
        public void Initialize(IAnimationManager manager, AnimationNames name, int startFrame)
        {
            Name = name;
            TickCounter = (uint)(startFrame * manager.Get(name).StepAfter);
        }

        /// <summary>
        /// Returns the current frame for the given animation, using a layer of 
        ///   indirection to support hot reloading.
        /// </summary>
        public AssetNames GetCurrentFrame(IAnimationManager manager)
        {
            var template = manager.Get(Name);
            if(template.StepAfter == 0 || template.Frames.Length == 1)
            {
                return template.Frames[0];
            }

            var frameCount = TickCounter / template.StepAfter;
            var frameIx = frameCount % template.Frames.Length;

            return template.Frames[frameIx];
        }

        /// <summary>
        /// Call to advance the animation state.
        /// 
        /// Note that an animation doesn't necessarily change on every frame, 
        ///    so this isn't guaranteed to change anything.
        /// </summary>
        public void Advance()
        {
            TickCounter++;
        }

        public void SwitchTo(AnimationNames animation)
        {
            Name = animation;
            TickCounter = 0;
        }

        public override string ToString() => Name.ToString();
    }
}
