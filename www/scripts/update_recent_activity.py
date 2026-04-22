#!/usr/bin/env python3

import re
from pathlib import Path
from datetime import datetime

FEED_DIR = Path("docs/updates/feed")
ACTIVITY_PAGE = Path("docs/updates/README.md")

MAX_ITEMS = 6

FRONT_MATTER_RE = re.compile(r"^```$(.*?)^```$", re.S | re.M)

SECTION_START = "<!-- RECENT_ACTIVITY_START -->"
SECTION_END = "<!-- RECENT_ACTIVITY_END -->"


def parse_front_matter(text: str) -> dict:
    match = FRONT_MATTER_RE.search(text)
    if not match:
        return {}

    data = {}
    for line in match.group(1).splitlines():
        if ":" in line:
            k, v = line.split(":", 1)
            data[k.strip()] = v.strip().strip('"')
    return data


def collect_feed_items():
    items = []

    for path in FEED_DIR.glob("*.md"):
        text = path.read_text(encoding="utf-8")
        meta = parse_front_matter(text)
        if not meta:
            continue

        date = datetime.fromisoformat(meta["date"])
        items.append({
            "date": date,
            "type": meta.get("type"),
            "title": meta.get("title", path.stem),
            "url": meta.get("url"),
        })

    items.sort(key=lambda x: x["date"], reverse=True)
    return items[:MAX_ITEMS]


def render_recent_activity(items):
    lines = ["## Recent Activity", ""]

    for item in items:
        icon = {
            "devlog": "📘",
            "video": "🎥",
            "commit": "🧠",
            "release": "🚀",
        }.get(item["type"], "•")
        
        line = f"- {icon} **{item['title']}**  \n  → {item['url']}"
        print(line)
        lines.append(line)

    return "\n".join(lines)


def update_activity_page():
    content = ACTIVITY_PAGE.read_text(encoding="utf-8")

    items = collect_feed_items()
    rendered = render_recent_activity(items)

    block = f"{SECTION_START}\n{rendered}\n{SECTION_END}"

    updated = re.sub(
        f"{SECTION_START}.*?{SECTION_END}",
        block,
        content,
        flags=re.S,
    )

    ACTIVITY_PAGE.write_text(updated, encoding="utf-8")


def main():
    update_activity_page()
    print("Recent Activity section updated")


if __name__ == "__main__":
    main()