## Entry 019 — 2026-03-14: Duplicate Data-Names — 98 to 139 NIST

Allowing duplicate data-names per §8.5.3.2 was the single highest-impact fix so far.
41 programs were failing solely because the symbol table rejected duplicate names that
are valid COBOL (same name in different records, disambiguated by IN/OF qualification).

The spec rule: duplicate names are valid at DECLARATION. They're errors only at POINT
OF USE when unqualified and ambiguous.

NIST: 139/391 (35.5%). Next target: 70% (274 programs).

---

