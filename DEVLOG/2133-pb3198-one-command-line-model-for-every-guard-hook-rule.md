## Entry 2133 — 2026-10-16 20:11 PDT — PB3198: one command-line model for every guard hook rule (heredoc bodies cut at their delimiter, git calls read once)

**Mechanism (kb/Work PB3198, R2 finding tests-scripts-ci-3--architecture--f2#2, cluster PB1754).** The guard hooks
had no one model of a shell command line. `forbidden_commands.py` read five private views of the raw text: rules 1
(stash), 2 (push to main) and 4 (test filters) scanned only the text before the FIRST `<<`; rule 2b scanned the text
with heredoc bodies removed and quotes blanked, plus `bash -c` payloads; rule 3 (escapes in a heredoc body) scanned
everything after the first `<<`; rules 5-7 read the shared parser `shell_location.steps`. `worktree_rm_allow.py` split
git's argv with `shlex`, a sixth view.

**Re-probe (base 494246263, attended).** After a `cat > msg.txt <<'EOF' … EOF` heredoc, `git stash`, `git push origin
main` and `dotnet test --filter "~Foo" > log.txt` each exited 0 (each exits 2 alone). The converse held too:
`python -c "print(1<<2)"; printf "a\n"` and `git commit -m "never git stash"` were blocked. The live hook blocked this
branch's own sweep (a `grep` whose quoted pattern held `<<`) during the work.

**Fix shape.** The model already existed: `shell_location.py` became the ONE model instead of a new file beside it.
`Step.text` and `Step.words` exclude heredoc bodies; `Step.heredocs` carries them, each cut at its own delimiter line
(several per line, `<<-`, `\EOF`, CRLF, to the end with no delimiter); a quoted `<<`, a here-string and an arithmetic
shift are no heredoc, and a heredoc inside `( … )` is skipped whole. `git_calls(step)` reads every git invocation
(global options, `-C` directory, subcommand, args); `git_dir` uses it. Every `forbidden_commands.py` rule reads one
`STEPS` / `GIT_CALLS`. The private scanners are deleted. Rule 2 now judges each push in its own directory and reads
`+main`. Rule 1 also reads `-c <x>.autostash=…`. Rule 4 checks each `dotnet test` command's own filter and its own
redirection or pipe reader. `worktree_rm_allow.py` takes git's argv from `Step.words`. It gives no decision for a
heredoc-fed call or a drive-relative path. `fleet_active_build.py` and `status_guard.py` stop reading a heredoc body as
a build or a commit through the model, with no edit of their own.

**Finished on resume (wave 1101 group A).** The CLI update of 2026-10-16 14:45 PDT killed wave 1098's agent before its
gate. Its three commits were cherry-picked onto fda09da03 without conflict, and two holes its self-review had left were
closed in the same model. First, a heredoc that a shell reads as its SCRIPT is walked as a command line
(`_reads_stdin_script`): `bash <<'EOF'`, `bash -s args`, `sh -e -o pipefail`, `pwsh -Command -` and `-File -`. Before
it, `bash <<'EOF'` / `git stash` / `EOF` passed every rule. A heredoc fed to a script operand (`bash run.sh <<EOF`)
stays that script's data. Second, `worktree_rm_allow.decide` reads its git invocation through `git_calls`, and its
private `-C` parse is deleted. It now accepts only `-C` global options, applied in order as git applies them, and gives
no decision for `-c`, `--work-tree` or any other option.

**Tests.** `test_forbidden_commands.py` 117 → 160: every rule after a heredoc, after a quoted `<<` and inside a
shell-read heredoc; its words inside a body or a quote; and the PB1725/PB1736 repros. Seventeen of the w1098c cases fail
on the old hook. `test_shell_location.py` 73 → 99 and `test_worktree_rm_allow.py` 45 → 56.

**Register.** PB3198 landed (`closes_rows: []`: guard-hook tooling, no inventory row). PB1725 and PB1736 were
discharged: commit 10499a6b5 had already anchored `VERDICT` at command position, and their repros are now pinned.

**File set.** `shell_location.py` and `test_shell_location.py` were outside wave 1098's declared set, which named only
the four `forbidden_commands`/`worktree_rm_allow` files. The member index does not index the hooks' Python, so the
planner could not see the model the note's target needs. Wave 1101's ledger entry declares the branch's full file set.

**Self-review.** Architecture: one parser, and every rule reads it, `worktree_rm_allow`'s git reading included. Code:
each rule re-derived from its docstring, keeping the old regexes' corner cases (relative `cd`, submodules,
`git log --grep=push`, quoted messages). A doubtful parse grants nothing in `worktree_rm_allow` and blocks in
`forbidden_commands`. Duplication: `_shell_named` and `git_calls` share `program()`, and `-C` is parsed in one place.

**Branch review round 1 (wave 1101 review finisher).** The reviewer found that the move onto one model made the rules
fail OPEN wherever the model did not walk a command line the base's raw-text scan had seen. R1-1: a command
substitution. `x=$(git stash)`, `` echo `git stash` ``, `out=$(git push -q origin HEAD:main 2>&1)` in a unit,
`r=$(dotnet test --filter "~Foo")`, PowerShell's `echo $(git stash)`, and the repository's own commit shape `git commit
-m "$(cat <<'EOF' … EOF)"` (rule 3 never saw that body) all exited 0. R1-2: an interpreter handed a string or a pipe,
as in `eval "…"`, `cmd /c "…"`, `echo '…' | bash` and `cat <<EOF | bash`. Re-probed on this branch: all of them passed.
The fix is in the model, not in the rules. The scanner now reads a substitution (Bash `$( … )`, a backquote, `<( … )`,
`>( … )`; PowerShell `$( … )`, `@( … )`, a `( … )` expression, a `{ … }` script block) by scanning its inside with
itself, so a quote, a bracket or a heredoc body inside it never ends it early. Its steps come before their command,
one level down, marked `Step.substitution`. One `_interpreter` replaces `_shell_child` and walks every interpreter: `-c`
in any single-letter cluster (`-euc`; Bash's `-C` is noclobber), pwsh `-Command`/`-cwa`/`-EncodedCommand` (decoded),
`cmd /c|/k|//c` (a third shell, cmd.exe: `&`, `^`, `cd /d`, and a cross-drive `cd` that does not move), Bash `eval`
and PowerShell `Invoke-Expression` (run in THIS shell, so a `cd` persists). These are also found behind
`timeout`/`env`/`xargs`/`wsl -e`/`&`. A shell reading its script from stdin now reads it from a heredoc, from a
here-string (the lead the round-0 report filed) or from the pipeline stage before it when the line spells that stage's
output (`echo`, `printf`, `Write-Output`, `cat` of a heredoc, a PowerShell literal). Rule 5 counts a substitution as
part of its command (`echo "EXIT=$? $(date)"` stays a capture), and rule 4 reads the pipe's reader past one.
`resolve` treats `%X%` and a PowerShell expression as undetermined. Tests: `test_forbidden_commands.py` 160 → 210 (32
of the new cases fail on the reviewed head abc57f110), `test_shell_location.py` 99 → 120.

Branch review round 2 (R2-1): the round-1 fix taught rule 4 to skip every substitution step when it looked for a filtered
`dotnet test`'s pipe reader, so a run that was ITSELF inside a substitution never found its reader, a step of that same
substitution. `$r = (dotnet test … --filter "FullyQualifiedName~Foo" | Select-String Passed)` and
`r=$(dotnet test … | tail -5)` (also inside `bash -c`) were blocked as unredirected; main and the round-0 head passed
them. The cause was the model's one bit: `Step.substitution` said only "inside some substitution". It is now the number
of substitutions that enclose the step, so the steps of one level that follow each other are that level's command line,
and rule 4's reader is the next step at the run's own level (the reader's own substitutions are one level deeper).
The sibling sweep found the same stage-structure gap on the other side of a pipe: `_command` found the stage a command
reads from as "the last step, if it is at my depth", so a group on either side of a `|` broke the link.
`( echo 'git stash' ) | bash` and `echo 'git stash' | ( bash )` passed, and a Bash `{ cd X; … } | cat` moved the
calling shell. `_walk` now passes the previous stage forward explicitly: a group hands it to its first command, the next
stage reads a group's last step, and a Bash `{ … }` pipeline side is a subshell. Rule 5 still treats a substitution
like a group, as one opaque part of its command. Tests: `test_forbidden_commands.py` 210 → 222 (7 of the new cases fail
on the reviewed head e24a57545), `test_shell_location.py` 120 → 125.

Branch review round 3 (R3-1): round 2's stage fix handed a group's upstream stage to the group's inner walk, and
`_command` read any upstream stage as "this command is a pipeline side", so the FIRST command inside a group on the
read side of a `|` never applied its `cd`. `cd /e/claude-skills; echo y | ( cd /e/COBOL && git push origin HEAD:main )`
and the `true | { cd /e/COBOL; git push origin main; }` form passed rule 2 (a push to this repository's main), and
`echo y | ( cd /e/claude-skills && git push -q origin main )` was blocked. The model had folded two facts into one
argument: what a command's stdin carries, and whether it is a side of a pipe of its own walk. `_walk` now takes the
walk's stdin (the texts `_output` spells) and gives each command that stdin unless a `|` of the walk itself precedes
it. Only that local `|` makes a command transient. The sweep found the same gap in the stdin itself: a group passed its
stage only to its first command, so `echo 'git stash' | ( cd X; bash )` passed, and neither a Bash substitution nor a
`-c` payload inherited the stdin of the command holding it (`echo 'git stash' | bash -c 'cd X; bash'`,
`echo 'git stash' | x=$(bash)`). Each now does; a script read from stdin consumes it. Tests: `test_forbidden_commands.py`
222 → 232 (6 of the new cases fail on the reviewed head ff8f1c01b), `test_shell_location.py` 125 → 135 (7 fail there),
`test_worktree_rm_allow.py` 56 → 57 (a pin: a group never grants).

