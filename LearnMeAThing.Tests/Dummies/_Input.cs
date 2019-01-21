using LearnMeAThing.Utilities;

namespace LearnMeAThing.Tests
{
    class _Input : IHardwareInput
    {
        PressedKeys Keys;
        public _Input(PressedKeys keys)
        {
            Keys = keys;
        }

        public PressedKeys GetPressed(int playerId) => Keys;
    }
}
