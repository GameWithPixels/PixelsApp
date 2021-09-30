
namespace Systemic.Unity.Pixels.Animations
{
    /// <summary>
    /// Animation instance data, refers to an animation preset but stores the instance data and
    /// (derived classes) implements logic for displaying the animation.
    /// </summary>
    public abstract class AnimationInstance
    {
        public IAnimation animationPreset;
        public DataSet.AnimationBits animationBits;
        public int startTime; //ms
        public byte remapFace;
        public bool loop;

        protected DataSet set;

        public AnimationInstance(IAnimation animation, DataSet.AnimationBits bits)
        {
            animationPreset = animation;
            animationBits = bits;
        }

        public virtual void start(int _startTime, byte _remapFace, bool _loop)
        {
            startTime = _startTime;
            remapFace = _remapFace;
            loop = _loop;
        }

        public abstract int updateLEDs(int ms, int[] retIndices, uint[] retColors);
        public abstract int stop(int[] retIndices);
    };
}
