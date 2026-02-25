using Bangkok.Application.Dto.Users;
using Bangkok.Application.Models;

namespace Bangkok.Application.Interfaces;

public interface IUserService
{
    Task<UserResponse?> GetUserAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<UserResponse>> GetUsersAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<UpdateUserResult> UpdateUserAsync(Guid id, UpdateUserRequest request, Guid currentUserId, string currentUserRole, CancellationToken cancellationToken = default);
    Task<DeleteUserResult> DeleteUserAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<RestoreUserResult> RestoreUserAsync(Guid id, CancellationToken cancellationToken = default);
    Task<HardDeleteUserResult> HardDeleteUserAsync(Guid id, Guid currentUserId, bool confirm, CancellationToken cancellationToken = default);
}

public enum UpdateUserResult
{
    Success,
    NotFound,
    Forbidden,
    BadRequest
}

public enum DeleteUserResult
{
    Success,
    NotFound,
    AlreadyDeleted,
    ForbiddenSelfDelete
}

public enum RestoreUserResult
{
    Success,
    NotFound,
    NotDeleted
}

public enum HardDeleteUserResult
{
    Success,
    NotFound,
    ForbiddenSelfDelete,
    ConfirmRequired
}
