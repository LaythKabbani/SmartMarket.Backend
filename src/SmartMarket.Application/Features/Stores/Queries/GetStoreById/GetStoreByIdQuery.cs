using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Stores.Dtos;

namespace SmartMarket.Application.Features.Stores.Queries.GetStoreById;

public record GetStoreByIdQuery(Guid Id) : IRequest<Result<StoreDto>>;