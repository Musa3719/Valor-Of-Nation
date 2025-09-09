using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class SelectionBox : MonoBehaviour
{
    public static SelectionBox _Instance;
    public bool _IsDragging { get; private set; }
    private bool _checkForDrag;
    private Vector2 _startPos;
    private Vector2 _endPos;
    private List<GameObject> _hoverUnits;

    private void Awake()
    {
        _Instance = this;
        _hoverUnits = new List<GameObject>();
    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !GameManager._Instance._IsGameStopped && EventSystem.current != null && !EventSystem.current.IsPointerOverGameObject())
        {
            _checkForDrag = true;
            _startPos = Input.mousePosition;
        }
        if (((Vector2)Input.mousePosition - _startPos).magnitude > 10f && _checkForDrag)
        {
            _checkForDrag = false;
            _IsDragging = true;
            CloseHovers();
            GameInputController._Instance.CloseCityHover();
            GameInputController._Instance.CloseUnitHover();
        }


        if (Input.GetMouseButtonUp(0))
        {
            _checkForDrag = false;
            if (_IsDragging)
            {
                _IsDragging = false;
                SelectUnits();
                CloseHovers();
            }

        }

        if (Input.GetMouseButton(0))
        {
            HoverUnits();
            _endPos = Input.mousePosition;
        }
    }

    private void OnGUI()
    {
        if (_IsDragging)
        {
            var rect = GetScreenRect(_startPos, _endPos);
            DrawScreenRect(rect, new Color(0.33f, 0.33f, 0.33f, 0.33f));
            DrawScreenRectBorder(rect, 2, new Color(0.25f, 0.25f, 0.25f, 0.8f));
        }
    }

    private void SelectUnits()
    {
        Rect selectionRect = GetScreenRect(_startPos, _endPos);

        foreach (var col in GameManager._Instance._FriendlyUnitColliders)
        {
            if (!col.transform.parent.gameObject.activeSelf) continue;

            Vector3 screenPos = Camera.main.WorldToScreenPoint(col.transform.position);
            screenPos.y = Screen.height - screenPos.y;

            Bounds b = col.bounds;
            Vector3 newExtents = b.extents * 0.3f;
            b = new Bounds(b.center, newExtents * 2f);


            Vector3[] corners = new Vector3[8];

            corners[0] = new Vector3(b.min.x, b.min.y, b.min.z);
            corners[1] = new Vector3(b.max.x, b.min.y, b.min.z);
            corners[2] = new Vector3(b.min.x, b.max.y, b.min.z);
            corners[3] = new Vector3(b.max.x, b.max.y, b.min.z);
            corners[4] = new Vector3(b.min.x, b.min.y, b.max.z);
            corners[5] = new Vector3(b.max.x, b.min.y, b.max.z);
            corners[6] = new Vector3(b.min.x, b.max.y, b.max.z);
            corners[7] = new Vector3(b.max.x, b.max.y, b.max.z);

            foreach (var c in corners)
            {
                screenPos = Camera.main.WorldToScreenPoint(c);
                screenPos.y = Screen.height - screenPos.y;
                if (selectionRect.Contains(screenPos, true))
                {
                    GameInputController._Instance.SelectUnit(col.transform.parent.gameObject);
                    break;
                }
            }
        }
    }
    private void HoverUnits()
    {
        CloseHovers();

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

        Rect selectionRect = GetScreenRect(_startPos, _endPos);
        bool isVisible;
        Vector3 screenPos;
        Bounds b;
        Vector3 newExtents;
        Vector3[] corners = new Vector3[8];

        foreach (var col in GameManager._Instance._FriendlyUnitColliders)
        {
            if (col == null)
            {
                Debug.Log("null collider still in the friendly unit colliders!");
                GameManager._Instance._FriendlyUnitColliders.Remove(col);
                continue;
            }
            isVisible = GeometryUtility.TestPlanesAABB(planes, col.bounds);
            if (!isVisible) continue;
            if (col.transform.parent.GetComponent<Unit>().IsSelected() || !col.transform.parent.gameObject.activeSelf) continue;

            screenPos = Camera.main.WorldToScreenPoint(col.transform.position);
            screenPos.y = Screen.height - screenPos.y;

            b = col.bounds;
            newExtents = b.extents * 0.3f;
            b = new Bounds(b.center, newExtents * 2f);

            corners[0] = new Vector3(b.min.x, b.min.y, b.min.z);
            corners[1] = new Vector3(b.max.x, b.min.y, b.min.z);
            corners[2] = new Vector3(b.min.x, b.max.y, b.min.z);
            corners[3] = new Vector3(b.max.x, b.max.y, b.min.z);
            corners[4] = new Vector3(b.min.x, b.min.y, b.max.z);
            corners[5] = new Vector3(b.max.x, b.min.y, b.max.z);
            corners[6] = new Vector3(b.min.x, b.max.y, b.max.z);
            corners[7] = new Vector3(b.max.x, b.max.y, b.max.z);

            foreach (var c in corners)
            {
                screenPos = Camera.main.WorldToScreenPoint(c);
                screenPos.y = Screen.height - screenPos.y;
                if (selectionRect.Contains(screenPos, true))
                {
                    GameInputController._Instance.OpenUnitHover(col.transform.parent.gameObject, false);
                    if (!_hoverUnits.Contains(col.transform.parent.gameObject))
                        _hoverUnits.Add(col.transform.parent.gameObject);
                    break;
                }
            }
        }
    }
    private void CloseHovers()
    {
        foreach (var item in _hoverUnits)
        {
            if (item != null && item.GetComponent<Unit>() != null && !item.GetComponent<Unit>().IsSelected())
                GameInputController._Instance.CloseUnitHover(item);
        }
        _hoverUnits.Clear();
    }

    public static Rect GetScreenRect(Vector2 screenPosition1, Vector2 screenPosition2)
    {
        screenPosition1.y = Screen.height - screenPosition1.y;
        screenPosition2.y = Screen.height - screenPosition2.y;

        var topLeft = Vector2.Min(screenPosition1, screenPosition2);
        var bottomRight = Vector2.Max(screenPosition1, screenPosition2);

        return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
    }

    public static void DrawScreenRect(Rect rect, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    public static void DrawScreenRectBorder(Rect rect, float thickness, Color color)
    {
        // Top
        DrawScreenRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
        // Left
        DrawScreenRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
        // Right
        DrawScreenRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
        // Bottom
        DrawScreenRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
    }

}
