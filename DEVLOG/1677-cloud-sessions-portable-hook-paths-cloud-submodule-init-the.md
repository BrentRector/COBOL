## Entry 1677 — 2026-09-24 11:53 PDT — Cloud sessions: portable hook paths, cloud submodule init, the canonical VM setup script

The owner has a $250 claude.ai "cloud session credit" (Settings → Usage: "Applies automatically to cloud sessions",
expires 2026-11-04) that is separate from the weekly quota — so compiler work can run in claude.ai/code cloud
sessions while the weekly quota recovers. The Claude GitHub App is now installed on every BrentRector repo (both
`CobolSharp` and the private `CobolSharp-private` submodule appear in the claude.ai/code repository picker). Three
things stood between this repo and a working cloud session:

1. **Every hook in `.claude/settings.json` hard-coded `python E:/CobolSharp/scripts/hooks/X.py`** — dead on the
   Ubuntu VM, and (latent locally) a worktree session ran the MAIN checkout's hook scripts, not its own. All six now
   spell `python "$CLAUDE_PROJECT_DIR/scripts/hooks/X.py"`. Verified locally: the `dotnet` PreToolUse hook and the
   Write/Edit PostToolUse hook both fire clean through the new paths. The SessionStart matcher gains `resume` (an
   expired cloud VM is re-provisioned on resume, without the submodule) and its timeout 120 → 300 s.
2. **A cloud session clones WITHOUT submodules**, so `specs/ISO_COBOL.md` and `cite.py --check` (CLAUDE.md rule 1)
   would be missing. `scripts/hooks/session_start.py` now runs `git submodule update --init --recursive --depth 1`
   first when `CLAUDE_CODE_REMOTE=true` and reports the outcome in the injected context; locally it is unchanged.
   Tested both modes (the forced cloud-mode run left the local submodule at its recorded commit on `master`).
3. **The VM lacks .NET 10 and pwsh.** New `scripts/cloud/setup-env.sh` is the CANONICAL copy of the text pasted into
   the environment's Setup-script field: .NET 10 SDK via dotnet-install.sh (channel 10.0 = what CI's setup-dotnet
   10.0.x resolves; Ubuntu `dotnet-sdk-10.0` as the fallback), pwsh as a .NET global tool (avoids the Microsoft apt
   feed's conflicts with Ubuntu's dotnet packages), `python-is-python3`. Java 21 (ANTLR) is pre-installed. No
   GnuCOBOL: the differential reads the GnuCOBOL 3.2 TEST CORPUS, not `cobc`. The environment needs CUSTOM network
   access = defaults + `builds.dotnet.microsoft.com` + `ftp.gnu.org` (the corpus fetch), both absent from Trusted.
   `bash -n` clean; its first real run is the cloud smoke session that follows this commit.

