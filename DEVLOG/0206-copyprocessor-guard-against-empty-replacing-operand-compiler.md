## Entry 206 — 2026-05-28: CopyProcessor — guard against empty REPLACING operand (compiler crash → diagnostic)

SM201A and SM206A crashed the compiler outright: `System.ArgumentException: The value cannot be
an empty string (Parameter 'oldValue')` from `string.Replace("", …)` in `ExpandCopyStatements`.
A COPY … REPLACING operand had parsed to an empty `from` string (the pseudo-text/word REPLACING
parser does not yet handle every multi-line form). Whatever the parse weakness, the preprocessor
must never throw on malformed library text. Added a guard that skips any replacement with an
empty `from`. SM201A/SM206A now reach the parser and report ordinary diagnostics instead of an
unhandled exception (their multi-line pseudo-text REPLACING is the next piece of work). Guard ALL
GREEN (1000 / 336 / 137).

