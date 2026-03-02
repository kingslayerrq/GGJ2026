import fs from "node:fs/promises";

const NOTION_TOKEN = process.env.NOTION_TOKEN;
const NOTION_DATA_SOURCE_ID = process.env.NOTION_DATA_SOURCE_ID;
const NOTION_VERSION = process.env.NOTION_VERSION ?? "2025-09-03";

const WH_ASSIGN = process.env.DISCORD_WEBHOOK_ASSIGNMENTS;
const WH_DEADLINES = process.env.DISCORD_WEBHOOK_DEADLINES;
const WH_DONE = process.env.DISCORD_WEBHOOK_DONE;

const REMIND_WITHIN_DAYS = Number(process.env.REMIND_WITHIN_DAYS ?? "2");
const MAP_PATH = process.env.NOTION_DISCORD_MAP_PATH ?? ".github/notion_discord_map.json";

// ---- Notion property names (match your DB exactly) ----
const P_STATUS = "Status";
const P_ASSIGNEE = "Assignee";
const P_DUE = "Due date";

const P_LAST_NOTIFIED_ASSIGNEES = "Last Notified Assignees";
const P_DUE_REMINDER_SENT_FOR = "Due Reminder Sent For";
const P_DONE_NOTIFIED = "Done Notified";

const STATUS_DONE = "Done";

function mustEnv(name, v) {
  if (!v) throw new Error(`Missing env var: ${name}`);
  return v;
}

mustEnv("NOTION_TOKEN", NOTION_TOKEN);
mustEnv("NOTION_DATA_SOURCE_ID", NOTION_DATA_SOURCE_ID);

const notionDiscordMap = JSON.parse(await fs.readFile(MAP_PATH, "utf8"));

function ymd(date) {
  const d = new Date(date);
  const yyyy = d.getUTCFullYear();
  const mm = String(d.getUTCMonth() + 1).padStart(2, "0");
  const dd = String(d.getUTCDate()).padStart(2, "0");
  return `${yyyy}-${mm}-${dd}`;
}

function startOfTodayUTC() {
  const now = new Date();
  return new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()));
}

function addDaysUTC(date, days) {
  const d = new Date(Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate()));
  d.setUTCDate(d.getUTCDate() + days);
  return d;
}

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

async function notionQueryAllPages() {
  const out = [];
  let start_cursor = undefined;

  while (true) {
    const body = { page_size: 100 };
    if (start_cursor) body.start_cursor = start_cursor;

    const data = await notionFetch(`/data_sources/${NOTION_DATA_SOURCE_ID}/query`, "POST", body);

    out.push(...(data.results ?? []));
    if (!data.has_more) break;
    start_cursor = data.next_cursor;
  }

  return out;
}

async function notionPatchPage(pageId, properties) {
  return notionFetch(`/pages/${pageId}`, "PATCH", { properties });
}

async function discordPost(webhookUrl, content) {
  if (!webhookUrl) return;
  const res = await fetch(webhookUrl, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      content,
      allowed_mentions: { parse: ["users"] }, // prevents @everyone/@here unless you explicitly allow it
    }),
  });

  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Discord webhook failed: ${res.status} ${text}`);
  }
}

function getProp(page, name) {
  return page.properties?.[name];
}

function getStatus(page) {
  return getProp(page, P_STATUS)?.status?.name ?? null;
}

function getAssignees(page) {
  return getProp(page, P_ASSIGNEE)?.people ?? [];
}

function getDueStart(page) {
  const start = getProp(page, P_DUE)?.date?.start ?? null;
  return start ? new Date(start) : null;
}

function getRichTextString(page, name) {
  const rt = getProp(page, name)?.rich_text;
  if (!Array.isArray(rt) || rt.length === 0) return "";
  return rt.map((x) => x.plain_text ?? "").join("");
}

function getCheckbox(page, name) {
  return !!getProp(page, name)?.checkbox;
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

function mentionForNotionUserId(notionUserId) {
  const discordId = notionDiscordMap[notionUserId];
  return discordId ? `<@${discordId}>` : null;
}

function formatAssigneeMentions(assignees) {
  if (!assignees.length) return "(unassigned)";
  const mentions = assignees.map((u) => mentionForNotionUserId(u.id)).filter(Boolean);
  return mentions.length ? mentions.join(" ") : "(no discord mapping)";
}

async function main() {
  const pages = await notionQueryAllPages();

  const todayStartUTC = startOfTodayUTC();
  const dueCutoffUTC = addDaysUTC(todayStartUTC, REMIND_WITHIN_DAYS);

  const unmapped = new Set();

  for (const page of pages) {
    const pageId = page.id;
    const title = getTitle(page);
    const url = (page.url ?? "(no url)").replace(/[>\])\.,;]+$/g, "");
    const status = getStatus(page);

    const assignees = getAssignees(page);

    for (const a of assignees) {
      if (!notionDiscordMap[a.id]) unmapped.add(a.id);
    }

    // --- A) Assignment change notification ---
    const assigneeKey = assignees.map((a) => a.id).sort().join(",");
    const lastNotifiedKey = getRichTextString(page, P_LAST_NOTIFIED_ASSIGNEES);

    if (assigneeKey !== lastNotifiedKey) {
      // Only notify when it becomes assigned (not when cleared)
      if (assignees.length > 0) {
        const mention = formatAssigneeMentions(assignees);
        const due = getDueStart(page);
        await discordPost(
          WH_ASSIGN,
          `📝 Assigned: **${title}** → ${mention}\nDue: ${due ? ymd(due) : "—"}\n${url}`
        );
      }

      await notionPatchPage(pageId, {
        [P_LAST_NOTIFIED_ASSIGNEES]: {
          rich_text: [{ type: "text", text: { content: assigneeKey } }],
        },
      });
    }

    // --- B) Due soon reminder (upcoming only; excludes overdue) ---
    if (status !== STATUS_DONE) {
      const due = getDueStart(page);
      if (due) {
        const dueYmd = ymd(due);
        const lastDueNotified = getRichTextString(page, P_DUE_REMINDER_SENT_FOR);

        // Compare dates at day precision in UTC
        const dueDateUTC = new Date(Date.UTC(due.getUTCFullYear(), due.getUTCMonth(), due.getUTCDate()));
        const isUpcomingDueSoon =
          dueDateUTC >= todayStartUTC && // exclude overdue
          dueDateUTC <= dueCutoffUTC;

        if (isUpcomingDueSoon && lastDueNotified !== dueYmd) {
          const mention = formatAssigneeMentions(assignees);
          await discordPost(
            WH_DEADLINES,
            `⏰ Due soon (${REMIND_WITHIN_DAYS}d): **${title}** → ${mention}\nDue: ${dueYmd}\n${url}`
          );

          await notionPatchPage(pageId, {
            [P_DUE_REMINDER_SENT_FOR]: {
              rich_text: [{ type: "text", text: { content: dueYmd } }],
            },
          });
        }
      }
    }

    // --- C) Done notification (once) ---
    if (status === STATUS_DONE && !getCheckbox(page, P_DONE_NOTIFIED)) {
      await discordPost(WH_DONE, `✅ Done: **${title}**\n${url}`);
      await notionPatchPage(pageId, {
        [P_DONE_NOTIFIED]: { checkbox: true },
      });
    }
  }

  if (unmapped.size) {
    console.log("Unmapped Notion user IDs (add to .github/notion_discord_map.json):");
    for (const id of unmapped) console.log(`- ${id}`);
  }
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});