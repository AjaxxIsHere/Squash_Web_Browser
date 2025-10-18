using System.Collections.Generic;

namespace Squash_Web_Browser.Services;

public interface INavService
{
    string? Current { get; }
    bool CanGoBack();
    bool CanGoForward();
    void NavigateTo(string url);
    string? GoBack();
    string? GoForward();

}

/*
Summary: This code defines a navigation service interface (INavService) and its implementation (NavService) for managing navigation history in a web browser context.
The NavService class maintains a list of URLs to represent the navigation stack and provides methods to navigate to new URLs, go back, and go forward in the navigation history. Its methods include:

- NavigateTo(string url): Adds a new URL to the navigation stack and updates the current index. Prevents adding duplicate consecutive URLs.
- GoBack(): Moves back in the navigation stack if possible and returns the current URL.
- GoForward(): Moves forward in the navigation stack if possible and returns the current URL.
- CanGoBack() and CanGoForward(): Check if backward or forward navigation is possible.
- Current: A property that returns the current URL in the navigation stack.

*/
public sealed class NavService : INavService
{
    private readonly List<string> _navigationStack = new();     // Initialize a simple navigation stack to keep track of URLs using a list. (kind of similar to a stack but allows forward navigation)
    private int _navigationIndex = 0;     // Index to track the current position in the navigation stack, 0 indicates first item

    // Property to get the current URL in the navigation stack
    public string? Current => (_navigationIndex >= 0 && _navigationIndex < _navigationStack.Count)
        ? _navigationStack[_navigationIndex]
        : null;

    public bool CanGoBack() => _navigationIndex > 0;
    public bool CanGoForward() => _navigationIndex < _navigationStack.Count - 1;

    public void NavigateTo(string url)
    {
        // Don't add to stack if it's the same as the current URL
        if (_navigationStack.Count > 0 && _navigationIndex >= 0 && _navigationIndex < _navigationStack.Count && _navigationStack[_navigationIndex] == url)
        {
            return;
        }

        if (_navigationIndex < _navigationStack.Count - 1)
        {
            // We are navigating forward from a point in the history, so we clear the forward stack
            _navigationStack.RemoveRange(_navigationIndex + 1, _navigationStack.Count - (_navigationIndex + 1));
        }
        _navigationStack.Add(url);
        _navigationIndex++;
    }

    public string? GoBack()
    {
        if (CanGoBack())
        {
            _navigationIndex--;
            return Current;
        }
        return null;
    }

    public string? GoForward()
    {
        if (CanGoForward())
        {
            _navigationIndex++;
            return Current;
        }
        return null;
    }
}
