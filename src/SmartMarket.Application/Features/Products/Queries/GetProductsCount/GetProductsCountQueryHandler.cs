using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces;

namespace SmartMarket.Application.Features.Products.Queries.GetProductsCount;

public class GetProductsCountQueryHandler : IRequestHandler<GetProductsCountQuery, int>
{
    private readonly IApplicationDbContext _context;

    public GetProductsCountQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(GetProductsCountQuery request, CancellationToken cancellationToken)
    {
        return await _context.Products.CountAsync(cancellationToken);
    }
}
