#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using UnityEngine;

public class WebGLScalerBootstrap : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void InstallPortraitScaler();
#endif

    void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        InstallPortraitScaler();
#endif
    }
}
