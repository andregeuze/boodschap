# Playwright MCP Smoke Test

Run the app first: `dotnet run --launch-profile http` (listens on `http://localhost:5091`).

## 1. Navigate to the app

```
mcp_playwright_browser_navigate → http://localhost:5091
```

## 2. Verify initial page structure

```
mcp_playwright_browser_snapshot
```

Expected:
- "New item" button
- Heading "Groceries"
- "Drag to reorder" paragraph
- FilterBar group "Filter groceries" with buttons: All, Needed, Purchased
- 6 list items: Milk, Eggs, Bread, Tomatoes, Cheese, Coffee (all unchecked)

## 3. Mark items as purchased

Click the Milk checkbox, then the Eggs checkbox.

```
mcp_playwright_browser_click → checkbox "Milk"
mcp_playwright_browser_click → checkbox "Eggs"
```

## 4. Test "Needed" filter

```
mcp_playwright_browser_click → button "Needed"
mcp_playwright_browser_snapshot
```

Expected:
- "Needed" button is `[active]`
- Only unchecked items shown: Bread, Tomatoes, Cheese, Coffee
- "Drag to reorder" hint is hidden

## 5. Test "Purchased" filter

```
mcp_playwright_browser_click → button "Purchased"
mcp_playwright_browser_snapshot
```

Expected:
- "Purchased" button is `[active]`
- Only checked items shown: Milk `[checked]`, Eggs `[checked]`

## 6. Test "All" filter

```
mcp_playwright_browser_click → button "All"
mcp_playwright_browser_snapshot
```

Expected:
- "All" button is `[active]`
- All 6 items shown
- "Drag to reorder" paragraph visible

## 7. Test "New item" quick-add

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
- "Bananas" appears in the list
- Input is cleared

## 8. Test remove

```
mcp_playwright_browser_click → button "Remove" (next to "Bananas")
mcp_playwright_browser_snapshot
```

Expected:
- "Bananas" is no longer in the list

## 9. Close browser

```
mcp_playwright_browser_close
```

## 10. Kill the host process

Stop the `dotnet run` process that was started at the beginning.

```
kill_terminal → <terminal-id from step 0>
```
