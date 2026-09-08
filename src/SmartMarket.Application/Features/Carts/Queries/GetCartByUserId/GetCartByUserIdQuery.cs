using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Carts.Dtos;

namespace SmartMarket.Application.Features.Carts.Queries.GetCartByUserId;

public record GetCartByUserIdQuery(Guid UserId) : IRequest<Result<CartDto>>;