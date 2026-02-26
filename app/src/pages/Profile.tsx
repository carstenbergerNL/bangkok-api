import { useCallback, useEffect, useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { getCurrentUserId, changePassword } from '../services/authService';
import { getProfileByUserId, createProfile, updateProfile } from '../services/profileService';
import { addToast } from '../utils/toast';
import type { Profile as ProfileType, CreateProfileRequest, UpdateProfileRequest } from '../models/Profile';

const MAX_AVATAR_BYTES = 2 * 1024 * 1024; // 2MB
const ACCEPT_IMAGE = 'image/jpeg,image/png';

function fileToBase64(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => {
      const result = reader.result as string;
      const base64 = result.includes(',') ? result.split(',')[1]! : result;
      resolve(base64);
    };
    reader.onerror = () => reject(new Error('Failed to read file'));
    reader.readAsDataURL(file);
  });
}

export function Profile() {
  useAuth(); // ensure authenticated
  const userId = getCurrentUserId();

  const [profile, setProfile] = useState<ProfileType | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [firstName, setFirstName] = useState('');
  const [middleName, setMiddleName] = useState('');
  const [lastName, setLastName] = useState('');
  const [dateOfBirth, setDateOfBirth] = useState('');
  const [phoneNumber, setPhoneNumber] = useState('');
  const [avatarBase64, setAvatarBase64] = useState<string | undefined>(undefined);
  const [avatarPreview, setAvatarPreview] = useState<string | null>(null);
  const [avatarError, setAvatarError] = useState<string | null>(null);
  const [avatarCleared, setAvatarCleared] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [passwordError, setPasswordError] = useState<string | null>(null);
  const [passwordSubmitting, setPasswordSubmitting] = useState(false);

  const loadProfile = useCallback(() => {
    if (!userId) {
      setLoading(false);
      setError('User ID not available. Please log in again.');
      return;
    }
    setLoading(true);
    setError(null);
    getProfileByUserId(userId)
      .then((res) => {
        if (res.success && res.data) {
          setProfile(res.data);
          setFirstName(res.data.firstName);
          setMiddleName(res.data.middleName ?? '');
          setLastName(res.data.lastName);
          setDateOfBirth(res.data.dateOfBirth ? res.data.dateOfBirth.slice(0, 10) : '');
          setPhoneNumber(res.data.phoneNumber ?? '');
          setAvatarBase64(res.data.avatarBase64);
          setAvatarPreview(res.data.avatarBase64 ? `data:image/jpeg;base64,${res.data.avatarBase64}` : null);
          setAvatarCleared(false);
        } else {
          setProfile(null);
          setFirstName('');
          setMiddleName('');
          setLastName('');
          setDateOfBirth('');
          setPhoneNumber('');
          setAvatarBase64(undefined);
          setAvatarPreview(null);
          setAvatarCleared(false);
        }
      })
      .catch((err: { response?: { status?: number } }) => {
        if (err.response?.status === 404) {
          setProfile(null);
          setError(null);
        } else {
          setError('Failed to load profile.');
          setProfile(null);
        }
      })
      .finally(() => setLoading(false));
  }, [userId]);

  useEffect(() => {
    loadProfile();
  }, [loadProfile]);

  const onAvatarChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setAvatarError(null);
    const file = e.target.files?.[0];
    if (!file) {
      setAvatarBase64(undefined);
      setAvatarPreview(null);
      return;
    }
    if (!file.type.match(/^image\/(jpeg|png)$/)) {
      setAvatarError('Please choose a JPEG or PNG image.');
      setAvatarBase64(undefined);
      setAvatarPreview(null);
      return;
    }
    if (file.size > MAX_AVATAR_BYTES) {
      setAvatarError('Image must be 2MB or smaller.');
      setAvatarBase64(undefined);
      setAvatarPreview(null);
      return;
    }
    fileToBase64(file)
      .then((base64) => {
        setAvatarBase64(base64);
        setAvatarPreview(URL.createObjectURL(file));
      })
      .catch(() => {
        setAvatarError('Failed to read image.');
        setAvatarBase64(undefined);
        setAvatarPreview(null);
      });
  };

  const clearAvatar = () => {
    setAvatarBase64(undefined);
    setAvatarPreview(null);
    setAvatarError(null);
    setAvatarCleared(true);
  };

  const handleChangePassword = (e: React.FormEvent) => {
    e.preventDefault();
    setPasswordError(null);
    if (!currentPassword.trim()) {
      setPasswordError('Current password is required.');
      return;
    }
    if (!newPassword || newPassword.length < 8) {
      setPasswordError('New password must be at least 8 characters.');
      return;
    }
    if (newPassword !== confirmPassword) {
      setPasswordError('New password and confirmation do not match.');
      return;
    }
    setPasswordSubmitting(true);
    changePassword({ currentPassword, newPassword })
      .then((res) => {
        if (res.success) {
          setCurrentPassword('');
          setNewPassword('');
          setConfirmPassword('');
          addToast('success', 'Password changed successfully.');
        } else {
          setPasswordError(res.error?.message ?? res.message ?? 'Failed to change password.');
        }
      })
      .catch(() => setPasswordError('Network or server error.'))
      .finally(() => setPasswordSubmitting(false));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    if (!firstName.trim()) {
      setFormError('First name is required.');
      return;
    }
    if (!lastName.trim()) {
      setFormError('Last name is required.');
      return;
    }
    if (!dateOfBirth) {
      setFormError('Date of birth is required.');
      return;
    }
    if (!userId) {
      setFormError('User ID not available. Please log in again.');
      return;
    }

    setSubmitting(true);
    if (profile) {
      const payload: UpdateProfileRequest = {
        firstName: firstName.trim(),
        middleName: middleName.trim() || undefined,
        lastName: lastName.trim(),
        dateOfBirth: dateOfBirth,
        phoneNumber: phoneNumber.trim() || undefined,
        avatarBase64: avatarCleared ? '' : (avatarBase64 !== undefined ? (avatarBase64 || undefined) : undefined),
      };
      updateProfile(userId, payload)
        .then((res) => {
          if (res.success && res.data) {
            setProfile(res.data);
            addToast('success', 'Profile updated.');
            window.dispatchEvent(new CustomEvent('profile-updated'));
          } else {
            setFormError(res.error?.message ?? res.message ?? 'Failed to update profile.');
          }
        })
        .catch(() => setFormError('Network or server error.'))
        .finally(() => setSubmitting(false));
    } else {
      const payload: CreateProfileRequest = {
        userId,
        firstName: firstName.trim(),
        middleName: middleName.trim() || undefined,
        lastName: lastName.trim(),
        dateOfBirth,
        phoneNumber: phoneNumber.trim() || undefined,
        avatarBase64,
      };
      createProfile(payload)
        .then((res) => {
          if (res.success && res.data) {
            setProfile(res.data);
            addToast('success', 'Profile created.');
            window.dispatchEvent(new CustomEvent('profile-updated'));
          } else {
            setFormError(res.error?.message ?? res.message ?? 'Failed to create profile.');
          }
        })
        .catch(() => setFormError('Network or server error.'))
        .finally(() => setSubmitting(false));
    }
  };

  if (!userId) {
    return (
      <div className="space-y-8">
        <div className="page-header">
          <h1>Profile</h1>
          <p>Manage your profile.</p>
        </div>
        <div className="card card-body">
          <div className="alert-error">User ID could not be determined. Please log out and log in again to access your profile.</div>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-8">
      <div className="page-header">
        <h1>Profile</h1>
        <p>Manage your personal profile and avatar.</p>
      </div>

      {loading && (
        <div className="card card-body">
          <p className="text-gray-500 dark:text-gray-400">Loading profile…</p>
        </div>
      )}

      {error && !loading && (
        <div className="card card-body">
          <div className="alert-error">{error}</div>
        </div>
      )}

      {!loading && (
        <div className="card card-body">
          <h2 className="card-header">{profile ? 'Edit profile' : 'Create profile'}</h2>
          <p className="card-description mt-1">
            {profile ? 'Update your name, date of birth, phone number and avatar.' : 'Fill in your profile. You can add an avatar (JPEG or PNG, max 2MB).'}
          </p>

          <form onSubmit={handleSubmit} className="mt-6 space-y-4 max-w-xl">
            {formError && <div className="alert-error">{formError}</div>}

            <div className="form-group">
              <label htmlFor="profile-firstName" className="input-label">First name *</label>
              <input
                id="profile-firstName"
                type="text"
                value={firstName}
                onChange={(e) => setFirstName(e.target.value)}
                className="input"
                maxLength={100}
                required
              />
            </div>
            <div className="form-group">
              <label htmlFor="profile-middleName" className="input-label">Middle name</label>
              <input
                id="profile-middleName"
                type="text"
                value={middleName}
                onChange={(e) => setMiddleName(e.target.value)}
                className="input"
                maxLength={100}
              />
            </div>
            <div className="form-group">
              <label htmlFor="profile-lastName" className="input-label">Last name *</label>
              <input
                id="profile-lastName"
                type="text"
                value={lastName}
                onChange={(e) => setLastName(e.target.value)}
                className="input"
                maxLength={100}
                required
              />
            </div>
            <div className="form-group">
              <label htmlFor="profile-dateOfBirth" className="input-label">Date of birth *</label>
              <input
                id="profile-dateOfBirth"
                type="date"
                value={dateOfBirth}
                onChange={(e) => setDateOfBirth(e.target.value)}
                className="input"
                required
              />
            </div>
            <div className="form-group">
              <label htmlFor="profile-phoneNumber" className="input-label">Phone number</label>
              <input
                id="profile-phoneNumber"
                type="text"
                value={phoneNumber}
                onChange={(e) => setPhoneNumber(e.target.value)}
                className="input"
                maxLength={30}
              />
            </div>

            <div className="form-group">
              <label className="input-label">Avatar</label>
              <p className="text-sm text-gray-500 dark:text-gray-400 mb-2">JPEG or PNG, max 2MB. Optional.</p>
              <input
                type="file"
                accept={ACCEPT_IMAGE}
                onChange={onAvatarChange}
                className="block w-full text-sm text-gray-600 dark:text-gray-400 file:mr-4 file:py-2 file:px-4 file:rounded file:border-0 file:font-medium file:bg-primary-50 file:text-primary-700 dark:file:bg-primary-900/30 dark:file:text-primary-300"
              />
              {avatarError && <p className="mt-1 text-sm text-red-600 dark:text-red-400">{avatarError}</p>}
              {avatarPreview && (
                <div className="mt-3 flex items-center gap-4">
                  <img src={avatarPreview} alt="Avatar preview" className="w-20 h-20 rounded-full object-cover border border-gray-200 dark:border-gray-700" />
                  <button type="button" onClick={clearAvatar} className="btn-secondary text-sm">Remove avatar</button>
                </div>
              )}
            </div>

            <div className="flex justify-end gap-2 pt-2">
              <button type="submit" disabled={submitting} className="btn-primary">
                {submitting ? 'Saving…' : profile ? 'Save changes' : 'Create profile'}
              </button>
            </div>
          </form>
        </div>
      )}

      {!loading && userId && (
        <div className="card card-body">
          <h2 className="card-header">Change password</h2>
          <p className="card-description mt-1">Update your account password. New password must be at least 8 characters.</p>
          <form onSubmit={handleChangePassword} className="mt-6 space-y-4 max-w-xl">
            {passwordError && <div className="alert-error">{passwordError}</div>}
            <div className="form-group">
              <label htmlFor="profile-currentPassword" className="input-label">Current password *</label>
              <input
                id="profile-currentPassword"
                type="password"
                value={currentPassword}
                onChange={(e) => setCurrentPassword(e.target.value)}
                className="input"
                autoComplete="current-password"
                required
              />
            </div>
            <div className="form-group">
              <label htmlFor="profile-newPassword" className="input-label">New password * (min 8 characters)</label>
              <input
                id="profile-newPassword"
                type="password"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                className="input"
                autoComplete="new-password"
                minLength={8}
                required
              />
            </div>
            <div className="form-group">
              <label htmlFor="profile-confirmPassword" className="input-label">Confirm new password *</label>
              <input
                id="profile-confirmPassword"
                type="password"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                className="input"
                autoComplete="new-password"
                minLength={8}
                required
              />
            </div>
            <div className="flex justify-end gap-2 pt-2">
              <button type="submit" disabled={passwordSubmitting} className="btn-primary">
                {passwordSubmitting ? 'Changing…' : 'Change password'}
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}
