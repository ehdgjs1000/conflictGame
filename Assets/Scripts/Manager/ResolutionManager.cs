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
        var sw = Display.main.systemWidth;   // 데스크톱 가로
        var sh = Display.main.systemHeight;  // 데스크톱 세로

        // 창 테두리/작업표시줄 여유 (환경에 맞게 조정)
        int margin = 120;
        int maxH = Mathf.Max(400, sh - margin);

        // 목표는 Portrait 9:16을 유지하면서 화면 안에 들어가게
        int targetH = Mathf.Min(1920, maxH);
        int targetW = Mathf.RoundToInt(targetH * 9f / 16f);

        Screen.fullScreenMode = FullScreenMode.Windowed;
        Screen.SetResolution(targetW, targetH, false);
    }
}