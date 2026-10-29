using System;
using UnityEngine;

namespace LoneTruth.Core
{
    public enum GameState
    {
        Exploration,
        Dialogue,
        Cutscene,
        Notebook   // the notebook is open; gameplay input is paused
    }

    public class GameMaster : MonoBehaviour
    {
        public static GameMaster Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.Exploration;

        public event Action<GameState, GameState> OnStateChanged;
        public event Action OnExplorationStarted;
        public event Action OnDialogueStarted;
        public event Action OnCutsceneStarted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetState(GameState newState)
        {
            if (CurrentState == newState)
            {
                return;
            }

            GameState previousState = CurrentState;
            CurrentState = newState;

            OnStateChanged?.Invoke(previousState, newState);

            switch (newState)
            {
                case GameState.Exploration:
                    OnExplorationStarted?.Invoke();
                    break;
                case GameState.Dialogue:
                    OnDialogueStarted?.Invoke();
                    break;
                case GameState.Cutscene:
                    OnCutsceneStarted?.Invoke();
                    break;
            }
        }

        public bool IsInState(GameState state)
        {
            return CurrentState == state;
        }
    }
}
