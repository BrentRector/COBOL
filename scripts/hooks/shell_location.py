#!/usr/bin/env python3
"""Where each command of a Bash or PowerShell tool call runs: the ONE parser of location changes the hooks share.

kb/Work PB2599. A hook that asks "which tree does this command act on?" must follow the command line the way the shell
does. Each hook used to carry its own partial parser: `fleet_active_build.py` honoured only a LEADING `cd <dir> &&`,
`worktree_rm_allow.py` a bare `cd|Set-Location <dir>` prefix, `forbidden_commands.py` an unquoted `cd|Set-Location`
anywhere, and `status_guard.py` none at all. So a PowerShell `Set-Location <worktree>; dotnet build` or a Bash
`git worktree add … && cd <worktree> && dotnet build` was judged to run in the payload's cwd (the main checkout), and
the build guard denied it while a main-tree fleet was live (2026-10-07/08, six denials in the operator's transcripts).

`steps(command, cwd, shell)` splits a tool call into its simple commands and gives each the directory it runs in,
following every location change the two shells accept:

  Bash         `cd [-L|-P] [dir]` (no dir: home; `-`: the previous dir), `pushd dir`, `popd`; a `( … )` subshell's
               changes end at its `)`; `bash|sh -c '…'` runs its payload in a child (its changes never persist).
  PowerShell   `Set-Location`, `sl`, `cd`, `chdir`, `Push-Location`, `pushd`, `Pop-Location`, `popd`, with the path
               positional or as `-Path`/`-LiteralPath`/`-LP`/`-PSPath` (also `-Path:dir`); `cd -` the previous dir;
               `pwsh|powershell [-WorkingDirectory|-wd dir] -Command "…"` runs its payload in a child in that dir;
               any other command's `-WorkingDirectory dir` (Start-Process) runs that one command there.
  Both         separators `&&`, `||`, `;`, `|` and newlines; Bash `&` (the left side runs in the background, so its
               `cd` does not persist); quotes, escapes, `$( … )`, `{ … }`, heredoc bodies and PowerShell here-strings
               never split a command; `~` is the home dir; a variable set earlier in the SAME call to a literal
               (`WT=/e/x`, `$wt = 'E:\\x'`) is substituted.

A directory the line does not determine (a target spelled with an unknown variable or a command substitution, `popd`
with nothing pushed in this call) is `None`, and so is every later step of that shell until an absolute location
change: a caller treats it as UNKNOWN, never as the payload's cwd. Paths: an MSYS drive path (`/e/x`, `/cygdrive/e/x`) is `E:/x` on Windows (PB474);
a path absolute on EITHER platform is kept as written; anything else is joined to the directory in effect.

Self-test: `python scripts/hooks/test_shell_location.py` (the gate's audits, CI's audits job, the Linux gate's hooks leg).
"""
from __future__ import annotations

import dataclasses
import os
import pathlib
import re

BASH, POWERSHELL = "bash", "powershell"

_BASH_LOCATION = {"cd", "pushd", "popd"}
_PS_LOCATION = {"set-location", "sl", "cd", "chdir", "push-location", "pushd", "pop-location", "popd"}
_PUSH = {"pushd", "push-location"}
_POP = {"popd", "pop-location"}
_PS_PATH_PARAMS = {"-path", "-literalpath", "-lp", "-pspath"}
_PS_SWITCHES = {"-passthru"}
_PS_VALUE_PARAMS = {"-stackname"}
_SHELLS = {"bash": BASH, "sh": BASH, "zsh": BASH, "pwsh": POWERSHELL, "powershell": POWERSHELL}
_SHELL_PAYLOAD_FLAGS = {"-c", "-lc", "-ic", "-command"}
_BASH_DECLARE = {"export", "declare", "local", "readonly", "typeset"}
_ASSIGN_BASH = re.compile(r"^([A-Za-z_][A-Za-z0-9_]*)=(.*)$", re.S)
_ASSIGN_PS = re.compile(r"^\$([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.+)$", re.S)
_VAR = re.compile(r"\$\{([A-Za-z_][A-Za-z0-9_]*)\}|\$(?:env:)?([A-Za-z_][A-Za-z0-9_]*)")
# Git Bash / MSYS spell a Windows drive path `/e/COBOL/…` (or `/cygdrive/e/…`); one letter, then `/` or the end, so a
# POSIX path such as `/tmp/x` is left alone (kb/Work PB474).
_MSYS_DRIVE = re.compile(r"^/(?:cygdrive/)?([A-Za-z])(?=/|$)(.*)$")


@dataclasses.dataclass(frozen=True)
class Step:
    """One simple command of a tool call."""
    text: str                    # as written, with any heredoc body it introduces
    words: tuple[str, ...]       # its words, quotes removed, known variables substituted
    cwd: str | None              # the directory it runs in; None when the line does not determine it
    sep: str                     # the separator that FOLLOWS it ("" at the end): && || ; | & or a newline
    depth: int                   # 0 = the tool call's own command line; >0 = inside a subshell, group or `-c` payload
    location: str | None = None  # a location change's target as written (after substitution); "" for one with no
                                 # argument (`cd` = home, `popd`); None when the step is not a location change


def shell_of(tool_name: str | None) -> str:
    """The shell a hook payload's tool runs: the PowerShell tool's commands are PowerShell, everything else Bash."""
    return POWERSHELL if (tool_name or "").lower() == "powershell" else BASH


def native_path(target: str) -> str:
    """A path as the OS spells it: MSYS `/e/x` -> `E:/x` on Windows; anything else unchanged."""
    if os.name != "nt":
        return target
    m = _MSYS_DRIVE.match(target)
    return f"{m.group(1).upper()}:{m.group(2) or '/'}" if m else target


def is_absolute(p: str) -> bool:
    """Absolute on EITHER platform (a POSIX root or a drive): `Path.is_absolute` answers for the host only, and a hook's
    self-test runs on Linux CI with Windows spellings in it (kb/Work PB2142)."""
    return pathlib.PurePosixPath(p).is_absolute() or pathlib.PureWindowsPath(p).is_absolute()


def resolve(target: str, cwd: str | None) -> str | None:
    """`target` (a location-change or `-C` argument, already substituted) as a directory, or None when undetermined."""
    if not target or "$" in target or "`" in target:
        return None
    if target == "~" or target.startswith(("~/", "~\\")):
        target = str(pathlib.Path.home()) + target[1:]
    target = native_path(target)
    if is_absolute(target):
        return target
    if cwd is None:
        return None
    return str(pathlib.Path(cwd) / target)


# ── the scanner: top-level commands and their separators ──────────────────────────────────────────────────────────


def _split(command: str, shell: str) -> list[tuple[str, str]]:
    """The tool call's top-level commands, each with the separator after it. Quotes, escapes, brackets, heredoc bodies
    and here-strings are kept inside the command they belong to."""
    out: list[tuple[str, str]] = []
    buf: list[str] = []
    i, n, depth = 0, len(command), 0
    esc = "\\" if shell == BASH else "`"
    heredocs: list[str] = []          # delimiters whose bodies start at the next newline (Bash)

    def flush(sep: str) -> None:
        text = "".join(buf).strip()
        if text:
            out.append((text, sep))
        elif out and sep and not out[-1][1]:
            out[-1] = (out[-1][0], sep)
        buf.clear()

    while i < n:
        c = command[i]
        if c == esc and i + 1 < n:
            buf.append(command[i:i + 2]); i += 2; continue
        if shell == POWERSHELL and c == "@" and command[i + 1:i + 2] in ("'", '"') and command[i + 2:i + 3] in ("\n", "\r"):
            end = command.find("\n" + command[i + 1] + "@", i + 2)
            end = n if end < 0 else end + 3
            buf.append(command[i:end]); i = end; continue
        if c == "'":
            j = command.find("'", i + 1)
            j = n if j < 0 else j + 1
            buf.append(command[i:j]); i = j; continue
        if c == '"':
            j = i + 1
            while j < n and command[j] != '"':
                j += 2 if command[j] == esc else 1
            j = min(j + 1, n)
            buf.append(command[i:j]); i = j; continue
        if c == "#" and (not buf or buf[-1][-1:].isspace() or buf[-1][-1:] in ";|&(") and depth == 0:
            j = command.find("\n", i)
            i = n if j < 0 else j
            continue
        if shell == BASH and command.startswith("<<", i) and not command.startswith("<<<", i):
            m = re.match(r"<<-?\s*(['\"]?)([A-Za-z_][A-Za-z0-9_]*)\1", command[i:])
            if m:
                heredocs.append(m.group(2))
                buf.append(m.group(0)); i += m.end(); continue
        if c in "({":
            depth += 1
        elif c in ")}" and depth:
            depth -= 1
        if depth == 0:
            if c in "\r\n":
                if heredocs:
                    # the bodies follow this newline, one after another, each ended by its delimiter on a line alone
                    j = i + 1
                    for delim in heredocs:
                        m = re.compile(r"^[ \t]*" + re.escape(delim) + r"[ \t]*$", re.M).search(command, j)
                        j = n if not m else m.end()
                    buf.append(command[i:j]); i = j
                    heredocs.clear()
                    continue
                flush("\n"); i += 1; continue
            two = command[i:i + 2]
            if two in ("&&", "||"):
                flush(two); i += 2; continue
            if c == ";":
                flush(";"); i += 1; continue
            if c == "|":
                flush("|"); i += 1; continue
            if c == "&" and shell == BASH and not (buf and buf[-1][-1:] in "<>") and command[i + 1:i + 2] != ">":
                flush("&"); i += 1; continue
        buf.append(c)
        i += 1
    flush("")
    return out


def _words(text: str, shell: str) -> list[str]:
    """The words of one command, quotes removed (Bash `\\x` is `x` outside single quotes; PowerShell keeps backslashes)."""
    words: list[str] = []
    cur: list[str] = []
    have = False
    i, n = 0, len(text)
    while i < n:
        c = text[i]
        if c.isspace():
            if have:
                words.append("".join(cur)); cur.clear(); have = False
            i += 1; continue
        have = True
        if c == "'":
            j = text.find("'", i + 1)
            j = n if j < 0 else j
            cur.append(text[i + 1:j]); i = j + 1; continue
        if c == '"':
            j = i + 1
            while j < n and text[j] != '"':
                if shell == BASH and text[j] == "\\" and j + 1 < n and text[j + 1] in '"\\$`':
                    cur.append(text[j + 1]); j += 2; continue
                if shell == POWERSHELL and text[j] == "`" and j + 1 < n:
                    cur.append(text[j + 1]); j += 2; continue
                cur.append(text[j]); j += 1
            i = j + 1; continue
        if shell == BASH and c == "\\" and i + 1 < n:
            cur.append(text[i + 1]); i += 2; continue
        cur.append(c); i += 1
    if have:
        words.append("".join(cur))
    return words


def _substitute(word: str, variables: dict[str, str]) -> str:
    return _VAR.sub(lambda m: variables.get(m.group(1) or m.group(2), m.group(0)), word)


def _group(text: str, shell: str) -> tuple[str, str] | None:
    """`( … )` or `{ … }` spanning the WHOLE command: (the opening bracket, the inside), else None. `(a).b` and
    `(a) + (b)` are not groups: the bracket that closes the first one must be the command's last character."""
    if not text or text[0] not in "({":
        return None
    esc = "\\" if shell == BASH else "`"
    depth, i, n = 0, 0, len(text)
    while i < n:
        c = text[i]
        if c == esc:
            i += 2
            continue
        if c in "'\"":
            j = text.find(c, i + 1)
            i = n if j < 0 else j + 1
            continue
        if c in "({":
            depth += 1
        elif c in ")}":
            depth -= 1
            if depth == 0:
                return (text[0], text[1:i]) if i == n - 1 else None
        i += 1
    return None


@dataclasses.dataclass
class _State:
    cwd: str | None
    previous: str | None
    stack: list[str | None]
    variables: dict[str, str]


def _walk(command: str, state: _State, shell: str, depth: int, out: list[Step]) -> None:
    for text, sep in _split(command, shell):
        grouped = _group(text, shell)
        if grouped is not None:
            bracket, inner = grouped
            # A Bash `( … )` is a subshell: its location changes end at the `)`. A `{ … }` group, and every PowerShell
            # bracket (location is per runspace), runs in the current shell, so its changes persist.
            child = _State(state.cwd, state.previous, list(state.stack), dict(state.variables))
            _walk(inner, child, shell, depth + 1, out)
            if not (shell == BASH and bracket == "("):
                state.cwd, state.previous, state.stack, state.variables = child.cwd, child.previous, child.stack, child.variables
            continue
        if shell == POWERSHELL and (m := _ASSIGN_PS.match(text)):
            value = _words(m.group(2), shell)
            if len(value) == 1:
                state.variables[m.group(1)] = _substitute(value[0], state.variables)
        raw = _words(text, shell)
        words = tuple(_substitute(w, state.variables) for w in raw)
        assigned = words[1:] if words and words[0] in _BASH_DECLARE else words
        if shell == BASH and assigned and all(_ASSIGN_BASH.match(w) for w in assigned):
            for w in assigned:
                name, value = _ASSIGN_BASH.match(w).groups()
                state.variables[name] = value
        # a background Bash command (`… &`) and one side of a Bash pipeline run in subshells: changes do not persist
        transient = shell == BASH and (sep in ("&", "|") or bool(out and out[-1].depth == depth and out[-1].sep == "|"))
        if _shell_child(words, state, depth, sep, out):
            continue
        location = _location(words, shell)
        if location is not None:
            verb, target = location
            out.append(Step(text, words, state.cwd, sep, depth, target if target is not None else ""))
            if transient:
                continue
            _move(state, verb, target)
            continue
        out.append(Step(text, words, _workdir(words, state.cwd), sep, depth))


def _location(words: tuple[str, ...], shell: str) -> tuple[str, str | None] | None:
    """(verb, target) when the command changes the location; target None = no argument (home, or a pop)."""
    if not words:
        return None
    verb = words[0].lower() if shell == POWERSHELL else words[0]
    if verb not in (_PS_LOCATION if shell == POWERSHELL else _BASH_LOCATION):
        return None
    verb = verb.lower()
    args = list(words[1:])
    target = None
    i = 0
    while i < len(args):
        a = args[i]
        low = a.lower()
        if shell == POWERSHELL and low.startswith("-") and len(a) > 1:
            name, _, inline = low.partition(":")
            if name in _PS_PATH_PARAMS:
                target = a.split(":", 1)[1] if inline else (args[i + 1] if i + 1 < len(args) else None)
                i += 1 if inline else 2
                continue
            if name in _PS_VALUE_PARAMS:
                i += 2
                continue
            if name in _PS_SWITCHES:
                i += 1
                continue
        if shell == BASH and a in ("-L", "-P", "-e", "-@", "--"):
            i += 1
            continue
        if target is None:
            target = a
        i += 1
    return verb, target


def _move(state: _State, verb: str, target: str | None) -> None:
    if verb in _POP:
        state.previous, state.cwd = state.cwd, (state.stack.pop() if state.stack else None)
        return
    if target == "-":
        new = state.previous
    elif target is None:
        new = str(pathlib.Path.home())
    else:
        new = resolve(target, state.cwd)
    if verb in _PUSH:
        state.stack.append(state.cwd)
    state.previous, state.cwd = state.cwd, new


def _shell_named(program: str) -> str | None:
    """The shell `program` starts (`bash`, `/usr/bin/sh`, `pwsh.exe` …), or None when it is not a shell."""
    return _SHELLS.get(re.sub(r"\.exe$", "", program.replace("\\", "/").rsplit("/", 1)[-1].lower()))


def _workdir_arg(words: tuple[str, ...]) -> str | None:
    """The directory a command names for itself: `-WorkingDirectory dir` (Start-Process, pwsh), or pwsh's `-wd dir`."""
    is_pwsh = bool(words) and _shell_named(words[0]) == POWERSHELL
    for i, w in enumerate(words[1:], 1):
        low = w.lower()
        if (low == "-workingdirectory" or (low == "-wd" and is_pwsh)) and i + 1 < len(words):
            return words[i + 1]
    return None


def _workdir(words: tuple[str, ...], cwd: str | None) -> str | None:
    """A command that names its own working directory runs there; every other command runs in `cwd`."""
    arg = _workdir_arg(words)
    return cwd if arg is None else resolve(arg, cwd)


def _shell_child(words: tuple[str, ...], state: _State, depth: int, sep: str, out: list[Step]) -> bool:
    """`bash -c '…'` / `pwsh [-WorkingDirectory dir] -Command "…"`: walk the payload in a child shell (its location
    changes never come back). True when `words` was such a call."""
    child_shell = _shell_named(words[0]) if words else None
    if child_shell is None:
        return False
    at = next((i for i, w in enumerate(words[1:-1], 1) if w.lower() in _SHELL_PAYLOAD_FLAGS), None)
    if at is None:
        return False
    payload = " ".join(words[at + 1:]) if child_shell == POWERSHELL else words[at + 1]
    child = _State(_workdir(words[:at], state.cwd), None, [], {})
    before = len(out)
    _walk(payload, child, child_shell, depth + 1, out)
    if len(out) > before:
        last = out[-1]
        out[-1] = dataclasses.replace(last, sep=sep)
    return True


def steps(command: str, cwd: str | None, shell: str = BASH) -> list[Step]:
    """Every simple command of `command`, in order, with the directory it runs in. `cwd` is the payload's cwd."""
    out: list[Step] = []
    _walk(command or "", _State(cwd or None, None, [], {}), shell, 0, out)
    return out


def location_targets(command: str, shell: str = BASH) -> list[str]:
    """The target of every location change in `command`, as written after substitution (forbidden_commands.py asks
    whether any of them leaves this repository). `-WorkingDirectory` arguments count; a bare `cd` (home) does not."""
    targets = []
    for s in steps(command, None, shell):
        if s.location:
            targets.append(s.location)
        if (arg := _workdir_arg(s.words)) is not None:
            targets.append(arg)
    return targets


def git_dir(step: Step) -> str | None:
    """The directory a git command acts on: its `-C <dir>` options applied in order to the step's cwd."""
    words = step.words
    if not words or words[0].lower() not in ("git", "git.exe"):
        return step.cwd
    cwd = step.cwd
    i = 1
    while i + 1 < len(words) and words[i] == "-C":
        cwd = resolve(words[i + 1], cwd)
        i += 2
    return cwd
