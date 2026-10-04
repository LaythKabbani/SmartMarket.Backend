using SmartMarket.Application.Common.Models;

namespace SmartMarket.Application.Common.Interfaces;

public record PaymentRequest(
    Guid StoreId,
    decimal Amount,
    string Currency,
    string CardNumber,
    string ExpiryMonth,
    string ExpiryYear,
    string Cvc
);

public interface IPaymentService
{
    Task<Result<string>> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
}