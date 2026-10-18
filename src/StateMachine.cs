using System;

namespace SCP096TheShyGuy
{
    // Intentionally has no WorldBox dependencies; this is testable in isolation.
    public enum ShyGuyState { Docile, Triggered, Rage, Calming }

    public sealed class ShyGuyBrain
    {
        public ShyGuyState State { get; private set; } = ShyGuyState.Docile;
        public float EnteredAt { get; private set; }
        public string LastReason { get; private set; } = "initial";

        public bool Transition(ShyGuyState next, float time, string reason)
        {
            if (next == State) return false;
            if (!Allowed(State, next)) return false;
            State = next;
            EnteredAt = time;
            LastReason = reason;
            return true;
        }

        public void Reset(float time)
        {
            State = ShyGuyState.Docile;
            EnteredAt = time;
            LastReason = "reset";
        }

        public void Tick(float time, bool hasValidVictim, float triggerDelay, float calmingDelay)
        {
            if (State == ShyGuyState.Triggered && !hasValidVictim)
                Transition(ShyGuyState.Calming, time, "victim invalid");
            else if (State == ShyGuyState.Triggered && time - EnteredAt >= triggerDelay)
                Transition(ShyGuyState.Rage, time, "preparation finished");
            else if (State == ShyGuyState.Rage && !hasValidVictim)
                Transition(ShyGuyState.Calming, time, "no targets");
            else if (State == ShyGuyState.Calming && time - EnteredAt >= calmingDelay)
                Transition(ShyGuyState.Docile, time, "calmed");
        }

        private static bool Allowed(ShyGuyState from, ShyGuyState to)
        {
            return (from == ShyGuyState.Docile && to == ShyGuyState.Triggered)
                || (from == ShyGuyState.Triggered && (to == ShyGuyState.Rage || to == ShyGuyState.Calming))
                || (from == ShyGuyState.Rage && to == ShyGuyState.Calming)
                || (from == ShyGuyState.Calming && (to == ShyGuyState.Docile || to == ShyGuyState.Triggered));
        }
    }
}
