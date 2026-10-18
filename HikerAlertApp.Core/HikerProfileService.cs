namespace HikerAlertApp.Core;

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

}
