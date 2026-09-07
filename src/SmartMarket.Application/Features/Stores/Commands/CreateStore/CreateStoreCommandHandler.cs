using FluentValidation;
using MediatR;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Domain.Entities;

namespace SmartMarket.Application.Features.Stores.Commands.CreateStore;

public class CreateStoreCommandHandler : IRequestHandler<CreateStoreCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateStoreCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateStoreCommand request, CancellationToken cancellationToken)
    {
        var store = new Store(
            request.Name,
            request.OwnerId,
            request.Description,
            request.LogoUrl
        );

        _context.Stores.Add(store);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(store.Id);
    }
}