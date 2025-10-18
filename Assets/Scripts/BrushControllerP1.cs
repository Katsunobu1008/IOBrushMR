using System.Collections;
using UnityEngine;
using UnityEngine.XR;

public class BrushControllerP1 : MonoBehaviour
{
    public PcaCapture capture;
    public PainterPlane painter;
    [Range(0.02f, 0.4f)] public float sampleSize = 0.15f; // 中央窓の一辺（正方，UV）
    public float longPressSec = 0.35f;
    public int snapshotSize = 512;

    InputDevice _right;
    float _pressTime;
    bool _prevPressed;

    void Start()
    {
        _right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
    }

    Rect CenterRect(float size)
    {
        float s = Mathf.Clamp01(size);
        return new Rect(0.5f - s * 0.5f, 0.5f - s * 0.5f, s, s);
    }

    void Update()
    {
        bool pressed = false;
        _right.TryGetFeatureValue(CommonUsages.triggerButton, out pressed);

        if (pressed && !_prevPressed)
        {
            _pressTime = Time.time;
        }
        if (!pressed && _prevPressed)
        {
            float held = Time.time - _pressTime;
            if (held < longPressSec)
            {
                // 短押し：平均色
                StartCoroutine(capture.SampleAverageColor(CenterRect(sampleSize), 16, c => {
                    painter.PaintColor(c);
                }));
            }
            else
            {
                // 長押し：スナップショット
                var tex = capture.CaptureSnapshot(CenterRect(sampleSize), snapshotSize);
                painter.PaintTexture(tex);
            }
        }
        _prevPressed = pressed;
    }
}
