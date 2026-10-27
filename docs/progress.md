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
