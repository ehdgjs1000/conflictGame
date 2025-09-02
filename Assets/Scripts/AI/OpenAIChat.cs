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
        "너는 갈등 시뮬레이터의 '선배직원' NPC다. 선배직원과의 대화 갈등문제를 시뮬레이션 할거야.";
    [TextArea]
    public string initialUserMessage =
        "제가 왜 선배님의 말씀을 무시한다고 생각하시나요.";
    public string feedBackPrompt;

    [Header("Model/Endpoint")]
    public string model = "gpt-4o-mini";
    public string endpoint = "https://conflict-game.vercel.app/api/chat";

    [Header("Auth (테스트용)")]
    [Tooltip("배포 전 반드시 환경변수/프록시로 전환")]
    public string apiKey = "API_KEY_HERE";

    [Header("UI: 입력/버튼")]
    public TMP_InputField inputTMP;
    public TextMeshProUGUI placeHolderText;
    public Button sendButton;


    static readonly HttpClient http = new HttpClient();
    bool _isBusy;

    async void Start()
    {
        //처음 주제 잡을때
        systemPrompt = BuildSystemPrompt();
        UIManager.Instance.ResetAverages();
    }
    
    string BuildSystemPrompt()
    {
        string cat = GameSession.Instance ? GameSession.Instance.majorCategory : "개인";
        string topic = GameSession.Instance ? GameSession.Instance.mainTopic : "연봉 협상";
        string counterpart = GameSession.Instance ? GameSession.Instance.counterpart : "직장상사";
        int age = GameSession.Instance ? GameSession.Instance.age : 20;
        string gender = GameSession.Instance ? GameSession.Instance.gender : "남자";
        string mbti = GameSession.Instance ? GameSession.Instance.mbti : "ISTP";
        int otherAge = GameSession.Instance ? GameSession.Instance.otherAge : 20;
        int otherGender= GameSession.Instance ? GameSession.Instance.otherGender : 0;
        string otherGenderText = "남자";
        if (otherGender == 0) otherGenderText = "남자";
        else otherGenderText = "여자";

        // 역할과 제약, 2줄 응답 규칙을 상황에 맞게
        return
          $"너는 갈등 시뮬레이터의 '{counterpart}' NPC다. " +
          $"대주제는 '{cat}', 메인 주제는 '{topic}'이다. " +
          $"너의 나이는 '{age}', 성별은 '{gender}', 성격유형은 '{mbti}'이다. " +
          $"상대방의 나이는 '{otherAge}대', 상대방의 성별은 '{otherGenderText}'이다. " +
          $"상황에 맞는 현실적 제약과 상황을 반영하라. 정확히 2줄로만 답하라(각 60자 이내). 한국어.";
    }
    public async void EndConversation()
    {
        // 버튼 등에서 호출
        sendButton.interactable = false;
        inputTMP.interactable = false;

        // 평균 점수 가져오기
        float emp = UIManager.Instance.GetAverage("empathy");
        float cla = UIManager.Instance.GetAverage("clarity");
        float sol = UIManager.Instance.GetAverage("solution");
        float rea = UIManager.Instance.GetAverage("realism");

        string prompt =
            $"플레이어의 대화 결과:\n" +
            $"공감 {emp:0.#}, 명확성 {cla:0.#}, 해결지향 {sol:0.#}, 현실적합성 {rea:0.#}.\n" +
            "이 점수를 바탕으로 더 좋은 대화 방식을 제안해줘. " +
            "구체적이고 간단하게 3줄 이내 한국어로.";

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
        // 1) 시스템 지침 강화
        var strictSystem = system +
            " 반드시 정확히 2줄로만 답하라. 각 줄은 60자 이내. 불릿/번호/서론/후기 금지. 과도한 설명 없이 핵심만. 한국어." +
            " 가능하면 JSON {\"line1\":\"...\",\"line2\":\"...\"} 형식으로 응답하라.";
        Debug.Log(strictSystem);

        // 2) 요청 페이로드(Structured Output 요청)
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
            // 원시 로그 확인용
            // Debug.Log($"[RAW]\n{body}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[2LINE] 네트워크 예외: {ex}");
            return "(네트워크 오류)";
        }

        if (!resp.IsSuccessStatusCode)
        {
            Debug.LogError($"[2LINE] API Error {(int)resp.StatusCode}: {body}");
            return "(API 오류)";
        }

        // 3) 파싱 루틴 (강건)
        try
        {
            var jo = JObject.Parse(body);

            // 3-1) 권장: output_json → json 객체
            var token = jo.SelectTokens("output[0].content[*]")
                          .FirstOrDefault(t => (string?)t["type"] == "output_json")
                          ?.SelectToken("json");

            if (token != null)
            {
                var obj = token.ToObject<TwoLineReply>();
                var ok = TryNormalizeTwoLines(obj?.line1, obj?.line2, out var merged);
                if (ok) return merged;
            }

            // 3-2) 예외: output_text 안에 JSON 문자열로 온 경우
            var textCandidate = jo["output_text"]?.ToString()
                             ?? jo.SelectToken("output[0].content[0].text")?.ToString();

            if (!string.IsNullOrWhiteSpace(textCandidate))
            {
                // 먼저 JSON 시도
                try
                {
                    var obj2 = JsonConvert.DeserializeObject<TwoLineReply>(textCandidate);
                    var ok2 = TryNormalizeTwoLines(obj2?.line1, obj2?.line2, out var merged2);
                    if (ok2) return merged2;
                }
                catch { /* 텍스트였던 경우 무시 */ }

                // 텍스트였다면 줄 단위 보강 파싱
                if (TrySplitPlainTextToTwoLines(textCandidate, out var merged3))
                    return merged3;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[2LINE] 파싱 예외: {ex}\nRAW: {body}");
        }

        // 3-3) 최종 폴백
        return "(두 줄 파싱 실패)";
    }

    // ? 헬퍼: 2줄 보정(Trim, 길이 제한, 비어있음 처리)
    bool TryNormalizeTwoLines(string l1, string l2, out string merged)
    {
        merged = null;
        l1 = (l1 ?? "").Trim();
        l2 = (l2 ?? "").Trim();

        // 최소 보정: 빈 줄이면 대체 문구
        if (string.IsNullOrEmpty(l1)) l1 = "의도를 명확히 해줘.";
        if (string.IsNullOrEmpty(l2)) l2 = "다음 행동을 한 줄로 제안해줘.";

        // 길이 제한(60자) 강제
        if (l1.Length > 60) l1 = l1.Substring(0, 60);
        if (l2.Length > 60) l2 = l2.Substring(0, 60);

        merged = $"{l1}\n{l2}";
        return true;
    }

    // ? 헬퍼: 일반 텍스트를 줄바꿈 기준으로 2줄로 만드는 보조 파서
    bool TrySplitPlainTextToTwoLines(string text, out string merged)
    {
        merged = null;
        if (string.IsNullOrWhiteSpace(text)) return false;

        // 개행/문장부호 단위로 분리
        var lines = text
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Split('\n')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        // 문장이 너무 길게 한 줄에 붙어 있으면 마침표/물음표 기준으로도 쪼개보기
        if (lines.Count < 2)
        {
            var extra = text
                .Replace("…", ".")
                .Split(new[] { '.', '!', '?' })
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
            foreach (var e in extra) if (!lines.Contains(e)) lines.Add(e);
        }

        if (lines.Count == 0) return false;
        var l1 = lines[0];
        var l2 = (lines.Count >= 2) ? lines[1] : "요점을 한 줄로 덧붙여줘.";

        // 길이 제한 보정
        if (l1.Length > 60) l1 = l1.Substring(0, 60);
        if (l2.Length > 60) l2 = l2.Substring(0, 60);

        merged = $"{l1}\n{l2}";
        return true;
    }
    /// <summary>
    /// 외부에서 호출: 플레이어 발화를 넣으면 NPC 응답 + 채점 + UI반영
    /// </summary>
    public async Task RunTurn(string playerUtterance)
    {
        // 1) NPC 응답(정확히 2줄)
        var reply = await SendChatTwoLineAsync(systemPrompt, playerUtterance);
        if (!string.IsNullOrEmpty(reply))
        {
            UIManager.Instance.SetAIMessage(reply);
            UIManager.Instance.AddChatMessage(reply, false);
        }

        // 2) 채점
        var score = await EvaluateTurnAsync(playerUtterance);
        if (score != null) UpdateScoreUI(score);
    }


    string GetApiKey()
    {
        // 1) 하드코딩 테스트
        if (!string.IsNullOrEmpty(apiKey) && apiKey != "API_KEY_HERE") return apiKey;

        // 2) 환경변수(배포 권장)
        var key =
            System.Environment.GetEnvironmentVariable("OPENAI_API_KEY") ??
            System.Environment.GetEnvironmentVariable("OPENAI_API_KEY",
                System.EnvironmentVariableTarget.User) ??
            System.Environment.GetEnvironmentVariable("OPENAI_API_KEY",
                System.EnvironmentVariableTarget.Machine);
        return key;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // A. 텍스트 응답 (비?스트리밍)
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
                return "(API 오류)";
            }
            return ExtractText(body);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CHAT] 네트워크 예외: {ex}");
            return "(네트워크 오류)";
        }
    }

    // B. 채점(Structured Outputs: text.format = json_schema)
    async Task<ConflictScore> EvaluateTurnAsync(string playerUtterance)
    {
        var payload = new
        {
            model = model,
            input = new object[] {
        new {
            role = "system",
            content = "너는 갈등 시뮬레이터의 심판이다. 플레이어 발화를 평가해 "+
                      "empathy/clarity/solution/realism(각 0~25)과 rationale을 스키마에 맞춰 제공하라."
        },
        new { role = "user", content = playerUtterance }
    },
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "ConflictTurnScore",   // ← 여기로 이동
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

            Debug.LogError("[EVAL] 점수 파싱 실패\n" + body);
            return null;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[EVAL] 네트워크 예외: {ex}");
            return null;
        }
    }

    // 텍스트 추출: output_text → output[0].content[*].text
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
            Debug.LogError($"[CHAT] JSON 파싱 예외: {ex}\nRAW: {body}");
            return "(파싱 오류)";
        }
    }

    // 점수 추출: output[*].content[*].type == "output_json" → json
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

            // 예외: 모델이 문자열 JSON을 text로 줄 때
            var textJson = jo["output_text"]?.ToString()
                           ?? jo.SelectToken("output[0].content[0].text")?.ToString();
            if (!string.IsNullOrEmpty(textJson))
            {
                score = JsonConvert.DeserializeObject<ConflictScore>(textJson);
                if (score != null) return true;
            }
        }
        catch { /* 무시 */ }

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
        // 필요하면 로딩 스피너 활성/비활성 등 추가
    }
}
