using UnityEngine;

namespace Systemic.Unity.Pixels.Messages
{
    public struct AccelFrame
    {
        public float accX;
        public float accY;
        public float accZ;
        public float jerkX;
        public float jerkY;
        public float jerkZ;
        public float slowSigma;
        public float fastSigma;
        float faceConfidence;
        int face;
        public uint time;
        public Vector3 acc { get { return new Vector3(accX, accY, accZ); } }
        public Vector3 jerk { get { return new Vector3(jerkX, jerkY, jerkZ); } }
    };
}
