using UnityEngine;

namespace LoneTruth.Player
{
    /// <summary>
    /// Keeps the first player alive across scene loads so inventory, money and the loaded revolver
    /// carry over. Every scene still contains its own Player (so it can be played on its own);
    /// that copy removes itself when a persistent player already exists.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PersistentPlayer : MonoBehaviour
    {
        public static PersistentPlayer Instance { get; private set; }

        public static GameObject Current
        {
            get
            {
                if (Instance != null)
                {
                    return Instance.gameObject;
                }

                PlayerController pc = FindAnyObjectByType<PlayerController>();
                return pc != null ? pc.gameObject : null;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
