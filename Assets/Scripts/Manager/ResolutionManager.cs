using UnityEngine;

public class ResolutionManager : MonoBehaviour
{
    public static ResolutionManager instance;
    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        var sw = Display.main.systemWidth;   // ����ũ�� ����
        var sh = Display.main.systemHeight;  // ����ũ�� ����

        // â �׵θ�/�۾�ǥ���� ���� (ȯ�濡 �°� ����)
        int margin = 120;
        int maxH = Mathf.Max(400, sh - margin);

        // ��ǥ�� Portrait 9:16�� �����ϸ鼭 ȭ�� �ȿ� ����
        int targetH = Mathf.Min(1920, maxH);
        int targetW = Mathf.RoundToInt(targetH * 9f / 16f);

        Screen.fullScreenMode = FullScreenMode.Windowed;
        Screen.SetResolution(targetW, targetH, false);
    }
}