using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class PortraitAspectLocker : MonoBehaviour
{
    // ��ǥ ����(1080x1920 = 0.5625)
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
        Camera.onPreCull += HandlePreCull;   // �����Ӹ��� �纸��(��/Play ���� ����)
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
            // ���ΰ� �� �д� �� �¿� �ʷ��ڽ�
            float desiredW = Target / screen;           // 0~1
            float inset = (1f - desiredW) * 0.5f;
            cam.rect = new Rect(inset, 0f, desiredW, 1f);
        }
        else
        {
            // ���ΰ� �� ��� �� ���� ���͹ڽ�
            float desiredH = screen / Target;           // 0~1
            float inset = (1f - desiredH) * 0.5f;
            cam.rect = new Rect(0f, inset, 1f, desiredH);
        }
    }
}
