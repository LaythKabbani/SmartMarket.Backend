using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Orders.Dtos;

namespace SmartMarket.Application.Features.Orders.Queries.GetUserOrders;

public record GetUserOrdersQuery(Guid UserId) : IRequest<Result<List<OrderDto>>>;