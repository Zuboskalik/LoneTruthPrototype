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
        [SerializeField] private WeaponFireEffects fireEffects;

        [Header("Weapon")]
        [SerializeField] private ItemSO weaponItem;
        [SerializeField] private ItemSO ammoItem;
        [SerializeField] private ItemType weaponItemType = ItemType.Weapon;
        [SerializeField] private ItemType ammoItemType = ItemType.Ammo;
        [SerializeField] private int loadedAmmo;
        [SerializeField] private int maxLoadedAmmo = 6;

        [Header("Input")]
        [SerializeField] private KeyCode useKey = KeyCode.G;
        [SerializeField] private KeyCode attackKey = KeyCode.Space;

        [Header("Raycast")]
        [SerializeField] private float fireRange = 20f;
        [SerializeField] private LayerMask hitLayers = ~0;
        [SerializeField] private int damagePerShot = 25;

        private bool isEquipped;

        public ItemSO WeaponItem => weaponItem;
        public int LoadedAmmo => loadedAmmo;
        public int MaxLoadedAmmo => maxLoadedAmmo;
        public bool IsEquipped => isEquipped;

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

            if (fireEffects == null)
            {
                fireEffects = GetComponent<WeaponFireEffects>();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(useKey))
            {
                UseSelectedItem();
            }

            if (isEquipped && Input.GetKeyDown(attackKey))
            {
                Fire();
            }
        }

        private void UseSelectedItem()
        {
            InventorySlot selectedSlot = inventoryManager.GetSelectedSlot();
            if (selectedSlot == null || selectedSlot.IsEmpty)
            {
                return;
            }

            if (selectedSlot.item.ItemType == weaponItemType)
            {
                ToggleEquip();
            }
            else if (selectedSlot.item.ItemType == ammoItemType)
            {
                Reload();
            }
        }

        private void ToggleEquip()
        {
            isEquipped = !isEquipped;
        }

        public bool Fire()
        {
            if (!isEquipped || loadedAmmo <= 0)
            {
                return false;
            }

            loadedAmmo--;

            Vector3 origin = firePoint != null ? firePoint.position : transform.position;
            Vector3 direction = (playerController != null && playerController.IsFacingRight) ? Vector3.right : Vector3.left;

            RaycastHit[] hits = Physics.RaycastAll(origin, direction, fireRange, hitLayers);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            Vector3 tracerEndPoint = origin + direction * fireRange;
            bool hitSomething = false;

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                tracerEndPoint = hit.point;
                hitSomething = true;
                OnHit(hit);
                break;
            }

            if (fireEffects != null)
            {
                fireEffects.PlayTracer(origin, tracerEndPoint);

                if (hitSomething)
                {
                    fireEffects.PlayImpactFlash(tracerEndPoint);
                }
            }

            return true;
        }

        private void OnHit(RaycastHit hit)
        {
            Debug.DrawLine(hit.point, hit.point + hit.normal, Color.red, 0.5f);

            TargetDummy dummy = hit.collider.GetComponentInParent<TargetDummy>();
            if (dummy != null)
            {
                dummy.TakeDamage(damagePerShot);
            }
        }

        public bool Reload()
        {
            if (inventoryManager == null || ammoItem == null)
            {
                return false;
            }

            int needed = maxLoadedAmmo - loadedAmmo;
            if (needed <= 0)
            {
                return false;
            }

            InventorySlot selectedSlot = inventoryManager.GetSelectedSlot();
            if (selectedSlot == null || selectedSlot.IsEmpty || selectedSlot.item != ammoItem)
            {
                return false;
            }

            int amountToLoad = Mathf.Min(needed, selectedSlot.quantity);
            if (amountToLoad <= 0)
            {
                return false;
            }

            if (!inventoryManager.RemoveItem(ammoItem, amountToLoad))
            {
                return false;
            }

            loadedAmmo += amountToLoad;
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
