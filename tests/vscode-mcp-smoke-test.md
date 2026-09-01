# VS Code MCP Smoke Test

Run the app first against a disposable smoke-test database so persisted local changes do not affect the seeded baseline. Do not run this smoke test against the production database.

This smoke test uses GitHub Copilot Chat in VS Code to call the Boodschap MCP tools. All shopping-list changes in this flow must be made through the `boodschap-local` MCP server. Do not select `boodschap-hosted`, because that server uses the hosted database.

## 1. Start the app with a disposable database

PowerShell:

```powershell
$env:ConnectionStrings__Boodschap='Data Source=App_Data/mcp-smoke-test.db'
$env:Mcp__AccessKey='boodschap-local-smoke'
Remove-Item .\sources\Boodschap\App_Data\mcp-smoke-test.db, .\sources\Boodschap\App_Data\mcp-smoke-test.db-shm, .\sources\Boodschap\App_Data\mcp-smoke-test.db-wal -ErrorAction SilentlyContinue
dotnet run --project sources/Boodschap/Boodschap.csproj --launch-profile http
```

Expected:

- The app listens on `http://localhost:5091`.
- The MCP endpoint is available at `http://localhost:5091/mcp`.
- The disposable database is initialized with the development seed data.

## 2. Verify the VS Code MCP configuration

Confirm that `.vscode/mcp.json` contains a server named `boodschap-local` with:

```json
{
  "type": "http",
  "url": "http://localhost:5091/mcp"
}
```

The configuration must send the prompted local access key as a bearer token. Do not store the access key directly in `.vscode/mcp.json`.

## 3. Start the MCP server in VS Code

1. Open the Command Palette.
2. Run `MCP: List Servers`.
3. Select `boodschap-local`.
4. Select `Start Server`, or `Restart Server` when it is already running.
5. Enter `boodschap-local-smoke` when VS Code prompts for the Boodschap MCP access key.

Expected:

- VS Code reports that the `boodschap-local` MCP server is running.
- The server exposes `list_shopping_lists`, `create_shopping_list`, `add_shopping_list_item`, and `remove_shopping_list_item`.
- No access key is written to a workspace file.

## 4. Verify the seeded baseline through MCP

Open GitHub Copilot Chat in Agent mode and submit:

```text
Use only the `boodschap-local` MCP server to list all shopping lists and their items. Do not use `boodschap-hosted` and do not make any changes.
```

Approve the `list_shopping_lists` tool call if VS Code requests confirmation.

Expected:

- Copilot uses the `boodschap-local` server's `list_shopping_lists` tool.
- Active lists `Etentje` and `Weekboodschappen` are returned.
- Archived list `Kampeerweekend` is returned.
- `Weekboodschappen` contains Melk, Eieren, Brood, Tomaten, Kaas, and Koffie.
- No list or item is changed.

## 5. Open the app as a realtime observer

Open `http://localhost:5091` in the VS Code integrated browser and sign in with the disposable development account when prompted:

- Username: `Geuze`
- Password: `Welkom01`

Expected:

- Heading `Kies een lijst en ga door.` is visible.
- The seeded active and archived lists match the MCP result from step 4.
- No list named `MCP smoketest` exists yet.

Keep this browser page open for the next step. Do not refresh it manually.

## 6. Create a populated list through MCP

In the same Copilot Chat, submit:

```text
Use only the `boodschap-local` MCP server to create a shopping list named "MCP smoketest" with the description "Aangemaakt vanuit GitHub Copilot in VS Code." and these items in order: Bananen, Sinaasappels, Koffiebonen. Do not use `boodschap-hosted`, the browser, or any API directly.
```

Approve the `create_shopping_list` tool call if VS Code requests confirmation.

Expected in Copilot Chat:

- Copilot uses the `boodschap-local` server's `create_shopping_list` tool exactly once.
- The result contains the name `MCP smoketest` and the requested description.
- The result contains Bananen, Sinaasappels, and Koffiebonen in that order.
- The new list is active and all three items are not purchased.

Expected in the already-open browser:

- `MCP smoketest` appears without a manual refresh.
- Its card shows `0/3 gekocht`.
- Opening the list shows Bananen, Sinaasappels, and Koffiebonen in that order.

## 7. Add an item to the existing list through MCP

In Copilot Chat, submit:

```text
Use only the `boodschap-local` MCP server to add "Appels" to the existing shopping list named "MCP smoketest". Do not create a new list and do not use `boodschap-hosted`, the browser, or any API directly.
```

Expected:

- Copilot resolves the list ID with `list_shopping_lists` and calls `add_shopping_list_item` exactly once.
- The tool result contains Bananen, Sinaasappels, Koffiebonen, and Appels in that order.
- Appels appears in the already-open browser without a manual refresh.

## 8. Remove the item through MCP

In Copilot Chat, submit:

```text
Use only the `boodschap-local` MCP server to remove "Appels" from the existing shopping list named "MCP smoketest". Resolve both IDs from the current shopping-list data, do not remove another item, and do not use `boodschap-hosted`, the browser, or any API directly.
```

Approve the destructive `remove_shopping_list_item` tool call if VS Code requests confirmation.

Expected:

- Copilot resolves the list and item IDs with `list_shopping_lists` and calls `remove_shopping_list_item` exactly once.
- The tool result contains Bananen, Sinaasappels, and Koffiebonen, but no Appels.
- Appels disappears from the already-open browser without a manual refresh.

## 9. Verify persistence through MCP

In Copilot Chat, submit:

```text
Use only the `boodschap-local` MCP server to list all shopping lists again. Verify that "MCP smoketest" is active, has the description "Aangemaakt vanuit GitHub Copilot in VS Code.", and contains exactly Bananen, Sinaasappels, and Koffiebonen in that order. Do not use `boodschap-hosted` and do not make any changes.
```

Expected:

- Copilot uses `list_shopping_lists` again.
- The newly created list is returned alongside the three seeded lists.
- Its persisted description and item order match the create request.
- The browser and MCP results describe the same state.

## 10. Stop the MCP server and close the browser

1. Run `MCP: List Servers` from the Command Palette.
2. Select `boodschap-local`.
3. Select `Stop Server`.
4. Close the integrated browser page used for this smoke test.

## 11. Stop the app and remove the disposable database

Stop the `dotnet run` process, then run:

```powershell
Remove-Item .\sources\Boodschap\App_Data\mcp-smoke-test.db, .\sources\Boodschap\App_Data\mcp-smoke-test.db-shm, .\sources\Boodschap\App_Data\mcp-smoke-test.db-wal -ErrorAction SilentlyContinue
```

Expected:

- The local app and MCP endpoint no longer respond on port `5091`.
- No `mcp-smoke-test.db`, `mcp-smoke-test.db-shm`, or `mcp-smoke-test.db-wal` file remains.
- The seeded baseline will be recreated on the next smoke-test run.