using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;

namespace HikerAlertApp.Core;

public sealed record NearbyTrail(
    string Name,
    string ImageUrl,
    string Location,
    string Weather,
    DateTimeOffset UpdatedAtUtc);

public interface INearbyTrailService
{
    Task<NearbyTrail?> GetNearbyTrailAsync(
        CancellationToken cancellationToken = default);
}

public sealed class NearbyTrailViewModel : INotifyPropertyChanged
{
    private readonly INearbyTrailService _trailService;
    private NearbyTrail? _trail;

    public NearbyTrailViewModel(INearbyTrailService trailService)
    {
        _trailService = trailService;
    }

    public NearbyTrail? Trail
    {
        get => _trail;
        private set
        {
            if (Equals(_trail, value))
            {
                return;
            }

            _trail = value;
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nameof(Trail)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        Trail = await _trailService.GetNearbyTrailAsync(cancellationToken);
    }
}
