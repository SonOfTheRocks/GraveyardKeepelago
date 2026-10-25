using UnityEngine;

namespace GraveyardKeepelago.GameModifications.UI;

// NGUI owns drawing and hit testing. These tiny point-filtered textures are mod-created geometry.
internal sealed class NativeSwitchVisual : MonoBehaviour
{
    private UITexture _track, _knob;
    private Texture2D _trackTexture, _knobTexture;
    private NativeControlGeometry _bounds;

    public void Initialize(int depth)
    {
        _trackTexture = RoundedTexture(32, 16);
        _knobTexture = RoundedTexture(12, 12);
        _track = Widget("Switch track", _trackTexture, depth, 1, 1);
        _knob = Widget("Switch knob", _knobTexture, depth + 1, 1, 1);
    }

    public void Resize(NativeControlGeometry bounds)
    {
        _bounds = bounds;
        _track.width = Mathf.RoundToInt(bounds.SwitchWidth);
        _track.height = Mathf.RoundToInt(bounds.SwitchHeight);
        _track.transform.localPosition = new Vector3(bounds.SwitchWidth / 2 - bounds.StateLeft, 0, 0);
        _knob.width = _knob.height = Mathf.RoundToInt(bounds.KnobSize);
    }

    public void SetState(bool value, bool editable)
    {
        if (_bounds != null)
            _knob.transform.localPosition = new Vector3(
                (value ? _bounds.KnobOnX : _bounds.KnobOffX) - _bounds.StateLeft, 0, 0);
        _track.color = value ? new Color(0.35f, 0.5f, 0.25f) : new Color(0.35f, 0.29f, 0.22f);
        _knob.color = new Color(0.85f, 0.76f, 0.58f);
        _track.alpha = _knob.alpha = editable ? 1f : 0.55f;
    }

    private UITexture Widget(string name, Texture texture, int depth, float width, float height)
    {
        var go = new GameObject(name);
        go.layer = gameObject.layer;
        go.transform.SetParent(transform, false);
        var widget = go.AddComponent<UITexture>();
        widget.mainTexture = texture;
        widget.width = Mathf.RoundToInt(width);
        widget.height = Mathf.RoundToInt(height);
        widget.depth = depth;
        return widget;
    }

    private static Texture2D RoundedTexture(int width, int height)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[width * height];
        var radius = height / 2f;
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var dx = x + 0.5f - Mathf.Clamp(x + 0.5f, radius, width - radius);
            var dy = y + 0.5f - radius;
            if (dx * dx + dy * dy <= radius * radius)
                pixels[y * width + x] = new Color32(255, 255, 255, 255);
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private void OnDestroy()
    {
        if (_trackTexture != null) Destroy(_trackTexture);
        if (_knobTexture != null) Destroy(_knobTexture);
    }
}
