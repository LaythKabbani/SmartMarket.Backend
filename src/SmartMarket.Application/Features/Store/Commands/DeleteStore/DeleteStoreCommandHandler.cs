using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;

namespace SmartMarket.Application.Features.Stores.Commands.DeleteStore;

public class DeleteStoreCommandHandler : IRequestHandler<DeleteStoreCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public DeleteStoreCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(DeleteStoreCommand request, CancellationToken cancellationToken)
    {
        var store = await _context.Stores
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (store == null)
        {
            return Result<bool>.Failure("Store not found.");
        }

        store.MarkAsDeleted();

        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}