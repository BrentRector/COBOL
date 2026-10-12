## README: a cloner's guide to working in the repository, building and running the compiler

The owner, 2026-10-12: "ensure the README.md in the COBOL repo root is up to date and comprehensively informs a repo
cloner how to work in the repo, build and run the compiler", written by a writing skill. The Mythos session rewrote
the front page under the writing-clearly-and-concisely skill (Strunk's rules: active voice, concrete words, no
needless ones).

What changed, and why each part is there:
- **What you need** names every tool the build and the gate call, with the version each pins: the .NET 10 SDK from
  `global.json`, PowerShell 7 and Java 21 because `Cobol.Net.Frontend.csproj` regenerates the ANTLR parser through
  them, Python 3.14 for the gate driver and the specification tools. WSL and GnuCOBOL are marked optional.
- **Get the code and build it** adds `git config merge.renormalize true` (the LF rule landed on 2026-10-12, DEVLOG
  1970), says where the built compiler is, and says why the whole solution is built.
- **Compile and run a program** is a complete hello-world: the source, the two commands, the four files the
  compiler writes beside the output (measured: `hello.dll`, `hello.runtimeconfig.json`, `Cobol.Net.Runtime.dll`,
  `hello.g.cs`), and a table of the options from `cobol --help`.
- **Run the tests** states the gate's one shape and its verdict line, how to run one project with `dotnet test
  --no-build`, the Linux gate under WSL, and what each test population holds.
- **Find your way around** maps the tree as it is today: the legacy `CobolSharp.*` line is gone (the engine is deleted
  from `main`), and `specs/`, `kb/Work/`, `DEVLOG/` and `scripts/` are named.
- **How work is done here** states the five rules a contributor meets first: the standard decides (with
  `cite.py --check`), the work register, tests and documents with every change, a DEVLOG entry per landing, one pull
  request per branch through the merge queue and the CLA.
- **Status** no longer carries typed counts. The first draft held a snapshot dated 2026-10-12; the operator flagged
  it as the thing a reviewer would object to (the 2026-09-26 block it replaced had gone stale the same way). The
  section now names the inventory file and the commands that compute the numbers for the commit the reader is on.

No code changed.
