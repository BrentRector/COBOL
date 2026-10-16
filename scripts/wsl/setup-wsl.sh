#!/usr/bin/env bash
# User-space WSL toolchain refresh for WiseOwl COBOL (parity target: scripts/cloud/setup-env.sh / CI setup-dotnet 10.0.x).
set -u
log() { echo "[wsl-tooling] $*"; }
# The installers' downloads and logs: a directory of this run's own, never fixed names in the machine-wide /tmp (kb/Work
# PB2922). Each failure prints its log's tail, so the directory goes when the script ends.
WORK="$(mktemp -d "${TMPDIR:-/tmp}/setup-wsl.XXXXXX")"
trap 'rm -rf "$WORK"' EXIT

# .NET 10 SDK -> ~/.dotnet
curl -fsSL https://dot.net/v1/dotnet-install.sh -o "$WORK/dotnet-install.sh" \
  && bash "$WORK/dotnet-install.sh" --channel 10.0 --install-dir "$HOME/.dotnet" > "$WORK/dotnet-install.log" 2>&1 \
  && log "dotnet-install ok" || { log "dotnet-install FAILED"; tail -5 "$WORK/dotnet-install.log"; }

# PATH wiring in ~/.profile (login shells) and ~/.bashrc (interactive)
for rc in "$HOME/.profile" "$HOME/.bashrc"; do
  grep -q 'DOTNET_ROOT="$HOME/.dotnet"' "$rc" 2>/dev/null || cat >> "$rc" <<'EOF'
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$HOME/.local/bin:$PATH"
EOF
done
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$HOME/.local/bin:$PATH"

# uv + latest CPython (owner rule: latest CPython everywhere)
if ! command -v uv >/dev/null 2>&1; then
  curl -LsSf https://astral.sh/uv/install.sh | sh > "$WORK/uv-install.log" 2>&1 && log "uv ok" || { log "uv FAILED"; tail -5 "$WORK/uv-install.log"; }
fi
uv self update > /dev/null 2>&1 || true
uv python install 3.14 --default > "$WORK/uv-python.log" 2>&1 && log "cpython 3.14 ok" || { log "cpython FAILED"; tail -5 "$WORK/uv-python.log"; }

# Python packages the scripts import, kept at parity with the Windows dev box (latest versions):
# pymupdf (fitz: scripts/render-spec-page.py and the spec tools), scipy + numpy (statistics for process evaluations),
# pyyaml (scripts/hooks/tooling_check.py proves every agent role and skill frontmatter parses, kb/Work PB3097).
# The uv-managed interpreter is marked externally managed (PEP 668); it is user-space and ours, so install into it.
PY_PACKAGES="pymupdf scipy numpy pyyaml"
uv pip install --upgrade --system --break-system-packages --python "$(command -v python3.14)" $PY_PACKAGES \
  > "$WORK/uv-pip.log" 2>&1 && log "python packages ok ($PY_PACKAGES)" || { log "python packages FAILED"; tail -5 "$WORK/uv-pip.log"; }

log "dotnet: $(dotnet --version 2>&1)"
log "python: $(python3.14 --version 2>&1) (python on PATH: $(command -v python) )"
log "pypkgs: $(python3.14 -c 'import fitz, scipy, numpy, yaml; print("pymupdf", fitz.VersionBind, "scipy", scipy.__version__, "numpy", numpy.__version__, "pyyaml", yaml.__version__)' 2>&1 | tail -1)"
log "pwsh:   $(pwsh -NoProfile -Command '$PSVersionTable.PSVersion.ToString()' 2>&1 | head -1)"
log "git:    $(git --version)"
log "DONE"
