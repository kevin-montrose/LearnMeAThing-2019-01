using LearnMeAThing.Managers;

namespace LearnMeAThing.Tests
{
    sealed class _IIdIssuer : IIdIssuer
    {
        private int NextId;
        public _IIdIssuer()
        {
            NextId = 1;
        }

        public int GetNextId()
        {
            var ret = NextId;
            NextId++;

            return ret;
        }
    }
}
