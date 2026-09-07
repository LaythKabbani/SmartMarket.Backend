using SmartMarket.Application.Common.Mappings;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;

namespace SmartMarket.Application.Features.Users.Dtos;

public class UserDto : IMapFrom<User>
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public UserRole Role { get; set; }
}