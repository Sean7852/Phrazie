using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using Phrazie.Core.Interfaces;
using Phrazie.Desktop.Views;

namespace Phrazie.Desktop.ViewModels;

public sealed partial class AccountSectionViewModel : SettingsSectionViewModel
{
    private readonly ISessionStore _store;
    private readonly Func<Task>    _signOut;

    public override string SectionName        => "Account";
    public override string SectionDescription => "Sign out and app information.";
    public override string SectionIcon        => "◎";

    public string? UserEmail => _store.CurrentUser?.Email;

    public AccountSectionViewModel(ISessionStore store, Func<Task> signOut)
    {
        _store   = store;
        _signOut = signOut;
    }

    [RelayCommand]
    private async Task SignOutAsync() => await _signOut();

    [RelayCommand]
    private void ShowAbout()
    {
        if (Application.Current?.ApplicationLifetime
                is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            var dialog = new AboutDialog();
            dialog.ShowDialog(lifetime.MainWindow!);
        }
    }
}
