# Playwright MCP Smoke Test

Run the app first against a disposable smoke-test database so persisted local changes do not affect the seeded baseline.

PowerShell:

```powershell
$env:ConnectionStrings__Boodschap='Data Source=App_Data/smoke-test.db'
Remove-Item .\App_Data\smoke-test.db, .\App_Data\smoke-test.db-shm, .\App_Data\smoke-test.db-wal -ErrorAction SilentlyContinue
dotnet run --launch-profile http
```

For Docker/startup configuration regressions, rerun the same smoke flow once with a raw database path instead of a full SQLite connection string:

```powershell
$env:ConnectionStrings__Boodschap='App_Data/docker-path-smoke.db'
Remove-Item .\App_Data\docker-path-smoke.db, .\App_Data\docker-path-smoke.db-shm, .\App_Data\docker-path-smoke.db-wal -ErrorAction SilentlyContinue
dotnet run --launch-profile http
```

The app listens on `http://localhost:5091`.

## 1. Navigate to the app

```
mcp_playwright_browser_navigate → http://localhost:5091
```

## 2. Verify initial page structure

```
mcp_playwright_browser_snapshot
```

Expected:
- Heading "Kies een lijst en ga door."
- Icon-only "Nieuwe lijst toevoegen" button
- Clicking "Nieuwe lijst toevoegen" opens inputs with placeholders "Titel" and "Beschrijving"
- Tab group "Status van boodschappenlijst" with buttons: Nieuw, Archief
- Visible list cards: Weekboodschappen and Etentje
- Active list cards show an edit icon button with tooltip "Hernoemen" and an Archiveren button

## 3. Test archived tab

```
mcp_playwright_browser_click → button "Archief"
mcp_playwright_browser_snapshot
```

Expected:
- "Archief" button is `[active]`
- Archived list card "Kampeerweekend" is shown
- New list cards are hidden
- Archived list cards show an edit icon button with tooltip "Hernoemen", plus Uit archief halen and Verwijderen buttons

## 4. Open a shopping list

```
mcp_playwright_browser_click → button "Weekboodschappen"
mcp_playwright_browser_snapshot
```

Expected:
- URL ends with `/lists/1`
- "Terug" button is visible
- Heading "Weekboodschappen"
- No list-management buttons are shown in the list view
- The icon-only "Nieuwe boodschap" button appears under the "Boodschappen" heading
- Paragraph: "Gebruik het bewerkicoon om te hernoemen. Sleep om de volgorde te wijzigen."
- No item filter buttons are shown
- Each item row shows the edit icon button before the remove icon button
- 6 list items: Melk, Eieren, Brood, Tomaten, Kaas, Koffie (all unchecked)

## 4a. Test inline item rename

```
mcp_playwright_browser_click → button "Brood bewerken"
mcp_playwright_browser_snapshot
```

Expected:
- Input with placeholder "Boodschap hernoemen" appears inline in the row
- Save and cancel icon buttons are visible on the same line as the input

Rename and save:

```
mcp_playwright_browser_fill_form → input "Boodschap hernoemen" with "Flatbread"
mcp_playwright_browser_click → button "Naam opslaan"
mcp_playwright_browser_snapshot
```

Expected:
- The item row now shows "Flatbread"
- Paragraph returns to "Gebruik het bewerkicoon om te hernoemen. Sleep om de volgorde te wijzigen."

## 5. Test drag and drop reorder

Drag `Koffie` onto `Flatbread`.

```
mcp_playwright_browser_snapshot
mcp_playwright_browser_drag → drag "Koffie" onto "Flatbread"
mcp_playwright_browser_snapshot
```

Expected:
- Item order becomes: Melk, Eieren, Koffie, Flatbread, Tomaten, Kaas
- Paragraph remains "Gebruik het bewerkicoon om te hernoemen. Sleep om de volgorde te wijzigen."

## 6. Mark items as purchased

Click the Melk row once, then the Eieren checkbox.

```
mcp_playwright_browser_click → text "Melk"
mcp_playwright_browser_click → checkbox "Eieren"
```

Expected:
- Melk and Eieren are both marked as purchased
- The row click toggles the same done state change as clicking the checkbox

## 7. Test "New item" quick-add

```
mcp_playwright_browser_click → button "Nieuwe boodschap"
mcp_playwright_browser_snapshot
```

Expected:
- Input with placeholder "Boodschap toevoegen" appears
- "Toevoegen" submit button appears

Type a new item and submit:

```
mcp_playwright_browser_fill_form → input "Boodschap toevoegen" with "Bananas"
mcp_playwright_browser_click → button "Toevoegen"
mcp_playwright_browser_snapshot
```

Expected:
- "Bananas" appears in the list below the existing unchecked items and above the checked items
- In the seeded smoke run after reordering Koffie before Flatbread and then checking off Melk and Eieren, "Bananas" should appear after Kaas and before Melk/Eieren
- Input is cleared

## 8. Test remove

```
mcp_playwright_browser_click → button "Bananas verwijderen"
mcp_playwright_browser_snapshot
```

Expected:
- "Bananas" is no longer in the list

## 9. Return to overview

```
mcp_playwright_browser_click → button "Terug"
mcp_playwright_browser_snapshot
```

Expected:
- URL returns to `/`
- Overview cards are visible again

## 10. Verify cross-session synchronization

Open a second browser page to the same list while the first page remains open.

```
mcp_playwright_browser_navigate → http://localhost:5091/lists/1 (page A)
mcp_playwright_browser_open → http://localhost:5091/lists/1 (page B)
```

Expected:
- Both pages show the same current list state

On page A, add a new item:

```
mcp_playwright_browser_click → button "Nieuwe boodschap" (page A)
mcp_playwright_browser_fill_form → input "Boodschap toevoegen" with "Oranges" (page A)
mcp_playwright_browser_click → button "Toevoegen" (page A)
```

Expected:
- Page A updates immediately
- Page B updates automatically without manual refresh
- "Oranges" appears below unchecked items and above checked items on both pages

On page A, remove the new item:

```
mcp_playwright_browser_click → button "Oranges verwijderen" (page A)
```

Expected:
- "Oranges" disappears on both pages without manual refresh

On page A, reorder an item:

```
mcp_playwright_browser_drag → drag "Koffie" onto "Flatbread" (page A)
```

Expected:
- The reordered item position updates on both pages without manual refresh

## 11. Close browser

```
mcp_playwright_browser_close
```

## 12. Kill the host process

Stop the `dotnet run` process that was started at the beginning.

```
kill_terminal → <terminal-id from step 0>
```

Optional cleanup:

```powershell
Remove-Item .\App_Data\smoke-test.db, .\App_Data\smoke-test.db-shm, .\App_Data\smoke-test.db-wal -ErrorAction SilentlyContinue
```

If cleanup runs immediately after stopping the host and SQLite still has the files open, retry the `Remove-Item` command once after the process has fully exited.
