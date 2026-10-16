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
  Both         separators `&&`, `||`, `;`, `|` and newlines; a single `&` (Bash, and PowerShell 7's background
               operator after a command) ends an and-or list and runs THE WHOLE LIST in a background subshell or job,
               so no location change in that list persists: `cd X && a & b` runs `a` under X and `b` in the caller's
               directory (kb/Work PB3095; the step after it carries the list's targets in `stranded`); quotes, escapes,
               comments, `$( … )`, `{ … }`, heredoc bodies and PowerShell here-strings never split a command; `~` is the home dir; a variable set earlier in the SAME call to a literal
               (`WT=/e/x`, `$wt = 'E:\\x'`) is substituted.

It is also the ONE model of a command line that every guard rule reads (kb/Work PB3198): a step's `text` and `words` are
its command without the BODIES of the heredocs it introduces (a body is cut at its own delimiter line, never at the first
`<<`, and a `<<` inside quotes or `$(( … ))` is no heredoc), the bodies ride beside it in `heredocs`, quoted text is one
word, a `-c`/`-Command` payload is walked as the command line it is, and `git_calls` reads every `git` invocation of a
step (its global options, the directory its `-C` options name, its subcommand). A rule that scans the raw text instead
sees a heredoc body as commands, or a command after a heredoc as body: the hole PB3198 closed in four rules at once.

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
    text: str                    # as written, WITHOUT the bodies of the heredocs it introduces (those are `heredocs`)
    words: tuple[str, ...]       # its words, quotes removed, known variables substituted (no heredoc body's words)
    cwd: str | None              # the directory it runs in; None when the line does not determine it
    sep: str                     # the separator that FOLLOWS it ("" at the end): && || ; | & or a newline; the last
                                 # step of a group or a `-c` payload carries the separator that follows the group
    depth: int                   # 0 = the tool call's own command line; >0 = inside a subshell, group or `-c` payload
    location: str | None = None  # a location change's target as written (after substitution); "" for one with no
                                 # argument (`cd` = home, `popd`); None when the step is not a location change
    stranded: tuple[str, ...] = ()  # on the FIRST step after an and-or list a single `&` sent to the background: that
                                 # list's own location changes (as `location` spells them), which moved only the
                                 # background job and do NOT apply here (forbidden_commands.py rule 7, kb/Work PB3095)
    heredocs: tuple[str, ...] = ()  # the bodies of the Bash heredocs (`<<DELIM`, `<<-DELIM`) the command introduces, in
                                 # order, each without its delimiter line; a `-c` payload's last step carries the
                                 # bodies that feed the payload (`bash -c '…' <<EOF`)


@dataclasses.dataclass(frozen=True)
class GitCall:
    """One `git` invocation among a step's words (`git_calls`)."""
    cwd: str | None              # the directory it acts on: the step's cwd with its `-C` options applied in order
    options: tuple[str, ...]     # git's global options before the subcommand, as written (`-c`'s `name=value` too)
    subcommand: str | None       # None when no subcommand follows
    args: tuple[str, ...]        # every word after the subcommand


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


# A Bash heredoc operator: `<<` or `<<-`, then its delimiter word, quoted (`'EOF'`, `"EOF"`), backslash-quoted (`\EOF`) or
# bare; a bare one starts with a letter or `_`, so `1<<2` outside `$(( … ))` is not read as a heredoc.
_HEREDOC = re.compile(r"<<-?[ \t]*(?:(['\"])([^'\"\n]+)\1|\\?([A-Za-z_][A-Za-z0-9_.-]*))")


def _split(command: str, shell: str) -> list[tuple[str, str, tuple[str, ...]]]:
    """The tool call's top-level commands, each with the separator after it and the bodies of the heredocs it
    introduces. Quotes, escapes, brackets and here-strings are kept inside the command they belong to; a heredoc body is
    cut out of the text at its own delimiter line and carried beside its command (a body is data, never a command)."""
    out: list[tuple[str, str]] = []
    buf: list[str] = []
    i, n, depth = 0, len(command), 0
    esc = "\\" if shell == BASH else "`"
    heredocs: list[tuple[str, int]] = []   # (delimiter, index in `out` of its command): bodies due at the next newline
    bodies: dict[int, list[str]] = {}       # index in `out` -> the bodies of that command's heredocs, in order

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
            m = _HEREDOC.match(command, i)
            if m:
                heredocs.append((m.group(2) or m.group(3), len(out)))
                buf.append(m.group(0)); i = m.end(); continue
        if c in "({":
            depth += 1
        elif c in ")}" and depth:
            depth -= 1
        if c in "\r\n" and heredocs:
            # the bodies follow this newline, one after another, each ended by its delimiter on a line alone (to the end
            # of the line when no such line comes, as Bash reads it). A top-level command's bodies are cut out of its
            # text; inside brackets they stay in the text, whole, for the walk of the group to cut (an apostrophe in a
            # body never opens a quote either way).
            j = i + 2 if command.startswith("\r\n", i) else i + 1
            for k, (delim, owner) in enumerate(heredocs):
                if k:   # the next body starts on the line after the previous delimiter
                    j += 2 if command.startswith("\r\n", j) else 1 if command.startswith("\n", j) else 0
                m = re.compile(r"^[ \t]*" + re.escape(delim) + r"[ \t]*\r?$", re.M).search(command, j)
                if depth == 0:
                    bodies.setdefault(owner, []).append(command[j:m.start() if m else n])
                j = n if not m else m.end()
            if depth:
                buf.append(command[i:j])
            i = j
            heredocs.clear()
            continue
        if depth == 0:
            if c in "\r\n":
                flush("\n"); i += 1; continue
            two = command[i:i + 2]
            if two in ("&&", "||"):
                flush(two); i += 2; continue
            if c == ";":
                flush(";"); i += 1; continue
            if c == "|":
                flush("|"); i += 1; continue
            # A single `&` is the background operator, never in a redirection (`2>&1`, `*>&1`, `&>`); in PowerShell a
            # `&` that STARTS a command is the call operator (`; & pwsh -File x.ps1`).
            if (c == "&" and not (buf and buf[-1][-1:] in "<>") and command[i + 1:i + 2] != ">"
                    and (shell == BASH or "".join(buf).strip())):
                flush("&"); i += 1; continue
        buf.append(c)
        i += 1
    flush("")
    return [(text, sep, tuple(bodies.get(k, ()))) for k, (text, sep) in enumerate(out)]


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
    moved: list[str] = dataclasses.field(default_factory=list)  # every location change that moved THIS shell, as
                                                                 # `Step.location` spells it, in order

    def copy(self) -> "_State":
        return _State(self.cwd, self.previous, list(self.stack), dict(self.variables), list(self.moved))

    def adopt(self, other: "_State") -> None:
        self.cwd, self.previous, self.stack, self.variables, self.moved = (
            other.cwd, other.previous, other.stack, other.variables, other.moved)


def _walk(command: str, state: _State, shell: str, depth: int, out: list[Step]) -> None:
    began: _State | None = None        # the shell's state when the current and-or list began
    stranded: tuple[str, ...] = ()     # a backgrounded list's location changes, for the next step
    for text, sep, heredocs in _split(command, shell):
        if began is None:
            began = state.copy()
        first = len(out)
        _command(text, sep, heredocs, state, shell, depth, out)
        if stranded and len(out) > first:
            out[first] = dataclasses.replace(out[first], stranded=stranded)
            stranded = ()
        if sep in ("&&", "||", "|"):
            continue
        if sep == "&":
            # A single `&` ends the and-or list and runs ALL of it in a background subshell (Bash) or job (PowerShell
            # 7): its location changes and variables never reach the calling shell (kb/Work PB3095).
            # The moves it strands are the ones that would have reached the shell without the `&` (a `{ … }` group's
            # too; a `( … )` subshell's, a pipeline side's or a `-c` payload's never would).
            stranded = tuple(state.moved[len(began.moved):])
            state.adopt(began)
        began = None


def _command(text: str, sep: str, heredocs: tuple[str, ...], state: _State, shell: str, depth: int,
             out: list[Step]) -> None:
    """One top-level command of a `_walk`: its steps appended to `out`, its location change applied to `state`."""
    grouped = _group(text, shell)
    if grouped is not None:
        bracket, inner = grouped
        # A Bash `( … )` is a subshell: its location changes end at the `)`. A `{ … }` group, and every PowerShell
        # bracket (location is per runspace), runs in the current shell, so its changes persist.
        child = state.copy()
        before = len(out)
        _walk(inner, child, shell, depth + 1, out)
        if len(out) > before:
            out[-1] = dataclasses.replace(out[-1], sep=sep)
        if not (shell == BASH and bracket == "("):
            state.adopt(child)
        return
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
    # each side of a Bash pipeline runs in a subshell, so its changes do not persist (a background `&` is the whole
    # list's, undone by `_walk`)
    transient = shell == BASH and (sep == "|" or bool(out and out[-1].depth == depth and out[-1].sep == "|"))
    if _shell_child(words, state, depth, sep, heredocs, out):
        return
    location = _location(words, shell)
    if location is not None:
        verb, target = location
        out.append(Step(text, words, state.cwd, sep, depth, target if target is not None else "",
                        heredocs=heredocs))
        if not transient:
            _move(state, verb, target)
        return
    out.append(Step(text, words, _workdir(words, state.cwd), sep, depth, heredocs=heredocs))


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
    state.moved.append(target or "")
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


def _shell_named(word: str) -> str | None:
    """The shell a command word starts (`bash`, `/usr/bin/sh`, `pwsh.exe` …), or None when it is not a shell."""
    return _SHELLS.get(program(word))


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


def _shell_child(words: tuple[str, ...], state: _State, depth: int, sep: str, heredocs: tuple[str, ...],
                 out: list[Step]) -> bool:
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
        out[-1] = dataclasses.replace(last, sep=sep, heredocs=last.heredocs + heredocs)
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


# git's global options that take their value as the NEXT word (`--git-dir=x` spells it in the same word)
_GIT_VALUE_OPTIONS = {"-C", "-c", "--git-dir", "--work-tree", "--namespace", "--config-env", "--super-prefix"}


def program(word: str) -> str:
    """The program a command word names, lower case, without its folder or `.exe` (`/usr/bin/git`, `git.exe` -> git)."""
    return re.sub(r"\.exe$", "", word.replace("\\", "/").rsplit("/", 1)[-1].lower())


def git_calls(step: Step) -> list[GitCall]:
    """Every `git` invocation among the step's words, wherever it stands (`git …`, `FOO=1 git …`, PowerShell's `& git …`,
    `xargs git …`): its global options (`-C dir`, `-c k=v`, `--no-pager`, `--git-dir=x` …), the directory its `-C`
    options name (applied in order to the step's cwd, as git applies them), its subcommand and that subcommand's words.
    Quoted text is one word, so a commit message that says "git push" is no call."""
    calls: list[GitCall] = []
    words = step.words
    for k, word in enumerate(words):
        if program(word) != "git":
            continue
        cwd, options, i = step.cwd, [], k + 1
        while i < len(words) and words[i].startswith("-") and words[i] != "-":
            if words[i] in _GIT_VALUE_OPTIONS and i + 1 < len(words):
                if words[i] == "-C":
                    cwd = resolve(words[i + 1], cwd)
                options += words[i:i + 2]
                i += 2
                continue
            options.append(words[i])
            i += 1
        calls.append(GitCall(cwd, tuple(options), words[i] if i < len(words) else None, tuple(words[i + 1:])))
    return calls


def git_dir(step: Step) -> str | None:
    """The directory a git command acts on: its first git call's `-C <dir>` options applied in order to the step's cwd
    (`git_calls`); the step's cwd when it calls no git."""
    calls = git_calls(step)
    return calls[0].cwd if calls else step.cwd
