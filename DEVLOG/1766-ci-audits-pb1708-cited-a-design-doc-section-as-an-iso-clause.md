## Entry 1766 — 2026-09-28 20:39 PDT — CI audits: PB1708 cited a design-doc section as an ISO clause

The landing of Entry 1765 failed CI: `audit_code_citations.py --check` reported a PHANTOM at `kb/Work/PB1708.md:56`,
"the standard has no clause §3.14.3". The `§` referred to a section of `DESIGN-test-build-ci.md`, not to the
standard. It now reads "DESIGN-test-build-ci.md section 3.14.3", and the audit reports 0 findings. The lesson: `§`
in kb/Work notes is reserved for ISO clauses, and design-doc sections are named by file plus "section".

