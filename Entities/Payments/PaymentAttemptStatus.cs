namespace Entities.Payments
{
    public enum PaymentAttemptStatus
    {
        Pending = 0,
        Success = 1,
        Failed = 2,
        Cancelled = 3,
        Verifying = 4
    }
}