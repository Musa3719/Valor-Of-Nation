using UnityEngine;

public class LookAt : MonoBehaviour
{

    private void LateUpdate()
    {
        transform.LookAt(GameManager._Instance._MainCamera.transform);
        //transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, GameManager._Instance._MainCamera.transform.localEulerAngles.y - 5f, transform.localEulerAngles.z);
    }
}
