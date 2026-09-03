namespace CoastRun
{
    /// Abstraction so PlayerController never talks to Input.touches / keyboard directly.
    public interface IInputReader
    {
        /// Consumed once: -1 left lane, +1 right lane, 0 none.
        int ConsumeLaneDelta();

        bool ConsumeJump();
        bool ConsumeCrouch();

        /// True while ducking is held (S / ↓ / finger dragged down and held).
        bool CrouchHeld { get; }

        /// True while the player holds a finger (tuck / lean into the wind).
        bool TuckHeld { get; }

        void Tick();
    }
}
