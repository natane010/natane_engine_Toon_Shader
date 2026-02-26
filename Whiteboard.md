# v1.3.0 Release Whiteboard

## Leader Work (DONE)
- [x] nav.js bilingual support (v3 - JA/EN data + IS_EN detection + makePath + lang toggle)
- [x] styles.css lang-toggle style
- [x] package.json → 1.3.0
- [x] CHANGELOG.md v1.3.0 entry
- [x] Website/index.html version badge → v1.3.0

## Agent Work (EN Pages)
- [ ] Agent A: en/index.html + en/params/index.html + en/params/basic/(6) + en/params/lighting/(8)
- [ ] Agent B: en/params/effects/(18)
- [ ] Agent C: en/params/environment/(4) + en/params/advanced/(8)
- [ ] Agent D: en/tools/index.html + en/tools/(26)

## EN Page Conversion Rules

### Path adjustments (relative to Website root):
| Page location | styles.css path | nav.js path |
|---|---|---|
| `en/index.html` | `../styles.css` | `../nav.js` |
| `en/params/index.html` | `../../styles.css` | `../../nav.js` |
| `en/params/{cat}/*.html` | `../../../styles.css` | `../../../nav.js` |
| `en/tools/index.html` | `../../styles.css` | `../../nav.js` |
| `en/tools/*.html` | `../../styles.css` | `../../nav.js` |

### HTML changes per page:
1. `<html lang="ja">` → `<html lang="en">`
2. Translate `<title>` to English
3. Translate ALL visible Japanese text to English
4. Keep unchanged: HTML structure, CSS classes, property names (_MainTex etc.), badge types, footer text
5. Adjust CSS/JS relative paths per table above

### Portal pages (params/index.html, tools/index.html):
- These have inline `<script>` using `window.NataneNav`
- nav.js v3 already provides correct EN labels via IS_EN detection
- Translate: static HTML text (h1, p.page-subtitle), inline script string literals (catDesc, etc.)
- The `data.CATEGORIES`, `data.PAGE_ORDER`, `data.TOOL_ORDER`, `data.TOOL_CATEGORIES` are auto-localized

## Release Steps (after all agents complete)
1. Commit all changes
2. `git tag v1.3.0`
3. `git push origin v1.1.5 && git push origin v1.3.0`
4. GitHub Actions auto-deploys
