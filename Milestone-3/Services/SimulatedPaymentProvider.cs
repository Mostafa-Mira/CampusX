namespace CampusX.Services;

// Development-only provider. Replace through DI when a verified gateway is available.
public class SimulatedPaymentProvider : IPaymentProvider
{
    public PaymentResult Charge(Guid buyerId, decimal amount, string description)
    {
        if (buyerId == Guid.Empty || amount <= 0 || string.IsNullOrWhiteSpace(description))
            return new PaymentResult(false, string.Empty);

        return new PaymentResult(true, $"SIM-{Guid.NewGuid():N}");
    }
}
