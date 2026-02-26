using Bangkok.Application.Dto.Profile;

namespace Bangkok.Application.Interfaces;

public interface IProfileService
{
    Task<ProfileDto?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(CreateProfileResult Result, string? ErrorMessage)> CreateProfileAsync(CreateProfileDto dto, Guid currentUserId, string? currentUserRole, CancellationToken cancellationToken = default);
    Task<(UpdateProfileResult Result, string? ErrorMessage)> UpdateProfileAsync(Guid userId, UpdateProfileDto dto, Guid currentUserId, string? currentUserRole, CancellationToken cancellationToken = default);
    Task<DeleteProfileResult> DeleteProfileAsync(Guid userId, Guid currentUserId, string? currentUserRole, CancellationToken cancellationToken = default);
}

public enum CreateProfileResult
{
    Success,
    NotFound,
    Forbidden,
    AlreadyExists,
    ValidationError
}

public enum UpdateProfileResult
{
    Success,
    NotFound,
    Forbidden,
    ValidationError
}

public enum DeleteProfileResult
{
    Success,
    NotFound,
    Forbidden
}
