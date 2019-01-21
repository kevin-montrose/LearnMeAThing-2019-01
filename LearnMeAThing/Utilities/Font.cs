using System;

namespace LearnMeAThing.Utilities
{
    /// <summary>
    /// Helpers for dealing with the sprite fonts
    ///   we're forced to use.
    /// </summary>
    static class Font
    {
        public const int CHARACTER_WIDTH = 8 * 2;
        public const int CHARACTER_HEIGHT = 16 * 2;
        
        public static void GetBounds(char c, out int x, out int y)
        {
            y = 0;
            if(c >= 'A' && c <= 'Z')
            {
                x = ((0 + (c - 'A')) * 8) * 2;
                return;
            }

            if(c >= 'a' && c <= 'z')
            {
                x = ((26 + (c - 'a')) * 8) * 2;
                return;
            }

            if(c >= '0' && c <= '9')
            {
                x = ((52 + (c - '0')) * 8) * 2;
                return;
            }

            switch (c)
            {
                case ' ': x = 62 * 8 * 2; return;
                case '-': x = 63 * 8 * 2; return;
                case '.': x = 64 * 8 * 2; return;
                case ',': x = 65 * 8 * 2; return;
                case '!': x = 66 * 8 * 2; return;
                case '?': x = 67 * 8 * 2; return;
                case '\'': x = 68 * 8 * 2; return;
                case ':': x = 69 * 8 * 2; return;
            }

            throw new InvalidOperationException($"Unexpected charater: {c}");
        }
    }
}
