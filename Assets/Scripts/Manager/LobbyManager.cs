using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager instance;
    [SerializeField] GameObject myDataPanel;

    [Header("UI")]
    public Button btnPublic;
    public Button btnPersonal;
    public Button btnGroup;
    public Button btnLover;
    public Button btnFamily;
    public Button btnSchool;
    public TMP_InputField topicInput;
    public TMP_InputField counterpartInput;
    public Button startButton;
    public TMP_Text messageText;
    public TMP_Text subMessageText;

    string selectedCategory = null;

    int otherAge;
    int otherGender;

    public string nickname,age,personality,job,academic,home;
    public int gender;

    void Awake()
    {
        if (instance == null) instance = this;

        // ��ư ���ε�
        if (btnPublic) btnPublic.onClick.AddListener(() => OnSelectCategory("����"));
        if (btnPersonal) btnPersonal.onClick.AddListener(() => OnSelectCategory("����"));
        if (btnGroup) btnGroup.onClick.AddListener(() => OnSelectCategory("����"));
        if (btnGroup) btnLover.onClick.AddListener(() => OnSelectCategory("����"));
        if (btnGroup) btnFamily.onClick.AddListener(() => OnSelectCategory("����"));
        if (btnGroup) btnSchool.onClick.AddListener(() => OnSelectCategory("�б�"));
        if (startButton) startButton.onClick.AddListener(OnClickStart);

        otherAge = -1;
        otherGender = -1;
    }
    private void Start()
    {
        UpdateMyData();
    }
    public void UpdateMyData()
    {
        gender = PlayerPrefs.GetInt("Gender");
        nickname = PlayerPrefs.GetString("Nickname");
        age = PlayerPrefs.GetString("Age");
        personality = PlayerPrefs.GetString("Personality");
        job = PlayerPrefs.GetString("Job");
        academic = PlayerPrefs.GetString("Academic");
        home = PlayerPrefs.GetString("Home");

        if(!PlayerPrefs.HasKey("Gender") || !PlayerPrefs.HasKey("Nickname") || !PlayerPrefs.HasKey("Age") || !PlayerPrefs.HasKey("Personality") || !PlayerPrefs.HasKey("Job") || !PlayerPrefs.HasKey("Academic") || !PlayerPrefs.HasKey("Home"))
        {
            myDataPanel.SetActive(true);
        }

    }
    public void GenderOnClick(int gender)
    {
        otherGender = gender;
    }
    public void AgeOnClick(int age)
    {
        otherAge = age;
    }

    void OnSelectCategory(string category)
    {
        selectedCategory = category;
        if (messageText) messageText.text = $"������ ����: {category}";
        // ���õ� ��ư�� ���̶���Ʈ�ϰ� �ʹٸ�, �� ��ȯ/��� ó�� �߰�
    }

    void OnClickStart()
    {
        var topic = topicInput ? topicInput.text.Trim() : "";
        var counterpart = counterpartInput ? counterpartInput.text.Trim() : "";
        if (string.IsNullOrEmpty(selectedCategory))
        {
            if (subMessageText) subMessageText.text = "�������� ������ �ּ���.";
            return;
        }
        if (string.IsNullOrEmpty(selectedCategory))
        {
            if (subMessageText) subMessageText.text = "�����븦 �Է��� �ּ���.";
            return;
        }
        if (string.IsNullOrEmpty(topic))
        {
            if (subMessageText) subMessageText.text = "���� ������ �Է��� �ּ���.";
            return;
        }
        if(otherAge == -1)
        {
            if (subMessageText) subMessageText.text = "��� ���̸� ����ּ���";
            return;
        }
        if (otherGender == -1)
        {
            if (messageText) messageText.text = "��� ������ ����ּ���";
            return;
        }

        // ���ǿ� ����
        if (GameSession.Instance == null)
        {
            // Ȥ�� Lobby ���� GameSession�� ���ٸ� ���� ����
            var go = new GameObject("GameSession");
            go.AddComponent<GameSession>();
        }
        GameSession.Instance.MyDataSync();
        GameSession.Instance.majorCategory = selectedCategory;
        GameSession.Instance.mainTopic = topic;
        GameSession.Instance.counterpart = counterpart;
        GameSession.Instance.otherGender = otherGender;
        GameSession.Instance.otherAge = otherAge;

        // Game �� �ε�
        SceneManager.LoadScene("InGameScene"); // �� �̸� ���缭
    }
}