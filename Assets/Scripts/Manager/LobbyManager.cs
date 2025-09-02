using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LobbyManager : MonoBehaviour
{
    [Header("UI")]
    public Button btnPublic;
    public Button btnPersonal;
    public Button btnGroup;
    public TMP_InputField topicInput;
    public TMP_InputField counterpartInput;
    public Button startButton;
    public TMP_Text messageText;

    string selectedCategory = null;

    int otherAge;
    int otherGender;

    void Awake()
    {
        // 버튼 바인딩
        if (btnPublic) btnPublic.onClick.AddListener(() => OnSelectCategory("공공"));
        if (btnPersonal) btnPersonal.onClick.AddListener(() => OnSelectCategory("개인"));
        if (btnGroup) btnGroup.onClick.AddListener(() => OnSelectCategory("조직"));
        if (startButton) startButton.onClick.AddListener(OnClickStart);

        otherAge = -1;
        otherGender = -1;
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
    }

    void OnClickStart()
    {
        var topic = topicInput ? topicInput.text.Trim() : "";
        var counterpart = counterpartInput ? counterpartInput.text.Trim() : "";
        if (string.IsNullOrEmpty(selectedCategory))
        {
            if (messageText) messageText.text = "대주제를 선택해 주세요.";
            return;
        }
        if (string.IsNullOrEmpty(selectedCategory))
        {
            if (messageText) messageText.text = "갈등상대를 입력해 주세요.";
            return;
        }
        if (string.IsNullOrEmpty(topic))
        {
            if (messageText) messageText.text = "메인 주제를 입력해 주세요.";
            return;
        }
        if(otherAge == -1)
        {
            if (messageText) messageText.text = "상대 나이를 골라주세요";
            return;
        }
        if (otherGender == -1)
        {
            if (messageText) messageText.text = "상대 성별을 골라주세요";
            return;
        }

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