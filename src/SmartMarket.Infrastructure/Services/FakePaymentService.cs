using Microsoft.Extensions.Logging;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;

namespace SmartMarket.Infrastructure.Services;

public class FakePaymentService : IPaymentService
{
    private readonly ILogger<FakePaymentService> _logger;

    private static readonly HashSet<string> UsedTokens = new();

    public FakePaymentService(ILogger<FakePaymentService> logger)
    {
        _logger = logger;
    }

    public async Task<Result<string>> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing fake payment for Store {StoreId} with amount {Amount} {Currency}",
            request.StoreId, request.Amount, request.Currency);

        await Task.Delay(500, cancellationToken);

        if (request.CardNumber.EndsWith("0000"))
        {
            _logger.LogWarning("Fake payment failed for Store {StoreId} due to simulated declined card", request.StoreId);
            return Result<string>.Failure("Card was declined (Simulated Payment Failure).");
        }

        var paymentToken = $"tok_fake_{Guid.NewGuid().ToString()[..8]}";

        _logger.LogInformation("Fake payment succeeded for Store {StoreId}. Token: {Token}",
            request.StoreId, paymentToken);

        return Result<string>.Success(paymentToken);
    }

    public async Task<Result<bool>> ValidateTokenAsync(string paymentToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating payment token: {Token}", paymentToken);

        await Task.Delay(200, cancellationToken);

        if (string.IsNullOrWhiteSpace(paymentToken) || !paymentToken.StartsWith("tok_"))
        {
            _logger.LogWarning("Invalid payment token format: {Token}", paymentToken);
            return Result<bool>.Failure("Invalid payment token format.");
        }

        lock (UsedTokens)
        {
            if (UsedTokens.Contains(paymentToken))
            {
                _logger.LogWarning("Payment token {Token} has already been used.", paymentToken);
                return Result<bool>.Failure("This payment token has already been used.");
            }

            UsedTokens.Add(paymentToken);
        }

        _logger.LogInformation("Payment token {Token} verified successfully.", paymentToken);
        return Result<bool>.Success(true);
    }
}