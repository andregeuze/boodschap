# Playwright MCP Smoke Test

Run the app first against a disposable smoke-test database so persisted local changes do not affect the seeded baseline.

PowerShell:

```powershell
$env:ConnectionStrings__Boodschap='Data Source=App_Data/smoke-test.db'
Remove-Item .\App_Data\smoke-test.db, .\App_Data\smoke-test.db-shm, .\App_Data\smoke-test.db-wal -ErrorAction SilentlyContinue
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
- Heading "Pick a list and keep moving."
- "New list" button
- Tab group "Shopping list status" with buttons: New, Archived
- Visible list cards: Weekly groceries and Dinner party

## 3. Test archived tab

```
mcp_playwright_browser_click → button "Archived"
mcp_playwright_browser_snapshot
```

Expected:
- "Archived" button is `[active]`
- Archived list card "Camping weekend" is shown
- New list cards are hidden

## 4. Open a shopping list

```
mcp_playwright_browser_click → button "Weekly groceries"
mcp_playwright_browser_snapshot
```

Expected:
- URL ends with `/lists/1`
- "Back" button is visible
- Heading "Weekly groceries"
- "New item" button
- "Drag to reorder" paragraph
- FilterBar group "Filter groceries" with buttons: All, Needed, Purchased
- 6 list items: Milk, Eggs, Bread, Tomatoes, Cheese, Coffee (all unchecked)

## 5. Mark items as purchased

Click the Milk checkbox, then the Eggs checkbox.

```
mcp_playwright_browser_click → checkbox "Milk"
mcp_playwright_browser_click → checkbox "Eggs"
```

## 6. Test "Needed" filter

```
mcp_playwright_browser_click → button "Needed"
mcp_playwright_browser_snapshot
```

Expected:
- "Needed" button is `[active]`
- Only unchecked items shown: Bread, Tomatoes, Cheese, Coffee
- "Drag to reorder" hint is hidden

## 7. Test "Purchased" filter

```
mcp_playwright_browser_click → button "Purchased"
mcp_playwright_browser_snapshot
```

Expected:
- "Purchased" button is `[active]`
- Only checked items shown: Milk `[checked]`, Eggs `[checked]`

## 8. Test "All" filter

```
mcp_playwright_browser_click → button "All"
mcp_playwright_browser_snapshot
```

Expected:
- "All" button is `[active]`
- All 6 items shown
- "Drag to reorder" paragraph visible

## 9. Test "New item" quick-add

```
mcp_playwright_browser_click → button "New item"
mcp_playwright_browser_snapshot
```

Expected:
- Input with placeholder "Add grocery item" appears
- "Add" submit button appears

Type a new item and submit:

```
mcp_playwright_browser_fill_form → input "Add grocery item" with "Bananas"
mcp_playwright_browser_click → button "Add"
mcp_playwright_browser_snapshot
```

Expected:
- "Bananas" appears in the list below the existing unchecked items and above the checked items
- In the seeded smoke run after checking off Milk and Eggs, "Bananas" should appear after Coffee and before Milk/Eggs
- Input is cleared

## 10. Test remove

```
mcp_playwright_browser_click → button "Remove" (next to "Bananas")
mcp_playwright_browser_snapshot
```

Expected:
- "Bananas" is no longer in the list

## 11. Return to overview

```
mcp_playwright_browser_click → button "Back"
mcp_playwright_browser_snapshot
```

Expected:
- URL returns to `/`
- Overview cards are visible again

## 12. Verify cross-session synchronization

Open a second browser page to the same list while the first page remains open.

```
mcp_playwright_browser_navigate → http://localhost:5091/lists/1 (page A)
mcp_playwright_browser_open → http://localhost:5091/lists/1 (page B)
```

Expected:
- Both pages show the same current list state

On page A, add a new item:

```
mcp_playwright_browser_click → button "New item" (page A)
mcp_playwright_browser_fill_form → input "Add grocery item" with "Oranges" (page A)
mcp_playwright_browser_click → button "Add" (page A)
```

Expected:
- Page A updates immediately
- Page B updates automatically without manual refresh
- "Oranges" appears below unchecked items and above checked items on both pages

On page A, remove the new item:

```
mcp_playwright_browser_click → button "Remove" (next to "Oranges") (page A)
```

Expected:
- "Oranges" disappears on both pages without manual refresh

On page A, reorder an item:

```
mcp_playwright_browser_drag → drag "Coffee" onto "Bread" (page A)
```

Expected:
- The reordered item position updates on both pages without manual refresh

## 13. Close browser

```
mcp_playwright_browser_close
```

## 14. Kill the host process

Stop the `dotnet run` process that was started at the beginning.

```
kill_terminal → <terminal-id from step 0>
```

Optional cleanup:

```powershell
Remove-Item .\App_Data\smoke-test.db, .\App_Data\smoke-test.db-shm, .\App_Data\smoke-test.db-wal -ErrorAction SilentlyContinue
```
