using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class PortraitAspectLocker : MonoBehaviour
{
    // 목표 비율(1080x1920 = 0.5625)
    [SerializeField] float targetWidth = 1080f;
    [SerializeField] float targetHeight = 1920f;

    Camera cam;
    float Target => targetWidth / targetHeight;   // 9:16

    void Awake()
    {
        cam = GetComponent<Camera>();
        Apply();
    }

    void OnEnable()
    {
        Apply();
        Camera.onPreCull += HandlePreCull;   // 프레임마다 재보정(웹/Play 래퍼 대응)
    }
    void OnDisable()
    {
        Camera.onPreCull -= HandlePreCull;
    }
    void HandlePreCull(Camera c)
    {
        if (c == cam) Apply();
    }

    void Apply()
    {
        float screen = (float)Screen.width / Mathf.Max(1, Screen.height);

        if (screen > Target)
        {
            // 가로가 더 넓다 → 좌우 필러박스
            float desiredW = Target / screen;           // 0~1
            float inset = (1f - desiredW) * 0.5f;
            cam.rect = new Rect(inset, 0f, desiredW, 1f);
        }
        else
        {
            // 세로가 더 길다 → 상하 레터박스
            float desiredH = screen / Target;           // 0~1
            float inset = (1f - desiredH) * 0.5f;
            cam.rect = new Rect(0f, inset, 1f, desiredH);
        }
    }
}
