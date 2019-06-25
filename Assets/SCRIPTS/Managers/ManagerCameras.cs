using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManagerCameras : MonoBehaviour {

    static Camera m_MainCamera;
    static Transform m_MainCameraTF;
    static void Init()
    {
        m_MainCamera = Camera.main;
        m_MainCameraTF = m_MainCamera.transform;
    }

    public static Camera GetMainCamera()
    {
        if (m_MainCamera == null) Init();
        return m_MainCamera;
    }

    public static Transform GetMainCameraTransform()
    {
        if (m_MainCameraTF == null) Init();
        return m_MainCameraTF;
    }

    private void OnDestroy()
    {
        m_MainCamera = null;
        m_MainCameraTF = null;
    }
}
