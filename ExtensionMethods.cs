using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UI;

public static class ExtensionMethods
{
    public static List<Vector3> Copy(this List<Vector3> from)
    {
        List<Vector3> newList = new List<Vector3>();
        if (from == null) return newList;
        foreach (var item in from)
        {
            newList.Add(new Vector3(item.x, item.y, item.z));
        }
        return newList;
    }
    public static List<Goods> Copy(this List<Goods> from)
    {
        List<Goods> newList = new List<Goods>();
        if (from == null) return newList;
        foreach (var item in from)
        {
            Goods newGoods = new Goods();
            newGoods._Amount = item._Amount;
            newGoods._Type = item._Type;
            newList.Add(newGoods);
        }
        return newList;
    }
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
    public static Vector3[] GetPointsInCircle(int pointCount, float radius)
    {
        Vector3[] points = new Vector3[pointCount];

        if (pointCount <= 0)
            return points;

        if (pointCount == 1)
        {
            points[0] = Vector3.zero;
            return points;
        }

        float angleStep = 360f / pointCount;

        for (int i = 0; i < pointCount; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            points[i] = new Vector3(x, 0, z);
        }

        return points;
    }


    public static float GetLowestSquadSpeed(this List<Squad> squads)
    {
        float lowest = float.MaxValue;
        foreach (var squad in squads)
        {
            if (squad._Speed < lowest)
                lowest = squad._Speed;
        }
        return lowest;
    }
    public static int GetTotalAmountOfType<T>(this List<Squad> squads) where T : Squad
    {
        return squads
        .Where(u => u is T)
        .Sum(u => u._Amount);
    }
    public static bool IsOnlyThisSquadType(this List<Squad> squads, System.Type type)
    {
        foreach (var squad in squads)
        {
            if (squad.GetType() == type)
                continue;
            else
                return false;
        }
        return true;
    }
    public static bool IsAllSquadsSameType(this List<Squad> squads)
    {
        if (squads == null || squads.Count == 0) return false;

        System.Type firstType = squads[0].GetType();
        return squads.IsOnlyThisSquadType(firstType);
    }
    public static bool IsSplitPossibleOnAny(this List<Squad> squads)
    {
        foreach (Squad squad in squads)
        {
            if (GameInputController._Instance.IsSplitPossible(squad._AttachedUnit))
                return true;
        }
        return false;
    }
    public static List<Squad> GetAllSquadsCombinedNumbers(this List<Squad> squads)
    {
        List<Squad> groupedSquads = new List<Squad>();

        foreach (var squad in squads)
        {
            bool merged = false;

            foreach (var groupedSquad in groupedSquads)
            {
                if (groupedSquad.GetType() == squad.GetType())
                {
                    groupedSquad._Amount += squad._Amount;
                    merged = true;
                    break;
                }
            }

            if (!merged)
            {
                Squad clone = (Squad)System.Activator.CreateInstance(squad.GetType());
                clone._Amount = squad._Amount;
                groupedSquads.Add(clone);
            }
        }

        return groupedSquads;
    }
}
