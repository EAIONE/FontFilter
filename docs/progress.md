# Execution ledger
Spec: docs/design.md; plan: docs/plan.md
User approved cross-platform direction and implementation (做吧).
Isolated project: FontFilter-CrossPlatform, independent git repository on codex/cross-platform; original archive untouched.
Execution: inline; final independent code review.
Preflight: shared core -> migration -> desktop services/UI -> packages. Preserve existing public core signatures.
Ruling: deliver local self-contained builds without Apple notarization or Windows signing; no signing credentials supplied.

Task 1: complete — core regression 32/32 passed.
Task 2: complete — import regression 35/35 passed (WAL source, unique match, ambiguity, corruption).
Task 3: complete — native Skia enumerates exact system styles; 4,278 styles, full incremental reuse. One unreadable GB18030 Bitmap recorded without deleting existing fonts.
Ruling: use native Skia typefaces for both discovery and preview, instead of Avalonia font fallback matching — synthetic/cached variants and unsupported weight matching changed identity; cost: custom preview drawing code to maintain.
Task 4: complete — three-column UI and dialogs, real interaction checks passed.
Final review: fixed tab detach disposal — rendered-pixel assertion RED (blank after tab switch) -> GREEN; core suite 41/41.
Final review: fixed sample draft loss — refresh draft assertion RED -> GREEN; retained editor/text/name across refresh; core suite 41/41.
Ruling: let shared native SKTypeface wrappers follow managed reference lifetime rather than dispose on visual detach — tab changes are temporary and wrappers may be shared; native draw operations still dispose their owned SKPaint. Cost: reclamation follows GC.

Final: actual archived user database exposed macOS system SQLite read-only WAL behavior without sidecar files. Added a closed-WAL-with-sidecars-removed regression and watched it fail; bundle portable SQLite instead of relying on OS builds. Native backup API still performs coherent WAL-aware snapshots; source is opened read-only.

Task 5: complete — self-contained osx-x64 and win-x64 packages built; final Mac ZIP extracted, strict signature checked, full app verification exited 0; final core suite 43/43. Windows PE and native SQLite DLL verified x86-64; Windows runtime unavailable.
Final: closed offline WAL regression RED -> GREEN with bundled SQLite 3.53.3; actual legacy import linked 1441 and retained 2827 unresolved records.
Ruling: bundle the same SQLite implementation on both platforms to avoid OS library read-only WAL differences; cost: an additional native runtime dependency, included in both packages.
Ruling: sign and archive in a local temporary staging directory because synced Documents adds FinderInfo; cost: temporary staging remains for inspection until OS cleanup.

## 0.2.1 user interface follow-up
User requested category/temperament/usage ordering, independent color roles, tag spacing/right-aligned action, rounded colored scrollbars, full-width comparison rows, wheel adjustments and reveal-local-font button.
Core regression: 48/48 passed; native UI verifies role switching/save/cancel, readable light primary, sorted groups, actual thumb width/color/roundness, full-width comparison and routed wheel events.
Mac CoreText located the exact installed ALIBABA-PUHUITI-REGULAR.OTF by PostScript name. Windows file lookup uses registry/font directories plus font fingerprint/TTC matching; native file candidates are deterministically disposed. Shared system typefaces remain under managed lifetime as previously ruled to avoid disposing wrappers used by previews.
Independent review found light primary could hide tags: reproduced with #FFFFFF, darkened readable ink, UI check green. New file lookup review fixed owned file-typeface and CoreText library-handle lifetime.
Both self-contained packages updated. Final Mac ZIP extracted and signature checked; packaged UI/scan verification exited 0. User data was backed up under ignored backups/ after normal app closure, and installed app updated/reopened with 22pt and 1.3 line spacing retained.

## 0.2.2 live theme
User requested gradient color bars and immediate application preview. Implemented decorative gradient tracks beneath unchanged native Sliders, channel labels, independent preview settings, committed-save guard and cancel/X rollback. Native template track overlay was reproduced and hidden at the correct nested template scope. Routed pointer drag verified, not only programmatic values.
Verification: old live-preview test RED -> GREEN; old-track-overlay test RED -> GREEN; core 48/48; final extracted package strict signature + complete --verify exit 0. Independent review found no important defect. Current user palette preserved and data backed up before install.

0.2.3: replace theme role dropdown with clickable swatch buttons, tick/border active state and editing label. Role button events, no ComboBox, palette draft preservation and full tile hit area verified. Visual QA found initial buttons shrank to labels; added failing width assertion, explicit stretch fixed it and final packaged UI suite passed. Core 48/48. Independent review found no important defect. User palette saved and data backed up before installed update.

0.2.4: implement five visible palette roles with backward-compatible stored color mapping, exact scrollbar color, theme-derived tag ink, independent canvas background, persisted right-panel collapse preserving manual drafts, aligned brand block, and globally softened field/expander/checkbox borders. Core 49/49; full native and final package verification passed. Reviewer prompted a dark-canvas checkbox check: observed RED, added context-aware checkbox text styles, GREEN. Original user data backed up before upgrade; colors kept.

2026-10-28: 0.2.5 adds compact hover browsing, comparison header hiding, compact details toggle, favorites scope notice, copyable font names, A5 book page preview with live/persisted margin/type controls and pagination. Both platforms packaged, Mac installed after data backup.
