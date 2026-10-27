# FontFilter Cross-platform Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Deliver shared C# Windows/macOS desktop FontFilter with preserved features and safe legacy import.
**Architecture:** Reuse existing core services in a shared net8.0 library; Avalonia desktop UI and platform font services supply scanning and glyph previews. Independent application directory preserves all original workspace artifacts.
**Tech Stack:** C#, .NET 8, Avalonia 11, Skia, SQLite.
**Spec:** docs/superpowers/specs/2026-10-27-fontfilter-cross-platform-design.md

## Global Constraints
- Preserve original archive and Windows application; no font installation or file copying.
- Intel macOS .app and Windows x64 self-contained packages.
- Preserve AND search, 172 tags, favorites, metadata/catalogue, AI review, samples, theme, six-font comparison and scan pause/cancel.
- Secrets remain memory-only; no automatic API calls.
- Legacy DB backup, WAL-safe import and unique-only cross-platform annotation association.
- State Windows runtime verification limitations explicitly.

## Review Focus
- Duplicate family/style names must not silently reassign user annotations (task 2).
- Cancelled or partially failed scans cannot mark previously known fonts absent (task 3).
- Glyph absence must remain visible, including supplementary Unicode (task 3).
- Busy or malformed imported databases cannot damage current data (task 2).
- UI refresh and scan updates cannot overwrite unsaved manual annotations (task 4).

### Task 1: Shared core and portable persistence
Files: src/FontFilter.Core/*, tests/FontFilter.Tests/*.
Interfaces: FontStore, FontRecord, SettingsStore and existing catalogue/AI signatures remain stable.
- [x] Copy unchanged core and regression tests; test persistence on macOS (expected native Windows binding failure).
- [x] Replace Windows SQLite dependency with portable native binding; isolate App.DataDirectory and theme validation.
- [x] Run core tests including original search/tag/catalogue/AI regressions; commit passing changes.

### Task 2: Safe legacy import
Files: src/FontFilter.Core/LegacyImport.cs, tests/FontFilter.Tests/ImportTests.cs.
Interfaces: LegacyImport.Import(directory, destinationStore, currentFonts) returns report; backup before import; retain unresolved entries.
- [x] Write failing tests for unique association, duplicate protection, preservation of existing tags, malformed sources, WAL-safe snapshot.
- [x] Implement read-only source snapshot via SQLite backup, staged annotation import, safe settings load, unresolved report.
- [x] Run tests proving current data survives failures; commit.

### Task 3: Platform fonts and real glyph previews
Files: src/FontFilter.Desktop/FontScanner.cs, GlyphPreview.cs, tests/FontFilter.Desktop.Tests/*.
Interfaces: FontScanner.ScanAsync and preview lookup; platform-neutral ID based on family/style/source.
- [x] Test scan reconciliation cancel/error rules and preview glyph coverage before new implementation.
- [x] Implement Avalonia system font discovery and glyph rendering without fallback, incremental cache and pause/cancel.
- [x] Run real iMac scan, glyph coverage and scan reconciliation tests; commit.

### Task 4: Desktop workspace and dialogs
Files: src/FontFilter.Desktop/MainWindow.*, Dialogs.*, App.*, ThemeService.cs.
Interfaces: existing shared services, explicit user commands for external requests; UI state for six-font comparison and manual edit preservation.
- [x] Write failing behavior tests for compare cap/dedup, family mode, unsaved edit protection and samples length.
- [x] Implement three-column workspace and search, tags, scan, preview, compare, favorites, metadata, offline catalogue, API review, import, sample and color dialogs.
- [x] Run headless UI smoke and state tests, then launch and inspect macOS UI; commit.

### Task 5: Packages and verification
Files: scripts/publish.sh, README.md, packaging/Info.plist, release/*.
- [x] Publish self-contained osx-x64 and win-x64, bundle official catalogue and icon, verify executable dependencies.
- [x] Run full suite and clean-data app launch; inspect real scanning and screenshots; record verification.
- [x] Fresh whole-project review, fix important findings with regression tests.
- [x] Produce application archives, Chinese usage notes, source archive; leave no claim of Windows runtime verification without evidence.
