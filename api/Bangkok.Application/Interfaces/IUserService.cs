using Bangkok.Application.Dto.Users;
using Bangkok.Application.Models;

namespace Bangkok.Application.Interfaces;

public interface IUserService
{
    Task<UserResponse?> GetUserAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<UserResponse>> GetUsersAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<UpdateUserResult> UpdateUserAsync(Guid id, UpdateUserRequest request, Guid currentUserId, string currentUserRole, CancellationToken cancellationToken = default);
}

public enum UpdateUserResult
{
    Success,
    NotFound,
    Forbidden,
    BadRequest
}
