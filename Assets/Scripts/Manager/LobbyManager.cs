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
        // ��ư ���ε�
        if (btnPublic) btnPublic.onClick.AddListener(() => OnSelectCategory("����"));
        if (btnPersonal) btnPersonal.onClick.AddListener(() => OnSelectCategory("����"));
        if (btnGroup) btnGroup.onClick.AddListener(() => OnSelectCategory("����"));
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
        if (messageText) messageText.text = $"������ ����: {category}";
        // ���õ� ��ư�� ���̶���Ʈ�ϰ� �ʹٸ�, �� ��ȯ/��� ó�� �߰�
    }

    void OnClickStart()
    {
        var topic = topicInput ? topicInput.text.Trim() : "";
        var counterpart = counterpartInput ? counterpartInput.text.Trim() : "";
        if (string.IsNullOrEmpty(selectedCategory))
        {
            if (messageText) messageText.text = "�������� ������ �ּ���.";
            return;
        }
        if (string.IsNullOrEmpty(selectedCategory))
        {
            if (messageText) messageText.text = "�����븦 �Է��� �ּ���.";
            return;
        }
        if (string.IsNullOrEmpty(topic))
        {
            if (messageText) messageText.text = "���� ������ �Է��� �ּ���.";
            return;
        }
        if(otherAge == -1)
        {
            if (messageText) messageText.text = "��� ���̸� ����ּ���";
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