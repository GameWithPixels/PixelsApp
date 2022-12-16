using UnityEngine;

namespace Systemic.Unity.Pixels.Messages
{
    public struct AccelerationFrame
    {
        public float accX;
        public float accY;
        public float accZ;
        public float jerkX;
        public float jerkY;
        public float jerkZ;
        public float smoothAccX;
        public float smoothAccY;
        public float smoothAccZ;
        public float sigma;
        public float faceConfidence;
        public uint time;
        public PixelRollState rollState;
        public byte faceIndex;
        public Vector3 acc { get { return new Vector3(accX, accY, accZ); } }
        public Vector3 jerk { get { return new Vector3(jerkX, jerkY, jerkZ); } }
    };
}
