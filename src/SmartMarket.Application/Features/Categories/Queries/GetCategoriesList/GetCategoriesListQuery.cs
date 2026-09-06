using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Features.Categories.Dtos;

namespace SmartMarket.Application.Features.Categories.Queries.GetCategoriesList;

public record GetCategoriesListQuery : IRequest<List<CategoryDto>>;