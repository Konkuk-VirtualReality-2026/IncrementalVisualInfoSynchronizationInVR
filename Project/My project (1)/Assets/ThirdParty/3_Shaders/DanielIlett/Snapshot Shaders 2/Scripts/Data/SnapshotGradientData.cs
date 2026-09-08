using UnityEngine;

namespace DanielIlett.SnapshotShaders2.URP
{
    public class SnapshotGradientData : ScriptableObject
    {
        public Gradient gradient;

        public SnapshotGradientData()
        {
            gradient = new Gradient();

            gradient.colorKeys = new GradientColorKey[2]
            {
                new GradientColorKey(Color.black, 0.0f),
                new GradientColorKey(Color.white, 1.0f)
            };

            gradient.mode = GradientMode.PerceptualBlend;
        }
    }
}
