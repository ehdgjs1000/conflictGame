// Assets/Scripts/OpenAIChat.cs
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking; // �� WebGL���� UnityWebRequest ���

[Serializable] public class TwoLineReply { public string line1; public string line2; }
[Serializable] public class ConflictScore { public float empathy, clarity, solution, realism; public string rationale; }

public class OpenAIChat : MonoBehaviour
{
    [Header("Prompts")]
    [TextArea]
    public string systemPrompt =
        "�ʴ� ���� �ùķ������� '��������' NPC��. ������������ ��ȭ ������� �ùķ��̼� �Ұž�.";
    [TextArea]
    public string initialUserMessage =
        "���� �� ������� ������ �����Ѵٰ� �����Ͻó���.";
    public string feedBackPrompt;

    [Header("Model/Endpoint")]
    public string model = "gpt-4o-mini";
    // Vercel ���Ͻ� (Ű�� �������� ����)
    public string endpoint = "https://conflict-game.vercel.app/api/chat";

    [Header("UI: �Է�/��ư")]
    [SerializeField] TextMeshProUGUI remainCountText;
    public TMP_InputField inputTMP;
    public TextMeshProUGUI placeHolderText;
    public Button sendButton;

    static readonly HttpClient http = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    bool _isBusy;
    int conversationCount = 0;

    async void Start()
    {
        // ���� ����
        systemPrompt = BuildSystemPrompt();
        UIManager.Instance.ResetAverages();

        // Ȥ�� ������ ���� ����� ������ ����(Authorization ����)
        try { http.DefaultRequestHeaders.Clear(); } catch { }
        Debug.Log($"[ENDPOINT] {endpoint}");
    }

    string BuildSystemPrompt()
    {
        string cat = GameSession.Instance ? GameSession.Instance.majorCategory : "����";
        string topic = GameSession.Instance ? GameSession.Instance.mainTopic : "���� ����";
        string counterpart = GameSession.Instance ? GameSession.Instance.counterpart : "������";
        int age = GameSession.Instance ? GameSession.Instance.age : 20;
        string gender = GameSession.Instance ? GameSession.Instance.gender : "����";
        string mbti = GameSession.Instance ? GameSession.Instance.mbti : "ISTP";
        int otherAge = GameSession.Instance ? GameSession.Instance.otherAge : 20;
        int otherGender = GameSession.Instance ? GameSession.Instance.otherGender : 0;
        string otherGenderText = (otherGender == 0) ? "����" : "����";

        return
          $"�ʴ� ���� �ùķ������� '{counterpart}' NPC��. " +
          $"�������� '{cat}', ���� ������ '{topic}'�̴�. " +
          $"���� ���̴� '{age}', ������ '{gender}', ���������� '{mbti}'�̴�. " +
          $"������ ���̴� '{otherAge}��', ������ ������ '{otherGenderText}'�̴�. " +
          $"��Ȳ�� �´� ������ ����� ��Ȳ�� �ݿ��϶�. ��Ȯ�� 2�ٷθ� ���϶�(�� 60�� �̳�). �ѱ���.";
    }
    public void EndConversationOnClick()
    {

    }

    public async void EndConversation()
    {
        sendButton.interactable = false;
        inputTMP.interactable = false;

        float emp = UIManager.Instance.GetAverage("empathy");
        float cla = UIManager.Instance.GetAverage("clarity");
        float sol = UIManager.Instance.GetAverage("solution");
        float rea = UIManager.Instance.GetAverage("realism");

        string prompt =
            $"�÷��̾��� ��ȭ ���:\n" +
            $"���� {emp:0.#}, ��Ȯ�� {cla:0.#}, �ذ����� {sol:0.#}, �������ռ� {rea:0.#}.\n" +
            "�� ������ �������� �� ���� ��ȭ ����� ��������. " +
            "��ü���̰� �����ϰ� 3�� �̳� �ѱ����.";

        var feedback = await SendChatOnceAsync(feedBackPrompt, prompt);
        UIManager.Instance.ShowEndPanel(feedback);
    }

    public async void SendMessageOnClick()
    {
        if (_isBusy) return;

        var userText = GetInputText();
        if (string.IsNullOrWhiteSpace(userText)) return;

        UIManager.Instance.AddChatMessage(userText, true);
        SetBusy(true);
        await RunTurn(userText);
        ClearInput();
        SetBusy(false);
        conversationCount++;
        remainCountText.text = $"{conversationCount}/20";
        if (conversationCount > 20) EndConversation();
    }

    /// <summary> �÷��̾� ��ȭ �� 2�� NPC���� + ä�� </summary>
    public async Task RunTurn(string playerUtterance)
    {
        // 1) NPC ����(��Ȯ�� 2��)
        var reply = await SendChatTwoLineAsync(systemPrompt, playerUtterance);
        if (!string.IsNullOrEmpty(reply))
        {
            UIManager.Instance.SetAIMessage(reply);
            UIManager.Instance.AddChatMessage(reply, false);
        }

        // 2) ä��
        var score = await EvaluateTurnAsync(playerUtterance);
        if (score != null) UpdateScoreUI(score);
    }

    // ����������������������������������������������������������������������������������������������������������������������������������������������������������
    // ����: JSON POST (�÷����� �б�)
    async Task<string> PostJsonAsync(string url, string json)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // �� WebGL: UnityWebRequest ���
        var req = new UnityWebRequest(url, "POST");
        var body = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Accept", "application/json");

        var op = req.SendWebRequest();
        while (!op.isDone) await Task.Yield();

        if (req.result != UnityWebRequest.Result.Success)
            throw new Exception($"UWR error: {req.error}, code={req.responseCode}, body={req.downloadHandler.text}");

        return req.downloadHandler.text;
#else
        // ������/���ĵ���/�����: HttpClient ���
        http.DefaultRequestHeaders.Clear(); // Ȥ�� �� �ܿ� ��� ����
        var resp = await http.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
        var bodyText = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new Exception($"HTTP {(int)resp.StatusCode}: {bodyText}");
        return bodyText;
#endif
    }

    // A. 2�� ���� ��û(Structured Output)
    async Task<string> SendChatTwoLineAsync(string system, string user)
    {
        var strictSystem = system +
            " �ݵ�� ��Ȯ�� 2�ٷθ� ���϶�. �� ���� 60�� �̳�. �Ҹ�/��ȣ/����/�ı� ����. ������ ���� ���� �ٽɸ�. �ѱ���." +
            " �����ϸ� JSON {\"line1\":\"...\",\"line2\":\"...\"} �������� �����϶�.";

        var payload = new
        {
            model = model,
            input = new object[] {
                new { role = "system", content = strictSystem },
                new { role = "user",   content = user }
            },
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "TwoLineReply",
                    schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            line1 = new { type = "string", maxLength = 60 },
                            line2 = new { type = "string", maxLength = 60 }
                        },
                        required = new[] { "line1", "line2" },
                        additionalProperties = false
                    },
                    strict = true
                }
            },
            max_output_tokens = 80
        };

        return await PostAndParseTwoLines(payload);
    }

    async Task<string> PostAndParseTwoLines(object payload)
    {
        string json = JsonConvert.SerializeObject(payload);
        try
        {
            var body = await PostJsonAsync(endpoint, json);

            // �Ľ�
            var jo = JObject.Parse(body);

            // output_json �켱
            var token = jo.SelectTokens("output[0].content[*]")
                          .FirstOrDefault(t => (string?)t["type"] == "output_json")
                          ?.SelectToken("json");

            if (token != null)
            {
                var obj = token.ToObject<TwoLineReply>();
                if (TryNormalizeTwoLines(obj?.line1, obj?.line2, out var mergedA))
                    return mergedA;
            }

            // output_text�� JSON ���ڿ��� �� ���
            var textCandidate = jo["output_text"]?.ToString()
                             ?? jo.SelectToken("output[0].content[0].text")?.ToString();

            if (!string.IsNullOrWhiteSpace(textCandidate))
            {
                try
                {
                    var obj2 = JsonConvert.DeserializeObject<TwoLineReply>(textCandidate);
                    if (TryNormalizeTwoLines(obj2?.line1, obj2?.line2, out var mergedB))
                        return mergedB;
                }
                catch { /* �ؽ�Ʈ�� �Ʒ��� */ }

                if (TrySplitPlainTextToTwoLines(textCandidate, out var mergedC))
                    return mergedC;
            }

            return "(�� �� �Ľ� ����)";
        }
        catch (Exception ex)
        {
            Debug.LogError($"[2LINE] ��Ʈ��ũ ����: {ex}");
            return "(��Ʈ��ũ ����)";
        }
    }

    // B. �ܹ� ����(�ǵ��) : systemPrompt/userPrompt ����
    async Task<string> SendChatOnceAsync(string system, string user)
    {
        var payload = new { model = model, systemPrompt = system, userPrompt = user };
        var json = JsonConvert.SerializeObject(payload);

        try
        {
            var body = await PostJsonAsync(endpoint, json);
            return ExtractText(body);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CHAT] ��Ʈ��ũ ����: {ex}");
            return "(��Ʈ��ũ ����)";
        }
    }

    // C. ä��(Structured Output)
    async Task<ConflictScore> EvaluateTurnAsync(string playerUtterance)
    {
        var payload = new
        {
            model = model,
            input = new object[] {
                new {
                    role = "system",
                    content = "�ʴ� ���� �ùķ������� �����̴�. �÷��̾� ��ȭ�� ���� "+
                              "empathy/clarity/solution/realism(�� 0~25)�� rationale�� ��Ű���� ���� �����϶�."
                },
                new { role = "user", content = playerUtterance }
            },
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "ConflictTurnScore",
                    schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            empathy = new { type = "integer", minimum = 0, maximum = 25 },
                            clarity = new { type = "integer", minimum = 0, maximum = 25 },
                            solution = new { type = "integer", minimum = 0, maximum = 25 },
                            realism = new { type = "integer", minimum = 0, maximum = 25 },
                            rationale = new { type = "string" }
                        },
                        required = new[] { "empathy", "clarity", "solution", "realism", "rationale" },
                        additionalProperties = false
                    },
                    strict = true
                }
            }
        };

        var json = JsonConvert.SerializeObject(payload);
        try
        {
            var body = await PostJsonAsync(endpoint, json);

            if (TryExtractScore(body, out var score)) return score;

            Debug.LogError("[EVAL] ���� �Ľ� ����\n" + body);
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EVAL] ��Ʈ��ũ ����: {ex}");
            return null;
        }
    }

    // ����������������������������������������������������������������������������������������������������������������������������������������������������������
    // �Ľ�/����

    string ExtractText(string body)
    {
        try
        {
            var jo = JObject.Parse(body);

            var text = jo["output_text"]?.ToString();
            if (!string.IsNullOrEmpty(text)) return text;

            var tokens = jo.SelectTokens("output[0].content[*]")
                           .Where(t => (string?)t["type"] == "output_text")
                           .Select(t => (string?)t["text"])
                           .Where(s => !string.IsNullOrEmpty(s))
                           .ToArray();
            if (tokens.Length > 0) return string.Join("\n", tokens);

            var alt = jo.SelectTokens("response.output[0].content[*]")
                        .Where(t => (string?)t["type"] == "output_text")
                        .Select(t => (string?)t["text"])
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToArray();
            if (alt.Length > 0) return string.Join("\n", alt);

            return body; // ���� ����
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CHAT] JSON �Ľ� ����: {ex}\nRAW: {body}");
            return "(�Ľ� ����)";
        }
    }

    bool TryExtractScore(string body, out ConflictScore score)
    {
        score = null;
        try
        {
            var jo = JObject.Parse(body);

            var token = jo.SelectTokens("output[0].content[*]")
                          .FirstOrDefault(t => (string?)t["type"] == "output_json")
                          ?.SelectToken("json");
            if (token != null)
            {
                score = token.ToObject<ConflictScore>();
                if (score != null) return true;
            }

            var textJson = jo["output_text"]?.ToString()
                           ?? jo.SelectToken("output[0].content[0].text")?.ToString();
            if (!string.IsNullOrEmpty(textJson))
            {
                score = JsonConvert.DeserializeObject<ConflictScore>(textJson);
                if (score != null) return true;
            }
        }
        catch { }
        return false;
    }

    bool TryNormalizeTwoLines(string l1, string l2, out string merged)
    {
        merged = null;
        l1 = (l1 ?? "").Trim();
        l2 = (l2 ?? "").Trim();
        if (string.IsNullOrEmpty(l1)) l1 = "�ǵ��� ��Ȯ�� ����.";
        if (string.IsNullOrEmpty(l2)) l2 = "���� �ൿ�� �� �ٷ� ��������.";
        if (l1.Length > 60) l1 = l1.Substring(0, 60);
        if (l2.Length > 60) l2 = l2.Substring(0, 60);
        merged = $"{l1}\n{l2}";
        return true;
    }

    bool TrySplitPlainTextToTwoLines(string text, out string merged)
    {
        merged = null;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var lines = text.Replace("\r\n", "\n").Replace("\r", "\n")
                        .Split('\n').Select(s => s.Trim())
                        .Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

        if (lines.Count < 2)
        {
            var extra = text.Replace("��", ".")
                .Split(new[] { '.', '!', '?' })
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            foreach (var e in extra) if (!lines.Contains(e)) lines.Add(e);
        }

        if (lines.Count == 0) return false;
        var l1 = lines[0];
        var l2 = (lines.Count >= 2) ? lines[1] : "������ �� �ٷ� ���ٿ���.";
        if (l1.Length > 60) l1 = l1.Substring(0, 60);
        if (l2.Length > 60) l2 = l2.Substring(0, 60);
        merged = $"{l1}\n{l2}";
        return true;
    }

    void UpdateScoreUI(ConflictScore s)
    {
        UIManager.Instance.UpdateScoreUI(s);
    }

    string GetInputText() => inputTMP ? inputTMP.text : "";
    void ClearInput() { if (inputTMP) inputTMP.text = ""; }
    void SetBusy(bool on) { _isBusy = on; if (sendButton) sendButton.interactable = !on; }
}
