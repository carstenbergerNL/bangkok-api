using System.Text.RegularExpressions;
using Bangkok.Application.Dto.Profile;
using Bangkok.Application.Interfaces;
using Bangkok.Domain;
using Microsoft.Extensions.Logging;

namespace Bangkok.Infrastructure.Services;

public class ProfileService : IProfileService
{
    /// <summary>Max decoded image size 2MB; base64 length ≈ 2*1024*1024*4/3.</summary>
    private const int MaxAvatarBase64Length = 2_796_203;

    private static readonly Regex DataUrlPrefix = new(@"^data:image/(jpeg|jpg|png);base64,", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IProfileRepository _profileRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(IProfileRepository profileRepository, IUserRepository userRepository, ILogger<ProfileService> logger)
    {
        _profileRepository = profileRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<ProfileDto?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        return profile == null ? null : MapToDto(profile);
    }

    public async Task<(CreateProfileResult Result, string? ErrorMessage)> CreateProfileAsync(CreateProfileDto dto, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (dto.UserId != currentUserId && !isAdmin)
        {
            _logger.LogWarning("Unauthorized create profile: user {CurrentUserId} tried to create profile for user {TargetUserId}", currentUserId, dto.UserId);
            return (CreateProfileResult.Forbidden, null);
        }

        var user = await _userRepository.GetByIdAsync(dto.UserId, cancellationToken).ConfigureAwait(false);
        if (user == null)
            return (CreateProfileResult.NotFound, "User not found.");

        var existing = await _profileRepository.GetByUserIdAsync(dto.UserId, cancellationToken).ConfigureAwait(false);
        if (existing != null)
            return (CreateProfileResult.AlreadyExists, "Profile already exists for this user.");

        var (valid, avatarError) = ValidateAndNormalizeAvatar(dto.AvatarBase64, out var normalizedAvatar);
        if (!valid)
            return (CreateProfileResult.ValidationError, avatarError);

        var profile = new Profile
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            FirstName = dto.FirstName.Trim(),
            MiddleName = string.IsNullOrWhiteSpace(dto.MiddleName) ? null : dto.MiddleName.Trim(),
            LastName = dto.LastName.Trim(),
            DateOfBirth = dto.DateOfBirth.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dto.DateOfBirth, DateTimeKind.Utc) : dto.DateOfBirth.ToUniversalTime(),
            PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim(),
            AvatarBase64 = normalizedAvatar,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _profileRepository.CreateAsync(profile, cancellationToken).ConfigureAwait(false);
        return (CreateProfileResult.Success, null);
    }

    public async Task<(UpdateProfileResult Result, string? ErrorMessage)> UpdateProfileAsync(Guid userId, UpdateProfileDto dto, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (userId != currentUserId && !isAdmin)
        {
            _logger.LogWarning("Unauthorized update profile: user {CurrentUserId} tried to update profile for user {TargetUserId}", currentUserId, userId);
            return (UpdateProfileResult.Forbidden, null);
        }

        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (profile == null)
            return (UpdateProfileResult.NotFound, null);

        string? normalizedAvatar = null;
        if (dto.AvatarBase64 != null)
        {
            if (dto.AvatarBase64.Length == 0)
                normalizedAvatar = null;
            else
            {
                var (valid, avatarError) = ValidateAndNormalizeAvatar(dto.AvatarBase64, out normalizedAvatar);
                if (!valid)
                    return (UpdateProfileResult.ValidationError, avatarError);
            }
        }

        if (dto.FirstName != null) profile.FirstName = dto.FirstName.Trim();
        if (dto.MiddleName != null) profile.MiddleName = string.IsNullOrWhiteSpace(dto.MiddleName) ? null : dto.MiddleName.Trim();
        if (dto.LastName != null) profile.LastName = dto.LastName.Trim();
        if (dto.DateOfBirth.HasValue)
            profile.DateOfBirth = dto.DateOfBirth.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dto.DateOfBirth.Value, DateTimeKind.Utc) : dto.DateOfBirth.Value.ToUniversalTime();
        if (dto.PhoneNumber != null) profile.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();
        if (dto.AvatarBase64 != null) profile.AvatarBase64 = normalizedAvatar;

        profile.UpdatedAtUtc = DateTime.UtcNow;
        await _profileRepository.UpdateAsync(profile, cancellationToken).ConfigureAwait(false);
        return (UpdateProfileResult.Success, null);
    }

    public async Task<DeleteProfileResult> DeleteProfileAsync(Guid userId, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (userId != currentUserId && !isAdmin)
        {
            _logger.LogWarning("Unauthorized delete profile: user {CurrentUserId} tried to delete profile for user {TargetUserId}", currentUserId, userId);
            return DeleteProfileResult.Forbidden;
        }

        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (profile == null)
            return DeleteProfileResult.NotFound;

        await _profileRepository.DeleteAsync(profile.Id, cancellationToken).ConfigureAwait(false);
        return DeleteProfileResult.Success;
    }

    private static ProfileDto MapToDto(Profile p) => new()
    {
        Id = p.Id,
        UserId = p.UserId,
        FirstName = p.FirstName,
        MiddleName = p.MiddleName,
        LastName = p.LastName,
        DateOfBirth = p.DateOfBirth,
        PhoneNumber = p.PhoneNumber,
        AvatarBase64 = p.AvatarBase64,
        CreatedAtUtc = p.CreatedAtUtc,
        UpdatedAtUtc = p.UpdatedAtUtc
    };

    /// <summary>Validates avatar base64 (size, format jpeg/png). Strips data URL prefix. Returns (true, null) or (false, errorMessage).</summary>
    private static (bool valid, string? errorMessage) ValidateAndNormalizeAvatar(string? avatarBase64, out string? normalized)
    {
        normalized = null;
        if (string.IsNullOrWhiteSpace(avatarBase64))
            return (true, null);

        var trimmed = avatarBase64.Trim();
        var base64Only = DataUrlPrefix.Replace(trimmed, string.Empty).Trim();
        if (base64Only.Length > MaxAvatarBase64Length)
            return (false, "Avatar must be at most 2MB (decoded).");

        if (base64Only.Length == 0)
            return (true, null);

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(base64Only);
        }
        catch
        {
            return (false, "Avatar must be valid base64.");
        }

        if (bytes.Length > 2 * 1024 * 1024)
            return (false, "Avatar must be at most 2MB.");

        if (!IsJpegOrPng(bytes))
            return (false, "Avatar must be a JPEG or PNG image.");

        normalized = base64Only;
        return (true, null);
    }

    private static bool IsJpegOrPng(byte[] bytes)
    {
        if (bytes.Length < 8) return false;
        // JPEG: FF D8 FF
        if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) return true;
        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
            bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A) return true;
        return false;
    }
}
