namespace HikerAlertApp.Core;

//ranger's one-tap speed dial to emergency services.
public sealed class RangerService
{
    public const string DefaultEmergencyNumber = "111";

    private readonly IEmergencyDialer _dialer;

    public RangerService(IEmergencyDialer dialer)
        => _dialer = dialer ?? throw new ArgumentNullException(nameof(dialer));

    public Task<bool> SpeedDialEmergencyServicesAsync(
        string emergencyNumber = DefaultEmergencyNumber,
        CancellationToken cancellationToken = default)
        => _dialer.DialAsync(emergencyNumber, cancellationToken);
}
