export default async function handler(req, res) {
  // 1) CORS 프리플라이트
  if (req.method === "OPTIONS") {
    res.setHeader("Access-Control-Allow-Origin", "*");
    res.setHeader("Access-Control-Allow-Methods", "POST,OPTIONS");
    res.setHeader("Access-Control-Allow-Headers", "Content-Type");
    res.status(200).end();
    return;
  }
  if (req.method !== "POST") {
    res.status(405).json({ error: "Method Not Allowed" });
    return;
  }

  try {
    // 2) Unity payload 수신
    //  - case A: { model, input: [...], text?: {...}, ... }  ← 두줄/채점에서 사용
    //  - case B: { model, systemPrompt, userPrompt }         ← 단일 채팅에서 사용
    const body = req.body || {};
    const model = body.model || "gpt-4o-mini";

    let openaiPayload;
    if (Array.isArray(body.input)) {
      openaiPayload = { ...body, model };
    } else {
      openaiPayload = {
        model,
        input: [
          { role: "system", content: body.systemPrompt ?? "" },
          { role: "user",   content: body.userPrompt ?? "" }
        ]
      };
    }

    // 3) OpenAI Responses API 호출
    const r = await fetch("https://api.openai.com/v1/responses", {
      method: "POST",
      headers: {
        "Authorization": `Bearer ${process.env.OpenAIKey}`, // ⭐ 필수
        "Content-Type": "application/json",
      },
      body: JSON.stringify(openaiPayload),
    });

    // 4) OpenAI 응답 그대로 반환(텍스트 그대로 전달)
    const dataText = await r.text();
    res.setHeader("Access-Control-Allow-Origin", "*");
    res.setHeader("Content-Type", "application/json");
    res.status(r.status).send(dataText);
  } catch (e) {
    res.setHeader("Access-Control-Allow-Origin", "*");
    res.status(500).json({ error: String(e) });
  }
}