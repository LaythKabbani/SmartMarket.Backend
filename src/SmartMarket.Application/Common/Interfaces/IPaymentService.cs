using SmartMarket.Application.Common.Models;

namespace SmartMarket.Application.Common.Interfaces;

public interface IPaymentService
{
    Task<Result<bool>> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
}