using LoneTruth.Inventory;
using LoneTruth.Player;
using UnityEngine;

namespace LoneTruth.Combat
{
    [RequireComponent(typeof(InventoryManager))]
    public class WeaponController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InventoryManager inventoryManager;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private Transform firePoint;

        [Header("Weapon")]
        [SerializeField] private ItemSO ammoItem;
        [SerializeField] private ItemType weaponItemType = ItemType.Weapon;
        [SerializeField] private int loadedAmmo;
        [SerializeField] private int maxLoadedAmmo = 6;

        [Header("Raycast")]
        [SerializeField] private float fireRange = 20f;
        [SerializeField] private LayerMask hitLayers = ~0;

        public int LoadedAmmo => loadedAmmo;

        private void Awake()
        {
            if (inventoryManager == null)
            {
                inventoryManager = GetComponent<InventoryManager>();
            }

            if (playerController == null)
            {
                playerController = GetComponent<PlayerController>();
            }
        }

        private void Update()
        {
            if (!IsWeaponSelected())
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                Fire();
            }
        }

        private bool IsWeaponSelected()
        {
            InventorySlot selectedSlot = inventoryManager.GetSelectedSlot();
            return selectedSlot != null && !selectedSlot.IsEmpty && selectedSlot.item.ItemType == weaponItemType;
        }

        public bool Fire()
        {
            if (loadedAmmo <= 0)
            {
                return false;
            }

            loadedAmmo--;

            Vector2 origin = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
            Vector2 direction = (playerController != null && playerController.IsFacingRight) ? Vector2.right : Vector2.left;

            RaycastHit2D hit = Physics2D.Raycast(origin, direction, fireRange, hitLayers);

            if (hit.collider != null)
            {
                OnHit(hit);
            }

            return true;
        }

        private void OnHit(RaycastHit2D hit)
        {
            Debug.DrawLine(hit.point, hit.point + hit.normal, Color.red, 0.5f);
        }

        public bool Reload()
        {
            if (inventoryManager == null || ammoItem == null)
            {
                return false;
            }

            if (loadedAmmo >= maxLoadedAmmo)
            {
                return false;
            }

            if (!inventoryManager.HasItem(ammoItem, 1))
            {
                return false;
            }

            if (!inventoryManager.RemoveItem(ammoItem, 1))
            {
                return false;
            }

            loadedAmmo++;
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            if (firePoint == null)
            {
                return;
            }

            Gizmos.color = Color.red;
            Vector3 direction = (playerController != null && playerController.IsFacingRight) ? Vector3.right : Vector3.left;
            Gizmos.DrawLine(firePoint.position, firePoint.position + direction * fireRange);
        }
    }
}
