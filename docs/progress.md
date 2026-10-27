# Execution ledger
Spec: docs/design.md; plan: docs/plan.md
User approved cross-platform direction and implementation (做吧).
Isolated project: FontFilter-CrossPlatform, independent git repository on codex/cross-platform; original archive untouched.
Execution: inline; final independent code review.
Preflight: shared core -> migration -> desktop services/UI -> packages. Preserve existing public core signatures.
Ruling: deliver local self-contained builds without Apple notarization or Windows signing; no signing credentials supplied.
