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
  return text.replace(/```[\s\S]*?```/g, "").replace(/`[^`]*`/g, "");
}

function truncate(text = "", max = 1024) {
  if (!text) return "";
  return text.length > max ? text.slice(0, max - 1) + "…" : text;
}

function extractGithubMentions(text = "") {
  const clean = stripCode(text);
  const re =
    /(^|[^A-Za-z0-9-])@([A-Za-z0-9](?:[A-Za-z0-9-]{0,37}[A-Za-z0-9])?)/g;
  const out = new Set();
  let m;
  while ((m = re.exec(clean)) !== null) out.add(m[2]);
  return [...out];
}

/**
 * Supports both schemas:
 * A) nested: { "users": { "login": { "discord_id": "...", "aliases": [...] } } }
 * B) flat:   { "login": "discord_id" } OR { "login": { "discord_id": "...", "aliases": [...] } }
 */
function buildUserIndex(mapJson) {
  const loginToDiscord = new Map(); // login -> discord_id
  const aliasToLogin = new Map(); // alias -> canonical login

  const addLogin = (login, discordId) => {
    if (!login || !discordId) return;
    loginToDiscord.set(norm(login), String(discordId));
  };

  const addAliases = (canonicalLogin, aliases) => {
    const canon = norm(canonicalLogin);
    for (const a of aliases ?? []) {
      if (!a) continue;
      aliasToLogin.set(norm(a), canon);
    }
  };

  // ---- Schema A (nested) ----
  if (mapJson?.users && typeof mapJson.users === "object") {
    for (const [login, info] of Object.entries(mapJson.users)) {
      if (!info) continue;
      if (typeof info === "string" || typeof info === "number") {
        addLogin(login, info);
      } else if (typeof info === "object") {
        addLogin(login, info.discord_id);
        addAliases(login, info.aliases);
      }
    }
  }
  // ---- Schema B (flat) ----
  else if (mapJson && typeof mapJson === "object") {
    for (const [login, val] of Object.entries(mapJson)) {
      if (login.startsWith("$") || login === "version") continue;

      if (typeof val === "string" || typeof val === "number") {
        addLogin(login, val);
      } else if (val && typeof val === "object") {
        addLogin(login, val.discord_id);
        addAliases(login, val.aliases);
      }
    }
  }

  function resolveDiscordId(login) {
    const n = norm(login);
    if (loginToDiscord.has(n)) return loginToDiscord.get(n);
    const canonical = aliasToLogin.get(n);
    if (canonical && loginToDiscord.has(canonical)) return loginToDiscord.get(canonical);
    return null;
  }

  return { resolveDiscordId };
}

async function postToDiscord({ content = "", embeds = [], userIdsToPing = [] }) {
  const body = {
    content,
    embeds,
    // Only allow pings to explicit user IDs we include here (prevents @everyone/@here)
    allowed_mentions: { users: userIdsToPing }
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

/**
 * Convert common GitHub comment formatting to Discord-friendly markdown.
 * Note: Discord does NOT render HTML; this strips wrappers and downgrades headings.
 */
function githubToDiscord(text = "") {
  let t = text;

  // Convert HTML image tags to raw URLs (still useful as clickable text)
  t = t.replace(/<img[^>]*src="([^"]+)"[^>]*>/gi, "$1");

  // Strip common HTML wrappers
  t = t.replace(/<\/?p[^>]*>/gi, "");
  t = t.replace(/<\/?br\s*\/?>/gi, "\n");

  // Convert heading level 4+ (#### ...) into bold lines (Discord only supports #/##/###)
  t = t.replace(/^\s*####+\s*(.+)$/gm, "**$1**");

  // Optional: compress extra blank lines
  t = t.replace(/\n{3,}/g, "\n\n").trim();

  return t;
}

function extractImageUrls(text = "") {
  const urls = new Set();

  // HTML: <img src="...">
  {
    const re = /<img[^>]*src="([^"]+)"[^>]*>/gi;
    let m;
    while ((m = re.exec(text)) !== null) {
      const u = m[1];
      if (u) urls.add(u);
    }
  }

  // Markdown images: ![alt](url)
  {
    const re = /!\[[^\]]*]\((https?:\/\/[^\s)]+)\)/g;
    let m;
    while ((m = re.exec(text)) !== null) {
      const u = m[1];
      if (u) urls.add(u);
    }
  }

  // Standalone URLs (limit to likely image hosts / image-looking urls)
  {
    const re = /(https?:\/\/[^\s<>()"]+)/g;
    let m;
    while ((m = re.exec(text)) !== null) {
      const u = m[1];
      const lower = u.toLowerCase();
      const looksLikeImage =
        lower.includes("github.com/user-attachments/assets/") ||
        lower.includes("user-images.githubusercontent.com/") ||
        lower.endsWith(".png") ||
        lower.endsWith(".jpg") ||
        lower.endsWith(".jpeg") ||
        lower.endsWith(".gif") ||
        lower.endsWith(".webp");
      if (looksLikeImage) urls.add(u);
    }
  }

  return [...urls];
}

function buildScreenshotEmbeds(urls, jumpUrl, max = 2) {
  return urls.slice(0, max).map((url, i) => ({
    title: `Screenshot ${i + 1}`,
    url: jumpUrl,
    image: { url }
  }));
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

  const formatted = githubToDiscord(issue.body ?? "");
  const embed = {
    title: `#${issue.number} — ${issue.title}`,
    url: issue.html_url,
    description: truncate(formatted, 2048) || "—",
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

  const imgUrls = extractImageUrls(issue.body ?? "");
  const screenshotEmbeds = buildScreenshotEmbeds(imgUrls, issue.html_url, 2);

  await postToDiscord({
    content,
    embeds: [embed, ...screenshotEmbeds],
    userIdsToPing: assigneeDid ? [assigneeDid] : []
  });
}

if (eventName === "issue_comment" && event.action === "created") {
  const issue = event.issue;
  const repo = event.repository;
  const comment = event.comment;

  const commenterLogin = comment.user?.login;

  // Extract @mentions from comment body, then resolve to Discord IDs
  const mentionedLogins = extractGithubMentions(comment.body).filter(
    u => norm(u) !== norm(commenterLogin)
  );
  const mentionedDiscordIds = [...new Set(mentionedLogins.map(resolveDiscordId).filter(Boolean))];

  const mentionPings = mentionedLogins.length
    ? mentionedLogins.map(l => formatDiscordPing(resolveDiscordId(l), l)).join(" ")
    : "—";

  // Ping only the mentioned users (if mapped)
  const content = mentionedDiscordIds.length ? `💬 ${mentionPings}` : `💬 New comment`;

  const formatted = githubToDiscord(comment.body ?? "");
  const embed = {
    title: `#${issue.number} — ${issue.title}`,
    url: comment.html_url, // deep link to the comment
    description: truncate(formatted, 2048) || "—",
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

  const imgUrls = extractImageUrls(comment.body ?? "");
  const screenshotEmbeds = buildScreenshotEmbeds(imgUrls, comment.html_url, 2);

  await postToDiscord({
    content,
    embeds: [embed, ...screenshotEmbeds],
    userIdsToPing: mentionedDiscordIds
  });
}