using System;
namespace LearnMeAThing.Components
{
    /// <summary>
    /// A "component" an entity may have that is purely binary,
    ///   they either have it or they don't there is no other state
    /// </summary>
    [Flags]
    enum FlagComponent : ulong
    {
        NONE = 0,

        Player = 1,
        Camera = 2,

        Flower = 4,

        // it is important that these compare such that floor < middle < top < ceiling
        //   althrough the exact numbers don't matter
        Level_Floor = 8,
        Level_Middle = 16,
        Level_Top = 32,
        Level_Ceiling = 64,

        // these two are probably going to grow some state, but can be flags for now
        TakesDamage = 128,
        DealsDamage = 256,

        CullAfterTransition = 512,

        DropShadow = 1024
    }

    /// <summary>
    /// Represents a collection of flag components.
    /// </summary>
    struct FlagComponents
    {
        private ulong SetFlags;

        public bool HasFlag(FlagComponent f) => (SetFlags & (ulong)f) != 0;
        public void SetFlag(FlagComponent f) => SetFlags |= (ulong)f;
        public void ClearFlag(FlagComponent f) => SetFlags &= ~(ulong)f;
    }
}
