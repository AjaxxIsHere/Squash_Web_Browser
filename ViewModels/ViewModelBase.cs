using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Squash_Web_Browser.ViewModels;


/*   This is a base class for all ViewModels in the application.
    It implements INotifyPropertyChanged to support data binding in the UI.
    Think of this as a "blueprint" for how the app's data should behave and notify the UI when things change.
*/
public abstract class ViewModelBase : INotifyPropertyChanged
{

    // When a property changes, this event is raised to notify any listeners (e.g the UI)
    public event PropertyChangedEventHandler? PropertyChanged;


    // This method is called to raise the PropertyChanged event.
    protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
