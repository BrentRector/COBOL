## Entry 1720 — 2026-09-26 17:05 PDT — PB1592 scope decided from Annex E.2 6) + 21)

PB1592 asked whether DOC-A.1-29 (compile-time arithmetic: System.Decimal, ties to even) applies at 2002/2014 and
said the answer needed the 2002/2014 text. It does not: E.2 6) says the previous standard "required the use of an
arithmetic mode that is no longer supported", and E.2 21) names the one arithmetic mode 2023 removed — "Standard
Arithmetic" (both `cite.py --check` OK; STANDARD-BINARY survives as obsolete, §8.8.1.4). So at 2002/2014 compile-time
expressions are evaluated in standard arithmetic, which this compiler already implements on the SDIDI engine it shares
with STANDARD-DECIMAL (`OptionsModel.cs`, `ArithmeticMode.Standard`). Decision recorded in the note: option (a) —
gate `CompileTimeExpressionEvaluator` by dialect (SDIDI at 2002/2014) and narrow DOC-A.1-29 and its row to 2023.
Left open, under the precedence rule: whether 2023's implementor-defined mode should also be the SDIDI (one engine),
pending a survey of GnuCOBOL's compile-time evaluator. No code changed.

