using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PcaCapture : MonoBehaviour
{
    [Header("Device")]
    [Tooltip("デバイス名に含まれるキーワードで優先選択（空なら先頭を使用）")]
    public string deviceNameContains = "quest"; // "headset","pass" など実機ログを見て調整
    public int liveWidth = 1280, liveHeight = 960, liveFps = 30;

    [Header("Live Output")]
    public Material cropMaterial;      // 切り抜き/回転用（無くても動く）
    public RenderTexture liveRt;       // ライブ描画先（自動生成）

    WebCamTexture _cam;
    bool _ready;

    void Start()
    {
        // デバイス選択
        var devices = WebCamTexture.devices;
        string pick = null;
        foreach (var d in devices)
        {
            Debug.Log($"[PCA] WebCamDevice: {d.name}");
            if (pick == null && !string.IsNullOrEmpty(deviceNameContains) &&
                d.name.ToLower().Contains(deviceNameContains.ToLower()))
                pick = d.name;
        }
        if (pick == null && devices.Length > 0) pick = devices[0].name;

        _cam = string.IsNullOrEmpty(pick)
            ? new WebCamTexture(liveWidth, liveHeight, liveFps)
            : new WebCamTexture(pick, liveWidth, liveHeight, liveFps);

        _cam.Play();

        liveRt = new RenderTexture(liveWidth, liveHeight, 0, RenderTextureFormat.ARGB32);
        liveRt.Create();
        _ready = true;
        Debug.Log($"[PCA] Using device: {pick ?? "(default)"}");
    }

    void OnDestroy()
    {
        if (_cam != null && _cam.isPlaying) _cam.Stop();
        if (liveRt) liveRt.Release();
    }

    void Update()
    {
        if (!_ready || _cam == null || !_cam.didUpdateThisFrame) return;

        if (cropMaterial) Graphics.Blit(_cam, liveRt, cropMaterial);
        else Graphics.Blit(_cam, liveRt);
    }

    /// <summary> ライブ映像の平均色を返す（uvRect: 0..1） </summary>
    public IEnumerator SampleAverageColor(Rect uvRect, int down = 16, System.Action<Color> onResult = null)
    {
        if (!_ready || liveRt == null) { onResult?.Invoke(Color.black); yield break; }

        var tmp = RenderTexture.GetTemporary(down, down, 0, RenderTextureFormat.ARGB32);
        // uvRectの切り抜きをcropMaterialで縮小Blit（無ければ全体からRectTransformで代替）
        if (cropMaterial)
        {
            cropMaterial.SetVector("_Crop", new Vector4(uvRect.x, uvRect.y, uvRect.width, uvRect.height));
            Graphics.Blit(liveRt, tmp, cropMaterial);
        }
        else
        {
            // cropMaterialがない場合は簡易矩形コピー（最小実装：一旦全面コピー）
            Graphics.Blit(liveRt, tmp);
        }

        var req = AsyncGPUReadback.Request(tmp, 0, TextureFormat.RGBA32);
        yield return new WaitUntil(() => req.done);
        Color avg = Color.black;
        if (!req.hasError)
        {
            var data = req.GetData<Color32>();
            long r = 0, g = 0, b = 0, a = 0;
            for (int i = 0; i < data.Length; i++) { r += data[i].r; g += data[i].g; b += data[i].b; a += data[i].a; }
            float n = data.Length;
            avg = new Color(r / (255f * n), g / (255f * n), b / (255f * n), a / (255f * n));
        }
        RenderTexture.ReleaseTemporary(tmp);
        onResult?.Invoke(avg);
    }

    /// <summary> ライブ映像の指定矩形を Texture2D として取得（uvRect: 0..1） </summary>
    public Texture2D CaptureSnapshot(Rect uvRect, int outSize = 512)
    {
        if (!_ready || liveRt == null) return null;
        var tmp = RenderTexture.GetTemporary(outSize, outSize, 0, RenderTextureFormat.ARGB32);

        if (cropMaterial)
        {
            cropMaterial.SetVector("_Crop", new Vector4(uvRect.x, uvRect.y, uvRect.width, uvRect.height));
            Graphics.Blit(liveRt, tmp, cropMaterial);
        }
        else
        {
            Graphics.Blit(liveRt, tmp);
        }

        var tex = new Texture2D(outSize, outSize, TextureFormat.RGBA32, false, false);
        RenderTexture.active = tmp;
        tex.ReadPixels(new Rect(0, 0, outSize, outSize), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(tmp);
        return tex;
    }
}
