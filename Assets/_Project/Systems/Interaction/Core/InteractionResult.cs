namespace SOLITUDE.Core.Interaction
{
    public enum InteractionResultType
    {
        Success,
        Fail,
        Blocked,
        NoTarget
    }

    public readonly struct InteractionResult
    {
        public readonly InteractionResultType Type;
        public readonly string Message;

        public bool IsSuccess => Type == InteractionResultType.Success;

        public InteractionResult(InteractionResultType type, string message = null)
        {
            Type = type;
            Message = message;
        }

        // Factory helpers (clean call sites)
        public static InteractionResult Success(string message = null)
            => new InteractionResult(InteractionResultType.Success, message);

        public static InteractionResult Fail(string message = null)
            => new InteractionResult(InteractionResultType.Fail, message);

        public static InteractionResult Blocked(string message = null)
            => new InteractionResult(InteractionResultType.Blocked, message);

        public static InteractionResult NoTarget()
            => new InteractionResult(InteractionResultType.NoTarget);
    }
}