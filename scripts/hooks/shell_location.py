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
               changes end at its `)`; `bash|sh -c '…'` runs its payload in a child (its changes never persist), and
               so does a heredoc fed to a shell with no script operand (`bash <<'EOF' … EOF`, `pwsh -Command - <<EOF`).
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
  cmd.exe      (a `cmd /c "…"` payload) `cd [/d] dir`, `chdir`, `pushd`, `popd`; `&` runs the next command after it.

It is also the ONE model of a command line that every guard rule reads (kb/Work PB3198): a step's `text` and `words` are
its command without the BODIES of the heredocs it introduces (a body is cut at its own delimiter line, never at the first
`<<`, and a `<<` inside quotes or `$(( … ))` is no heredoc), the bodies ride beside it in `heredocs`, quoted text is one
word, and `git_calls` reads every `git` invocation of a step (its global options, the directory its `-C` options name,
its subcommand). EVERY place either shell takes a command line from is walked as the command line it is:
  - a command substitution, in a word or inside double quotes: Bash `$( … )`, a backquote, `<( … )`, `>( … )`;
    PowerShell `$( … )`, `@( … )`, a `( … )` expression and a `{ … }` script block. Its steps come BEFORE the step
    whose text holds it (they run first) and carry `substitution`, how many substitutions enclose them (a step of
    the line itself is 0, a substitution's 1, one inside that 2); the step's own `words` keep it as one word;
  - an interpreter handed a string: `bash|sh -c '…'` (and `-lc`, `-ec` …), `pwsh -Command|-c|-EncodedCommand …`,
    `cmd /c|/k …`, Bash `eval …`, PowerShell `Invoke-Expression|iex …`, also behind a command that runs another
    (`timeout 5 bash -c …`, `env X=1 sh -c …`, `xargs sh -c …`, `wsl -e bash -c …`, PowerShell's `&`);
  - a shell reading its script from stdin (no script operand, `-s`, `-Command -`, `-File -`) and `iex`: stdin is the
    heredocs fed to it, its here-string (`bash <<< "…"`), or what the pipeline stage before it writes when the line
    spells that (`echo|printf|Write-Output` of words, `cat` of a heredoc or here-string, a PowerShell literal string),
    or the stdin a group reading a pipe, a Bash substitution or a `-c` payload inherits from the command holding it.
A rule that scans the raw text instead sees a heredoc body as commands, or a command after a heredoc as body: the hole
PB3198 closed in four rules at once; a model that skips any of those places hides the commands inside it from every rule
(the branch review of PB3198, R1-1 and R1-2: a stash or a push inside `$( … )`, `eval "…"`, `cmd /c "…"` or `| bash`).

A directory the line does not determine (a target spelled with an unknown variable or a command substitution, `popd`
with nothing pushed in this call) is `None`, and so is every later step of that shell until an absolute location
change: a caller treats it as UNKNOWN, never as the payload's cwd. Paths: an MSYS drive path (`/e/x`, `/cygdrive/e/x`) is `E:/x` on Windows (PB474);
a path absolute on EITHER platform is kept as written; anything else is joined to the directory in effect.

Self-test: `python scripts/hooks/test_shell_location.py` (the gate's audits, CI's audits job, the Linux gate's hooks leg).
"""
from __future__ import annotations

import base64
import dataclasses
import os
import pathlib
import re

BASH, POWERSHELL, CMD = "bash", "powershell", "cmd"

_BASH_LOCATION = {"cd", "pushd", "popd"}
_CMD_LOCATION = {"cd", "chdir", "pushd", "popd"}
_PS_LOCATION = {"set-location", "sl", "cd", "chdir", "push-location", "pushd", "pop-location", "popd"}
_PUSH = {"pushd", "push-location"}
_POP = {"popd", "pop-location"}
_PS_PATH_PARAMS = {"-path", "-literalpath", "-lp", "-pspath"}
_PS_SWITCHES = {"-passthru"}
_PS_VALUE_PARAMS = {"-stackname"}
_SHELLS = {"bash": BASH, "sh": BASH, "zsh": BASH, "dash": BASH, "pwsh": POWERSHELL, "powershell": POWERSHELL,
           "cmd": CMD}
# the commands that run their command line in THIS shell: Bash's `eval`, PowerShell's `Invoke-Expression`
_EVAL = {"eval": BASH, "invoke-expression": POWERSHELL, "iex": POWERSHELL}
# commands that run ANOTHER command named among their words (`timeout 5 bash -c …`, `xargs sh -c …`, `wsl -e bash -c …`,
# PowerShell's call operator `& pwsh -c …`): the interpreter is the first later word that names one (`_runs`)
_PREFIX_COMMANDS = {"timeout", "env", "sudo", "doas", "nohup", "nice", "ionice", "time", "xargs", "exec", "command",
                    "builtin", "stdbuf", "watch", "find", "wsl", "&"}
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
    substitution: int = 0        # how many command substitutions, `<( … )`s or PowerShell expressions / script blocks
                                 # enclose the step (0 = none): such a step runs BEFORE the step whose text holds it (the
                                 # next step at a LOWER level), and its output is that step's text, not a command of the
                                 # line; the steps of one level that follow each other are that level's own command line
                                 # (`$(a | b)`: b, at a's level, reads a; a step of b's own substitutions is one deeper)


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
    """`target` (a location-change or `-C` argument, already substituted) as a directory, or None when undetermined: a
    variable (`$X`, cmd's `%X%`), a substitution, or a PowerShell expression or script block (`(Join-Path …)`)."""
    if not target or any(ch in target for ch in "$`%") or target.startswith(("(", "@(", "{")):
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
_ESCAPE = {BASH: "\\", POWERSHELL: "`", CMD: "^"}


def _opener(text: str, i: int, shell: str, quoted: bool) -> tuple[int, str] | None:
    """At `text[i]`, a construct whose inside is a command line: (where the inside starts, the text that closes it), else
    None. Bash `$( … )`, a backquote, `<( … )`, `>( … )` (inside double quotes only `$(` and the backquote); PowerShell
    `$( … )`, `@( … )` and, outside double quotes, a `( … )` expression or a `{ … }` script block (`${name}` is a
    variable). `$(( … ))` is arithmetic, never a command line; cmd.exe has none."""
    if shell == BASH:
        if text.startswith("$(", i) and not text.startswith("$((", i):
            return i + 2, ")"
        if text[i] == "`":
            return i + 1, "`"
        if not quoted and text.startswith(("<(", ">("), i):
            return i + 2, ")"
    elif shell == POWERSHELL:
        if text.startswith(("$(", "@("), i):
            return i + 2, ")"
        if not quoted and text[i] == "(":
            return i + 1, ")"
        if not quoted and text[i] == "{" and not (i and text[i - 1] == "$"):
            return i + 1, "}"
    return None


def _substitution(text: str, i: int, shell: str, quoted: bool) -> tuple[str, int] | None:
    """The substitution (`_opener`) that starts at `text[i]`: (its inside, the index after its closer), else None. Its
    end is found by the scanner itself, so a quote, a bracket or a heredoc body inside it never ends it early."""
    opened = _opener(text, i, shell, quoted)
    if opened is None:
        return None
    start, closer = opened
    _, stop = _split(text, shell, start, closer)
    return text[start:stop], (stop + len(closer) if stop < len(text) else stop)


def _arithmetic_end(text: str, i: int) -> int:
    """The index after the `(( … ))` that starts at `text[i]`: kept whole, so its `<<` is a shift, never a heredoc."""
    j, d = i, 0
    while j < len(text):
        d += {"(": 1, ")": -1}.get(text[j], 0)
        if d == 0:
            break
        j += 1
    return min(j + 1, len(text))


def _split(command: str, shell: str, start: int = 0, closer: str | None = None
           ) -> tuple[list[tuple[str, str, tuple[str, ...], tuple[str, ...]]], int]:
    """The commands of `command` from `start`, each with the separator after it, the bodies of the heredocs it introduces
    and the insides of the substitutions it holds (`_opener`), in order; and the index where the scan stopped: at
    `closer` (the text that ends a substitution, found at bracket depth 0) or at the end. Quotes, escapes, brackets,
    substitutions and here-strings are kept inside the command they belong to; a heredoc body is cut out of the text at
    its own delimiter line and carried beside its command (a body is data, never a command)."""
    out: list[tuple[str, str]] = []
    buf: list[str] = []
    subs: list[str] = []                    # the substitutions of the command being read
    i, n, depth = start, len(command), 0
    esc = _ESCAPE[shell]
    heredocs: list[tuple[str, int]] = []   # (delimiter, index in `out` of its command): bodies due at the next newline
    bodies: dict[int, list[str]] = {}       # index in `out` -> the bodies of that command's heredocs, in order
    inners: dict[int, list[str]] = {}       # index in `out` -> that command's substitutions, in order

    def flush(sep: str) -> None:
        text = "".join(buf).strip()
        if text:
            out.append((text, sep))
            inners[len(out) - 1] = list(subs)
        elif out and sep and not out[-1][1]:
            out[-1] = (out[-1][0], sep)
        buf.clear()
        subs.clear()

    def substitution(at: int, quoted: bool) -> int | None:
        found = _substitution(command, at, shell, quoted)
        if found is None:
            return None
        subs.append(found[0])
        return found[1]

    while i < n:
        c = command[i]
        if c == esc and i + 1 < n:
            buf.append(command[i:i + 2]); i += 2; continue
        if closer is not None and depth == 0 and command.startswith(closer, i):
            break
        if shell == POWERSHELL and c == "@" and command[i + 1:i + 2] in ("'", '"') and command[i + 2:i + 3] in ("\n", "\r"):
            end = command.find("\n" + command[i + 1] + "@", i + 2)
            end = n if end < 0 else end + 3
            buf.append(command[i:end]); i = end; continue
        if c == "'" and shell != CMD:
            j = command.find("'", i + 1)
            j = n if j < 0 else j + 1
            buf.append(command[i:j]); i = j; continue
        if c == '"':
            j = i + 1
            while j < n and command[j] != '"':
                if command[j] == esc and shell != CMD:
                    j += 2
                    continue
                end = substitution(j, True)
                j = j + 1 if end is None else end
            j = min(j + 1, n)
            buf.append(command[i:j]); i = j; continue
        if (c == "#" and shell != CMD and depth == 0
                and (not buf or buf[-1][-1:].isspace() or buf[-1][-1:] in ";|&(")):
            j = command.find("\n", i)
            i = n if j < 0 else j
            continue
        if shell == BASH and command.startswith("((", i):
            # arithmetic, `$(( … ))` or `(( … ))`: kept whole, so its `<<` is a shift and never opens a heredoc
            j = _arithmetic_end(command, i)
            buf.append(command[i:j]); i = j; continue
        if shell == BASH and command.startswith("<<", i) and not command.startswith("<<<", i):
            m = _HEREDOC.match(command, i)
            if m:
                heredocs.append((m.group(2) or m.group(3), len(out)))
                buf.append(m.group(0)); i = m.end(); continue
        end = substitution(i, False)
        if end is not None:
            buf.append(command[i:end]); i = end; continue
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
            # `&` that STARTS a command is the call operator (`; & pwsh -File x.ps1`); in cmd.exe it runs the next
            # command after this one, as `;` does.
            if (c == "&" and not (buf and buf[-1][-1:] in "<>") and command[i + 1:i + 2] != ">"
                    and (shell != POWERSHELL or "".join(buf).strip())):
                flush(";" if shell == CMD else "&"); i += 1; continue
        buf.append(c)
        i += 1
    stop = min(i, n)
    flush("")
    return ([(text, sep, tuple(bodies.get(k, ())), tuple(inners.get(k, ()))) for k, (text, sep) in enumerate(out)],
            stop)


def _here_string(text: str, i: int) -> tuple[str, int] | None:
    """The PowerShell here-string (`@'` or `@"` ending its line, closed by `'@`/`"@` at a line start) at `text[i]`: (its
    content, the index after it), else None."""
    if not (text[i] == "@" and text[i + 1:i + 2] in ("'", '"') and text[i + 2:i + 3] in ("\n", "\r")):
        return None
    start = i + 4 if text.startswith("\r\n", i + 2) else i + 3
    end = text.find("\n" + text[i + 1] + "@", start - 1)
    if end < 0:
        return text[start:], len(text)
    return text[start:end].removesuffix("\r"), end + 3


def _words(text: str, shell: str) -> list[str]:
    """The words of one command, quotes removed (Bash `\\x` is `x` outside single quotes; PowerShell keeps backslashes;
    cmd.exe's `^x` is `x` outside double quotes). A substitution and an arithmetic expansion stay whole, inside the word
    they stand in (`x=$(git rev-parse HEAD)` is one word); a PowerShell here-string is one word, its content."""
    words: list[str] = []
    cur: list[str] = []
    have = False
    i, n = 0, len(text)

    def construct_end(at: int, quoted: bool) -> int | None:
        if shell == BASH and text.startswith("((", at):
            return _arithmetic_end(text, at)
        found = _substitution(text, at, shell, quoted)
        return None if found is None else found[1]

    while i < n:
        c = text[i]
        if c.isspace():
            if have:
                words.append("".join(cur)); cur.clear(); have = False
            i += 1; continue
        have = True
        if shell == POWERSHELL and (here := _here_string(text, i)) is not None:
            cur.append(here[0]); i = here[1]; continue
        if c == "'" and shell != CMD:
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
                if shell != CMD and (end := construct_end(j, True)) is not None:
                    cur.append(text[j:end]); j = end; continue
                cur.append(text[j]); j += 1
            i = j + 1; continue
        if shell == BASH and c == "\\" and i + 1 < n:
            cur.append(text[i + 1]); i += 2; continue
        if shell == CMD and c == "^" and i + 1 < n:
            cur.append(text[i + 1]); i += 2; continue
        if (end := construct_end(i, False)) is not None:
            cur.append(text[i:end]); i = end; continue
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
    esc = _ESCAPE[shell]
    quotes = '"' if shell == CMD else "'\""
    depth, i, n = 0, 0, len(text)
    while i < n:
        c = text[i]
        if c == esc:
            i += 2
            continue
        if c in quotes:
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


def _walk(command: str, state: _State, shell: str, depth: int, out: list[Step], stdin: tuple[str, ...] = ()) -> None:
    """The steps of `command` appended to `out`. `stdin` is what the walk's OWN stdin carries, when the line spells it
    (`_output`): a group that is the read side of a `|` (`echo x | ( cd X; bash )`), a Bash substitution and a `-c`
    payload inherit the stdin of the command that holds them. Every command of the walk reads it except the read side
    of a `|` of the walk itself, which reads the stage before it. Inheriting a stdin never makes a command a pipeline
    side: that is the enclosing group's, so a `cd` inside the group still moves the group's later steps (the branch
    review of PB3198, R3-1)."""
    began: _State | None = None        # the shell's state when the current and-or list began
    stranded: tuple[str, ...] = ()     # a backgrounded list's location changes, for the next step
    piped: tuple[str, ...] | None = None  # when a `|` of THIS walk precedes the next command: what that stage writes
    for text, sep, heredocs, subs in _split(command, shell)[0]:
        if began is None:
            began = state.copy()
        first = len(out)
        _command(text, sep, heredocs, subs, state, shell, depth, out, stdin if piped is None else piped, piped is not None)
        # the next stage reads this command's last step: a group's last step (`( echo x ) | bash`), never a step of its
        # substitutions (those come before it)
        piped = (_output(out[-1], shell) if len(out) > first else ()) if sep == "|" else None
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


def _command(text: str, sep: str, heredocs: tuple[str, ...], subs: tuple[str, ...], state: _State, shell: str,
             depth: int, out: list[Step], fed: tuple[str, ...], reads_pipe: bool) -> None:
    """One top-level command of a `_walk`: its steps appended to `out`, its location change applied to `state`. `fed` is
    what its stdin carries: the stage before it when `reads_pipe` (a `|` of its OWN walk precedes it), else the walk's
    stdin. Each side of a Bash pipeline runs in a subshell, so a command that writes to or reads from a `|` of its walk
    moves nothing; a background `&` is the whole list's, undone by `_walk`."""
    transient = shell == BASH and (sep == "|" or reads_pipe)
    grouped = _group(text, shell)
    if grouped is not None:
        bracket, inner = grouped
        # A Bash `( … )` is a subshell: its location changes end at the `)`. A `{ … }` group, and every PowerShell or
        # cmd.exe bracket (location is per runspace / process), runs in the current shell, so its changes persist,
        # except a Bash `{ … }` that is a pipeline side (a subshell like any other side).
        # Its substitutions are its own commands', found again by this walk.
        child = state.copy()
        before = len(out)
        _walk(inner, child, shell, depth + 1, out, fed)
        if len(out) > before:
            out[-1] = dataclasses.replace(out[-1], sep=sep)
        if not (shell == BASH and (bracket == "(" or transient)):
            state.adopt(child)
        return
    # its substitutions run first: Bash's in a subshell that inherits the command's stdin (a word is expanded before the
    # command's redirections apply, so a heredoc of the command is not the substitution's); a PowerShell expression or
    # script block in this runspace, so one that moves the location leaves the directory undetermined (a script block
    # may or may not have run), and its pipeline input (`$input`, `$_`) is no stdin this model spells
    for inner in subs:
        child = state.copy()
        before = len(out)
        _walk(inner, child, shell, depth + 1, out, fed if shell == BASH else ())
        out[before:] = [dataclasses.replace(s, substitution=s.substitution + 1) for s in out[before:]]
        if shell == POWERSHELL and len(child.moved) > len(state.moved):
            state.cwd = None
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
    stdin = heredocs + _here_strings(words, shell) + fed
    if _interpreter(words, state, shell, depth, sep, heredocs, stdin, out):
        return
    location = _location(words, shell, state.cwd)
    if location is not None:
        verb, target = location
        out.append(Step(text, words, state.cwd, sep, depth, target if target is not None else "",
                        heredocs=heredocs))
        if not transient:
            _move(state, verb, target)
        return
    out.append(Step(text, words, _workdir(words, state.cwd), sep, depth, heredocs=heredocs))


def _location(words: tuple[str, ...], shell: str, cwd: str | None) -> tuple[str, str | None] | None:
    """(verb, target) when the command changes the location; target None = no argument (home, or a pop)."""
    if not words:
        return None
    verb = words[0] if shell == BASH else words[0].lower()
    if verb not in {BASH: _BASH_LOCATION, POWERSHELL: _PS_LOCATION, CMD: _CMD_LOCATION}[shell]:
        return None
    args = list(words[1:])
    target = None
    drive_switch = False
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
        if shell == CMD and low == "/d":
            drive_switch = True
            i += 1
            continue
        if target is None:
            target = a
        i += 1
    if shell == CMD and verb in ("cd", "chdir"):
        # cmd.exe's bare `cd` prints the directory; without `/d`, a `cd` to another drive sets THAT drive's directory
        # and leaves the current one where it is
        if target is None:
            return None
        there, here = pathlib.PureWindowsPath(target).drive, pathlib.PureWindowsPath(native_path(cwd or "")).drive
        if not drive_switch and there and here and there.lower() != here.lower():
            return None
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
    """The shell a command word starts (`bash`, `/usr/bin/sh`, `pwsh.exe`, `cmd` …), or None when it is not a shell."""
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


def _reads_stdin_script(words: tuple[str, ...], shell: str) -> bool:
    """A shell called with no script operand (`bash`, `bash -s args`, `sh -e`, `pwsh -NoProfile`, `cmd /q`) reads its
    commands from stdin, so a heredoc, a here-string or a pipe fed to it is a command line, never data. A first operand
    that is not an option (or an option's value, or a redirection) is a script file, and then stdin is that script's
    input."""
    i = 1
    while i < len(words):
        w, low = words[i], words[i].lower()
        if re.match(r"^[0-9&]*(<<?-?|>>?|<>)", w):        # a redirection; a bare operator's target is the next word
            i += 2 if re.fullmatch(r"[0-9&]*(<<<|<<-?|<|>>?|<>)&?", w) else 1
            continue
        if shell == CMD:
            if not w.startswith("/"):
                return False
            i += 1
            continue
        if shell == BASH and w == "-s":
            return True                                  # `-s`: commands from stdin, the rest are its arguments
        if shell == BASH and w in ("-o", "+o", "-O", "+O", "--rcfile", "--init-file"):
            i += 2
            continue
        if shell == POWERSHELL and low in ("-file", "-f"):
            return i + 1 < len(words) and words[i + 1] == "-"   # `-File -` reads stdin; `-File x.ps1` runs x.ps1
        if shell == POWERSHELL and low in _PS_SHELL_VALUE_OPTIONS:
            i += 2
            continue
        if w.startswith(("-", "+")) and w not in ("-", "--"):
            i += 1
            continue
        return False
    return True


# pwsh's command-line options that take their value as the next word (`pwsh -?` lists them)
_PS_SHELL_VALUE_OPTIONS = {"-executionpolicy", "-ep", "-ex", "-workingdirectory", "-wd", "-configurationname", "-config",
                           "-settingsfile", "-settings", "-outputformat", "-of", "-o", "-inputformat", "-if", "-inp",
                           "-windowstyle", "-w", "-custompipename"}


def _payload_flag(word: str, shell: str) -> str | None:
    """How a shell's option hands it a command line: "text" (Bash `-c` alone or among single-letter options, `-lc`,
    `-ec`; pwsh `-Command` and its prefixes from `-comm`, `-c`, `-CommandWithArgs`/`-cwa`; cmd.exe `/c`, `/k`, and Git
    Bash's `//c`), "encoded" (pwsh `-EncodedCommand`, `-e`, `-ec`, prefixes from `-enc`), else None. Bash's options are
    case-sensitive (`-C` is noclobber), pwsh's and cmd.exe's are not."""
    if shell == BASH:
        return "text" if re.fullmatch(r"-[A-Za-z]*c[A-Za-z]*", word) else None
    if shell == CMD:
        return "text" if re.fullmatch(r"//?[ck]", word, re.I) else None
    low = word.lower()
    if low in ("-c", "-cwa") or (len(low) >= 5 and ("-command".startswith(low) or "-commandwithargs".startswith(low))):
        return "text"
    if low in ("-e", "-ec") or (len(low) >= 4 and "-encodedcommand".startswith(low)):
        return "encoded"
    return None


def _decoded(word: str) -> str | None:
    """pwsh's `-EncodedCommand` value: Base64 of the UTF-16LE command line; None when it is not one."""
    try:
        return base64.b64decode(word, validate=True).decode("utf-16-le")
    except (ValueError, UnicodeDecodeError):
        return None


def _runs(words: tuple[str, ...], shell: str) -> int:
    """The index of the word naming the program a step runs: past Bash's `NAME=value` prefixes, and through a command
    that runs another (`_PREFIX_COMMANDS`: `timeout 5 bash -c …`, `xargs sh -c …`, PowerShell's `& pwsh …`) to the
    first later word that names a shell or an eval."""
    k = 0
    if shell == BASH:
        while k < len(words) and _ASSIGN_BASH.match(words[k]):
            k += 1
    if k < len(words) and program(words[k]) in _PREFIX_COMMANDS:
        later = next((j for j in range(k + 1, len(words)) if program(words[j]) in _SHELLS or program(words[j]) in _EVAL),
                     None)
        if later is not None:
            return later
    return k


def _here_strings(words: tuple[str, ...], shell: str) -> tuple[str, ...]:
    """The text each Bash here-string of the command (`<<< word`, `<<<word`) feeds its stdin."""
    if shell != BASH:
        return ()
    found = []
    for k, w in enumerate(words):
        m = re.fullmatch(r"[0-9]*<<<(.*)", w, re.S)
        if m and (m.group(1) or k + 1 < len(words)):
            found.append(m.group(1) or words[k + 1])
    return tuple(found)


def _output(step: Step, shell: str) -> tuple[str, ...]:
    """What a pipeline stage writes when the line spells it: `cat` of its heredocs or here-string, `echo`/`printf`/
    `Write-Output` of its words (a `\\n` in Bash's or cmd.exe's read as a line end), a PowerShell literal string;
    () when the line does not determine it (`cat file`, a program's output)."""
    w = step.words
    if not w:
        return ()
    name = program(w[0])
    if name == "cat":
        return step.heredocs + _here_strings(w, shell)
    if shell == POWERSHELL:
        if name in ("echo", "write-output", "write"):
            return ("\n".join(w[1:]),) if len(w) > 1 else ()
        return (w[0],) if len(w) == 1 and step.text[:1] in ("'", '"', "@") else ()
    if name in ("echo", "printf"):
        args = list(w[1:])
        while name == "echo" and shell == BASH and args and re.fullmatch(r"-[neE]+", args[0]):
            args.pop(0)
        return (" ".join(args).replace("\\n", "\n"),) if args else ()
    return ()


def _interpreter(words: tuple[str, ...], state: _State, shell: str, depth: int, sep: str, heredocs: tuple[str, ...],
                 stdin: tuple[str, ...], out: list[Step]) -> bool:
    """A command that hands a command line to an interpreter: walk that command line, in place of the command. `bash -c
    '…'`, `pwsh [-WorkingDirectory dir] -Command "…"` / `-EncodedCommand …`, `cmd /c …`, and a shell reading its script
    from `stdin` (`bash <<'EOF'`, `echo '…' | bash`, `pwsh -Command -`) run it in a CHILD shell, whose location changes
    never come back; Bash's `eval …` and PowerShell's `Invoke-Expression …` (or `… | iex`) run it in THIS one. True
    when `words` was such a call."""
    call = words[_runs(words, shell):]
    if not call:
        return False
    name = program(call[0])
    if _EVAL.get(name) == shell:
        args = [w for w in call[1:] if not (shell == POWERSHELL and w.lower() == "-command")]
        payloads = [" ".join(args)] if args else list(stdin) if shell == POWERSHELL else []
        inherited = stdin if args else ()
        child_shell, run_in = shell, (lambda: state)
    else:
        child_shell = _SHELLS.get(name)
        if child_shell is None:
            return False
        at = next((i for i, w in enumerate(call[1:-1], 1) if _payload_flag(w, child_shell)), None)
        if at is not None and call[at + 1] != "-":
            if _payload_flag(call[at], child_shell) == "encoded":
                payloads = [p for p in (_decoded(call[at + 1]),) if p]
            else:
                payloads = [call[at + 1] if child_shell == BASH else " ".join(call[at + 1:])]
            inherited = stdin          # `echo 'git stash' | bash -c 'cd X; bash'`: the payload reads the same stdin
            workdir = _workdir(call[:at], state.cwd)
        elif stdin and (at is not None or _reads_stdin_script(call, child_shell)):
            payloads = list(stdin)     # stdin IS the script (`-Command -` names stdin): each text is a command line
            inherited = ()             # ... so the script consumed it
            workdir = _workdir(call, state.cwd)
        else:
            return False
        run_in = lambda: _State(workdir, None, [], {})  # noqa: E731 — a fresh child shell per payload
    if not payloads:
        return False
    before = len(out)
    for payload in payloads:
        _walk(payload, run_in(), child_shell, depth + 1, out, inherited)
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
