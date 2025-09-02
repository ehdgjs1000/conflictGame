export default async function handler(req, res) {
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

  const apiKey =
    process.env.OPENAI_API_KEY ||
    process.env.OpenAIKey ||
    process.env.OPENAIKEY ||
    process.env.openai_api_key;

  // 🔎 핵심 로그 3개
  console.log("[chat] hasKey:", !!apiKey);
  console.log("[chat] host:", req.headers.host, "url:", req.url);
  console.log("[chat] mode:", Array.isArray(req.body?.input) ? "input+format" : "system/user");

  if (!apiKey) {
    res.setHeader("Access-Control-Allow-Origin", "*");
    return res.status(500).json({ error: "OPENAI_API_KEY missing on server" });
  }

  try {
    const body = req.body || {};
    const model = body.model || "gpt-4o-mini";
    const openaiPayload = Array.isArray(body.input)
      ? { ...body, model }
      : { model, input: [
          { role: "system", content: body.systemPrompt ?? "" },
          { role: "user",   content: body.userPrompt ?? "" }
        ] };

    const r = await fetch("https://api.openai.com/v1/responses", {
      method: "POST",
      headers: {
        "Authorization": `Bearer ${apiKey}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify(openaiPayload),
    });

    console.log("[chat] openaiStatus:", r.status);

    const text = await r.text();
    res.setHeader("Access-Control-Allow-Origin", "*");
    res.setHeader("Content-Type", "application/json");
    return res.status(r.status).send(text);
  } catch (e) {
    console.error("[chat] error:", e);
    res.setHeader("Access-Control-Allow-Origin", "*");
    return res.status(500).json({ error: String(e) });
  }
}