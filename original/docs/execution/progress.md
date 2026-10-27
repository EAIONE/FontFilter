# Execution ledger — plan: docs/superpowers/plans/2026-10-25-font-filter.md

Ruling: 新建独立目录，无现存仓库；在此初始化feature分支，避免修改键盘屏显项目。
Ruling: 使用.NET 8及Windows内置winsqlite3，不依赖NuGet SQLite包；减少离线安装依赖。
Pre-flight: scanner→store使用FontRecord；search→UI返回FontRecord；metadata→store只更新选定字体介绍和推断标签，人工标签独立保存。

Task 1/2: core indexing, persistence, manual override and keyword tests RED→GREEN; 3000 synthetic query 5.6ms.
Task 3: WPF compiled. Actual integration scan: 1517 families, 7648 unique faces, 0 read errors. Missing glyph and cancellation tests pass.
Task 4: official domain and HTML extraction tests RED→GREEN. UI import requires explicit font confirmation; no automatic whole-site crawler.
Ruling: 官网补充采用官方地址提取/手工摘要并确认关联；自动全站发现不在首版启用，以免错误匹配。代价：需要用户先提供页面。
Ruling: 中文试排用直接GlyphRun避免静默字体替代；复杂文字塑形和OpenType特性不在当前预览实现中，不能替代排版软件校样。
UI validation: native app accessibility tree verified; screenshot/input geometry unavailable, visual QA blocked by desktop capture timeout.

Final review: independent reviewer found metadata UI cross-thread save, incremental source mismatch, response-body timeout, and unguarded family naming. All corrected; regression tests RED→GREEN.
Final: fixed duplicate reuse counts, sans-serif classification, and window close reentry — regressions RED→GREEN. Full suite21/21. Glyph preview rendered by an STA test.
Final: no deferred code-review minors. Screenshot/real scrolling QA remains unverified because desktop capture times out.

Follow-up: comparison now auto-activates with a count/empty hint, batch selection, removal and deduplication; actual two-column glyph control render inspected.
Follow-up: Chinese support persisted in cjk column, six dictionary probes instead of enumerating cmap.Keys, cached flag reused; Chinese-named Chinese fonts first.
Follow-up: 172 tags in 8 categories, searchable picker, background rule migration preserves manual tags. Shared tokenization no longer silently truncates labels or query terms.
Follow-up validation: 28 tests pass; 7648 faces incremental scan 2327ms; independent review findings fixed. Existing Data folder is preserved on publish.

Light/API update: warm white/green three-column workspace, modern control templates, fixed comparison tray, samples editor collapsed. Full/minimum-width renders inspected. API compatible Chat Completions configurable; model is user supplied; key session-only. Source includes font name/face/summary only, no file path or manual notes. No provider calls made without user configuration.
Independent review: AI confirm could discard pending manual edits; reproduced failure and fixed by merging visible editor text. Full suite34/34 including injected HTTP transport and WPF renders. Actual provider interoperability remains unverified without a user key.

Official catalogue: 177/177 Founder list pages,4236 Founder-site records +713 Hanyi families.920 targeted detail checks,710 descriptions,301 official-tag entries,0 remaining targeted matches. Only180char excerpts and metadata; no font binaries/full articles persisted.
Independent review caught global detail-page alias collection including recommendations. Scoped to current product before fontDisplay, rebuilt177-page identity catalogue, parser regression and real fz:194 match pass.
Full suite38/38; parser tests passed. SQLite VACUUM INTO backup saved before real import;4268 blank descriptions filled,1033 matched styles without descriptions skipped. Verified manualTagChanges=0 and existingSummaryChanges=0. Publication succeeded. Paid API calls=0.
