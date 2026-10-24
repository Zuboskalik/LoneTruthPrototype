using LoneTruth.Player;
using UnityEngine;

namespace LoneTruth.Interaction
{
    /// <summary>
    /// Where the player appears after a SceneTransitionDoor. The scene's player starts at its
    /// default position; if a transition set PendingSpawnId, the player is moved here instead.
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
        public static string PendingSpawnId;

        [SerializeField] private string spawnId = "from_1-01";

        public string SpawnId => spawnId;

        private void Start()
        {
            if (string.IsNullOrEmpty(PendingSpawnId) || PendingSpawnId != spawnId)
            {
                return;
            }

            PendingSpawnId = null;

            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player == null)
            {
                return;
            }

            Vector3 target = new Vector3(transform.position.x, player.transform.position.y, player.transform.position.z);
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = target;
            }

            player.transform.position = target;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}
