namespace CampusX.Services;

public sealed record PaymentResult(bool IsApproved, string Reference);

public interface IPaymentProvider
{
    PaymentResult Charge(Guid buyerId, decimal amount, string description);
}
