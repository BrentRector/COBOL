#!/usr/bin/env bash
# Cloud-environment setup script for claude.ai/code sessions (Anthropic-hosted VM: Ubuntu 24.04 x86_64, run as
# root BEFORE Claude Code launches; the result is snapshotted and reused by later sessions, and re-runs only when
# the script or the allowed hosts change, or after ~7 days).
#
# ⛔ This file is the CANONICAL copy. The environment runs the text pasted into its "Setup script" field at
# claude.ai/code → environment settings — after editing this file, paste it there again, in the same change set.
#
# The environment's network access must be CUSTOM = the default list ("Also include default list of common package
# managers") PLUS:
#     builds.dotnet.microsoft.com   (dotnet-install.sh downloads the SDK from here)
#     ftp.gnu.org                   (scripts/fetch-gnucobol-tests.ps1 — the GnuCOBOL 3.2 differential corpus)
# GitHub (the repo AND the private specs-private submodule) goes through the separate GitHub proxy regardless — but
# that proxy only authorizes repositories ATTACHED TO THE SESSION, so the specs-private submodule (the licensed PDF,
# needed only by render-spec-page.py and the figure audits — specs/ISO_COBOL.md and cite.py are in the main repo)
# clones only when BrentRector/COBOL-private is attached too; otherwise "could not read Username … terminal
# prompts disabled". `claude --cloud` attaches ONE repository; the claude.ai/code page attaches both ("+" beside the
# repo chip, or the link https://claude.ai/code?repositories=BrentRector/COBOL,BrentRector/COBOL-private).
#
# What the VM lacks that this repo needs (everything else — git, python3, java 21 for ANTLR — is pre-installed):
#   * .NET 10 SDK — global.json pins 10.0.100 (rollForward latestMinor); CI uses setup-dotnet 10.0.x
#   * pwsh        — the Frontend's ANTLR generation, session-probe.ps1, fetch-gnucobol-tests.ps1
#   * `python`    — the LATEST CPython (3.14, the owner's standard 2026-09-24, same as the dev box); every hook and
#                   doc command spells it `python`, and the scripts need >= 3.12 (PEP 701 f-strings)
# Per-CLONE work (the specs-private submodule, the git-ignored GnuCOBOL corpus under tests/external/) is NOT done
# here: this script's result is SNAPSHOTTED and reused while the repo is cloned fresh per session, so the
# SessionStart hook (scripts/hooks/session_start.py) does it on every cloud session. What this script owns is making
# that hook RUN: with two repositories attached (CobolSharp + CobolSharp-private — see above) Claude
# Code starts in their PARENT, /home/user, so the repo's .claude/settings.json never loads and no hook fires (cloud
# smoke #2, 2026-09-24: "Found 0 total hooks in registry"). The user-level SessionStart hook installed below closes
# that gap, and the corpus tarball is pre-cached here so the hook's per-clone fetch is a local copy.
#
# Constraints: must exit 0 (non-zero fails the session start) and finish in ~5 minutes.
set -uo pipefail

# Everything this script prints is ALSO kept in the snapshot: the platform shows setup stdout nowhere a session can
# read it, so without this copy no session can verify a `[setup-env]` line (cloud smoke #3, 2026-09-24).
SETUP_LOG=/var/log/cobolsharp-setup-env.log
exec > >(tee "$SETUP_LOG") 2>&1

log() { echo "[setup-env] $*"; }

DOTNET_DIR=/usr/share/dotnet

# ── apt index refresh, in parallel with the SDK download (the dotnet apt fallback needs it) ──────────────────────
( export DEBIAN_FRONTEND=noninteractive; apt-get update -qq || log "WARN: apt-get update failed" ) &
APT_PID=$!

# ── .NET 10 SDK: dotnet-install.sh (latest 10.0 = what CI's setup-dotnet 10.0.x resolves), apt as the fallback ──
install_dotnet() {
  if curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
     && bash /tmp/dotnet-install.sh --channel 10.0 --install-dir "$DOTNET_DIR" >/tmp/dotnet-install.log 2>&1; then
    log "dotnet-install.sh ok"
  else
    log "WARN: dotnet-install.sh failed (tail below); falling back to Ubuntu's dotnet-sdk-10.0"
    tail -5 /tmp/dotnet-install.log 2>/dev/null
    wait "$APT_PID" 2>/dev/null
    DEBIAN_FRONTEND=noninteractive apt-get install -y -qq dotnet-sdk-10.0 >/dev/null \
      && DOTNET_DIR=/usr/lib/dotnet && log "apt dotnet-sdk-10.0 ok" \
      || { log "ERROR: no .NET 10 SDK installed"; return 1; }
  fi
  ln -sf "$DOTNET_DIR/dotnet" /usr/local/bin/dotnet
  # apphosts (pwsh below, any dotnet tool) locate the runtime through this file when DOTNET_ROOT is unset.
  mkdir -p /etc/dotnet && echo "$DOTNET_DIR" > /etc/dotnet/install_location
}
install_dotnet || true
wait "$APT_PID" 2>/dev/null || true

# ── python = the latest CPython 3.14. Ubuntu 24.04 packages stop at 3.12, and the image's own /usr/local/bin/python{,3}
#    → python3.11 sits AHEAD of /usr/bin on PATH (the first smoke session, 2026-09-24, failed 3 Unit tests on it: the
#    scripts use PEP 701 f-strings). uv comes from PyPI (default allowlist) and installs a standalone CPython 3.14;
#    fallback = the newest /usr/bin/python3.N with N >= 12. Both names in /usr/local/bin point at the result, and the
#    repo's only third-party imports (PyMuPDF for the spec renderers, fontTools) go into it. ─────────────────────────
PYVER=3.14
PY=""
if python3 -m pip install -q --break-system-packages --root-user-action=ignore uv >/tmp/uv-install.log 2>&1 \
   && UV_PYTHON_INSTALL_DIR=/opt/uv-python python3 -m uv python install "$PYVER" >>/tmp/uv-install.log 2>&1; then
  PY=$(UV_PYTHON_INSTALL_DIR=/opt/uv-python python3 -m uv python find --managed-python "$PYVER" 2>/dev/null)
fi
if [ -z "$PY" ]; then
  log "WARN: uv could not install CPython $PYVER (tail below); falling back to the newest system python3.12+"
  tail -5 /tmp/uv-install.log 2>/dev/null
  PY=$(ls /usr/bin/python3.[0-9]* 2>/dev/null | grep -E '/python3\.[0-9]+$' | sort -t. -k2 -n | tail -1)
  [ -n "$PY" ] && [ "${PY##*.}" -ge 12 ] || { log "WARN: no python3.12+ found (${PY:-none})"; PY=""; }
fi
if [ -n "$PY" ]; then
  ln -sf "$PY" /usr/local/bin/python && ln -sf "$PY" /usr/local/bin/python3 && log "python -> $PY"
  python -m pip install -q --break-system-packages --root-user-action=ignore --no-warn-script-location \
      PyMuPDF fonttools >/tmp/pip-deps.log 2>&1 \
    && log "PyMuPDF + fontTools ok" || { log "WARN: pip deps failed"; tail -5 /tmp/pip-deps.log; }
fi

cat > /etc/profile.d/dotnet.sh <<EOF
export DOTNET_ROOT=$DOTNET_DIR
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
EOF

# ── pwsh as a .NET global tool (NuGet is on the default allowlist; avoids adding the Microsoft apt feed, whose
#    dotnet packages conflict with Ubuntu's) ──────────────────────────────────────────────────────────────────────
if command -v dotnet >/dev/null; then
  export DOTNET_ROOT=$DOTNET_DIR DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
  dotnet tool install PowerShell --tool-path /usr/local/share/dotnet-tools >/tmp/pwsh-install.log 2>&1 \
    && ln -sf /usr/local/share/dotnet-tools/pwsh /usr/local/bin/pwsh && log "pwsh ok" \
    || { log "WARN: pwsh install failed"; tail -5 /tmp/pwsh-install.log; }
fi

# ── the GnuCOBOL 3.2 corpus tarball, pre-cached (pinned: the same URL + SHA-256 as scripts/fetch-gnucobol-tests.ps1,
#    which the hook runs per clone after copying this file into tests/external/ — the fetch then skips the download).
#    GPL-3.0 test text: cached OUTSIDE the repo, never committed (the fetch script's licensing posture). ──────────
CACHE_DIR=/opt/cobolsharp-cache
GNUCOBOL_TGZ=gnucobol-3.2.tar.xz
GNUCOBOL_SHA=3bb48af46ced4779facf41fdc2ee60e4ccb86eaa99d010b36685315df39c2ee2
mkdir -p "$CACHE_DIR"
if curl -fsSL "https://ftp.gnu.org/gnu/gnucobol/$GNUCOBOL_TGZ" -o "$CACHE_DIR/$GNUCOBOL_TGZ.part" \
   && echo "$GNUCOBOL_SHA  $CACHE_DIR/$GNUCOBOL_TGZ.part" | sha256sum -c --quiet; then
  mv "$CACHE_DIR/$GNUCOBOL_TGZ.part" "$CACHE_DIR/$GNUCOBOL_TGZ" && log "GnuCOBOL corpus tarball cached"
else
  rm -f "$CACHE_DIR/$GNUCOBOL_TGZ.part"; log "WARN: GnuCOBOL tarball not cached (the hook's fetch will download it)"
fi

# ── the USER-LEVEL SessionStart hook (see the header): a shim that runs the repo's hook when the session's project
#    dir is NOT the repo itself (the project-level hook already covers that case — never run it twice). Merged into
#    ~/.claude/settings.json, never overwriting keys the platform may have put there. ──────────────────────────────
cat > /usr/local/bin/cobolsharp-session-start <<'EOF'
#!/usr/bin/env bash
# User-level SessionStart shim installed by CobolSharp scripts/cloud/setup-env.sh — see that file's header.
for repo in "${CLAUDE_PROJECT_DIR:-/home/user}/CobolSharp" /home/user/CobolSharp; do
  [ -f "$repo/scripts/hooks/session_start.py" ] || continue
  [ "$(realpath "${CLAUDE_PROJECT_DIR:-.}")" = "$(realpath "$repo")" ] && exit 0   # the project hook runs instead
  exec python "$repo/scripts/hooks/session_start.py"
done
exit 0
EOF
chmod 755 /usr/local/bin/cobolsharp-session-start
python - <<'EOF' && log "user-level SessionStart hook installed" || log "WARN: user-level SessionStart hook NOT installed"
import json, os, pathlib
p = pathlib.Path(os.path.expanduser("~/.claude/settings.json"))
p.parent.mkdir(parents=True, exist_ok=True)
s = json.loads(p.read_text(encoding="utf-8")) if p.exists() and p.read_text(encoding="utf-8").strip() else {}
entries = s.setdefault("hooks", {}).setdefault("SessionStart", [])
cmd = "/usr/local/bin/cobolsharp-session-start"
if not any(h.get("command") == cmd for e in entries for h in e.get("hooks", [])):
    entries.append({"matcher": "startup|resume|clear|compact",
                    "hooks": [{"type": "command", "command": cmd, "timeout": 300}]})
p.write_text(json.dumps(s, indent=2) + "\n", encoding="utf-8")
EOF

# ── report (visible in the session's setup log) ──────────────────────────────────────────────────────────────────
log "dotnet: $(dotnet --version 2>&1 | head -1)"
log "pwsh:   $(pwsh -NoProfile -Command '$PSVersionTable.PSVersion.ToString()' 2>&1 | head -1)"
log "python: $(python --version 2>&1)"
log "java:   $(java -version 2>&1 | head -1)"
exit 0
