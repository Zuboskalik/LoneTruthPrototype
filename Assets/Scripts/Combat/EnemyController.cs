using System.Collections.Generic;
using LoneTruth.Core;
using LoneTruth.Player;
using UnityEngine;

namespace LoneTruth.Combat
{
    public enum EnemyKind
    {
        Hollow, // slow, a telegraphed pick swing; backs away from a burning flare
        Thug,   // patrols with a lantern, sees only ahead; may run away wounded
        Dog     // fast, barks (calls the thugs); leaves for a piece of meat
    }

    /// <summary>
    /// The chapter's enemies (design doc 7.1). They walk along the X axis, see Holt unless he hides,
    /// and hit him after a readable wind-up that a dodge roll goes through. Stands still during
    /// cutscenes, so a cutscene can move it with its usual steps. Once killed it stays dead: its
    /// deadFlag is story state, so a reloaded scene does not bring it back.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class EnemyController : MonoBehaviour
    {
        private enum Mode { Asleep, Patrol, Chase, Windup, Recover, Blinded, Retreat, Flee, Leaving, Dead }

        private const float FadeSeconds = 1.2f;

        private static readonly List<EnemyController> Active = new List<EnemyController>();
        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        private static readonly int AttackParam = Animator.StringToHash("Attack");
        private static readonly int BlindParam = Animator.StringToHash("Blind");
        private static readonly int RetreatParam = Animator.StringToHash("Retreat");
        private static readonly int SleepParam = Animator.StringToHash("Sleep");
        private static readonly int DieParam = Animator.StringToHash("Die");

        [SerializeField] private EnemyKind kind = EnemyKind.Hollow;
        [SerializeField] private float patrolMinX;
        [SerializeField] private float patrolMaxX;
        [SerializeField] private float worldMinX;
        [SerializeField] private float worldMaxX = 100f;
        [SerializeField] private bool startAsleep;
        [SerializeField] private string deadFlag;
        [SerializeField] private Transform drop;

        [Header("Stats")]
        [SerializeField] private float walkSpeed = 0.9f;
        [SerializeField] private float chaseSpeed = 1.3f;
        [SerializeField] private float sightRange = 8f;
        [SerializeField] private float attackRange = 1.3f;
        [SerializeField] private int attackDamage = 25;
        [SerializeField] private float windup = 0.8f;
        [SerializeField] private float recover = 1f;

        private Mode mode;
        private Health health;
        private Animator animator;
        private SpriteRenderer sprite;
        private Collider body;
        private float timer;
        private float patrolDir = 1f;
        private float deathFade;
        private Color baseColor = Color.white;
        private float flash;

        public EnemyKind Kind => kind;
        public bool IsDead => mode == Mode.Dead;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Active.Clear();
        }

        /// <summary>Builder setup.</summary>
        public void Configure(EnemyKind enemyKind, float minX, float maxX, float boundsMin, float boundsMax, bool asleep, string flagWhenDead, Transform dropOnDeath)
        {
            kind = enemyKind;
            patrolMinX = minX;
            patrolMaxX = maxX;
            worldMinX = boundsMin;
            worldMaxX = boundsMax;
            startAsleep = asleep;
            deadFlag = flagWhenDead;
            drop = dropOnDeath;
            switch (kind)
            {
                case EnemyKind.Hollow:
                    walkSpeed = 0.8f; chaseSpeed = 1.3f; sightRange = 9f; attackRange = 1.4f; attackDamage = 25; windup = 0.9f; recover = 1.1f;
                    break;
                case EnemyKind.Thug:
                    walkSpeed = 1.3f; chaseSpeed = 3f; sightRange = 6f; attackRange = 1.3f; attackDamage = 15; windup = 0.6f; recover = 0.9f;
                    break;
                case EnemyKind.Dog:
                    walkSpeed = 1.6f; chaseSpeed = 6f; sightRange = 7f; attackRange = 1f; attackDamage = 10; windup = 0.35f; recover = 0.8f;
                    break;
            }
        }

        /// <summary>A shot, a roll or a lit flare: whoever is asleep or on patrol nearby comes running.</summary>
        public static void MakeNoise(Vector3 position, float radius)
        {
            foreach (EnemyController e in Active.ToArray())
            {
                if ((e.mode == Mode.Asleep || e.mode == Mode.Patrol) &&
                    Mathf.Abs(e.transform.position.x - position.x) <= radius)
                {
                    e.Alert();
                }
            }
        }

        /// <summary>The flare's flash: blinds everyone close by; a consecrated one also burns the Hollow.</summary>
        public static void BlindAround(Vector3 position, float radius, float seconds, int damageToHollows)
        {
            foreach (EnemyController e in Active.ToArray())
            {
                if (e.mode == Mode.Dead || Mathf.Abs(e.transform.position.x - position.x) > radius)
                {
                    continue;
                }

                e.Blind(seconds);
                if (damageToHollows > 0 && e.kind == EnemyKind.Hollow)
                {
                    e.health.TakeDamage(damageToHollows);
                }
            }
        }

        /// <summary>Is anyone after Holt right next to him (too late to hide)?</summary>
        public static bool AnyHunterNear(Vector3 position, float radius)
        {
            foreach (EnemyController e in Active)
            {
                bool hunting = e.mode == Mode.Chase || e.mode == Mode.Windup || e.mode == Mode.Recover;
                if (hunting && Mathf.Abs(e.transform.position.x - position.x) <= radius)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Holt uses meat: the nearest dog in range takes it and leaves.</summary>
        public static bool TryFeedDog(Vector3 position, float radius)
        {
            foreach (EnemyController e in Active)
            {
                if (e.kind == EnemyKind.Dog && e.mode != Mode.Dead && e.mode != Mode.Leaving &&
                    Mathf.Abs(e.transform.position.x - position.x) <= radius)
                {
                    e.Leave();
                    return true;
                }
            }

            return false;
        }

        private void Awake()
        {
            health = GetComponent<Health>();
            animator = GetComponentInChildren<Animator>();
            sprite = GetComponentInChildren<SpriteRenderer>();
            body = GetComponent<Collider>();
            if (sprite != null)
            {
                baseColor = sprite.color;
            }

            health.OnDamaged += HandleDamaged;
            health.OnDeath += HandleDeath;
        }

        private void OnDestroy()
        {
            health.OnDamaged -= HandleDamaged;
            health.OnDeath -= HandleDeath;
        }

        private void OnEnable()
        {
            if (StoryState.Has(deadFlag))
            {
                gameObject.SetActive(false);
                return;
            }

            Active.Add(this);
            if (mode == Mode.Asleep || mode == Mode.Patrol)
            {
                mode = startAsleep ? Mode.Asleep : Mode.Patrol;
                SetBool(SleepParam, startAsleep);
            }
        }

        private void OnDisable()
        {
            Active.Remove(this);
        }

        private void Start()
        {
            // Animators start from their defaults: set again once this one is up.
            SetBool(SleepParam, mode == Mode.Asleep);
        }

        private void Update()
        {
            if (sprite != null && flash > 0f)
            {
                flash -= Time.deltaTime;
                sprite.color = flash > 0f ? Color.Lerp(baseColor, Color.red, 0.7f) : baseColor;
            }

            if (mode == Mode.Dead)
            {
                UpdateDeath();
                return;
            }

            GameMaster gm = GameMaster.Instance;
            if (gm != null && !gm.IsInState(GameState.Exploration))
            {
                return; // a cutscene owns the scene
            }

            GameObject player = PersistentPlayer.Current;
            if (player == null)
            {
                return;
            }

            PlayerController pc = player.GetComponent<PlayerController>();
            float dx = player.transform.position.x - transform.position.x;
            timer -= Time.deltaTime;

            switch (mode)
            {
                case Mode.Asleep:
                    SetSpeed(0f);
                    break;

                case Mode.Patrol:
                    Patrol();
                    if (Sees(pc, dx))
                    {
                        Alert();
                    }

                    break;

                case Mode.Chase:
                    if (pc.IsHidden || Mathf.Abs(dx) > sightRange * 2.5f)
                    {
                        mode = Mode.Patrol; // lost him
                        break;
                    }

                    if (kind == EnemyKind.Hollow && FlareEffect.IsBurningNear(transform.position.x, FlareEffect.RepelRadius))
                    {
                        mode = Mode.Retreat;
                        SetBool(RetreatParam, true);
                        break;
                    }

                    Face(dx);
                    if (Mathf.Abs(dx) <= attackRange)
                    {
                        mode = Mode.Windup;
                        timer = windup;
                        SetSpeed(0f);
                        SetTrigger(AttackParam);
                    }
                    else
                    {
                        Move(Mathf.Sign(dx) * chaseSpeed);
                    }

                    break;

                case Mode.Windup:
                    if (timer <= 0f)
                    {
                        // The swing lands where he is now: a roll or a step back beats it.
                        if (Mathf.Abs(dx) <= attackRange + 0.3f && !pc.IsDodging && !pc.IsHidden)
                        {
                            player.GetComponent<Health>().TakeDamage(attackDamage);
                        }

                        mode = Mode.Recover;
                        timer = recover;
                    }

                    break;

                case Mode.Recover:
                    if (timer <= 0f)
                    {
                        mode = Mode.Chase;
                    }

                    break;

                case Mode.Blinded:
                    SetSpeed(0f);
                    if (timer <= 0f)
                    {
                        SetBool(BlindParam, false);
                        mode = Mode.Chase;
                    }

                    break;

                case Mode.Retreat:
                    if (!FlareEffect.IsBurningNear(transform.position.x, FlareEffect.RepelRadius))
                    {
                        SetBool(RetreatParam, false);
                        mode = Mode.Chase;
                        break;
                    }

                    float away = Mathf.Sign(transform.position.x - FlareEffect.NearestX(transform.position.x));
                    Face(-away);
                    Move(away * walkSpeed);
                    break;

                case Mode.Flee:
                case Mode.Leaving:
                    float run = patrolDir * chaseSpeed;
                    Face(run);
                    Move(run);
                    if (timer <= 0f || transform.position.x <= worldMinX + 0.2f || transform.position.x >= worldMaxX - 0.2f)
                    {
                        StoryState.Set(deadFlag);
                        gameObject.SetActive(false);
                    }

                    break;
            }
        }

        private void Patrol()
        {
            if (patrolMaxX - patrolMinX < 0.1f)
            {
                SetSpeed(0f);
                return;
            }

            float x = transform.position.x;
            if (x >= patrolMaxX) patrolDir = -1f;
            else if (x <= patrolMinX) patrolDir = 1f;
            Face(patrolDir);
            Move(patrolDir * walkSpeed);
        }

        private bool Sees(PlayerController pc, float dx)
        {
            if (pc.IsHidden || Mathf.Abs(dx) > sightRange)
            {
                return false;
            }

            // A thug only sees what his lantern lights ahead of him.
            return kind != EnemyKind.Thug || Mathf.Sign(dx) == patrolDir || Mathf.Abs(dx) < 1.5f;
        }

        private void Alert()
        {
            SetBool(SleepParam, false);
            mode = Mode.Chase;
            if (kind == EnemyKind.Dog)
            {
                // Its bark calls the guards from anywhere in the yard.
                MakeNoise(transform.position, 40f);
            }
        }

        private void Blind(float seconds)
        {
            if (mode == Mode.Flee || mode == Mode.Leaving)
            {
                return;
            }

            SetBool(SleepParam, false);
            SetBool(RetreatParam, false);
            SetBool(BlindParam, true);
            SetSpeed(0f);
            mode = Mode.Blinded;
            timer = seconds;
        }

        private void Leave()
        {
            mode = Mode.Leaving;
            patrolDir = Mathf.Sign(worldMaxX - transform.position.x);
            timer = 5f;
        }

        private void HandleDamaged(int damage, int remaining)
        {
            flash = 0.15f;
            if (mode == Mode.Asleep || mode == Mode.Patrol)
            {
                Alert();
            }

            // «Может убежать, если ранен».
            if (kind == EnemyKind.Thug && remaining > 0 && Random.value < 0.5f)
            {
                GameObject player = PersistentPlayer.Current;
                patrolDir = player != null ? Mathf.Sign(transform.position.x - player.transform.position.x) : 1f;
                mode = Mode.Flee;
                timer = 6f;
            }
        }

        private void HandleDeath()
        {
            mode = Mode.Dead;
            SetSpeed(0f);
            SetBool(BlindParam, false);
            SetBool(RetreatParam, false);
            SetTrigger(DieParam);
            if (body != null)
            {
                body.enabled = false;
            }

            if (drop != null)
            {
                Vector3 p = drop.position;
                p.x = transform.position.x;
                drop.position = p;
            }

            StoryState.Set(deadFlag);
        }

        private void UpdateDeath()
        {
            deathFade += Time.deltaTime;
            float wait = kind == EnemyKind.Hollow ? 1.6f : 0.4f; // the Hollow crumbles to coal dust first
            if (sprite != null && deathFade > wait)
            {
                Color c = baseColor;
                c.a = Mathf.Clamp01(1f - (deathFade - wait) / FadeSeconds);
                sprite.color = c;
            }

            if (deathFade > wait + FadeSeconds)
            {
                gameObject.SetActive(false);
            }
        }

        private void Move(float velocity)
        {
            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x + velocity * Time.deltaTime, worldMinX, worldMaxX);
            transform.position = p;
            SetSpeed(Mathf.Abs(velocity));
        }

        private void Face(float dx)
        {
            if (sprite != null && Mathf.Abs(dx) > 0.01f)
            {
                sprite.flipX = dx < 0f; // the art faces right
            }
        }

        private void SetSpeed(float value)
        {
            if (HasParam(SpeedParam, AnimatorControllerParameterType.Float))
            {
                animator.SetFloat(SpeedParam, value);
            }
        }

        private void SetBool(int param, bool value)
        {
            if (HasParam(param, AnimatorControllerParameterType.Bool))
            {
                animator.SetBool(param, value);
            }
        }

        private void SetTrigger(int param)
        {
            if (HasParam(param, AnimatorControllerParameterType.Trigger))
            {
                animator.SetTrigger(param);
            }
        }

        private bool HasParam(int hash, AnimatorControllerParameterType type)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return false;
            }

            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.nameHash == hash && p.type == type)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
