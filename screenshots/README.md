# Android and web screenshot comparison

This directory contains matching Android and web captures for visual parity checks.

## Capture baseline

- Android device: OPPO CPH2747 (`3B15B800GJ300000`)
- Android physical screen: `1272x2772`
- Android density override: `476 dpi`
- Android app area: `1272x2583` after the `141 px` status bar and `48 px` navigation bar
- Approximate Android app viewport: `450x868` logical pixels
- Web viewport and normalized PNG size: `450x868` CSS pixels
- Web device pixel ratio: `1`
- Web emulation scale: `72%`
- VS Code webview capture: resize to `451x868`, then `450x868` after each navigation so the webview recalculates its scale before capture
- Web screenshots must be taken AFTER the resize.
- Android package: `nl.andregeuze.boodschap.debug`
- Local web host: `http://127.0.0.1:5091`

The Android captures include the system status and navigation bars. Web captures exclude the extra VS Code browser canvas and are normalized to the Android app's logical viewport. Compare Android application content between the system bars with the full web image.

## Naming convention

Store each state under the same lowercase kebab-case filename in both platform folders:

```text
screenshots/android/<screen-or-state>.png
screenshots/web/<screen-or-state>.png
```

Use a screen name for a default view, such as `overview.png`. Append the interaction state for dialogs or variants, such as `overview-new-list.png` or `shopping-list-detail-edit-item.png`.

Keep these conditions equal between a pair:

- Same signed-in user and persisted data
- Same logical screen and interaction state
- Same selected list, item, filter, and archive state
- Top of the page unless the filename identifies a scrolled state
- Web viewport fixed at `450x868`

Do not overwrite a baseline with different fixture data. Either restore the shared data first or add a descriptive state suffix.

## Current inventory

| Capture | Android | Web | Parity note |
| --- | --- | --- | --- |
| `login.png` | Complete | Complete | Shared card hierarchy, localized content, and closed-registration state |
| `overview.png` | Complete | Complete | Shared workflow |
| `shopping-list-detail.png` | Complete | Complete | Shared workflow |
| `account.png` | Complete | Complete | Shared workflow with password management, administrator account creation, and version status |

## Capture backlog

- [ ] `overview-new-list.png`
- [ ] `overview-edit-list.png`
- [ ] `overview-archived.png`
- [ ] `shopping-list-detail-add-item.png`
- [ ] `shopping-list-detail-edit-item.png`
- [ ] `shopping-list-detail-purchased-item.png`
- [ ] `shopping-list-detail-dragging-item.png`
- [ ] `menu-open.png`
- [ ] Empty, loading, validation-error, and network-error states where both clients expose them

When a workflow exists on only one platform, still use the same filename in both folders when possible. Capture the nearest counterpart and document the functional difference in the inventory table.