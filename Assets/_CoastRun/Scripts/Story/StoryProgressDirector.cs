using System;
using UnityEngine;

namespace CoastRun
{
    /// Orchestrates story acts in order:
    /// Prologue → Run → GoldenHour → BlueHour → Arrival
    public class StoryProgressDirector : MonoBehaviour
    {
        [SerializeField] private StoryConfig config;
        [SerializeField] private StoryManager story;
        [SerializeField] private PlayerController player;
        [SerializeField] private UpgradeManager upgrades;
        [SerializeField] private DynamicEnvironmentManager dayCycle;
        [SerializeField] private UI_FinalDestinationController destinationUi;

        private StoryAct _act = StoryAct.Prologue;
        private bool[] _actFired;

        public StoryAct CurrentAct => _act;
        public event Action<StoryAct> OnActChanged;

        public void Bind(StoryConfig storyConfig, StoryManager storyManager,
            PlayerController playerController, UpgradeManager upgradeManager,
            DynamicEnvironmentManager env, UI_FinalDestinationController destUi)
        {
            config = storyConfig ?? CoastConfigRegistry.StoryConfig;
            story = storyManager;
            player = playerController;
            upgrades = upgradeManager;
            dayCycle = env;
            destinationUi = destUi;

            if (config?.actBeats != null)
                _actFired = new bool[config.actBeats.Length];

            if (story != null)
            {
                story.OnPrologueFinished -= EnterRunAct;
                story.OnPrologueFinished += EnterRunAct;
            }
        }

        private void OnDestroy()
        {
            if (story != null)
                story.OnPrologueFinished -= EnterRunAct;
        }

        private void EnterRunAct()
        {
            SetAct(StoryAct.Run);
        }

        public void NotifyArrival()
        {
            SetAct(StoryAct.Arrival);
        }

        private void Update()
        {
            if (_act == StoryAct.Prologue || _act >= StoryAct.Arrival || player == null)
                return;

            if (dayCycle == null)
                return;

            if (dayCycle.CurrentPhase == DayPhase.BlueHour && _act < StoryAct.BlueHourApproach)
                SetAct(StoryAct.BlueHourApproach);
            else if (dayCycle.CurrentPhase == DayPhase.GoldenHour && _act < StoryAct.GoldenHour)
                SetAct(StoryAct.GoldenHour);
        }

        private void SetAct(StoryAct act)
        {
            if (act == _act)
                return;
            if (act < _act && act != StoryAct.Arrival)
                return;

            _act = act;
            FireActBeat(act);
            OnActChanged?.Invoke(act);
        }

        private void FireActBeat(StoryAct act)
        {
            if (config?.actBeats == null || _actFired == null)
                return;

            for (int i = 0; i < config.actBeats.Length; i++)
            {
                if (_actFired[i] || config.actBeats[i].act != act)
                    continue;

                _actFired[i] = true;
                var beat = config.actBeats[i];

                if (!string.IsNullOrEmpty(beat.narratorLine))
                    destinationUi?.ShowStoryLine(beat.narratorLine);
                // hudLabel intentionally unused — journey HUD has no act strip.
            }
        }
    }
}
