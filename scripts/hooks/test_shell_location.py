#!/usr/bin/env python3
"""Self-test for shell_location.py — every location change either tool's shell accepts moves the commands after it, and
nothing else does (kb/Work PB2599).

The parser is shared by fleet_active_build.py, worktree_rm_allow.py, forbidden_commands.py and status_guard.py, so a case
that resolves to the wrong directory here is a false deny (a build refused in its own worktree), a false allow (a build of
a tree a fleet is probing) or a guard that checks the wrong tree. Paths are built from a temporary directory, so every
case means the same on Windows and on Linux. Run: python scripts/hooks/test_shell_location.py (the gate's audits, CI's
audits job and the Linux gate's hooks leg run it).
"""
import os
import pathlib
import sys
import tempfile

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import shell_location as sl  # noqa: E402

fails, total = [], 0


def same(a, b) -> bool:
    if a is None or b is None:
        return a is b
    return os.path.normcase(os.path.normpath(str(a))) == os.path.normcase(os.path.normpath(str(b)))


def check(name, got, want) -> None:
    global total
    total += 1
    ok = all(same(g, w) for g, w in zip(got, want)) and len(got) == len(want) if isinstance(want, list) else same(got, want)
    if not ok:
        fails.append(f"{name}: got {got!r}, want {want!r}")


def dirs_of(command: str, cwd, shell: str, verb: str = "dotnet") -> list:
    """The directory of every step that runs `verb` (a word of the step: `(Measure-Command { dotnet … })` runs it too)."""
    return [s.cwd for s in sl.steps(command, str(cwd), shell) if verb in s.words]


with tempfile.TemporaryDirectory() as tmp:
    root = pathlib.Path(tmp).resolve()
    main, wt, spaced = root / "main", root / "wt", root / "with space"
    home = pathlib.Path.home()
    W, M, S = wt.as_posix(), main.as_posix(), spaced.as_posix()
    Wn = str(wt)   # the host's own spelling (backslashes on Windows) for PowerShell
    B, P = sl.BASH, sl.POWERSHELL

    # ── Bash ─────────────────────────────────────────────────────────────────────────────────────────────────────
    for name, command, want in [
        ("leading cd &&", f"cd {W} && dotnet build", [wt]),
        ("leading cd ;", f"cd {W}; dotnet build", [wt]),
        ("cd after another command (2026-10-08 03:31 shape)", f"git worktree add -q --detach x abc && cd {W} && dotnet build", [wt]),
        ("cd after mkdir (06:06 shape)", f"mkdir -p {M}/x && cd {W} && dotnet build", [wt]),
        ("cd after ls ; (07:00 shape)", f"ls {M}/STOP; cd {W} && dotnet build", [wt]),
        ("quoted target with a space", f'cd "{S}" && dotnet build', [spaced]),
        ("single-quoted target", f"cd '{S}' && dotnet build", [spaced]),
        ("relative target", "cd sub && dotnet build", [main / "sub"]),
        ("no location change", "dotnet build Cobol.Net.sln > b.log 2>&1; echo EXIT=$?", [main]),
        ("cd with no argument is home", "cd && dotnet build", [home]),
        ("cd - returns", f"cd {W} && cd - && dotnet build", [main]),
        ("cd -P option", f"cd -P {W} && dotnet build", [wt]),
        ("pushd then popd", f"pushd {W} && dotnet build && popd && dotnet test", [wt, main]),
        ("a subshell's cd ends at its )", f"(cd {W} && dotnet build) && dotnet test", [wt, main]),
        ("a { } group's cd persists", f"{{ cd {W}; }} && dotnet build", [wt]),
        ("a background cd does not move the shell", f"cd {W} & dotnet build", [main]),
        # a single `&` backgrounds the WHOLE and-or list, so a `cd` joined to it by `&&` moves only that job (PB3095)
        ("a backgrounded list's cd ends at its &", f"cd {W} && git fetch & dotnet build", [main]),
        ("inside the backgrounded list the cd binds", f"cd {W} && dotnet build & git status", [wt]),
        ("a backgrounded { } group's cd ends at its &", f"{{ cd {W}; }} & dotnet build", [main]),
        ("a comment's apostrophe opens no quote (review R1-1)",
         f"# don't build in main\ncd {W} && git fetch & dotnet build", [main]),
        ("a cd in its own list moves the shell", f"cd {W}; git fetch & dotnet build", [wt]),
        ("a piped cd does not move the shell", f"cd {W} | cat; dotnet build", [main]),
        ("a variable set in the same call", f"WT={W}; cd $WT && dotnet build", [wt]),
        ("a braced variable", f"export WT={W}; cd ${{WT}}/ && dotnet build", [wt]),
        ("an unknown variable is UNKNOWN", "cd $NOWHERE && dotnet build", [None]),
        ("a command substitution is UNKNOWN", "cd $(git rev-parse --show-toplevel) && dotnet build", [None]),
        ("UNKNOWN sticks to the later commands", "cd $NOWHERE; ls; dotnet build", [None]),
        ("popd with nothing pushed is UNKNOWN", "popd; dotnet build", [None]),
        ("bash -c payload runs in a child", f"bash -lc 'cd {W} && dotnet build'; dotnet test", [wt, main]),
        ("a heredoc body is data, not a cd", f"cat > m.txt <<'EOF'\ncd {W}\nEOF\ndotnet build", [main]),
        ("a quoted cd is text", f'echo "cd {W}" && dotnet build', [main]),
        ("a comment is not a cd", f"# cd {W}\ndotnet build", [main]),
        ("2>&1 does not split", f"cd {W} 2>&1 && dotnet build", [wt]),
        ("newline separates", f"cd {W}\ndotnet build", [wt]),
    ]:
        check(f"bash: {name}", dirs_of(command, main, B), want)

    if os.name == "nt":
        drive, rest = os.path.splitdrive(str(wt))
        msys = "/" + drive[0].lower() + rest.replace(chr(92), "/")
        check("bash: an MSYS drive path (PB474)", dirs_of(f"cd {msys} && dotnet build", main, B), [wt])
        check("bash: a /cygdrive path (PB474)", dirs_of(f"cd /cygdrive{msys} && dotnet build", main, B), [wt])
    check("native_path leaves a POSIX path alone", sl.native_path("/tmp/x"), "/tmp/x")
    # The drive mapping is Windows-only: on Linux `/e` is a real POSIX directory and stays as spelled (kb/Work PB2142).
    if os.name == "nt":
        check("native_path maps a bare drive `/e` to the drive root", sl.native_path("/e"), "E:/")
    else:
        check("native_path leaves `/e` alone off Windows", sl.native_path("/e"), "/e")

    # ── PowerShell ───────────────────────────────────────────────────────────────────────────────────────────────
    for name, command, want in [
        ("Set-Location ; (2026-10-07 21:02 shape)", f"Set-Location {Wn}; dotnet build -c Debug", [wt]),
        ("Set-Location then a parenthesised script block (01:04 shape)",
         f"Set-Location {Wn}; (Measure-Command {{ dotnet publish x.csproj }}).TotalSeconds", [wt]),
        ("set-location in any case", f"set-location {Wn}; dotnet build", [wt]),
        ("sl alias", f"sl {Wn}; dotnet build", [wt]),
        ("cd alias", f"cd {Wn}; dotnet build", [wt]),
        ("chdir alias", f"chdir {Wn}; dotnet build", [wt]),
        ("-Path", f"Set-Location -Path '{Wn}'; dotnet build", [wt]),
        ("-LiteralPath with a space", f'Set-Location -LiteralPath "{spaced}"; dotnet build', [spaced]),
        ("-Path:value", f"Set-Location -Path:{Wn}; dotnet build", [wt]),
        ("&& in PowerShell 7", f"Set-Location {Wn} && dotnet build", [wt]),
        ("Push-Location / Pop-Location", f"Push-Location {Wn}; dotnet build; Pop-Location; dotnet test", [wt, main]),
        ("pushd / popd aliases", f"pushd {Wn}; dotnet build; popd; dotnet test", [wt, main]),
        ("a variable set in the same call", f"$wt = '{Wn}'; Set-Location $wt; dotnet build", [wt]),
        ("an unknown variable is UNKNOWN", "Set-Location $env:NOWHERE_PB2599; dotnet build", [None]),
        ("pwsh -WorkingDirectory -Command", f'pwsh -NoProfile -WorkingDirectory "{Wn}" -Command "dotnet build"; dotnet test', [wt, main]),
        ("pwsh -wd", f"pwsh -wd {Wn} -Command dotnet build", [wt]),
        ("a child's Set-Location does not come back",
         f'pwsh -Command "Set-Location {Wn}; dotnet build"; dotnet test', [wt, main]),
        ("& is the call operator, not a separator", f'Set-Location {Wn}; & dotnet build', [wt]),
        ("a backgrounded chain's Set-Location ends at its & (PB3095)", f"Set-Location {Wn} && git fetch & dotnet build",
         [main]),
        ("a here-string is data", f"$m = @'\nSet-Location {Wn}\n'@\ndotnet build", [main]),
        ("*> redirection does not split", f"Set-Location {Wn}; dotnet build *> b.log; \"EXIT=$LASTEXITCODE\"", [wt]),
    ]:
        check(f"powershell: {name}", dirs_of(command, main, P), want)
    started = [s.cwd for s in sl.steps(f"Start-Process dotnet -ArgumentList build -WorkingDirectory {Wn}", str(main), P)]
    check("powershell: Start-Process -WorkingDirectory runs that command there", started, [wt])

    # ── the other questions the hooks ask ────────────────────────────────────────────────────────────────────────
    check("shell_of: the PowerShell tool", sl.shell_of("PowerShell"), P)
    check("shell_of: the Bash tool", sl.shell_of("Bash"), B)
    check("location_targets: every target, raw", sl.location_targets(f"cd /e/x && git push; pushd '{S}'", B), ["/e/x", S])
    check("location_targets: PowerShell -Path and -WorkingDirectory",
          sl.location_targets(f"Set-Location -Path E:/a; Start-Process git -WorkingDirectory E:/b", P), ["E:/a", "E:/b"])
    check("location_targets: a bare cd names nothing", sl.location_targets("cd && git push", B), [])
    stranded = lambda cmd, shell=B: [s.stranded for s in sl.steps(cmd, str(main), shell) if s.stranded]
    check("stranded: the step after a backgrounded list's cd", stranded("cd /e/x && a & b &"), [("/e/x",)])
    check("stranded: nothing when the cd is its own list", stranded("cd /e/x; a & b"), [])
    check("stranded: nothing after a trailing &", stranded("cd /e/x && a &"), [])
    check("stranded: an escaped quote opens no quote (review R1-1)",
          stranded("echo it\\'s here && cd /e/x && a & b"), [("/e/x",)])
    check("stranded: a PowerShell 7 job", stranded("Set-Location E:/x && a & b", P), [("E:/x",)])
    commit = lambda cmd, shell=B: [sl.git_dir(s) for s in sl.steps(cmd, str(main), shell) if "commit" in s.words]
    check("git_dir: -C", commit(f"git -C {W} commit -m x"), [wt])
    check("git_dir: cd then -C relative", commit(f"cd {W} && git -C sub commit -m x"), [wt / "sub"])
    check("git_dir: Set-Location then commit", commit(f"Set-Location {Wn}; git commit -m x", P), [wt])
    check("git_dir: -c before -C (git_calls walks every global option)", commit(f"git -c a=b -C {W} commit -m x"), [wt])

    # ── the ONE model of a command line every guard rule reads (kb/Work PB3198) ────────────────────────────────────
    def exact(name, got, want) -> None:
        global total
        total += 1
        if got != want:
            fails.append(f"{name}: got {got!r}, want {want!r}")

    shape = lambda cmd, shell=B: [(s.text, s.heredocs) for s in sl.steps(cmd, str(main), shell)]
    exact("heredoc: the body is cut out of the text at its delimiter, the command after it is a step",
          shape("cat > m.txt <<'EOF'\ngit stash\nEOF\ngit status"),
          [("cat > m.txt <<'EOF'", ("git stash\n",)), ("git status", ())])
    exact("heredoc: two on one line, bodies in order, `<<-` and a backslash-quoted delimiter",
          shape("cat <<-A <<\\B\n\ta1\n\tA\nb1\nB\necho x"),
          [("cat <<-A <<\\B", ("\ta1\n", "b1\n")), ("echo x", ())])
    exact("heredoc: the body belongs to its command, not to the pipeline side after it",
          shape("cat <<EOF | grep y\ny\nEOF"), [("cat <<EOF", ("y\n",)), ("grep y", ())])
    exact("heredoc: no delimiter line means the body runs to the end, as Bash reads it",
          shape("cat <<EOF\nline\ngit push"), [("cat <<EOF", ("line\ngit push",))])
    exact("heredoc: CRLF line ends", shape("cat <<EOF\r\nx\r\nEOF\r\necho y"),
          [("cat <<EOF", ("x\r\n",)), ("echo y", ())])
    exact("no heredoc: `<<` inside quotes", shape('python -c "print(1<<2)"; echo x'),
          [('python -c "print(1<<2)"', ()), ("echo x", ())])
    exact("no heredoc: a here-string", shape('grep x <<< "a"; echo y'), [('grep x <<< "a"', ()), ("echo y", ())])
    exact("no heredoc: an arithmetic shift", shape("echo $((1<<2)); echo y"), [("echo $((1<<2))", ()), ("echo y", ())])
    exact("heredoc in a group: an apostrophe in its body opens no quote, the next line is its own step",
          [s.text for s in sl.steps("( cat <<EOF\ndon't\nEOF\n)\necho z", str(main), B)][-1], "echo z")
    exact("heredoc feeding a -c payload rides on the payload's last step",
          shape("bash -c 'cat' <<EOF\nbody\nEOF"), [("cat", ("body\n",))])
    exact("words never hold a heredoc body",
          [s.words for s in sl.steps("cat > m <<'EOF'\ndotnet build\nEOF", str(main), B)], [("cat", ">", "m", "<<EOF")])
    exact("a heredoc body that names a build is no build step (fleet_active_build's sibling)",
          dirs_of("cat > m.txt <<'EOF'\ndotnet build\nEOF\necho done", main, B), [])
    calls = lambda cmd, shell=B: [(c.subcommand, c.options, c.args) for s in sl.steps(cmd, str(main), shell)
                                  for c in sl.git_calls(s)]
    exact("git_calls: global options in every spelling, then the subcommand and its words",
          calls("FOO=1 git -c a=b --no-pager --git-dir=x -C /y push -q origin HEAD:main"),
          [("push", ("-c", "a=b", "--no-pager", "--git-dir=x", "-C", "/y"), ("-q", "origin", "HEAD:main"))])
    exact("git_calls: PowerShell's call operator and git.exe", calls("& git.exe stash list", P), [("stash", (), ("list",))])
    exact("git_calls: a quoted `git push` is one word, no call", calls('git commit -m "then git push"'),
          [("commit", (), ("-m", "then git push"))])
    exact("git_calls: a -c payload's git is a call", calls("bash -c 'git push origin main'"),
          [("push", (), ("origin", "main"))])
    exact("git_calls: no subcommand", calls("git --version"), [(None, ("--version",), ())])
    check("git_calls: -C applied to the step's cwd", [c.cwd for s in sl.steps(f"cd {W} && git -C sub push", str(main), B)
                                                     for c in sl.git_calls(s)], [wt / "sub"])

for f in fails:
    print("FAIL:", f)
print(f"shell_location self-test: {total - len(fails)}/{total} " + ("GREEN" if not fails else "RED"))
sys.exit(1 if fails else 0)
