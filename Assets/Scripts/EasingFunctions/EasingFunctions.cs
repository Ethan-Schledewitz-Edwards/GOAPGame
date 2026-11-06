using UnityEngine;

namespace Math.Core
{
    public static class EasingFunctions
    {
        public static float EaseOutQuart(float lerpPercentage)
        {
            return 1 - Mathf.Pow(1 - lerpPercentage, 4);
        }
    }   
}
