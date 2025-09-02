using UnityEngine;

public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    public string majorCategory; // "����" / "����" ��
    public string mainTopic;     // �Է� Ű����(��: ��������, ���� ����)
    public string counterpart; // ���� ���

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
        if (genderInt == 0) gender = "����";
        else gender = "����";
        mbti = PlayerPrefs.GetString("MBTI");
    }



}
