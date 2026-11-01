# In-App Updates Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Add silent download and install-on-normal-exit updates to the existing Windows and Intel Mac portable application.
**Architecture:** A signed update feed and verified ZIP downloader in Core, an Avalonia update UI/coordinator, and a separate updater executable with constrained swap/rollback. GitHub Releases remains the release origin; HTTPS proxy fallback is configurable.
**Tech Stack:** .NET 8, Avalonia 11.3.22, HttpClient, System.Security.Cryptography, System.IO.Compression.
**Spec:** docs/superpowers/specs/2026-11-01-in-app-updates-design.md

## Global Constraints
- Windows x64 and Intel macOS 12+; preserve existing .app and folder distribution.
- Startup automatically checks/downloads; install only after normal user exit. No prompt, forced restart, or interruption.
- All update metadata authenticated and package bytes verified before execution.
- Preserve font data, settings, tags and favorites; no forced application termination or privilege escalation.
- Use version 1.1.0 for the bootstrap; no remote publish without explicit user authorization.

## Review Focus
- Mirror can return an HTML error, truncated archive or modified metadata: reject/fallback without closing the app.
- Archive paths may escape staging or contain symbolic links: reject before installing.
- Installation path may contain Chinese text/spaces or be read-only: preserve quoting and produce a useful failure.
- Application may be scanning or refuse closure: save/exit normally and time out safely.
- Program replacement/relaunch can fail mid-operation: recover original files and keep user data intact.

### Task 1: Trusted feed, selection and download
**Files:** Core/Updates/UpdateManifest.cs, UpdateClient.cs, UpdateTrust.cs; tests/FontFilter.Tests/UpdateTests.cs; SettingsStore.cs.
- [x] Add failing tests for signed metadata, strict semantic version comparison, target platform, URI/asset validation, corrupt bytes, proxy fallback and cancellation.
- [x] Implement typed manifest/assets, embedded verifier and configurable HTTPS proxy/source routes with bounded network timeouts and downloads.
- [x] Verify tests pass, with tampered signatures/length/hash rejected and no installer action on network failures.

### Task 2: Staging and updater transaction
**Files:** Core/Updates/UpdateStaging.cs, UpdateTransaction.cs; src/FontFilter.Updater/; updater integration tests.
- [x] Add failing tests for traversal, symlinks, ZIP limits/layout, Unicode paths, process wait timeout, permission failures and rollback.
- [x] Implement constrained staging and transaction operations; publish a helper independent of the currently running binary and preserve executable modes on Mac.
- [x] Run isolated Windows replacement/relaunch tests and verify unchanged user database/settings hashes.

### Task 3: Application experience
**Files:** Desktop/MainWindow.Updates.cs, MainWindow.cs, DesktopApp.cs, Program.cs; update UI verification.
- [x] Add verification for startup quietness, silent staging, manual check, cancellation and gating installation until verified download and normal user closure.
- [x] Add quiet status feedback, manual check and startup scheduling disabled during automated verification; integrate normal save/close and independent helper handoff.
- [x] Verify latest/no-update/offline/tampered/download-cancelled states do not alter current application or data.

### Task 4: Reproducible releases
**Files:** version properties, packaging/Info.plist, scripts/publish.sh, publish-windows.ps1, release signing script, README and verification docs.
- [x] Set consistent 1.1.0 version, create local signing key and embed its public counterpart, excluding private key from all archives.
- [x] Extend packaging with helper and signed metadata for both platforms; build archives locally and back up previous releases.
- [x] Run core tests, Windows installed-package UI/helper tests, ZIP CRC and SHA-256 checks; cross-build Mac and inspect signature digests/permissions, explicitly recording native Mac testing still required.
- [x] Review the completed change before requesting any remote publishing action.


Execution ledger: 69 core/regression tests pass. Verified actual Windows normal-close -> independent helper -> signed 1.1.1 fixture install -> next-launch tag verification; portable settings and user settings retained. Reviewer identified startup races; resolved with handoff file lock, reopen request, and loaded Desktop-vs-disk version re-exec. Native Mac installation still requires Mac host. No remote push/upload performed.
