namespace Statecraft.Sim
{
    /// <summary>One line of the "Why did this happen?" report.</summary>
    public sealed class Cause
    {
        public string System { get; }
        public string Target { get; }
        public double Delta { get; }
        public string Message { get; }
        /// <summary>Optional presentation-only identity; unknown or absent keys keep <see cref="Message"/>.
        /// Rebuilt with the report on replay, never stored in game state or the command log.</summary>
        public string MessageKey { get; }
        public Cause(string system, string target, double delta, string message)
            : this(system, target, delta, message, "") { }
        public Cause(string system, string target, double delta, string message, string? messageKey)
        {
            System = system; Target = target; Delta = delta;
            Message = PlayerText.Capitalised(message);   // sentence case (playtest 2, P2-6)
            MessageKey = messageKey ?? "";
        }
        public override string ToString() => $"[{System}] {Message}";
    }
}
