using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phrazie.Core.Interfaces;

namespace Phrazie.Desktop.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _auth;

    [ObservableProperty] private string _email        = string.Empty;
    [ObservableProperty] private string _password     = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool   _isSignUpMode = false;
    [ObservableProperty] private bool   _isBusy       = false;

    public string ToggleLabel  => IsSignUpMode ? "Already have an account? Sign in" : "No account yet? Sign up";
    public string SubmitLabel  => IsSignUpMode ? "Create account"                   : "Sign in";
    public string HeadingLabel => IsSignUpMode ? "Create your account"              : "Welcome back";

    /// <summary>Raised on successful sign-in or sign-up. The main window listens and shows the app.</summary>
    public event Action? LoginSucceeded;

    public LoginViewModel(IAuthService auth) => _auth = auth;

    [RelayCommand]
    private void ToggleMode()
    {
        IsSignUpMode = !IsSignUpMode;
        ErrorMessage = string.Empty;
        OnPropertyChanged(nameof(ToggleLabel));
        OnPropertyChanged(nameof(SubmitLabel));
        OnPropertyChanged(nameof(HeadingLabel));
    }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync()
    {
        ErrorMessage = string.Empty;
        IsBusy       = true;

        var result = IsSignUpMode
            ? await _auth.SignUpAsync(Email.Trim(), Password)
            : await _auth.SignInAsync(Email.Trim(), Password);

        IsBusy = false;

        if (result.Success)
            LoginSucceeded?.Invoke();
        else
            ErrorMessage = result.ErrorMessage ?? "Something went wrong.";
    }

    private bool CanSubmit() =>
        !IsBusy &&
        !string.IsNullOrWhiteSpace(Email) &&
        Password.Length >= 6;

    partial void OnEmailChanged(string    value) => SubmitCommand.NotifyCanExecuteChanged();
    partial void OnPasswordChanged(string value) => SubmitCommand.NotifyCanExecuteChanged();
    partial void OnIsBusyChanged(bool     value) => SubmitCommand.NotifyCanExecuteChanged();
}
