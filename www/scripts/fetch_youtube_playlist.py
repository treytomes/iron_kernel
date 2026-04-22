#!/usr/bin/env python3

import re
from datetime import datetime
from pathlib import Path

import feedparser


PLAYLIST_ID = "PLsC0KLtToCh1DPg2FbalEoDgaxMMsVHPa"
FEED_URL = f"https://www.youtube.com/feeds/videos.xml?playlist_id={PLAYLIST_ID}"

OUTPUT_DIR = Path("docs/updates/feed")
OUTPUT_DIR.mkdir(parents=True, exist_ok=True)


def slugify(text: str) -> str:
    text = text.lower()
    text = re.sub(r"[^\w\s-]", "", text)
    text = re.sub(r"[\s_-]+", "-", text)
    return text.strip("-")


def entry_path(published: datetime, slug: str) -> Path:
    return OUTPUT_DIR / f"{published:%Y-%m-%d}-youtube-{slug}.md"


def write_entry(entry) -> bool:
    published = datetime(*entry.published_parsed[:6])
    title = entry.title.strip()
    summary = entry.summary.strip().replace("#", "##")
    url = entry.link
    video_id = entry.yt_videoid
    slug = slugify(title)

    path = entry_path(published, slug)
    if path.exists():
        return False

    content = f"""
# Video: {title}

[Watch on YouTube]({url})

<iframe width="560" height="315" src="https://www.youtube.com/embed/{video_id}" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

{summary}

---

```
date: {published:%Y-%m-%d}
type: video
source: youtube
title: "{title}"
url: {url}
video_id: {video_id}
```
"""

    path.write_text(content, encoding="utf-8")
    return True


def main():
    feed = feedparser.parse(FEED_URL)
    created = 0

    for entry in feed.entries:
        if write_entry(entry):
            created += 1

    print(f"YouTube playlist: created {created} new entries")


if __name__ == "__main__":
    main()