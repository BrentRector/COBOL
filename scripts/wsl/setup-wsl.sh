#!/usr/bin/env bash
# User-space WSL toolchain refresh for COBOL.NET (parity target: scripts/cloud/setup-env.sh / CI setup-dotnet 10.0.x).
set -u
log() { echo "[wsl-tooling] $*"; }

# .NET 10 SDK -> ~/.dotnet
curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
  && bash /tmp/dotnet-install.sh --channel 10.0 --install-dir "$HOME/.dotnet" > /tmp/dotnet-install.log 2>&1 \
  && log "dotnet-install ok" || { log "dotnet-install FAILED"; tail -5 /tmp/dotnet-install.log; }

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
  curl -LsSf https://astral.sh/uv/install.sh | sh > /tmp/uv-install.log 2>&1 && log "uv ok" || { log "uv FAILED"; tail -5 /tmp/uv-install.log; }
fi
uv self update > /dev/null 2>&1 || true
uv python install 3.14 --default > /tmp/uv-python.log 2>&1 && log "cpython 3.14 ok" || { log "cpython FAILED"; tail -5 /tmp/uv-python.log; }

log "dotnet: $(dotnet --version 2>&1)"
log "python: $(python3.14 --version 2>&1) (python on PATH: $(command -v python) )"
log "pwsh:   $(pwsh -NoProfile -Command '$PSVersionTable.PSVersion.ToString()' 2>&1 | head -1)"
log "git:    $(git --version)"
log "DONE"
