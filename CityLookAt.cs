using UnityEngine;

public class CityLookAt : MonoBehaviour
{

    private void LateUpdate()
    {
        transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, GameManager._Instance._MainCamera.transform.localEulerAngles.y - 5f, transform.localEulerAngles.z);
    }
}
