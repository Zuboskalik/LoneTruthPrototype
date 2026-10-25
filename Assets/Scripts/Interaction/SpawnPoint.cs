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

            GameObject playerObject = PersistentPlayer.Current;
            if (playerObject == null)
            {
                return;
            }

            Transform player = playerObject.transform;

            Vector3 target = new Vector3(transform.position.x, player.position.y, player.position.z);
            Rigidbody rb = playerObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = target;
            }

            player.position = target;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}
