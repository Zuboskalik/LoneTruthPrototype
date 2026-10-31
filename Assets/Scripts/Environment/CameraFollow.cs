using UnityEngine;

namespace LoneTruth.Environment
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Follow")]
        [SerializeField] private float smoothTime = 0.2f;
        [SerializeField] private float maxSpeed = 20f;

        [Header("Bounds")]
        [SerializeField] private bool useBounds = true;
        [SerializeField] private float minX = -10f;
        [SerializeField] private float maxX = 10f;

        // A jump of the target this far in one frame is a spawn or a teleport, not a walk.
        private const float TeleportDistance = 3f;

        private Vector3 velocity = Vector3.zero;
        private bool snapNext = true; // the first frame of the scene: start on the player
        private float lastTargetX;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void SetBounds(float newMinX, float newMaxX)
        {
            minX = newMinX;
            maxX = newMaxX;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                // The scene's own player is replaced by the persistent one after a scene load.
                GameObject player = LoneTruth.Player.PersistentPlayer.Current;
                if (player == null)
                {
                    return;
                }

                target = player.transform;
                snapNext = true;
            }

            float targetX = target.position.x;

            if (useBounds)
            {
                targetX = Mathf.Clamp(targetX, minX, maxX);
            }

            Vector3 desiredPosition = new Vector3(targetX, transform.position.y, transform.position.z);

            // After a scene transition (or a teleport) the camera starts on the player instead of
            // flying across the location to him.
            if (snapNext || Mathf.Abs(target.position.x - lastTargetX) > TeleportDistance)
            {
                snapNext = false;
                velocity = Vector3.zero;
                transform.position = desiredPosition;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime, maxSpeed);
            }

            lastTargetX = target.position.x;
        }

        private void OnDrawGizmosSelected()
        {
            if (!useBounds)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Vector3 top = transform.position + Vector3.up * 5f;
            Vector3 bottom = transform.position + Vector3.down * 5f;

            Gizmos.DrawLine(new Vector3(minX, bottom.y, transform.position.z), new Vector3(minX, top.y, transform.position.z));
            Gizmos.DrawLine(new Vector3(maxX, bottom.y, transform.position.z), new Vector3(maxX, top.y, transform.position.z));
        }
    }
}
