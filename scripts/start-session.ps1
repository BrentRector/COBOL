<#
.SYNOPSIS
  Start a fresh ATTENDED Claude Code session in this repository with the restart prompt.
.DESCRIPTION
  A session cannot start its own successor: something outside it must launch the new one. This is that launcher, and it is the
  ONLY one: the Startup entry and a context-full restart both run it.
    pwsh -NoProfile -File scripts/start-session.ps1
  The session is named COBOL (`-n COBOL`). The prompt names no state of its own: the memory index's START HERE note is the
  single source, so the same script serves a boot start and a context-full restart. All campaign state is on disk (git, kb/Work,
  the memory notes), so no context is lost; agents of the previous session died with it and left worktrees, WIP commits and
  checkpoints behind.

  Context-full procedure (the attended session does this BEFORE asking the owner to run this script; there is no automatic
  trigger, a model cannot restart itself):
    1. wind down running work without losing it: `pwsh scripts/orchestrator/stop.ps1` (the loop's agents checkpoint and hand off);
    2. save: a resume note in memory marked START HERE in MEMORY.md, WIP checkpoint commits on the worktree branches;
    3. tell the owner to run this script in a new terminal.

  Rules this file keeps (each was an owner report):
  - `-n COBOL`, never `--resume COBOL`: --resume takes a SESSION ID, and any other value opens the interactive session picker and
    blocks forever (owner, 2026-09-20).
  - `--dangerously-skip-permissions` IS intentional on this path (owner decision 2026-09-20), so a boot start never waits on a
    permission prompt. It bypasses every permission check for the whole session; the gates that remain are structural: implementers
    work in .claude/worktrees/, every push to main goes through scripts/push-main.sh, main requires the ci-gate check with
    enforce_admins on, and the PreToolUse hook (scripts/hooks/forbidden_commands.py) still runs. `skipDangerousModePermissionPrompt`
    is already true in the user settings, so the flag raises no confirmation screen.
  - Do not put this file in the Windows Startup folder: Windows launches every file there by its file association, so a .ps1 there
    opens in Notepad. The Startup entry is a .cmd that calls this script.
#>
[CmdletBinding()]
param(
    # The Claude Code executable (a test seam; the default is the user-level install).
    [string]$ClaudeExe = (Join-Path $env:USERPROFILE '.local\bin\claude.exe'),
    # Print what would run and start nothing.
    [switch]$DryRun
)
$ErrorActionPreference = 'Stop'
$Repo = Split-Path $PSScriptRoot -Parent
$Prompt = @'
RESTART (owner-authorized auto-start; also used when the previous session's context filled). The previous session saved its state to disk and wound down running work. Do this, in order: (1) run the session-start skill; (2) read the memory index MEMORY.md and the note it marks START HERE, and follow that note's first steps; trust `pwsh scripts/session-probe.ps1` and `python scripts/spec/work.py next` over any remembered state; (3) do NOT start the orchestrator loop, dispatch a fleet or spend quota until you have read the note's quota and stop-file instructions: a STOP file may be present in E:\COBOL-coord and elsewhere on purpose, and the weekly allowance in force is stated in the note; (4) say in one short line what you found and what you are doing first, then continue autonomously. Agents of the previous session died with it; their worktrees, WIP commits and checkpoints survive on disk.
'@
if ($DryRun) {
    Write-Host "would run in ${Repo}: $ClaudeExe -n COBOL --dangerously-skip-permissions <prompt of $($Prompt.Length) characters>"
    return
}
Set-Location $Repo
& $ClaudeExe -n COBOL --dangerously-skip-permissions $Prompt
