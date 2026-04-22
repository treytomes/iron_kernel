#!/usr/bin/env python3

import os
import re
import requests
from datetime import datetime
from pathlib import Path
from typing import Optional

# -------------------------------------------------
# Configuration
# -------------------------------------------------

OWNER = "treytomes"
REPO = "iron_kernel"
BRANCH = "main"

OUTPUT_DIR = Path("docs/updates/feed")
OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

# Feature flag:
#   True  -> only releases + filtered commits
#   False -> include all commits
FILTER_COMMITS = True

# Commit filters (used only if FILTER_COMMITS = True)
ALLOWED_PREFIXES = (
    "arch:",
    "ui:",
    "kernel:",
    "design:",
    "docs:",
    "chore:",
)

MERGE_REGEX = re.compile(r"^merge pull request", re.IGNORECASE)

# Pagination
PER_PAGE = 100
MAX_PAGES = 10  # hard stop to avoid runaway CI

# Optional time bounding
SINCE: Optional[str] = None   # e.g. "2026-01-01T00:00:00Z"
UNTIL: Optional[str] = None

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

def slugify(text: str) -> str:
    text = text.lower()
    text = re.sub(r"[^\w\s-]", "", text)
    text = re.sub(r"[\s_-]+", "-", text)
    return text.strip("-")


def write_if_new(path: Path, content: str) -> bool:
    if path.exists():
        return False
    path.write_text(content, encoding="utf-8")
    return True


def iso_to_date(iso: str) -> datetime:
    return datetime.fromisoformat(iso.replace("Z", "+00:00"))


# -------------------------------------------------
# Releases
# -------------------------------------------------

def fetch_releases():
    url = f"{GITHUB_API}/repos/{OWNER}/{REPO}/releases"
    resp = requests.get(url, headers=HEADERS)
    resp.raise_for_status()
    return resp.json()


def generate_release_entries() -> int:
    created = 0

    for release in fetch_releases():
        published = iso_to_date(release["published_at"])
        title = release["name"] or release["tag_name"]
        slug = slugify(title)
        url = release["html_url"]

        path = OUTPUT_DIR / f"{published:%Y-%m-%d}-github-release-{slug}.md"

        body = (release.get("body") or "").strip()

        content = f"""```
date: {published:%Y-%m-%d}
type: release
source: github
title: "{title}"
url: {url}
```

## Release: {title}

{body}

[View release on GitHub]({url})
"""

        if write_if_new(path, content):
            created += 1

    return created


# -------------------------------------------------
# Commits
# -------------------------------------------------

def commit_allowed(message: str, is_merge: bool) -> bool:
    if not FILTER_COMMITS:
        return True
    if is_merge:
        return True
    return message.lower().startswith(ALLOWED_PREFIXES)


def fetch_commits_page(page: int):
    params = {
        "sha": BRANCH,
        "per_page": PER_PAGE,
        "page": page,
    }
    if SINCE:
        params["since"] = SINCE
    if UNTIL:
        params["until"] = UNTIL

    url = f"{GITHUB_API}/repos/{OWNER}/{REPO}/commits"
    resp = requests.get(url, headers=HEADERS, params=params)
    resp.raise_for_status()
    return resp.json()


def generate_commit_entries() -> int:
    created = 0

    for page in range(1, MAX_PAGES + 1):
        commits = fetch_commits_page(page)
        if not commits:
            break

        for commit in commits:
            message = commit["commit"]["message"].split("\n", 1)[0]
            is_merge = len(commit.get("parents", [])) > 1

            if not commit_allowed(message, is_merge):
                continue

            sha = commit["sha"][:7]
            url = commit["html_url"]
            date = iso_to_date(commit["commit"]["committer"]["date"])

            slug = slugify(message)[:40]
            path = OUTPUT_DIR / f"{date:%Y-%m-%d}-github-commit-{sha}.md"

            content = f"""---
date: {date:%Y-%m-%d}
type: commit
source: github
title: "{message}"
url: {url}
sha: {sha}
---

## Commit: {message}

- Commit: `{sha}`
- Date: {date:%Y-%m-%d}

[View commit on GitHub]({url})
"""

            if write_if_new(path, content):
                created += 1
            else:
                # We’ve hit already-known history; stop early
                return created

    return created


# -------------------------------------------------
# Main
# -------------------------------------------------

def main():
    releases = generate_release_entries()
    commits = generate_commit_entries()

    print(f"GitHub releases: {releases} new entries")
    print(f"GitHub commits:  {commits} new entries")
    print(f"Commit filtering: {'ON' if FILTER_COMMITS else 'OFF'}")


if __name__ == "__main__":
    main()