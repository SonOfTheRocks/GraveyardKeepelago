using System;
using GraveyardKeepelago.Archipelago;
using UnityEngine;

namespace GraveyardKeepelago.GameModifications;

public sealed class MainMenuConnectionWindow
{
    public delegate bool ConnectDelegate(ConnectionSettings settings, out string message);
    public delegate bool DisconnectDelegate(out string message);

    private const int WindowID = 0x47594B;
    private const float ButtonSize = 56f;
    private const float ButtonMargin = 20f;
    private const float WindowWidth = 410f;
    private const float WindowHeight = 330f;

    private readonly ConnectionMenuModel _model;
    private readonly ConnectDelegate _connect;
    private readonly DisconnectDelegate _disconnect;
    private readonly Func<bool> _isConnected;
    private readonly Func<bool> _isGameInitialized;
    private Rect _windowRect;
    private string _status = "Not connected";
    private MessageType _messageType = MessageType.Neutral;
    private bool _wasConnected;
    private bool _isOpen;
    private Texture2D _logoTexture;
    private GUIStyle _iconButtonStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _statusStyle;

    public MainMenuConnectionWindow(ConnectionSettings settings, ConnectDelegate connect,
        DisconnectDelegate disconnect, Func<bool> isConnected, Func<bool> isGameInitialized)
    {
        _model = new ConnectionMenuModel(settings);
        _connect = connect;
        _disconnect = disconnect;
        _isConnected = isConnected;
        _isGameInitialized = isGameInitialized;
    }

    public void Draw()
    {
        EnsureStyles();
        EnsureLogo();

        var connected = _isConnected();
        UpdateConnectionStatus(connected);

        var buttonRect = new Rect(
            Screen.width - ButtonSize - ButtonMargin,
            ButtonMargin,
            ButtonSize,
            ButtonSize);
        var previousColor = GUI.color;
        GUI.color = connected ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f);
        var tooltip = connected ? "Archipelago: connected" : "Archipelago: not connected";
        if (GUI.Button(buttonRect, new GUIContent(_logoTexture, tooltip), _iconButtonStyle))
        {
            _isOpen = !_isOpen;
            if (_isOpen && _windowRect.width <= 0f)
            {
                _windowRect = new Rect(
                    (Screen.width - WindowWidth) / 2f,
                    (Screen.height - WindowHeight) / 2f,
                    WindowWidth,
                    WindowHeight);
            }
        }
        GUI.color = previousColor;

        if (buttonRect.Contains(Event.current.mousePosition) && !string.IsNullOrEmpty(GUI.tooltip))
        {
            var tooltipRect = new Rect(buttonRect.x - 155f, buttonRect.y + ButtonSize + 4f, 210f, 24f);
            GUI.Label(tooltipRect, GUI.tooltip, _statusStyle);
        }

        if (!_isOpen)
            return;

        if (_windowRect.width <= 0f)
            _windowRect = new Rect(
                (Screen.width - WindowWidth) / 2f,
                (Screen.height - WindowHeight) / 2f,
                WindowWidth,
                WindowHeight);

        _windowRect.x = Mathf.Clamp(_windowRect.x, 0f, Mathf.Max(0f, Screen.width - _windowRect.width));
        _windowRect.y = Mathf.Clamp(_windowRect.y, 0f, Mathf.Max(0f, Screen.height - _windowRect.height));
        _windowRect = GUI.ModalWindow(WindowID, _windowRect, DrawContents, "Archipelago Connection");
    }

    private void DrawContents(int windowID)
    {
        if (GUI.Button(new Rect(WindowWidth - 31f, 3f, 25f, 20f), "X"))
        {
            _isOpen = false;
            return;
        }

        GUILayout.Space(4f);
        GUILayout.Label("Multiworld Connection", _titleStyle);
        GUILayout.Space(6f);

        var connected = _isConnected();
        var gameInitialized = _isGameInitialized();
        var previousEnabled = GUI.enabled;
        GUI.enabled = previousEnabled && !connected && !gameInitialized;

        _model.ServerAddress = DrawTextField("Server (host:port)", _model.ServerAddress);
        _model.SlotName = DrawTextField("Slot name", _model.SlotName);
        _model.Password = DrawPasswordField("Password (optional)", _model.Password);
        _model.DeathLink = GUILayout.Toggle(_model.DeathLink, "Enable DeathLink");

        GUI.enabled = previousEnabled;
        GUILayout.Space(6f);

        if (connected)
        {
            _messageType = MessageType.Success;
            if (!gameInitialized && GUILayout.Button("Disconnect", GUILayout.Height(30f)))
            {
                if (_disconnect(out var message))
                {
                    _status = message;
                    _messageType = MessageType.Neutral;
                    _wasConnected = _isConnected();
                }
                else
                {
                    _status = message;
                    _messageType = MessageType.Error;
                }
            }
            else if (gameInitialized)
            {
                GUILayout.Label("The connection is locked after loading a save.");
            }
        }
        else if (gameInitialized)
        {
            _messageType = MessageType.Error;
            GUILayout.Label("Return to the title screen and restart the game to change the connection.");
        }
        else if (GUILayout.Button("Connect", GUILayout.Height(30f)))
        {
            Connect();
        }

        var previousColor = GUI.contentColor;
        GUI.contentColor = GetMessageColor();
        GUILayout.Label(_status, _statusStyle, GUILayout.MinHeight(38f));
        GUI.contentColor = previousColor;

        GUI.DragWindow(new Rect(0f, 0f, WindowWidth, 28f));
    }

    private void Connect()
    {
        if (!_model.TryCreateSettings(out var settings, out var validationError))
        {
            _status = validationError;
            _messageType = MessageType.Error;
            return;
        }

        if (_connect(settings, out var message))
        {
            _status = message;
            _messageType = MessageType.Success;
            _wasConnected = _isConnected();
            _isOpen = false;
        }
        else
        {
            _status = message;
            _messageType = MessageType.Error;
        }
    }

    private static string DrawTextField(string label, string value)
    {
        GUILayout.Label(label);
        return GUILayout.TextField(value ?? string.Empty, GUILayout.Height(24f));
    }

    private static string DrawPasswordField(string label, string value)
    {
        GUILayout.Label(label);
        return GUILayout.PasswordField(value ?? string.Empty, '*', GUILayout.Height(24f));
    }

    private Color GetMessageColor()
    {
        switch (_messageType)
        {
            case MessageType.Success:
                return new Color(0.45f, 1f, 0.45f);
            case MessageType.Error:
                return new Color(1f, 0.45f, 0.45f);
            default:
                return Color.white;
        }
    }

    private void UpdateConnectionStatus(bool connected)
    {
        if (_wasConnected && !connected)
        {
            _status = "Connection lost. Check the server and connect again.";
            _messageType = MessageType.Error;
        }
        else if (!_wasConnected && connected)
        {
            _status = "Connected";
            _messageType = MessageType.Success;
        }

        _wasConnected = connected;
    }

    private void EnsureLogo()
    {
        if (_logoTexture != null)
            return;

        var sprite = APSpriteLoader.GetSprite("archipelago_32x32");
        _logoTexture = sprite?.texture;
    }

    private void EnsureStyles()
    {
        if (_titleStyle != null)
            return;

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = 16,
        };
        _statusStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperCenter,
            wordWrap = true,
        };
        _iconButtonStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            imagePosition = ImagePosition.ImageOnly,
            padding = new RectOffset(3, 3, 3, 3),
        };
    }

    private enum MessageType
    {
        Neutral,
        Success,
        Error,
    }
}
