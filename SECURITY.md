# Security Policy

## Reporting a vulnerability

**Please do not report security vulnerabilities through public GitHub issues, discussions or pull requests.**

Report them privately through GitHub's
[private vulnerability reporting](https://github.com/BrentRector/CobolSharp/security/advisories/new)
(the **Security** tab → **Report a vulnerability**). Only the maintainer can see the report.

Please include as much of the following as you can:

- the kind of issue (for example: code execution through a crafted source file, path traversal in COPY library
  resolution, memory or resource exhaustion in the compiler, unsafe behavior in the runtime's file handlers);
- the commit (`git rev-parse --short HEAD`) and the command line you ran, including `--std` and any other options;
- a minimal COBOL program, copybook or data file that reproduces it;
- the impact as you understand it, and how an attacker could use it.

## What to expect

- An acknowledgement within **5 business days**.
- An assessment — confirmed, not a vulnerability, or needs more information — within **14 days**.
- A fix developed privately, with credit to you in the advisory and the change log unless you ask otherwise.
- Coordinated disclosure: the advisory is published when a fix is available on `main`. Please keep the report
  confidential until then.

## Supported versions

CobolSharp has not yet made a versioned release. Security fixes are made on the `main` branch only.

| Version | Supported |
|---|---|
| `main` | ✅ |
| anything else | ❌ |

## Scope

In scope: the compiler (`src/Cobol.Net.*`, the `cobol` command), the runtime library that compiled programs load,
and the build and CI scripts in this repository.

Out of scope: defects in the logic of a COBOL program you compile (the compiler faithfully compiling an insecure
program is not a compiler vulnerability), the third-party test corpora under `tests/`, and wrong-answer or
conformance defects with no security impact — please file those as ordinary issues.
