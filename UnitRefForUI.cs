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

        if (GameInputController._Instance._SelectedSquads.Contains(_UnitReferance._Squad))
            GameInputController._Instance.DeSelectSquad(_UnitReferance._Squad);
        else
            GameInputController._Instance.SelectSquad(_UnitReferance._Squad);
        

        GameInputController._Instance.UpdateUI();
    }
}
