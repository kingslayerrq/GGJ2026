import fs from "node:fs";

const webhookUrl = process.env.DISCORD_WEBHOOK_URL;
if (!webhookUrl) throw new Error("Missing DISCORD_WEBHOOK_URL secret.");

function loadJson(path) {
  return JSON.parse(fs.readFileSync(path, "utf8"));
}

function norm(s) {
  return (s ?? "").toLowerCase();
}

function stripCode(text = "") {
  return text
    .replace(/```[\s\S]*?```/g, "")
    .replace(/`[^`]*`/g, "");
}

function truncate(text = "", max = 1024) {
  if (!text) return "";
  return text.length > max ? text.slice(0, max - 1) + "…" : text;
}

function extractGithubMentions(text = "") {
  const clean = stripCode(text);
  const re = /(^|[^A-Za-z0-9-])@([A-Za-z0-9](?:[A-Za-z0-9-]{0,37}[A-Za-z0-9])?)/g;
  const out = new Set();
  let m;
  while ((m = re.exec(clean)) !== null) out.add(m[2]);
  return [...out];
}

function buildUserIndex(mapJson) {
  const users = mapJson?.users ?? {};

  // login -> discord_id
  const loginToDiscord = new Map();
  // alias -> canonical login
  const aliasToLogin = new Map();

  for (const [login, info] of Object.entries(users)) {
    if (!info?.discord_id) continue;
    loginToDiscord.set(norm(login), String(info.discord_id));
    for (const a of info.aliases ?? []) {
      aliasToLogin.set(norm(a), norm(login));
    }
  }

  function resolveDiscordId(login) {
    const n = norm(login);
    if (loginToDiscord.has(n)) return loginToDiscord.get(n);
    if (aliasToLogin.has(n)) return loginToDiscord.get(aliasToLogin.get(n));
    return null;
  }

  return { resolveDiscordId };
}

async function postToDiscord({ content, embeds, userIdsToPing }) {
  // Webhook + allowed_mentions is the safest way to ensure ONLY intended users get pinged. :contentReference[oaicite:4]{index=4}
  const body = {
    content,
    embeds,
    allowed_mentions: { users: userIdsToPing } // prevents @everyone/@here unless you explicitly add them
  };

  const res = await fetch(webhookUrl, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body)
  });

  if (!res.ok) {
    const txt = await res.text().catch(() => "");
    throw new Error(`Discord webhook failed: ${res.status} ${res.statusText} ${txt}`);
  }
}

function formatDiscordPing(discordId, fallbackGhLogin) {
  return discordId ? `<@${discordId}>` : `@${fallbackGhLogin}`;
}

function issueType(issue) {
  // In issue_comment events, PR conversation comments come through with issue.pull_request set.
  return issue?.pull_request ? "Pull Request" : "Issue";
}

function labelsString(issue) {
  const labels = issue?.labels ?? [];
  const names = labels.map(l => (typeof l === "string" ? l : l?.name)).filter(Boolean);
  return names.length ? names.join(", ") : "—";
}

function assigneesString(issue, resolver) {
  const assignees = issue?.assignees ?? [];
  if (!assignees.length) return "—";
  const parts = assignees.map(a => {
    const login = a?.login;
    const did = resolver(login);
    return formatDiscordPing(did, login);
  });
  return parts.join(" ");
}

const event = loadJson(process.env.GITHUB_EVENT_PATH);
const eventName = process.env.GITHUB_EVENT_NAME;

const mapJson = loadJson(".github/discord-map.json");
const { resolveDiscordId } = buildUserIndex(mapJson);

if (eventName === "issues" && event.action === "assigned") {
  const issue = event.issue;
  const repo = event.repository;

  const assigneeLogin = event.assignee?.login;
  const assignerLogin = event.sender?.login;

  const assigneeDid = resolveDiscordId(assigneeLogin);
  const assignerDid = resolveDiscordId(assignerLogin);

  const assigneePing = formatDiscordPing(assigneeDid, assigneeLogin);
  const assignerPing = formatDiscordPing(assignerDid, assignerLogin);

  const content = `📌 ${assigneePing} — assigned`;

  const embed = {
    title: `#${issue.number} — ${issue.title}`,
    url: issue.html_url,
    description: truncate(issue.body ?? "", 2048) || "—",
    color: 0x57F287, // green
    author: {
      name: `Assigned by ${assignerLogin ?? "unknown"}`,
      icon_url: event.sender?.avatar_url
    },
    thumbnail: {
      url: event.assignee?.avatar_url
    },
    fields: [
      { name: "Repository", value: repo?.full_name ?? "—", inline: true },
      { name: "Type", value: issueType(issue), inline: true },
      { name: "State", value: issue.state ?? "—", inline: true },

      { name: "Assignee", value: assigneePing, inline: true },
      { name: "Assigner", value: assignerPing, inline: true },
      { name: "Author", value: issue.user?.login ?? "—", inline: true },

      { name: "Labels", value: truncate(labelsString(issue), 1024), inline: false },
      { name: "Milestone", value: issue.milestone?.title ?? "—", inline: true },
      { name: "Assignees (all)", value: truncate(assigneesString(issue, resolveDiscordId), 1024), inline: false }
    ],
    timestamp: issue.updated_at ?? new Date().toISOString(),
    footer: { text: "GitHub → Discord" }
  };

  await postToDiscord({
    content,
    embeds: [embed],
    userIdsToPing: assigneeDid ? [assigneeDid] : []
  });
}

if (eventName === "issue_comment" && event.action === "created") {
  const issue = event.issue;
  const repo = event.repository;
  const comment = event.comment;

  const commenterLogin = comment.user?.login;

  // Extract @mentions from comment body, then resolve to Discord IDs
  const mentionedLogins = extractGithubMentions(comment.body).filter(u => norm(u) !== norm(commenterLogin));
  const mentionedDiscordIds = [...new Set(mentionedLogins.map(resolveDiscordId).filter(Boolean))];

  const mentionPings = mentionedLogins.length
    ? mentionedLogins.map(l => formatDiscordPing(resolveDiscordId(l), l)).join(" ")
    : "—";

  // Ping only the mentioned users (if mapped)
  const content = mentionedDiscordIds.length ? `💬 ${mentionPings}` : `💬 New comment`;

  const embed = {
    title: `#${issue.number} — ${issue.title}`,
    url: comment.html_url, // deep link to the comment
    description: truncate(comment.body ?? "", 2048) || "—",
    color: 0x5865F2, // blurple
    author: {
      name: `Comment by ${commenterLogin ?? "unknown"}`,
      icon_url: comment.user?.avatar_url
    },
    fields: [
      { name: "Repository", value: repo?.full_name ?? "—", inline: true },
      { name: "Type", value: issueType(issue), inline: true },
      { name: "State", value: issue.state ?? "—", inline: true },

      { name: "Mentions", value: truncate(mentionPings, 1024), inline: false },
      { name: "Assignees", value: truncate(assigneesString(issue, resolveDiscordId), 1024), inline: false },
      { name: "Labels", value: truncate(labelsString(issue), 1024), inline: false }
    ],
    timestamp: comment.created_at ?? new Date().toISOString(),
    footer: { text: "GitHub → Discord" }
  };

  await postToDiscord({
    content,
    embeds: [embed],
    userIdsToPing: mentionedDiscordIds
  });
}