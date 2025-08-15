using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UI;

public static class ExtensionMethods
{
    public static T GetSquadThisType<T>(this List<Squad> squads) where T : Squad
    {
        for (int i = 0; i < squads.Count; i++)
        {
            if (squads[i] is T) return squads[i] as T;
        }
        return null;
    }
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
        List<GameObject> copy = new List<GameObject>();
        for (int i = 0; i < selectedObjects.Count; i++)
        {
            copy.Add(selectedObjects[i]);
        }
        foreach (var selectedObj in copy)
        {
            GameInputController._Instance.DeSelectUnit(selectedObj);
        }
        selectedObjects.Clear();

        GameInputController._Instance._SelectedSquads.ClearSelected();
    }
    public static void ClearSelected(this List<Squad> selectedSquads)
    {
        List<Squad> copy = new List<Squad>();
        for (int i = 0; i < selectedSquads.Count; i++)
        {
            copy.Add(selectedSquads[i]);
        }
        foreach (var selectedSquad in copy)
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



/*
ArrangeOrderGhostForPlayer()
if (!isPressingShift || executerObject.GetComponent<Unit>()._TargetPositions.Count == 0)
{
    executerObject.transform.Find("PotentialRouteGhost").GetComponent<LineRenderer>().positionCount = 2;

    TerrainController._Instance.ArrangeMergingLineRenderer(executerObject.transform.Find("PotentialRouteGhost").GetComponent<LineRenderer>(), firstPos.Value, secondPos.Value);
    //executerObject.transform.Find("PotentialRouteGhost").GetComponent<LineRenderer>().SetPosition(0, firstPos.Value);
    //executerObject.transform.Find("PotentialRouteGhost").GetComponent<LineRenderer>().SetPosition(1, secondPos.Value);
}
else
{
    executerObject.transform.Find("PotentialRouteGhost").GetComponent<LineRenderer>().positionCount = executerObject.transform.Find("CurrentRouteGhost").GetComponent<LineRenderer>().positionCount + 1;

    for (int i = 0; i < executerObject.transform.Find("CurrentRouteGhost").GetComponent<LineRenderer>().positionCount; i++)
    {
        executerObject.transform.Find("PotentialRouteGhost").GetComponent<LineRenderer>().SetPosition(i, executerObject.transform.Find("CurrentRouteGhost").GetComponent<LineRenderer>().GetPosition(i));
    }
    executerObject.transform.Find("PotentialRouteGhost").GetComponent<LineRenderer>().SetPosition(executerObject.transform.Find("PotentialRouteGhost").GetComponent<LineRenderer>().positionCount - 1, secondPos.Value + Vector3.up * 10f);
}


*/