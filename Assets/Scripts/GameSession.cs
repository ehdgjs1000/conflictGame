using UnityEngine;

public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    public string majorCategory; // "공공" / "개인" 등
    public string mainTopic;     // 입력 키워드(예: 층간소음, 연봉 협상)
    public string counterpart; // 갈등 상대

    public int age;
    public string mbti;
    public string gender;

    public int otherAge;
    public int otherGender;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public void MyDataSync()
    {
        age = PlayerPrefs.GetInt("Age");
        int genderInt = PlayerPrefs.GetInt("Gender");
        if (genderInt == 0) gender = "남자";
        else gender = "여자";
        mbti = PlayerPrefs.GetString("MBTI");
    }



}
