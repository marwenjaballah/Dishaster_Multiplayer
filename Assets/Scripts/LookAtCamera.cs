using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCamera : MonoBehaviour {


    private enum Mode {
        LookAt,
        LookAtInverted,
        CameraForward,
        CameraForwardInverted,
    }


    [SerializeField] private Mode mode;


    private static Camera _cachedCamera;
    private static Transform _cachedCameraTransform;

    public static Transform MainCameraTransform {
        get {
            if (_cachedCamera == null || !_cachedCamera.gameObject.activeInHierarchy) {
                _cachedCamera = Camera.main;
                if (_cachedCamera != null) {
                    _cachedCameraTransform = _cachedCamera.transform;
                }
            }
            return _cachedCameraTransform;
        }
    }

    private void LateUpdate() {
        Transform camTr = MainCameraTransform;
        if (camTr == null) return;

        switch (mode) {
            case Mode.LookAt:
                transform.LookAt(camTr);
                break;
            case Mode.LookAtInverted:
                Vector3 dirFromCamera = transform.position - camTr.position;
                transform.LookAt(transform.position + dirFromCamera);
                break;
            case Mode.CameraForward:
                transform.forward = camTr.forward;
                break;
            case Mode.CameraForwardInverted:
                transform.forward = -camTr.forward;
                break;
        }
    }

}