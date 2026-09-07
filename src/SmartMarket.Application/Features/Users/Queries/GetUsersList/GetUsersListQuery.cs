using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Mappings;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Users.Dtos;

namespace SmartMarket.Application.Features.Users.Queries.GetUsersList;

public record GetUsersListQuery(
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<PaginatedList<UserDto>>;