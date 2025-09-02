// Assets/Scripts/ConflictAIController.cs
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class TwoLineReply
{
    public string line1;
    public string line2;
}
[System.Serializable]
public class ConflictScore
{
    public float empathy, clarity, solution, realism;
    public string rationale;
}

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
    public string endpoint = "https://conflict-game.vercel.app/api/chat";

    [Header("Auth (�׽�Ʈ��)")]
    [Tooltip("���� �� �ݵ�� ȯ�溯��/���Ͻ÷� ��ȯ")]
    public string apiKey = "API_KEY_HERE";

    [Header("UI: �Է�/��ư")]
    public TMP_InputField inputTMP;
    public TextMeshProUGUI placeHolderText;
    public Button sendButton;


    static readonly HttpClient http = new HttpClient();
    bool _isBusy;

    async void Start()
    {
        //ó�� ���� ������
        systemPrompt = BuildSystemPrompt();
        UIManager.Instance.ResetAverages();
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
        int otherGender= GameSession.Instance ? GameSession.Instance.otherGender : 0;
        string otherGenderText = "����";
        if (otherGender == 0) otherGenderText = "����";
        else otherGenderText = "����";

        // ���Ұ� ����, 2�� ���� ��Ģ�� ��Ȳ�� �°�
        return
          $"�ʴ� ���� �ùķ������� '{counterpart}' NPC��. " +
          $"�������� '{cat}', ���� ������ '{topic}'�̴�. " +
          $"���� ���̴� '{age}', ������ '{gender}', ���������� '{mbti}'�̴�. " +
          $"������ ���̴� '{otherAge}��', ������ ������ '{otherGenderText}'�̴�. " +
          $"��Ȳ�� �´� ������ ����� ��Ȳ�� �ݿ��϶�. ��Ȯ�� 2�ٷθ� ���϶�(�� 60�� �̳�). �ѱ���.";
    }
    public async void EndConversation()
    {
        // ��ư ��� ȣ��
        sendButton.interactable = false;
        inputTMP.interactable = false;

        // ��� ���� ��������
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
        if (string.IsNullOrWhiteSpace(userText))
        {
            return;
        }

        UIManager.Instance.AddChatMessage(userText, true);
        SetBusy(true);
        await RunTurn(userText);
        ClearInput();
        SetBusy(false);
    }
    async Task<string> SendChatTwoLineAsync(string system, string user)
    {
        // 1) �ý��� ��ħ ��ȭ
        var strictSystem = system +
            " �ݵ�� ��Ȯ�� 2�ٷθ� ���϶�. �� ���� 60�� �̳�. �Ҹ�/��ȣ/����/�ı� ����. ������ ���� ���� �ٽɸ�. �ѱ���." +
            " �����ϸ� JSON {\"line1\":\"...\",\"line2\":\"...\"} �������� �����϶�.";
        Debug.Log(strictSystem);

        // 2) ��û ���̷ε�(Structured Output ��û)
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

        var json = JsonConvert.SerializeObject(payload);
        HttpResponseMessage resp;
        string body;

        try
        {
            resp = await http.PostAsync(
                endpoint,
                new StringContent(json, Encoding.UTF8, "application/json")
            );
            body = await resp.Content.ReadAsStringAsync();
            // ���� �α� Ȯ�ο�
            // Debug.Log($"[RAW]\n{body}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[2LINE] ��Ʈ��ũ ����: {ex}");
            return "(��Ʈ��ũ ����)";
        }

        if (!resp.IsSuccessStatusCode)
        {
            Debug.LogError($"[2LINE] API Error {(int)resp.StatusCode}: {body}");
            return "(API ����)";
        }

        // 3) �Ľ� ��ƾ (����)
        try
        {
            var jo = JObject.Parse(body);

            // 3-1) ����: output_json �� json ��ü
            var token = jo.SelectTokens("output[0].content[*]")
                          .FirstOrDefault(t => (string?)t["type"] == "output_json")
                          ?.SelectToken("json");

            if (token != null)
            {
                var obj = token.ToObject<TwoLineReply>();
                var ok = TryNormalizeTwoLines(obj?.line1, obj?.line2, out var merged);
                if (ok) return merged;
            }

            // 3-2) ����: output_text �ȿ� JSON ���ڿ��� �� ���
            var textCandidate = jo["output_text"]?.ToString()
                             ?? jo.SelectToken("output[0].content[0].text")?.ToString();

            if (!string.IsNullOrWhiteSpace(textCandidate))
            {
                // ���� JSON �õ�
                try
                {
                    var obj2 = JsonConvert.DeserializeObject<TwoLineReply>(textCandidate);
                    var ok2 = TryNormalizeTwoLines(obj2?.line1, obj2?.line2, out var merged2);
                    if (ok2) return merged2;
                }
                catch { /* �ؽ�Ʈ���� ��� ���� */ }

                // �ؽ�Ʈ���ٸ� �� ���� ���� �Ľ�
                if (TrySplitPlainTextToTwoLines(textCandidate, out var merged3))
                    return merged3;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[2LINE] �Ľ� ����: {ex}\nRAW: {body}");
        }

        // 3-3) ���� ����
        return "(�� �� �Ľ� ����)";
    }

    // ? ����: 2�� ����(Trim, ���� ����, ������� ó��)
    bool TryNormalizeTwoLines(string l1, string l2, out string merged)
    {
        merged = null;
        l1 = (l1 ?? "").Trim();
        l2 = (l2 ?? "").Trim();

        // �ּ� ����: �� ���̸� ��ü ����
        if (string.IsNullOrEmpty(l1)) l1 = "�ǵ��� ��Ȯ�� ����.";
        if (string.IsNullOrEmpty(l2)) l2 = "���� �ൿ�� �� �ٷ� ��������.";

        // ���� ����(60��) ����
        if (l1.Length > 60) l1 = l1.Substring(0, 60);
        if (l2.Length > 60) l2 = l2.Substring(0, 60);

        merged = $"{l1}\n{l2}";
        return true;
    }

    // ? ����: �Ϲ� �ؽ�Ʈ�� �ٹٲ� �������� 2�ٷ� ����� ���� �ļ�
    bool TrySplitPlainTextToTwoLines(string text, out string merged)
    {
        merged = null;
        if (string.IsNullOrWhiteSpace(text)) return false;

        // ����/�����ȣ ������ �и�
        var lines = text
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Split('\n')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        // ������ �ʹ� ��� �� �ٿ� �پ� ������ ��ħǥ/����ǥ �������ε� �ɰ�����
        if (lines.Count < 2)
        {
            var extra = text
                .Replace("��", ".")
                .Split(new[] { '.', '!', '?' })
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
            foreach (var e in extra) if (!lines.Contains(e)) lines.Add(e);
        }

        if (lines.Count == 0) return false;
        var l1 = lines[0];
        var l2 = (lines.Count >= 2) ? lines[1] : "������ �� �ٷ� ���ٿ���.";

        // ���� ���� ����
        if (l1.Length > 60) l1 = l1.Substring(0, 60);
        if (l2.Length > 60) l2 = l2.Substring(0, 60);

        merged = $"{l1}\n{l2}";
        return true;
    }
    /// <summary>
    /// �ܺο��� ȣ��: �÷��̾� ��ȭ�� ������ NPC ���� + ä�� + UI�ݿ�
    /// </summary>
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


    string GetApiKey()
    {
        // 1) �ϵ��ڵ� �׽�Ʈ
        if (!string.IsNullOrEmpty(apiKey) && apiKey != "API_KEY_HERE") return apiKey;

        // 2) ȯ�溯��(���� ����)
        var key =
            System.Environment.GetEnvironmentVariable("OPENAI_API_KEY") ??
            System.Environment.GetEnvironmentVariable("OPENAI_API_KEY",
                System.EnvironmentVariableTarget.User) ??
            System.Environment.GetEnvironmentVariable("OPENAI_API_KEY",
                System.EnvironmentVariableTarget.Machine);
        return key;
    }

    // ����������������������������������������������������������������������������������������������������������������������������������������������������������
    // A. �ؽ�Ʈ ���� (��?��Ʈ����)
    async Task<string> SendChatOnceAsync(string system, string user)
    {
        var payload = new
        {
            model = model,
            systemPrompt = system,
            userPrompt = user
        };

        var json = JsonConvert.SerializeObject(payload);
        try
        {
            var resp = await http.PostAsync(
                endpoint,
                new StringContent(json, Encoding.UTF8, "application/json")
            );
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                Debug.LogError($"[CHAT] API Error {(int)resp.StatusCode}: {body}");
                return "(API ����)";
            }
            return ExtractText(body);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CHAT] ��Ʈ��ũ ����: {ex}");
            return "(��Ʈ��ũ ����)";
        }
    }

    // B. ä��(Structured Outputs: text.format = json_schema)
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
                    name = "ConflictTurnScore",   // �� ����� �̵�
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
            var resp = await http.PostAsync(
                endpoint,
                new StringContent(json, Encoding.UTF8, "application/json")
            );
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                Debug.LogError($"[EVAL] API Error {(int)resp.StatusCode}: {body}");
                return null;
            }

            if (TryExtractScore(body, out var score))
                return score;

            Debug.LogError("[EVAL] ���� �Ľ� ����\n" + body);
            return null;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[EVAL] ��Ʈ��ũ ����: {ex}");
            return null;
        }
    }

    // �ؽ�Ʈ ����: output_text �� output[0].content[*].text
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

            return body;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CHAT] JSON �Ľ� ����: {ex}\nRAW: {body}");
            return "(�Ľ� ����)";
        }
    }

    // ���� ����: output[*].content[*].type == "output_json" �� json
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

            // ����: ���� ���ڿ� JSON�� text�� �� ��
            var textJson = jo["output_text"]?.ToString()
                           ?? jo.SelectToken("output[0].content[0].text")?.ToString();
            if (!string.IsNullOrEmpty(textJson))
            {
                score = JsonConvert.DeserializeObject<ConflictScore>(textJson);
                if (score != null) return true;
            }
        }
        catch { /* ���� */ }

        return false;
    }

    void UpdateScoreUI(ConflictScore s)
    {
        float[] scoreArr = new float[4] { s.empathy, s.clarity, s.solution, s.realism };
        //UIManager.Instance.SetScores(scoreArr);
        UIManager.Instance.UpdateScoreUI(s);
    }


    string GetInputText()
    {
        return inputTMP ? inputTMP.text : "";
    }

    void ClearInput()
    {
        if (inputTMP) inputTMP.text = "";
    }

    void SetBusy(bool on)
    {
        _isBusy = on;
        if (sendButton) sendButton.interactable = !on;
        // �ʿ��ϸ� �ε� ���ǳ� Ȱ��/��Ȱ�� �� �߰�
    }
}
