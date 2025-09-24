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
using UnityEngine.Networking; // ★ WebGL에서 UnityWebRequest 사용

[Serializable] public class TwoLineReply { public string line1; public string line2; }
[Serializable]
public class ConflictScore
{
    public float empathy, clarity, solution, realism;
    public string rationale;
    public bool isResolved;     // 추가
    public bool isEscalated;    // 추가
    public float resolvedConfidence;   // 0.0 ~ 1.0
    public float escalatedConfidence;  // 0.0 ~ 1.0
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
    // Vercel 프록시 (키는 서버에만 있음)
    public string endpoint = "https://conflict-game.vercel.app/api/chat";

    [Header("UI: 입력/버튼")]
    [SerializeField] TextMeshProUGUI remainCountText;
    [SerializeField] GameObject gameEndCheckPanel; 
    public TMP_InputField inputTMP;
    public TextMeshProUGUI placeHolderText;
    public Button sendButton;

    static readonly HttpClient http = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    [SerializeField] private bool debugEndDecisionLogs = true;
    [SerializeField] private int minTurnForEndCheck = 7;    // 종료 판정 최소 턴
    [SerializeField] private float confidenceThreshold = 0.70f; // 신뢰도 임계치
    [SerializeField] private int streakNeeded = 2;     // 연속 만족 횟수(히스테리시스)

    private int resolvedStreak = 0;
    private int escalatedStreak = 0;
    bool _isBusy;
    public int conversationCount = 0;
    string conversationStyle;

    int _otherAge,_otherGender = 0;

    struct Thresholds
    {
        public float Emp;
        public float Sol;
        public float ClaPlusRea; // 필요 없으면 0
    }

    void LogEndDecision(string rule, ConflictScore s, Thresholds? th = null)
    {
        if (!debugEndDecisionLogs) return;

        // 마지막 턴 점수(이번 평가치)
        float e = s?.empathy ?? -1;
        float c = s?.clarity ?? -1;
        float so = s?.solution ?? -1;
        float r = s?.realism ?? -1;

        // 평균(누적 기준)
        float avgE = UIManager.Instance.GetAverage("empathy");
        float avgC = UIManager.Instance.GetAverage("clarity");
        float avgS = UIManager.Instance.GetAverage("solution");
        float avgR = UIManager.Instance.GetAverage("realism");

        string thStr = "";
        if (th.HasValue)
        {
            var t = th.Value;
            var parts = new System.Collections.Generic.List<string>();
            if (t.Emp > 0) parts.Add($"Emp≥{t.Emp} or ≤{t.Emp}");
            if (t.Sol > 0) parts.Add($"Sol≥{t.Sol} or ≤{t.Sol}");
            if (t.ClaPlusRea > 0) parts.Add($"(Cla+Rea)≥{t.ClaPlusRea}");
            thStr = $" | thresholds: {string.Join(", ", parts)}";
        }

        string msg =
            $"[END_DECISION] rule={rule} | turn={conversationCount} " +
            $"| flags: isResolved={s?.isResolved}({s?.resolvedConfidence:0.00}), " +
            $"isEscalated={s?.isEscalated}({s?.escalatedConfidence:0.00}) " +
            $"| last: Emp={e:0.#}, Cla={c:0.#}, Sol={so:0.#}, Rea={r:0.#} " +
            $"| avg: Emp={avgE:0.#}, Cla={avgC:0.#}, Sol={avgS:0.#}, Rea={avgR:0.#} " +
            $"{thStr} " +
            $"| rationale: {(string.IsNullOrEmpty(s?.rationale) ? "(none)" : s.rationale)}";

        Debug.Log(msg);
    }

    async void Start()
    {
        // 주제 세팅
        systemPrompt = BuildSystemPrompt();
        UIManager.Instance.ResetAverages();
        string topic = GameSession.Instance.mainTopic;
        UIManager.Instance.UpdateTopic(topic);

        // 혹시 이전에 남은 헤더가 있으면 제거(Authorization 금지)
        try { http.DefaultRequestHeaders.Clear(); } catch { }
        Debug.Log($"[ENDPOINT] {endpoint}");
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
        int otherGender = GameSession.Instance ? GameSession.Instance.otherGender : 0;
        string otherGenderText = (otherGender == 0) ? "남자" : "여자";

        if (conversationCount <= 3)
        {
            conversationStyle = "상대방의 대화 방식에 따라 비슷한 방식으로 대화하라";
        }
        else if (conversationCount > 3 && conversationCount <= 10)
        {
            conversationStyle = "반론을 제기하며 비판적으로 대화하여 갈등을 고조시켜라.";
        }
        else
        {
            conversationStyle = "갈등고조단계에 맞게 대화하라";
        }
        return
          $"너는 갈등 시뮬레이터의 '{counterpart}'(이)다. " +
          $"대주제는 '{cat}', 메인 주제는 '{topic}'이다. " +
          $"너의 나이는 '{age}', 성별은 '{gender}', 성격유형은 '{mbti}'이다. " +
          $"너의 대화 스타일은 '{conversationStyle}'이다. 꼭 명심하라. " +
          $"상대방의 나이는 '{otherAge}대', 상대방의 성별은 '{otherGenderText}'이다. " +
          $"너의 나이, 성격에 맞는 현실적 제약과 상황을 반영하라. 정확히 2줄로만 답하라(각 60자 이내). 한국어.";
    }
    public void EndConversationOnClick()
    {
        if (conversationCount <= 4) Debug.Log("아직 끝낼 수 없다");
        else 
        {
            gameEndCheckPanel.SetActive(true);
            //EndConversation("대화종료");
        }
    }
    public void EndConversationCheckClick(bool _check)
    {
        if (_check)
        {
            EndConversation("대화종료");
            gameEndCheckPanel.SetActive(false);
        }
        else
        {
            gameEndCheckPanel.SetActive(false);
        }
    }
    public async void EndConversation(string _reason)
    {
        sendButton.interactable = false;
        inputTMP.interactable = false;

        float emp = UIManager.Instance.GetAverage("empathy");
        float cla = UIManager.Instance.GetAverage("clarity");
        float sol = UIManager.Instance.GetAverage("solution");
        float rea = UIManager.Instance.GetAverage("realism");
        float[] scores = new float[4] {emp,cla,sol, rea};

        string prompt =
            $"플레이어의 대화 결과:\n" +
            $"공감 {emp:0.#}, 명확성 {cla:0.#}, 해결지향 {sol:0.#}, 현실적합성 {rea:0.#}.\n" +
            "이 점수를 바탕으로 더 좋은 대화 방식을 제안해줘. " +
            "구체적이고 간단하게 3줄 이내 한국어로 각 줄은 번호로 구분해줘.";
        var feedback = await SendChatOnceAsync(feedBackPrompt, prompt);

        string recommendPromt = $"플레이어가 전송한 대화 중 고처야할 멘트가 있다면,\n" +
            "말한 내용을 다시 알려주고 다음줄로 수정했으면 하는 방식을 구체적이고 간단하게 1~2줄 이내 한국어로 알려줘.";
        var recommend = await SendChatOnceAsync(feedBackPrompt, recommendPromt);
        UIManager.Instance.ShowEndPanel(feedback,_reason, recommend, scores);
    }

    public async void SendMessageOnClick()
    {
        if (_isBusy) return;

        var userText = GetInputText();
        if (string.IsNullOrWhiteSpace(userText)) return;
        //StartCoroutine(UIManager.Instance.BlockIF());
        UIManager.Instance.AddChatMessage(userText, true);
        SetBusy(true);
        await RunTurn(userText);
        ClearInput();
        SetBusy(false);
        conversationCount++;
        BuildSystemPrompt();
        remainCountText.text = $"{conversationCount}/20";
        if (conversationCount > 20) EndConversation("갈등을 해결하지 못하였습니다.");
    }

    /// <summary> 플레이어 발화 → 2줄 NPC응답 + 채점 </summary>
    public async Task RunTurn(string playerUtterance)
    {
        // 1) NPC 2줄 응답
        var reply = await SendChatTwoLineAsync(systemPrompt, playerUtterance);
        if (!string.IsNullOrEmpty(reply))
        {
            UIManager.Instance.SetAIMessage(reply);
            UIManager.Instance.AddChatMessage(reply, false);
        }

        // 2) 채점 + 종료 판단
        var score = await EvaluateTurnAsync(playerUtterance);
        if (score != null)
        {
            UpdateScoreUI(score);
            CheckConflictEnd(score);    
        }
    }
    void CheckConflictEnd(ConflictScore s)
    {
        // 최소 턴 전에는 종료 판정 보류(관찰만)
        if (conversationCount < minTurnForEndCheck)
        {
            if (debugEndDecisionLogs)
                Debug.Log($"[END_DECISION] hold: turn={conversationCount} < minTurnForEndCheck={minTurnForEndCheck}");
            return;
        }

        // 신뢰도 충족 여부
        bool strongResolve = s.isResolved && s.resolvedConfidence >= confidenceThreshold;
        bool strongEscalate = s.isEscalated && s.escalatedConfidence >= confidenceThreshold;

        // 스택 업데이트
        if (strongResolve && !strongEscalate)
        {
            resolvedStreak++;
            escalatedStreak = 0;
            LogEndDecision($"RESOLVED_SIGNAL(streak={resolvedStreak}/{streakNeeded})", s);

            if (resolvedStreak >= streakNeeded)
            {
                LogEndDecision("END_BY_RESOLVED", s);
                EndConversation("갈등이 충분히 해결되었습니다.");
                return;
            }
        }
        else if (strongEscalate && !strongResolve)
        {
            escalatedStreak++;
            resolvedStreak = 0;
            LogEndDecision($"ESCALATED_SIGNAL(streak={escalatedStreak}/{streakNeeded})", s);

            if (escalatedStreak >= streakNeeded)
            {
                LogEndDecision("END_BY_ESCALATED", s);
                EndConversation("갈등이 과도하게 고조되어 시뮬레이션을 종료합니다.");
                return;
            }
        }
        else
        {
            // 둘 다 false거나 상충 → 스택 리셋
            if (debugEndDecisionLogs)
                Debug.Log($"[END_DECISION] continue: ambiguous/ongoing. " +
                          $"resolve({s.isResolved},{s.resolvedConfidence:0.00}), " +
                          $"escalate({s.isEscalated},{s.escalatedConfidence:0.00})");
            resolvedStreak = 0;
            escalatedStreak = 0;
        }

        // --- 백업 휴리스틱(선택): 평균 점수 기반 보조 ---
        float emp = UIManager.Instance.GetAverage("empathy");
        float sol = UIManager.Instance.GetAverage("solution");
        float cla = UIManager.Instance.GetAverage("clarity");
        float rea = UIManager.Instance.GetAverage("realism");

        // 해결 백업 규칙
        if (conversationCount >= (minTurnForEndCheck + 1) && emp >= 18 && sol >= 18 && (cla + rea) >= 30)
        {
            LogEndDecision("BACKUP_RESOLVED(avg thresholds)", s, new Thresholds { Emp = 18, Sol = 18, ClaPlusRea = 30 });
            EndConversation("갈등이 충분히 해결되었습니다.");
            return;
        }

        // 고조 백업 규칙
        if (conversationCount >= (minTurnForEndCheck + 2) && emp <= 8 && sol <= 8)
        {
            LogEndDecision("BACKUP_ESCALATED(avg thresholds)", s, new Thresholds { Emp = 8, Sol = 8 });
            EndConversation("대화가 더 진행되기 어려운 상태로 판단됩니다.");
            return;
        }

        if (debugEndDecisionLogs)
            Debug.Log($"[END_DECISION] continue: no end yet. " +
                      $"turn={conversationCount}, resolvedStreak={resolvedStreak}, escalatedStreak={escalatedStreak}");
    }
    // ─────────────────────────────────────────────────────────────────────────────
    // 공통: JSON POST (플랫폼별 분기)
    async Task<string> PostJsonAsync(string url, string json)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // ★ WebGL: UnityWebRequest 사용
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
        // 에디터/스탠드얼론/모바일: HttpClient 사용
        http.DefaultRequestHeaders.Clear(); // 혹시 모를 잔여 헤더 제거
        var resp = await http.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
        var bodyText = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new Exception($"HTTP {(int)resp.StatusCode}: {bodyText}");
        return bodyText;
#endif
    }

    // A. 2줄 응답 요청(Structured Output)
    async Task<string> SendChatTwoLineAsync(string system, string user)
    {
        var strictSystem = system +
            " 반드시 정확히 2줄로만 답하라. 각 줄은 60자 이내. 불릿/번호/서론/후기 금지. 과도한 설명 없이 핵심만. 한국어." +
            " 가능하면 JSON {\"line1\":\"...\",\"line2\":\"...\"} 형식으로 응답하라.";

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

            // 파싱
            var jo = JObject.Parse(body);

            // output_json 우선
            var token = jo.SelectTokens("output[0].content[*]")
                          .FirstOrDefault(t => (string?)t["type"] == "output_json")
                          ?.SelectToken("json");

            if (token != null)
            {
                var obj = token.ToObject<TwoLineReply>();
                if (TryNormalizeTwoLines(obj?.line1, obj?.line2, out var mergedA))
                    return mergedA;
            }

            // output_text에 JSON 문자열이 온 경우
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
                catch { /* 텍스트면 아래로 */ }

                if (TrySplitPlainTextToTwoLines(textCandidate, out var mergedC))
                    return mergedC;
            }

            return "(두 줄 파싱 실패)";
        }
        catch (Exception ex)
        {
            Debug.LogError($"[2LINE] 네트워크 예외: {ex}");
            return "(네트워크 오류)";
        }
    }

    // B. 단문 응답(피드백) : systemPrompt/userPrompt 형태
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
            Debug.LogError($"[CHAT] 네트워크 예외: {ex}");
            return "(네트워크 오류)";
        }
    }

    // C. 채점(Structured Output)
    async Task<ConflictScore> EvaluateTurnAsync(string playerUtterance)
    {
        // 현재 평균을 가져와서 모델 판단에 참고로 제공
        float avgEmp = UIManager.Instance.GetAverage("empathy");
        float avgCla = UIManager.Instance.GetAverage("clarity");
        float avgSol = UIManager.Instance.GetAverage("solution");
        float avgRea = UIManager.Instance.GetAverage("realism");

        var payload = new
        {
            model = model,
            input = new object[] {
            new {
                role = "system",
                content =
                    "너는 갈등 시뮬레이터의 심판이다. 플레이어 발화를 평가해 " +
                    "empathy/clarity/solution/realism(각 0~25)과 rationale을 제공하고, " +
                    "대화 상태를 판정하라. isResolved, isEscalated는 동시에 false일 수 있다(진행 중/불확실). " +
                    "불확실하면 둘 다 false로 하고 rationale에 추가 근거를 서술하라. " +
                    "해결(resolved)은 상호 이해 및 다음 행동 합의가 성립한 경우, " +
                    "고조(escalated)는 상호 공격·대화 거부·관계 악화로 더 나아갈 수 없는 경우로 정의한다. " +
                    "판정의 확신도를 0.0~1.0로 반환하라(resolvedConfidence, escalatedConfidence)."
            },
            new {
                role = "user",
                content =
                    $"플레이어 발화: {playerUtterance}\n" +
                    $"현재 평균점수 - empathy:{avgEmp:0.#}, clarity:{avgCla:0.#}, solution:{avgSol:0.#}, realism:{avgRea:0.#}\n" +
                    "평가와 판정을 스키마에 맞춰 반환하라."
            }
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
                            rationale = new { type = "string" },
                            isResolved = new { type = "boolean" },   // 추가
                            isEscalated = new { type = "boolean" },    // 추가
                            resolvedConfidence = new { type = "number", minimum = 0.0, maximum = 1.0 },
                            escalatedConfidence = new { type = "number", minimum = 0.0, maximum = 1.0 }
                        },
                        required = new[] { "empathy","clarity","solution","realism","rationale",
                   "isResolved","isEscalated","resolvedConfidence","escalatedConfidence" },
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

            Debug.LogError("[EVAL] 점수 파싱 실패\n" + body);
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EVAL] 네트워크 예외: {ex}");
            return null;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 파싱/보조

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

            return body; // 최후 폴백
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CHAT] JSON 파싱 예외: {ex}\nRAW: {body}");
            return "(파싱 오류)";
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
        if (string.IsNullOrEmpty(l1)) l1 = "의도를 명확히 해줘.";
        if (string.IsNullOrEmpty(l2)) l2 = "다음 행동을 한 줄로 제안해줘.";
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
            var extra = text.Replace("…", ".")
                .Split(new[] { '.', '!', '?' })
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            foreach (var e in extra) if (!lines.Contains(e)) lines.Add(e);
        }

        if (lines.Count == 0) return false;
        var l1 = lines[0];
        var l2 = (lines.Count >= 2) ? lines[1] : "요점을 한 줄로 덧붙여줘.";
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
    void SetBusy(bool on) 
    {   _isBusy = on; 
        if (sendButton) sendButton.interactable = !on;
        if (inputTMP) inputTMP.interactable = !on;
    }
}
