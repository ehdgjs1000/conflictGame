// api/chat.js
export default async function handler(req, res) {
  // CORS
  if (req.method === "OPTIONS") {
    res.setHeader("Access-Control-Allow-Origin", "*");
    res.setHeader("Access-Control-Allow-Methods", "POST,OPTIONS");
    res.setHeader("Access-Control-Allow-Headers", "Content-Type");
    return res.status(200).end();
  }

  if (req.method !== "POST") {
    res.setHeader("Access-Control-Allow-Origin", "*");
    return res.status(405).json({ error: "Method Not Allowed" });
  }

  // ✅ 환경변수 이름을 둘 다 허용 (대문자/소문자 섞여 등록했을 가능성 방지)
  const apiKey =
    process.env.OPENAI_API_KEY ||
    process.env.OpenAIKey ||
    process.env.OPENAIKEY ||
    process.env.openai_api_key;

  if (!apiKey) {
    res.setHeader("Access-Control-Allow-Origin", "*");
    return res.status(500).json({
      error: "OPENAI_API_KEY is missing on server. Set it in Vercel → Project → Settings → Environment Variables and redeploy."
    });
  }

  try {
    const body = req.body || {};
    const model = body.model || "gpt-4o-mini";

    // ✅ Unity가 보내는 두 가지 케이스 모두 처리
    // (A) 이미 input/text.format 포함해서 보낸 경우 → 그대로 패스
    // (B) systemPrompt/userPrompt로 보낸 경우 → 여기서 input 조합
    const openaiPayload = Array.isArray(body.input)
      ? { ...body, model }
      : {
          model,
          input: [
            { role: "system", content: body.systemPrompt ?? "" },
            { role: "user",   content: body.userPrompt ?? "" }
          ]
        };

    const r = await fetch("https://api.openai.com/v1/responses", {
      method: "POST",
      headers: {
        "Authorization": `Bearer ${apiKey}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify(openaiPayload),
    });

    const text = await r.text(); // 그대로 전달(바이너리/텍스트 상관없이)
    res.setHeader("Access-Control-Allow-Origin", "*");
    res.setHeader("Content-Type", "application/json");
    return res.status(r.status).send(text);
  } catch (e) {
    res.setHeader("Access-Control-Allow-Origin", "*");
    return res.status(500).json({ error: String(e) });
  }
}
