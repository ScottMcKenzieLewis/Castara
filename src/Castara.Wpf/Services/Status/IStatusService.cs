using System.ComponentModel;
using Castara.Wpf.Models;

namespace Castara.Wpf.Services.Status;

public interface IStatusService : INotifyPropertyChanged
{
    StatusState Current { get; }
    void Set(StatusState state);
    void Set(AppStatusLevel level, string leftText, string rightText);
}