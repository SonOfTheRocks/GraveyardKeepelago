using System;
using System.Linq;
using UnityEngine;

namespace GraveyardKeepelago.GameModifications.UI;

internal sealed class NativeConnectionButton : IDisposable
{
    private readonly GameObject _host;
    private readonly UIButton _button;
    private readonly UIButton _prototype;
    private readonly UIBasicSprite _background;
    private readonly UITexture _icon;
    private readonly Texture2D _colorIcon, _grayIcon;
    private readonly Color _normal, _hover, _pressed;
    private readonly Sprite _normalSprite;
    private readonly string _normalSpriteName;
    private bool? _connected;
    private int _verifyAfterFrame;
    private bool _placementVerified;

    public NativeConnectionButton(MainMenuGUI menu, DialogGUI dialog, Action clicked)
    {
        var prototypes = dialog.GetComponentsInChildren<DialogButtonGUI>(true)
            .SelectMany(d => d.GetComponentsInChildren<UIButton>(true));
        _prototype = prototypes.FirstOrDefault(b => FindBackground(b) != null);
        if (_prototype == null)
            throw new InvalidOperationException("No native DialogButtonGUI background and UIButton prototype found.");
        var source = FindBackground(_prototype);
        var panel = menu.GetComponentInParent<UIPanel>();
        if (panel == null)
            throw new InvalidOperationException("Main menu has no native UI panel.");
        var screen = GUIElements.me.screen_size_widget;
        if (screen == null)
            throw new InvalidOperationException("Native screen_size_widget is missing; cannot anchor the connection button.");
        _colorIcon = APSpriteLoader.GetSprite("archipelago_32x32")?.texture;
        if (_colorIcon == null)
            throw new InvalidOperationException("Archipelago icon resource is missing.");
        _normal = _prototype.defaultColor;
        _hover = _prototype.hover;
        _pressed = _prototype.pressed;
        _normalSprite = _prototype.normalSprite2D;
        _normalSpriteName = _prototype.normalSprite;
        _grayIcon = Grayscale(_colorIcon);
        _host = new GameObject("Archipelago Connection Button");
        _host.SetActive(false);
        _host.layer = menu.gameObject.layer;
        _host.transform.SetParent(menu.transform, false);
        var go = NativeUiPrototypeCatalog.Clone(source.gameObject, _host.transform, "Native button frame");
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        foreach (var label in go.GetComponentsInChildren<UILabel>(true))
            label.enabled = false;
        _background = go.GetComponent<UIBasicSprite>();
        var autoSize = go.GetComponent<PolygonColliderAutoSize>();
        if (autoSize != null)
            autoSize.enabled = false;
        var depth = menu.GetComponentsInChildren<UIWidget>(true).Max(w => w.depth) + 1;
        _background.depth = depth;
        var collider = go.GetComponent<Collider2D>();
        if (collider == null)
            NGUITools.AddWidgetCollider(go);
        if (collider != null)
            collider.enabled = true;
        _button = go.AddComponent<UIButton>();
        _button.tweenTarget = go;
        _button.duration = _prototype.duration;
        _button.pressedSprite2D = _prototype.pressedSprite2D;
        _button.hoverSprite2D = _prototype.hoverSprite2D;
        _button.disabledSprite2D = _prototype.disabledSprite2D;
        _button.hoverSprite = _prototype.hoverSprite;
        _button.pressedSprite = _prototype.pressedSprite;
        _button.disabledSprite = _prototype.disabledSprite;
        _button.onClick.Add(new EventDelegate((EventDelegate.Callback)(() => clicked())));
        var iconGo = new GameObject("Connection status icon");
        iconGo.layer = _host.layer;
        iconGo.transform.SetParent(_host.transform, false);
        _icon = iconGo.AddComponent<UITexture>();
        _icon.width = _icon.height = 48; // Includes the resource's 8-pixel transparent margin.
        _icon.depth = depth + 1;
        _icon.mainTexture = _grayIcon;
        var anchor = _host.AddComponent<UIAnchor>();
        // Graveyard Keeper's UI camera renders screen_size_widget at two screen pixels per unit.
        // UIPanel/UIRoot's default activeHeight does not describe this canvas and places us off-screen.
        anchor.container = screen.gameObject;
        anchor.side = UIAnchor.Side.TopRight;
        anchor.pixelOffset = new Vector2(-30, -30); // 10 native units outside the 40-unit button.
        anchor.runOnlyOnce = false;
    }

    private static UIBasicSprite FindBackground(UIButton button) =>
        button.tweenTarget?.GetComponent<UIBasicSprite>() ?? button.GetComponentInChildren<UIBasicSprite>(true);

    public void SetVisible(bool visible)
    {
        if (_host != null && _host.activeSelf != visible)
        {
            _host.SetActive(visible);
            _verifyAfterFrame = Time.frameCount + 2;
        }
    }

    public void VerifyPlacement(Action<string> report)
    {
        if (_placementVerified || !_host.activeInHierarchy || Time.frameCount < _verifyAfterFrame)
            return;
        _placementVerified = true;
        var camera = NGUITools.FindCameraForLayer(_host.layer);
        var viewport = camera == null ? new Vector3(-1, -1, -1) : camera.WorldToViewportPoint(_icon.transform.position);
        if (camera == null || !NativeButtonVisibility.IsUsable(viewport.x, viewport.y,
                _background.isVisible, _icon.isVisible, _button.isEnabled))
            throw new InvalidOperationException($"Native AP button is not visible and clickable: viewport={viewport}, " +
                $"frameVisible={_background.isVisible}, iconVisible={_icon.isVisible}, clickable={_button.isEnabled}.");
        report?.Invoke($"Native AP button placement verified: viewport={viewport}, frame and icon visible, collider enabled.");
    }

    public void SetConnected(bool connected)
    {
        if (_connected == connected)
            return;
        _connected = connected;
        _icon.mainTexture = connected ? _colorIcon : _grayIcon;
        _button.defaultColor = connected ? _normal : _prototype.disabledColor;
        _button.hover = connected ? _hover : Color.Lerp(_prototype.disabledColor, Color.white, 0.15f);
        _button.pressed = connected ? _pressed : _prototype.disabledColor * new Color(0.8f, 0.8f, 0.8f, 1);
        _button.normalSprite2D = !connected && _prototype.disabledSprite2D != null
            ? _prototype.disabledSprite2D : _normalSprite;
        _button.normalSprite = !connected && !string.IsNullOrEmpty(_prototype.disabledSprite)
            ? _prototype.disabledSprite : _normalSpriteName;
        _button.hoverSprite = connected ? _prototype.hoverSprite : _button.normalSprite;
        _button.pressedSprite = connected ? _prototype.pressedSprite : _button.normalSprite;
        _button.hoverSprite2D = connected ? _prototype.hoverSprite2D : _button.normalSprite2D;
        _button.pressedSprite2D = connected ? _prototype.pressedSprite2D : _button.normalSprite2D;
        // Disconnected describes appearance, not interactivity: this button opens the connection form.
        _button.SetState(UIButtonColor.State.Normal, true);
        NativeSpriteFrame.Fit(_background, 40, 40, Vector2.zero);
        var drawn = _background.drawingDimensions;
        var collider = _background.GetComponent<Collider2D>();
        if (collider is PolygonCollider2D polygon)
            polygon.points = new[] { new Vector2(drawn.x, drawn.y), new Vector2(drawn.x, drawn.w),
                new Vector2(drawn.z, drawn.w), new Vector2(drawn.z, drawn.y) };
        else if (collider is BoxCollider2D box)
        {
            box.size = new Vector2(drawn.z - drawn.x, drawn.w - drawn.y);
            box.offset = new Vector2((drawn.x + drawn.z) / 2, (drawn.y + drawn.w) / 2);
        }
    }

    private static Texture2D Grayscale(Texture2D source)
    {
        var pixels = source.GetPixels32();
        var bytes = new byte[pixels.Length * 4];
        for (var i = 0; i < pixels.Length; i++)
        {
            bytes[i * 4] = pixels[i].r;
            bytes[i * 4 + 1] = pixels[i].g;
            bytes[i * 4 + 2] = pixels[i].b;
            bytes[i * 4 + 3] = pixels[i].a;
        }
        bytes = ConnectionIconPixels.ForConnectionState(bytes, false);
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(bytes[i * 4], bytes[i * 4 + 1], bytes[i * 4 + 2], bytes[i * 4 + 3]);
        var texture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }

    public void Dispose()
    {
        if (_host != null) UnityEngine.Object.Destroy(_host);
        if (_grayIcon != null) UnityEngine.Object.Destroy(_grayIcon);
    }
}
