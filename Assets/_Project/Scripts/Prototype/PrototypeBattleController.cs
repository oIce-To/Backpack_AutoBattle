using System;
using BackpackAutoBattle.Combat;
using UnityEngine;

namespace BackpackAutoBattle.Prototype
{
    /// <summary>Runs combat and updates only the dedicated battle screen.</summary>
    public sealed class PrototypeBattleController : MonoBehaviour
    {
        private PrototypeBattleScreen _screen;
        private Action<CombatResult> _finished;

        public CombatSimulation Simulation { get; private set; }
        public bool AutoTick { get; set; } = true;
        public RectTransform ScreenRoot => _screen.Root;

        public void Initialize(Transform canvas, Action<CombatResult> finished, Action continueAction, PrototypeItemTooltip tooltip)
        {
            _finished = finished;
            _screen = new PrototypeBattleScreen(canvas, continueAction, tooltip);
            SetVisible(false);
        }

        public void BeginBattle(CombatantSnapshot player, CombatantSnapshot enemy, int round, int income, float maximumSeconds)
        {
            Simulation = new CombatSimulation(player, enemy, maximumDuration: maximumSeconds);
            _screen.Begin(Simulation, round, income);
        }

        private void Update()
        {
            if (AutoTick) TickCombat(Time.deltaTime);
        }

        public void TickCombat(float deltaSeconds)
        {
            if (Simulation == null || Simulation.Result != CombatResult.Running) return;
            foreach (CombatItemEvent itemEvent in Simulation.Advance(deltaSeconds)) _screen.ShowAttack(itemEvent);
            _screen.Render(Simulation);
            if (Simulation.Result != CombatResult.Running) _finished(Simulation.Result);
        }

        public void ShowResult(PrototypePhase phase) => _screen.ShowResult(phase);
        public void SetVisible(bool visible) => _screen.Root.gameObject.SetActive(visible);

        public void EndBattle()
        {
            Simulation = null;
            SetVisible(false);
        }
    }
}
