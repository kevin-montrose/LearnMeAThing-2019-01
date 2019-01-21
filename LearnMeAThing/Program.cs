using System;

namespace LearnMeAThing
{
    public static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var contentOverride = args.Length > 0 ? args[0] : null;

            using (var game = new LearnMeAThingGame(contentOverride))
            {
                game.Run();
            }
        }
    }
}
