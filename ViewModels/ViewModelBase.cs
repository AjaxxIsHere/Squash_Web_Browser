using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Squash_Web_Browser.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{

    // When a property changes, this event is raised to notify any listeners (e.g., the UI)
    // For dummies: This is like a "change alarm" that tells the app when something important has changed.
    public event PropertyChangedEventHandler? PropertyChanged;


    // This method is called to raise the PropertyChanged event.
    // For dummies: When you change something important, this method rings the "change alarm"
    protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
