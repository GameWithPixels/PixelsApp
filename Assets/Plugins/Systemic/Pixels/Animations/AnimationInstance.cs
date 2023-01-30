
namespace Systemic.Unity.Pixels.Animations
{
    /// <summary>
    /// Animation instance data, refers to an animation preset but stores the instance data and
    /// (derived classes) implements logic for displaying the animation.
    /// </summary>
    public abstract class AnimationInstance
    {
        public IAnimationPreset animationPreset
        {
            get; private set;
        }
        public DataSet.AnimationBits animationBits
        {
            get; private set;
        }
        public int startTime
        {
            get; private set;
        }

        protected DataSet set;

        public AnimationInstance(IAnimationPreset preset, DataSet.AnimationBits bits)
        {
            animationPreset = preset;
            animationBits = bits;
        }

        public virtual void start(int _startTime)
        {
            startTime = _startTime;
        }

        public abstract int updateLEDs(int ms, int[] retIndices, uint[] retColors);
        public abstract int stop(int[] retIndices);
    };
}
