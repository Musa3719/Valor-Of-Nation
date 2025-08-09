using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UnitRefForUI : MonoBehaviour
{
    public Unit _UnitReferance;
    private bool _isMouseOver;
    private void Update()
    {
        _isMouseOver = RectTransformUtility.RectangleContainsScreenPoint(transform.Find("SelfContent").GetComponent<RectTransform>(), Input.mousePosition, null);
        if (_isMouseOver && !GameInputController._Instance.IsMouseOverAnyButtonOrSliderUI() && Mouse.current.leftButton.wasPressedThisFrame)
            OnClick();
    }
    private void OnClick()
    {
        if (GameManager._Instance._InputActions.FindAction("Sprint").ReadValue<float>() == 0f)
            GameInputController._Instance._SelectedSquads.ClearSelected();

        bool isAllSquadsSelected = IsAllSquadsSelected();
        foreach (Squad squad in _UnitReferance._Squads)
        {
            if (isAllSquadsSelected)
            {
                if (GameInputController._Instance._SelectedSquads.Contains(squad))
                {
                    GameInputController._Instance.DeSelectSquad(squad);
                }
            }
            else
            {
                if (!GameInputController._Instance._SelectedSquads.Contains(squad))
                {
                    GameInputController._Instance.SelectSquad(squad);
                }
            }

        }

        GameInputController._Instance.UpdateUI();
    }
    private bool IsAllSquadsSelected()
    {
        foreach (Squad squad in _UnitReferance._Squads)
        {
            if (!squad._IsSquadSelected) return false;
        }
        return true;
    }



}
