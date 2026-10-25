using LoneTruth.Core;
using LoneTruth.Inventory;
using LoneTruth.Player;
using LoneTruth.UI;
using UnityEngine;

namespace LoneTruth.Combat
{
    /// <summary>
    /// Resident Evil / Silent Hill–style shooting: with the revolver drawn, hold the aim key
    /// (RMB / Ctrl) to aim — Holt stops and can only turn — then fire with the action key (E, the
    /// same as interact) or LMB. Aiming is only allowed inside a CombatZone.
    /// </summary>
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
        [SerializeField] private KeyCode aimKey = KeyCode.LeftControl;
        [SerializeField] private int aimMouseButton = 1;
        [SerializeField] private KeyCode fireKey = KeyCode.E;
        [SerializeField] private KeyCode reloadKey = KeyCode.R;
        [SerializeField] private float fireCooldown = 0.35f;

        [Header("Lines")]
        [SerializeField] private string holsteredLine = "Револьвер в кобуре. Сначала достаньте его: выберите (Q) и примените (G).";
        [SerializeField] private string notHereLine = "Не то место, чтобы хвататься за револьвер.";
        [SerializeField] private string emptyLine = "Барабан пуст. R — перезарядить.";

        [Header("Raycast")]
        [SerializeField] private float fireRange = 20f;
        [SerializeField] private LayerMask hitLayers = ~0;
        [SerializeField] private int damagePerShot = 25;

        private bool isEquipped;
        private bool isAiming;
        private float fireTimer;

        public ItemSO WeaponItem => weaponItem;
        public ItemSO AmmoItem => ammoItem;
        public int LoadedAmmo => loadedAmmo;
        public int MaxLoadedAmmo => maxLoadedAmmo;
        public bool IsEquipped => isEquipped;
        public bool IsAiming => isAiming;

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
            // Using the selected item (G) now lives in InventoryActions; this handles aiming, fire and reload.
            if (GameMaster.Instance != null && !GameMaster.Instance.IsInState(GameState.Exploration))
            {
                SetAiming(false);
                return;
            }

            if (fireTimer > 0f)
            {
                fireTimer -= Time.deltaTime;
            }

            if (Input.GetKeyDown(reloadKey))
            {
                Reload();
            }

            bool aimHeld = Input.GetKey(aimKey) || Input.GetMouseButton(aimMouseButton);
            bool aimPressed = Input.GetKeyDown(aimKey) || Input.GetMouseButtonDown(aimMouseButton);
            if (!isAiming && aimPressed)
            {
                TryStartAiming();
            }
            else if (isAiming && (!aimHeld || !isEquipped || !CombatZone.WeaponsAllowed))
            {
                SetAiming(false);
            }

            bool firePressed = Input.GetKeyDown(fireKey) || (Input.GetMouseButtonDown(0) && !GameUI.PointerOverHud);
            if (isAiming && firePressed && fireTimer <= 0f)
            {
                if (loadedAmmo <= 0)
                {
                    Say(emptyLine);
                }
                else if (Fire())
                {
                    fireTimer = fireCooldown;
                }
            }
        }

        private void OnDisable()
        {
            SetAiming(false);
        }

        private void TryStartAiming()
        {
            if (!isEquipped)
            {
                Say(holsteredLine);
                return;
            }

            if (!CombatZone.WeaponsAllowed)
            {
                Say(notHereLine);
                return;
            }

            SetAiming(true);
        }

        private void SetAiming(bool value)
        {
            if (isAiming == value)
            {
                return;
            }

            isAiming = value;
            if (playerController != null)
            {
                playerController.SetAiming(value);
            }
        }

        private static void Say(string line)
        {
            if (GameUI.Instance != null)
            {
                GameUI.Instance.Toast(line, 2.5f);
            }
        }

        public void ToggleEquip()
        {
            isEquipped = !isEquipped;
            if (!isEquipped)
            {
                SetAiming(false);
            }
        }

        public bool Fire()
        {
            if (!isEquipped || !isAiming || loadedAmmo <= 0)
            {
                return false;
            }

            loadedAmmo--;
            if (playerController != null)
            {
                playerController.PlayAnimationTrigger("Fire");
            }

            bool right = playerController == null || playerController.IsFacingRight;
            Vector3 direction = right ? Vector3.right : Vector3.left;
            Vector3 origin = transform.position;
            if (firePoint != null)
            {
                // The fire point is authored for facing right; mirror it when Holt faces left.
                Vector3 local = firePoint.localPosition;
                local.x = right ? Mathf.Abs(local.x) : -Mathf.Abs(local.x);
                origin = transform.TransformPoint(local);
            }

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

            // Rounds come from anywhere in the inventory, not only from the selected slot.
            int amountToLoad = Mathf.Min(needed, inventoryManager.CountItem(ammoItem));
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
