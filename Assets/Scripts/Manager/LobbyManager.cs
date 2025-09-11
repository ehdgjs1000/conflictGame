using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager instance;
    [SerializeField] GameObject myDataPanel;
    [SerializeField] GameObject sunCategoryPanel;

    [Header("UI")]
    public Button btnPublic;
    public Button btnPersonal;
    public Button btnGroup;
    public TMP_InputField topicInput;
    public TMP_InputField counterpartInput;
    public Button startButton;
    public TMP_Text messageText;
    public TMP_Text subMessageText;

    string selectedCategory = null;

    int otherAge;
    int otherGender;

    [HideInInspector] public string nickname,age,personality,job,academic,home;
    [HideInInspector] public int gender;

    void Awake()
    {
        if (instance == null) instance = this;

        // 버튼 바인딩
        if (btnPublic) btnPublic.onClick.AddListener(() => OnSelectCategory("공공"));
        if (btnPersonal) btnPersonal.onClick.AddListener(() => OnSelectCategory("개인"));
        if (btnGroup) btnGroup.onClick.AddListener(() => OnSelectCategory("조직"));
        if (startButton) startButton.onClick.AddListener(OnClickStart);

        otherAge = -1;
        otherGender = -1;
    }
    private void Start()
    {
        UpdateMyData();
        Debug.Log($"[WEBGL] Screen {Screen.width}x{Screen.height} dpiScale:{(Application.platform == RuntimePlatform.WebGLPlayer ? 1 : Screen.dpi)}");

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
        if (messageText) messageText.text = $"대주제 선택: {category}";
        // 선택된 버튼만 하이라이트하고 싶다면, 색 전환/토글 처리 추가
        sunCategoryPanel.transform.DOScale(Vector3.one, 0.5f);
    }

    void OnClickStart()
    {
        var topic = topicInput ? topicInput.text.Trim() : "";
        var counterpart = counterpartInput ? counterpartInput.text.Trim() : "";
        if (string.IsNullOrEmpty(selectedCategory))
        {
            if (subMessageText) subMessageText.text = "대주제를 선택해 주세요.";
            return;
        }
        if (string.IsNullOrEmpty(selectedCategory))
        {
            if (subMessageText) subMessageText.text = "갈등상대를 입력해 주세요.";
            return;
        }
        if (string.IsNullOrEmpty(topic))
        {
            if (subMessageText) subMessageText.text = "메인 주제를 입력해 주세요.";
            return;
        }
        if(otherAge == -1)
        {
            if (subMessageText) subMessageText.text = "상대 나이를 골라주세요";
            return;
        }
        if (otherGender == -1)
        {
            if (messageText) messageText.text = "상대 성별을 골라주세요";
            return;
        }
        sunCategoryPanel.transform.DOScale(Vector3.zero, 0.0f);
        // 세션에 저장
        if (GameSession.Instance == null)
        {
            // 혹시 Lobby 씬에 GameSession이 없다면 동적 생성
            var go = new GameObject("GameSession");
            go.AddComponent<GameSession>();
        }
        GameSession.Instance.MyDataSync();
        GameSession.Instance.majorCategory = selectedCategory;
        GameSession.Instance.mainTopic = topic;
        GameSession.Instance.counterpart = counterpart;
        GameSession.Instance.otherGender = otherGender;
        GameSession.Instance.otherAge = otherAge;

        // Game 씬 로드
        SceneManager.LoadScene("InGameScene"); // 씬 이름 맞춰서
    }
}