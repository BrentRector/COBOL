## Entry 1654 — 2026-09-22 23:23 PDT — Every clause in the ledger's burn-down is named

Owner: "some, like section 14 have a label … others, like section 8 … are not labeled. I have no idea what section 8
is." `gen_ledger.py`'s CLAUSE_TITLE named only §13, §14, §15 and Annex A.1. It now carries every top-level clause
title of ISO/IEC 1989:2023 exactly as `specs/ISO_COBOL.md` prints them (`## N Title`): §4 Conformance · §5 Description
techniques · §6 Reference format · §7 Compiler directing facility · §8 Language fundamentals · §9 I-O, objects, and
user-defined functions · §10 Structured compilation group · §11 Identification Division · §12 Environment Division
· §16 Standard classes (plus §1–§3). A static map by design: the spec is a private submodule CI does not check out,
and `--check` must render identically there. Published as ledger v64.

