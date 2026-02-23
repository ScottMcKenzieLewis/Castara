using Castara.Wpf.Models;
using System.ComponentModel;

namespace Castara.Wpf.Services.Navigation;

public interface INavigationService : INotifyPropertyChanged
{
    object CurrentView { get; }
    string PageTitle { get; }

    void Navigate(NavRoute route);
}