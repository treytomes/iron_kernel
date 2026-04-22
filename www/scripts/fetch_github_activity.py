#!/usr/bin/env python3

import os
import re
import requests
from collections import defaultdict
from datetime import datetime
from pathlib import Path

# -------------------------------------------------
# Configuration
# -------------------------------------------------

OWNER = "treytomes"
REPO = "iron_kernel"
BRANCH = "main"

OUTPUT_DIR = Path("docs/updates/feed")
OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

FILTER_COMMITS = True

ALLOWED_PREFIXES = (
    "arch:",
    "ui:",
    "kernel:",
    "design:",
    "docs:",
    "chore:",
)

MERGE_REGEX = re.compile(r"^merge pull request", re.IGNORECASE)

PER_PAGE = 100
MAX_PAGES = 10

GITHUB_API = "https://api.github.com"
TOKEN = os.getenv("GITHUB_TOKEN")

HEADERS = {
    "Accept": "application/vnd.github+json",
}
if TOKEN:
    HEADERS["Authorization"] = f"Bearer {TOKEN}"

# -------------------------------------------------
# Helpers
# -------------------------------------------------

def iso_to_dt(iso: str) -> datetime:
    return datetime.fromisoformat(iso.replace("Z", "+00:00"))


def month_key(dt: datetime) -> str:
    return f"{dt.year:04d}-{dt.month:02d}"


def commit_allowed(message: str, is_merge: bool) -> bool:
    if not FILTER_COMMITS:
        return True
    if is_merge:
        return True
    return message.lower().startswith(ALLOWED_PREFIXES)


# -------------------------------------------------
# Fetch commits
# -------------------------------------------------

def fetch_commits():
    commits = []
    for page in range(1, MAX_PAGES + 1):
        resp = requests.get(
            f"{GITHUB_API}/repos/{OWNER}/{REPO}/commits",
            headers=HEADERS,
            params={
                "sha": BRANCH,
                "per_page": PER_PAGE,
                "page": page,
            },
        )
        resp.raise_for_status()
        batch = resp.json()
        if not batch:
            break
        commits.extend(batch)
    return commits


# -------------------------------------------------
# Generate monthly files
# -------------------------------------------------

def generate_monthly_commit_tables(commits):
    grouped = defaultdict(list)

    for c in commits:
        msg = c["commit"]["message"].split("\n", 1)[0]
        is_merge = len(c.get("parents", [])) > 1

        if not commit_allowed(msg, is_merge):
            continue

        dt = iso_to_dt(c["commit"]["committer"]["date"])
        grouped[month_key(dt)].append({
            "date": dt.date().isoformat(),
            "sha": c["sha"][:7],
            "url": c["html_url"],
            "message": msg,
        })

    for month, entries in grouped.items():
        path = OUTPUT_DIR / f"{month}-github-commits.md"

        entries.sort(key=lambda e: e["date"], reverse=True)

        lines = [
            f"# GitHub Activity — {month}",
            "",
            "| Date | Commit | Message |",
            "|------|--------|---------|",
        ]

        for e in entries:
            lines.append(
                f"| {e['date']} | [`{e['sha']}`]({e['url']}) | {e['message']} |"
            )

        content = "\n".join(lines) + "\n"
        path.write_text(content, encoding="utf-8")


# -------------------------------------------------
# Main
# -------------------------------------------------

def main():
    commits = fetch_commits()
    generate_monthly_commit_tables(commits)
    print("GitHub monthly commit feed generated")


if __name__ == "__main__":
    main()