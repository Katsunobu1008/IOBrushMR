using UnityEngine;

public class PainterPlane : MonoBehaviour
{
    public Renderer target;    // Quadや任意メッシュ
    public Material unlit;     // Unlit/Texture 系

    public void PaintColor(Color c)
    {
        if (!target) return;
        var m = new Material(unlit);
        m.color = c;
        target.sharedMaterial = m;
    }

    public void PaintTexture(Texture2D tex)
    {
        if (!target) return;
        var m = new Material(unlit);
        m.mainTexture = tex;
        target.sharedMaterial = m;
    }
}
