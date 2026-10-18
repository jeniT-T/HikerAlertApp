<<<<<<< HEAD
﻿using System;
using System.Collections.Generic;
using System.Text;

namespace HikerAlertApp.Core;
public sealed record HikerProfile(
    string? Name = null,
    string? BloodType = null,
    string? MobilePhone = null,
    string? EmergencyContactName = null,
    string? EmergencyContactPhone = null,
    string? EmergencyNote = null);

public interface IHikerProfileRepository
{
    Task<HikerProfile?> GetAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(HikerProfile profile, CancellationToken cancellationToken = default);
}

public interface IAccountDeletionService
{
    // Must delete the signed-in account, its credentials, and associated profile data.
    Task DeleteCurrentAccountAsync(CancellationToken cancellationToken = default);
}

public sealed class HikerProfileService(
    IHikerProfileRepository profileRepository,
    IAccountDeletionService accountDeletion)
{
    public Task<HikerProfile?> GetAsync(CancellationToken cancellationToken = default) =>
        profileRepository.GetAsync(cancellationToken);

    public Task SaveAsync(
        HikerProfile profile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        // Every field is optional. Empty or whitespace-only values are stored as null.
        var cleanedProfile = profile with
        {
            Name = Clean(profile.Name),
            BloodType = Clean(profile.BloodType),
            MobilePhone = Clean(profile.MobilePhone),
            EmergencyContactName = Clean(profile.EmergencyContactName),
            EmergencyContactPhone = Clean(profile.EmergencyContactPhone),
            EmergencyNote = Clean(profile.EmergencyNote)
        };

        return profileRepository.SaveAsync(cleanedProfile, cancellationToken);
    }

    public Task DeleteAccountAsync(CancellationToken cancellationToken = default) =>
        accountDeletion.DeleteCurrentAccountAsync(cancellationToken);

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
=======
﻿namespace HikerAlertApp.Core;

public sealed record HikerProfile(
    string normalisedEmail,
    string Name = "",
    string BloodType = "",
    string MobilePhone = "",
    string EmergencyContact = "",
    string MedicalNotes = "");

//FR02 — create, edit and read a hiker profile. All fields are optional.

public sealed class HikerProfileService
{
    private readonly Dictionary<string, HikerProfile> _profiles = new(StringComparer.Ordinal);

    public HikerProfile GetOrCreate(string normalisedEmail)
        => _profiles.TryGetValue(normalisedEmail, out var profile)
            ? profile
            : new HikerProfile(normalisedEmail);

    public bool TryGet(string normalisedEmail, out HikerProfile profile)
        => _profiles.TryGetValue(normalisedEmail, out profile!);

    public HikerProfile Save(HikerProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        _profiles[profile.normalisedEmail] = profile;
        return profile;
    }

    public bool Delete(string normalisedEmail) => _profiles.Remove(normalisedEmail);

    public int Count => _profiles.Count;

>>>>>>> homepage
}
