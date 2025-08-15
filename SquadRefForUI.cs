using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SquadRefForUI : MonoBehaviour
{
    public Squad _SquadReferance;
    public bool _IsPermittedToRightClickSelect;
    public bool _IsMouseOver;

    private void Update()
    {
        _IsMouseOver = RectTransformUtility.RectangleContainsScreenPoint(GetComponent<RectTransform>(), Input.mousePosition, null);
        if (_IsMouseOver && !GameInputController._Instance.IsMouseOverAnyButtonOrSliderUI() && Mouse.current.leftButton.wasPressedThisFrame)
            OnClick();
        else if (_IsMouseOver && Mouse.current.rightButton.isPressed && _IsPermittedToRightClickSelect)
            OnRightClick();
    }



    private void OnClick()
    {
        if (GameManager._Instance._InputActions.FindAction("Sprint").ReadValue<float>() == 0f)
            GameInputController._Instance._SelectedSquads.ClearSelected();

        if (GameInputController._Instance._SelectedSquads.Contains(_SquadReferance))
        {
            GameInputController._Instance.DeSelectSquad(_SquadReferance);
        }
        else
        {
            GameInputController._Instance.SelectSquad(_SquadReferance);
        }

        GameInputController._Instance.UpdateUI();
    }
    private void OnRightClick()
    {
        _IsPermittedToRightClickSelect = false;

        if (GameInputController._Instance._SelectedSquads.Contains(_SquadReferance))
        {
            GameInputController._Instance.DeSelectSquad(_SquadReferance);
        }
        else
        {
            GameInputController._Instance.SelectSquad(_SquadReferance);
        }

        GameInputController._Instance.UpdateUI();
    }
}
