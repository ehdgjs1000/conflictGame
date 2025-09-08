using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] Text reponseText;
    [SerializeField] OpenAIChat AI;

    [SerializeField] Text[] scoreTexts;
    [SerializeField] Image[] scoreImages;
    [SerializeField] Image step9Image;
    [SerializeField] Sprite[] step9Sprites;
    public GameObject endPanel;
    public TMP_Text feedbackText;

    [Header("��ȭ �α� UI")]
    public Transform chatContent;       // Scroll View �� Viewport �� Content
    public GameObject chatBubblePrefab;

    private float sumEmpathy, sumClarity, sumSolution, sumRealism;
    int evalCount;

    public ScrollRect scrollRect;
    public Image conversationProgressBar;

    private void Awake()
    {
        // �̱��� �ʱ�ȭ
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    public float GetAverage(string type)
    {
        if (type == "empathy") return sumEmpathy / evalCount;
        else if(type == "clarity") return sumClarity / evalCount;
        else if(type == "solution") return sumSolution / evalCount;
        else return sumRealism / evalCount;
    }
    public void ShowEndPanel(string feedback)
    {
        if (endPanel) endPanel.SetActive(true);
        if (feedbackText) feedbackText.text = feedback;
    }
    public void ResetAverages()
    {
        sumEmpathy = sumClarity = sumSolution = sumRealism = 0f;
        evalCount = 0;
        // �ʱ� ���� 0���� Ŭ�����ϰ� ������:
        SetScores(new float[] { 0, 0, 0, 0 });
    }
    public void SetScores(float[] scores)
    {
        float scoreSum = 0;
        for (int i = 0; i < scores.Length; i++)
        {
            scoreSum += scores[i];
            scoreTexts[i].text = scores[i].ToString("0.#"); ;
            scoreImages[i].fillAmount = scores[i]/25;
        }
        scoreTexts[4].text = scoreSum.ToString("0.#"); ;
        scoreImages[4].fillAmount = scoreSum / 100.0f;
        if(scoreSum <= 20 && AI.conversationCount > 5) AI.EndConversation();
        scoreSum = Mathf.Clamp(scoreSum, 0f, 100f);
        int scoreIndex = Mathf.FloorToInt(scoreSum / 10f);
        if(evalCount > 5)
        {
            if (scoreIndex >= 8) scoreIndex = 8;
            step9Image.sprite = step9Sprites[scoreIndex];
        }
        
    }

    public void SetAIMessage(string _msg)
    {
        reponseText.text = _msg;
    }
    public void AddChatMessage(string msg, bool isPlayer)
    {
        GameObject bubble = Instantiate(chatBubblePrefab, chatContent);
        TMP_Text txt = bubble.GetComponentInChildren<TMP_Text>();
        txt.text = msg;

        // ����/���� �ٸ��� (��: AI�� ����, �÷��̾�� ������)
        Image bg = bubble.GetComponent<Image>();
        if (isPlayer)
        {
            bg.color = new Color(1f, 0.6f, 0.6f); // �ϴû� ��
            txt.alignment = TextAlignmentOptions.Right;
        }
        else
        {
            bg.color = new Color(0.9f, 0.9f, 0.9f); // ȸ�� ��
            txt.alignment = TextAlignmentOptions.Left;
        }
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f; // 0=�Ʒ�, 1=��
    }
    public void UpdateScoreUI(ConflictScore s)
    {
        // 1) ����
        sumEmpathy += Mathf.Clamp(s.empathy, 0f, 25f);
        sumClarity += Mathf.Clamp(s.clarity, 0f, 25f);
        sumSolution += Mathf.Clamp(s.solution, 0f, 25f);
        sumRealism += Mathf.Clamp(s.realism, 0f, 25f);
        evalCount++;
        conversationProgressBar.fillAmount = (float)(evalCount / 20);

        // 2) ���(0~25)
        float avgEmpathy = sumEmpathy / evalCount;
        float avgClarity = sumClarity / evalCount;
        float avgSolution = sumSolution / evalCount;
        float avgRealism = sumRealism / evalCount;

        // 3) UI ������Ʈ (UIManager�� 0~25 ������ ����)
        SetScores(new float[] {avgEmpathy, avgClarity, avgSolution, avgRealism});

    }
    public void ToLobbyOnClick()
    {
        SceneManager.LoadScene("LobbyScene");
    }


}
