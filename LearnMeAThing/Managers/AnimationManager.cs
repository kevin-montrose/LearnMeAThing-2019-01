using LearnMeAThing.Assets;
using System;
using System.IO;

namespace LearnMeAThing.Managers
{
    interface IAnimationManager
    {
        AnimationTemplate Get(AnimationNames names);
    }

    sealed class AnimationManager: IAnimationManager
    {
        public string AnimationPath { get; private set; }

        AnimationTemplate[] Templates;

        public AnimationManager(string animationPath)
        {
            AnimationPath = animationPath;
        }

        public AnimationTemplate Get(AnimationNames names) => Templates[(int)names];
        
        public void Initialize()
        {
            Templates = LoadAllTemplates(AnimationPath);
        }

        public void Reload()
        {
            Templates = LoadAllTemplates(AnimationPath);
        }

        private static AnimationTemplate[] LoadAllTemplates(string path)
        {
            var max = 0;
            foreach(AnimationNames name in Enum.GetValues(typeof(AnimationNames)))
            {
                var asInt = (int)name;
                if(asInt > max)
                {
                    max = asInt;
                }
            }

            var ret = new AnimationTemplate[max + 1];
            foreach(var file in Directory.EnumerateFiles(path, "*.txt"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (!Enum.TryParse<AnimationNames>(name, ignoreCase: true, result: out var parsedName)) continue;

                var template = LoadTemplate(parsedName, file);

                ret[(int)parsedName] = template;
            }

            return ret;
        }

        private static AnimationTemplate LoadTemplate(AnimationNames name, string file)
        {
            var text = File.ReadAllText(file);
            var parts = text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) throw new InvalidOperationException("Couldn't load animation, insufficient parts");

            if (!int.TryParse(parts[0], out var steps)) throw new InvalidOperationException($"Couldn't parse step for animation, expected integer, found: {parts[0]}");
            if(steps < 0) throw new InvalidOperationException("Steps for animation is < 0");

            var frames = new AssetNames[4];
            var frameIx = 0;

            for (var i = 1; i < parts.Length; i++)
            {
                var frame = parts[i];
                if (!Enum.TryParse<AssetNames>(frame, ignoreCase: true, out var parsedFrame)) throw new InvalidOperationException($"Couldn't parse animation frame, found: {frame}");

                if(frameIx == frames.Length)
                {
                    Array.Resize(ref frames, frames.Length * 2);
                }

                frames[frameIx] = parsedFrame;
                frameIx++;
            }

            Array.Resize(ref frames, frameIx);

            return new AnimationTemplate(name, frames, steps);
        }
    }
}
