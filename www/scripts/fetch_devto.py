#!/usr/bin/env python3

import os
import re
import requests
from datetime import datetime
from pathlib import Path

SERIES_ID = 35708
API_URL = f"https://dev.to/api/articles?collection_id={SERIES_ID}&per_page=100"

OUTPUT_DIR = Path("docs/updates/feed")
OUTPUT_DIR.mkdir(parents=True, exist_ok=True)


def slugify(text: str) -> str:
    text = text.lower()
    text = re.sub(r"[^\w\s-]", "", text)
    text = re.sub(r"[\s_-]+", "-", text)
    return text.strip("-")


def entry_filename(date: datetime, slug: str) -> Path:
    return OUTPUT_DIR / f"{date:%Y-%m-%d}-devto-{slug}.md"


def fetch_series():
    response = requests.get(API_URL)
    response.raise_for_status()
    return response.json()


def write_entry(article):
    published = datetime.fromisoformat(article["published_at"].replace("Z", "+00:00"))
    title = article["title"].strip()
    url = article["url"]
    slug = slugify(title)

    path = entry_filename(published, slug)
    if path.exists():
        return False

    description = article.get("description", "").strip()

    content = f"""
# {title}

{description}

```
date: {published:%Y-%m-%d}
type: devlog
source: dev.to
title: "{title}"
url: {url}
```
"""

    path.write_text(content, encoding="utf-8")
    return True


def main():
    articles = fetch_series()
    created = 0

    for article in articles:
        if write_entry(article):
            created += 1

    print(f"dev.to: created {created} new entries")


if __name__ == "__main__":
    main()
