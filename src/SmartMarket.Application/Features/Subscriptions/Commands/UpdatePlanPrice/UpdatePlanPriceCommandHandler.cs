using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;

namespace SmartMarket.Application.Features.Subscriptions.Commands.UpdatePlanPrice;

public class UpdatePlanPriceCommandHandler : IRequestHandler<UpdatePlanPriceCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public UpdatePlanPriceCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(UpdatePlanPriceCommand request, CancellationToken cancellationToken)
    {
        var plan = await _context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.PlanType == request.PlanType, cancellationToken);

        if (plan == null)
            return Result<bool>.Failure($"Subscription plan '{request.PlanType.ToString()}' was not found.");

        plan.UpdatePrice(request.NewPrice);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}