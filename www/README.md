# App Site

## Bootstrapping

```bash
python3 -m venv .venv
source .venv/bin/activate
pip install zensical
```

# Creating the site

```bash
zensical new .
```

# Updating feeds

```bash
python scripts/fetch_devto.py
```

# Local Test

```bash
zensical serve
```

# Build

```bash
zensical build
```
