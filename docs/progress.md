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
