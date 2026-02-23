using System.ComponentModel;
using Castara.Wpf.Models;

namespace Castara.Wpf.Services.Status;

public sealed class StatusService : IStatusService
{
    private StatusState _current = new(AppStatusLevel.Ok, "Ready", "SQLite • Local");

    public StatusState Current
    {
        get => _current;
        private set
        {
            if (Equals(_current, value)) return;
            _current = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Current)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Set(StatusState state) => Current = state;

    public void Set(AppStatusLevel level, string leftText, string rightText)
        => Current = new StatusState(level, leftText, rightText);
}