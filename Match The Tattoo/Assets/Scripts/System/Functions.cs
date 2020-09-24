using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Functions
{
    public static void DrawPolygonCollider(PolygonCollider2D collider)
    {
        if (collider.GetComponent<LineRenderer>() != null)
            MonoBehaviour.Destroy(collider.GetComponent<LineRenderer>());
        LineRenderer _lr = collider.gameObject.AddComponent<LineRenderer>();
        _lr.startWidth = 0.025f;
        _lr.endWidth = 0.025f;
        _lr.useWorldSpace = false;
        _lr.positionCount = collider.points.Length + 1;
        for (int i = 0; i < collider.points.Length; i++)
        {
            _lr.SetPosition(i,new Vector3(collider.points[i].x,collider.points[i].y));
        }
        _lr.SetPosition(collider.points.Length, new Vector3(collider.points[0].x, collider.points[0].y));
    }
    public static void ScalePolygonCollider(PolygonCollider2D collider,float scaleChange)
    {
        for (int i = 0; i < collider.points.Length; i++)
        {
            collider.points[i] = collider.points[i] * scaleChange;
        }
    }
    public static string EnumerableAsString(IEnumerable enumerable, char separator = '|')
    {
        string _s = "";
        foreach (System.Object _o in enumerable)
        {
            _s += separator + _o.ToString() + separator;
        }
        return _s;
    }
    public static Texture2D CutAndLerpTexture(Texture2D source, int percent, float lerpCoeffitient, Color color, bool useTextureAlpha = true)
    {
        if (lerpCoeffitient > 1f)
        {
            lerpCoeffitient = 1f;
            Debug.LogWarning("Unable to use interpolation coeffitient " + lerpCoeffitient + " used 1 instead");
        }
        if (percent > 100)
        {
            percent = 100;
            Debug.LogWarning("Unable to use drawing percent" + percent + " used 100 instead");
        }
        Texture2D _txt = new Texture2D(source.width, source.height, source.format, false);
        Color currentColor = color;
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture renderTexture = new RenderTexture(source.width, source.height, 3, RenderTextureFormat.ARGB32);
        Graphics.Blit(source, renderTexture);
        _txt.ReadPixels(new Rect(0f, 0f, renderTexture.width, renderTexture.height), 0, 0);
        _txt.Apply();
        for (int x = 0; x < _txt.width; x++)
            for (int y = 0; y < _txt.height; y++)
            {
                if (useTextureAlpha)
                    currentColor.a = _txt.GetPixel(x, y).a;
                if (y >= _txt.height * (percent / 100f))
                {
                    currentColor.a = 0f;
                    _txt.SetPixel(x, y, currentColor);
                }
                else
                    _txt.SetPixel(x, y, Color.Lerp(_txt.GetPixel(x, y), currentColor, lerpCoeffitient));
            }
        _txt.Apply();
        RenderTexture.active = currentRT;
        renderTexture.Release();
        return _txt;
    }
}