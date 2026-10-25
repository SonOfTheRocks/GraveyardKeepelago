using System;
using GraveyardKeepelago.Archipelago;
using GraveyardKeepelago.GameModifications.UI;

namespace GraveyardKeepelago.GameModifications;

public sealed class MainMenuConnectionWindow : IDisposable
{
    public delegate bool ConnectDelegate(ConnectionSettings settings, out string message);
    public delegate bool DisconnectDelegate(out string message);
    private readonly ConnectionMenuController _controller;
    private readonly Func<bool> _isConnected;
    private readonly Action<string> _logError;
    private readonly Action<string> _logInfo;
    private DialogGUI _dialog;
    private NativeModalForm _form;
    private NativeTextInput _server, _slot, _password;
    private NativeToggle _deathLink;
    private NativeConnectionButton _menuButton;
    private MainMenuGUI _menuButtonOwner;
    private string _displayPrimary, _displayText;
    public bool IsOpen { get; private set; }

    public MainMenuConnectionWindow(ConnectionSettings settings, ConnectDelegate connect,
        DisconnectDelegate disconnect, Func<bool> isConnected, Func<bool> isGameInitialized,
        Action<string> logError = null, Action<string> logInfo = null)
    {
        _isConnected = isConnected;
        _logError = logError;
        _logInfo = logInfo;
        _controller = new ConnectionMenuController(settings, connect.Invoke, disconnect.Invoke,
            isConnected, isGameInitialized);
    }

    public void Update()
    {
        _controller.RefreshConnectionStatus();
        UpdateMenuButton();
        if (!IsOpen)
            return;
        if (_dialog == null || !_dialog.is_shown)
        {
            OnDialogHidden();
            return;
        }
        SetEditable();
        _form?.Reposition();
        if (_dialog.is_shown_and_top &&
            (_displayPrimary != _controller.PrimaryLabel || _displayText != BuildDialogText()))
        {
            _dialog.Hide();
            OnDialogHidden();
            OpenNativeDialog();
        }
    }

    private void OpenNativeDialog()
    {
        var dialog = GUIElements.me?.dialog;
        if (dialog == null || dialog.is_shown)
            return;
        try
        {
            // Build before opening, so a missing prototype cannot strand the native modal stack.
            if (_dialog != dialog || _form == null)
            {
                DisposeForm();
                _dialog = dialog;
                _form = new NativeModalForm(dialog);
                _server = _form.AddTextInput("Server (host:port)");
                _slot = _form.AddTextInput("Slot name");
                _password = _form.AddTextInput("Password (optional)", true);
                _deathLink = _form.AddToggle("DeathLink");
            }
            var model = _controller.Model;
            _server.Value = model.ServerAddress;
            _slot.Value = model.SlotName;
            _password.Value = model.Password;
            _deathLink.Value = model.DeathLink;
            SetEditable();
            _form.Prepare();
            IsOpen = true;
            _displayPrimary = _controller.PrimaryLabel;
            _displayText = BuildDialogText();
            dialog.Open(_displayText, _displayPrimary, OnPrimaryAction,
                _controller.Locked ? null : "Back", null, OnDialogHidden);
            _form.Show();
        }
        catch (Exception ex)
        {
            if (IsOpen && dialog.is_shown)
                dialog.Hide();
            DisposeForm();
            _logError?.Invoke($"Could not open native Archipelago form: {ex}");
        }
    }

    private string BuildDialogText()
    {
        var status = _controller.Locked ? "The connection is locked after loading a save." : _controller.Status;
        var color = _controller.HasError ? "E77B73" : "C9BDA5";
        return "[b]Archipelago connection[/b]" + new string('\n', 15) + $"[{color}]{status}[-]";
    }

    private void SetEditable()
    {
        var editable = _controller.Editable;
        _server?.SetEditable(editable);
        _slot?.SetEditable(editable);
        _password?.SetEditable(editable);
        _deathLink?.SetEditable(editable);
    }

    private void OnDialogHidden()
    {
        // DialogGUI invokes this before its primary action; capture even the focused input.
        if (_form != null)
        {
            var model = _controller.Model;
            model.ServerAddress = _server.Value;
            model.SlotName = _slot.Value;
            model.Password = _password.Value;
            model.DeathLink = _deathLink.Value;
            _form.Hide();
        }
        IsOpen = false;
    }

    private void OnPrimaryAction()
    {
        if (!_controller.ExecutePrimary())
            OpenNativeDialog();
    }

    private void DisposeForm()
    {
        _form?.Dispose();
        _form = null;
        _server = _slot = _password = null;
        _deathLink = null;
        IsOpen = false;
    }

    public void Dispose()
    {
        DisposeForm();
        _menuButton?.Dispose();
        _menuButton = null;
    }

    private void UpdateMenuButton()
    {
        var menu = GUIElements.me?.main_menu;
        if (menu == null || GUIElements.gui_is_initializing)
            return;
        if (_menuButtonOwner != menu)
        {
            _menuButton?.Dispose();
            _menuButton = null;
            _menuButtonOwner = menu;
            try
            {
                _menuButton = new NativeConnectionButton(menu, GUIElements.me.dialog, OpenNativeDialog);
            }
            catch (Exception ex)
            {
                _logError?.Invoke($"Could not create native Archipelago button: {ex}");
            }
        }
        _menuButton?.SetConnected(_isConnected());
        _menuButton?.SetVisible(menu.is_shown_and_top && !IsOpen);
        try
        {
            _menuButton?.VerifyPlacement(_logInfo);
        }
        catch (Exception ex)
        {
            _logError?.Invoke(ex.Message);
        }
    }
}
