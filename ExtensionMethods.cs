using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UI;

public static class ExtensionMethods
{
    public static bool Contains(this Button[] buttons, GameObject isContainsObj)
    {
        if (buttons == null) return false;

        foreach (var button in buttons)
        {
            if (button.gameObject == isContainsObj) return true;
        }
        return false;
    }
    public static bool Contains(this Slider[] sliders, GameObject isContainsObj)
    {
        if (sliders == null) return false;

        foreach (var slider in sliders)
        {
            if (slider.gameObject == isContainsObj || slider.transform.Find("Background").gameObject == isContainsObj) return true;
        }
        return false;
    }


    public static void ClearSelected(this List<GameObject> selectedObjects)
    {
        foreach (var selectedObj in selectedObjects)
        {
            if (selectedObj.transform.Find("PotentialRouteGhost") != null)
                selectedObj.transform.Find("PotentialRouteGhost").gameObject.SetActive(false);
            if (selectedObj.transform.Find("CurrentRouteGhost") != null)
                selectedObj.transform.Find("CurrentRouteGhost").gameObject.SetActive(false);
        }
        selectedObjects.Clear();

        GameInputController._Instance._SelectedSquads.ClearSelected();
    }
    public static void ClearSelected(this List<Squad> selectedSquads)
    {
        foreach (var selectedSquad in selectedSquads)
        {
            GameInputController._Instance.DeSelectSquad(selectedSquad);
        }
        selectedSquads.Clear();
    }

    public static void Reverse(this Spline spline)
    {
        int count = spline.Count;
        var reversedKnots = new BezierKnot[count];

        for (int i = 0; i < count; i++)
        {
            var knot = spline[i];

            // Tangent'larýn yönünü ters çevir
            var reversedKnot = new BezierKnot
            {
                Position = knot.Position,
                Rotation = knot.Rotation,
                TangentIn = knot.TangentOut,
                TangentOut = knot.TangentIn
            };

            reversedKnots[count - 1 - i] = reversedKnot;
        }

        spline.Clear();
        foreach (var knot in reversedKnots)
        {
            spline.Add(knot);
        }
    }
    public static void ConvertToCurved(this Spline spline, float curveStrength = 0.75f, float maxTangentLength = 20f)
    {
        for (int i = 0; i < spline.Count; i++)
        {
            var knot = spline[i];
            Vector3 pos = knot.Position;

            Vector3 prevPos = i > 0 ? spline[i - 1].Position : pos;
            Vector3 nextPos = i < spline.Count - 1 ? spline[i + 1].Position : pos;

            Vector3 dir = (nextPos - prevPos).normalized;
            float rawLength = Vector3.Distance(prevPos, nextPos) * curveStrength;
            float length = Mathf.Min(rawLength, maxTangentLength);

            if (i == 0 || i == spline.Count - 1)
            {
                knot.TangentIn = Vector3.zero;
                knot.TangentOut = Vector3.zero;
            }
            else
            {
                knot.TangentIn = -dir * length * 0.5f;
                knot.TangentOut = dir * length * 0.5f;
            }

            spline.SetKnot(i, knot);
        }
    }
}
