# Windows Project Changelog

## Unreleased
- Added `xmlns:local="clr-namespace:Windows"` to the root `<Window>` in `ChatWindow.xaml`, resolving XAML namespace issues.
- Rebuilt all projects.
- Build error in Windows project:
    - `ChatWindow.xaml.cs(148,70): error CS1503: Argument 2: cannot convert from 'System.Threading.CancellationToken' to 'string'.`
- All other projects build cleanly.
- Fixed SendMessage invocation at ChatWindow.xaml.cs:148 by passing the correct string argument instead of a CancellationToken. This corrects a legacy oversight after cancellation support was added and resolves a key build-blocking issue.
- Added a new public `InverseBoolToVisibilityConverter` implementing `IValueConverter` in the Windows namespace. This resolves a missing converter build error referenced in ChatWindow.xaml, and enables correct visibility binding for UI elements controlled by boolean properties.
