using UnityEngine;

namespace LoneTruth.Combat
{
    /// <summary>
    /// Marks a scene (or a part of it, while this object is active) as a place where Holt may
    /// aim and shoot. Everywhere else aiming is refused with a line.
    /// </summary>
    public class CombatZone : MonoBehaviour
    {
        private static int activeCount;

        public static bool WeaponsAllowed => activeCount > 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            activeCount = 0;
        }

        private void OnEnable()
        {
            activeCount++;
        }

        private void OnDisable()
        {
            activeCount--;
        }
    }
}
