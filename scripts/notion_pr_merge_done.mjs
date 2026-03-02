import fs from "node:fs/promises";

const NOTION_TOKEN = process.env.NOTION_TOKEN;
const NOTION_VERSION = process.env.NOTION_VERSION ?? "2025-09-03";

const WH_DONE = process.env.DISCORD_WEBHOOK_DONE;

const P_STATUS = "Status";
const STATUS_DONE = "Done";

const P_DONE_NOTIFIED = "Done Notified";

function mustEnv(name, v) {
  if (!v) throw new Error(`Missing env var: ${name}`);
  return v;
}

mustEnv("NOTION_TOKEN", NOTION_TOKEN);
// Optional: uncomment if you want hard-fail when webhook missing
// mustEnv("DISCORD_WEBHOOK_DONE", WH_DONE);

async function notionFetch(path, method, body) {
  const res = await fetch(`https://api.notion.com/v1${path}`, {
    method,
    headers: {
      Authorization: `Bearer ${NOTION_TOKEN}`,
      "Notion-Version": NOTION_VERSION,
      "Content-Type": "application/json",
    },
    body: body ? JSON.stringify(body) : undefined,
  });

  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Notion ${method} ${path} failed: ${res.status} ${text}`);
  }
  return res.json();
}

async function notionPatchPage(pageId, properties) {
  return notionFetch(`/pages/${pageId}`, "PATCH", { properties });
}

async function notionGetPage(pageId) {
  return notionFetch(`/pages/${pageId}`, "GET");
}

async function discordPost(webhookUrl, content) {
  if (!webhookUrl) return; // allow "no webhook" mode
  const res = await fetch(webhookUrl, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      content,
      allowed_mentions: { parse: ["users"] },
    }),
  });
  if (!res.ok) {
    throw new Error(`Discord webhook failed: ${res.status} ${await res.text()}`);
  }
}

function normalizeNotionId(hexOrHyphenated) {
  const s = hexOrHyphenated.replace(/-/g, "");
  if (!/^[0-9a-fA-F]{32}$/.test(s)) return null;
  return `${s.slice(0, 8)}-${s.slice(8, 12)}-${s.slice(12, 16)}-${s.slice(16, 20)}-${s.slice(20)}`.toLowerCase();
}

function extractNotionPageIds(text) {
  if (!text) return [];
  const ids = new Set();

  // Only accept IDs that appear inside a Notion URL (notion.so or *.notion.site)
  const urlRe = /https?:\/\/[^\s)]+/g;

  for (const m of text.matchAll(urlRe)) {
    // Trim common trailing punctuation from Markdown or sentences
    const url = m[0].replace(/[>\])\.,;]+$/g, "");

    if (!/notion\.(so|site)\//i.test(url)) continue;

    const idMatch =
      url.match(/[0-9a-fA-F]{32}/) ||
      url.match(/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}/);

    if (!idMatch) continue;

    const id = normalizeNotionId(idMatch[0]);
    if (id) ids.add(id);
  }

  return [...ids];
}

function getTitle(page) {
  const props = page.properties ?? {};
  for (const v of Object.values(props)) {
    if (v?.type === "title") {
      const t = v.title ?? [];
      return t.map((x) => x.plain_text ?? "").join("") || "Untitled";
    }
  }
  return "Untitled";
}

async function main() {
  const eventPath = process.env.GITHUB_EVENT_PATH;
  const raw = await fs.readFile(eventPath, "utf8");
  const evt = JSON.parse(raw);

  const pr = evt.pull_request;
  if (!pr || pr.merged !== true) {
    console.log("PR not merged; nothing to do.");
    return;
  }

  // Extra safety: only act for merges into main
  if (pr.base?.ref !== "main") {
    console.log(`Merged into ${pr.base?.ref}; skipping (only sync for main).`);
    return;
  }

  const ids = extractNotionPageIds(`${pr.title}\n${pr.body ?? ""}`);
  if (!ids.length) {
    console.log("No Notion page IDs found in PR title/body.");
    return;
  }

  for (const pageId of ids) {
    try {
      // Atomically mark Done + Done Notified to prevent poller duplicates
      await notionPatchPage(pageId, {
        [P_STATUS]: { status: { name: STATUS_DONE } },
        [P_DONE_NOTIFIED]: { checkbox: true },
      });

      const page = await notionGetPage(pageId);
      const title = getTitle(page);
      const url = page.url ?? "(no url)";

      await discordPost(
        WH_DONE,
        `✅ Done (via PR merge): **${title}**\nPR: ${pr.html_url}\n${url}`
      );
    } catch (err) {
      console.warn(`Skipping Notion page ${pageId}: ${String(err)}`);
      continue;
    }
  }
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});