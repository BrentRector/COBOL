## Entry 238 — File-I/O wall, part 2: spec-correct FILE STATUS (groups, qualified names) + REWRITE

With the parse wall cleared (DEVLOG 237), the dominant remaining SQ COMPILE_FAILs were three
over-strict SEMANTIC checks. All three were wrong vs ISO; fixed:

- **`CBL3203: FILE STATUS cannot be group item`** (~38 SQ tests). ISO §12.4.5.8.3 requires the FILE
  STATUS item to be "a two-character data item of category alphanumeric" and (rule 3) not a
  *variable-length* group. A group item IS category alphanumeric, so a 2-byte fixed group of two
  PIC X items (which the CCVS suite uses pervasively) is valid. `FileStatusValidator` no longer
  rejects all groups — it rejects only variable-length groups (subordinate OCCURS DEPENDING ON) or
  groups shorter than 2 bytes; elementary items keep the alphanumeric/length checks.

- **`CBL1901: REWRITE not allowed for file organization`** (~4 SQ tests). REWRITE is valid for
  sequential, relative, AND indexed organizations (ISO §14.9.35 — it needs I-O mode + a prior READ
  at runtime, not a particular organization; the only sequential syntax restriction is "no INVALID
  KEY phrase"). The blanket check in `BoundTreeValidator.ValidateRewrite` was a false positive and
  was removed; the now-unused CBL1901 descriptor was deleted, and the unit test
  `CBL1901_RewriteOnSequentialFile` (which asserted the wrong behavior) was converted to
  `Rewrite_OnSequentialFile_IsAllowed` (asserts no CBL1901).

- **`CBL3201: FILE STATUS must be a data-name`** (~7 SQ tests). The CCVS form `FILE STATUS
  data-name IN group-name` is a *qualified* reference; `SemanticBuilder` was storing
  `dataReference().GetText()`, which concatenates the `IN group` suffix into an unresolvable string.
  Now it stores the base `cobolWord` only (the flat data scope resolves the item by its own name).

Result: **SQ compile count 26 → 75** (of 85). Remaining 10 are parse-form gaps: LINAGE-COUNTER
special register (4), FD `RECORD … CHARACTERS` (2), `RECORD DELIMITER` clause (2), and two others.
Full guard ALL GREEN (1000 / 348 / 152) — no regression. (Runtime FAIL* correctness for the now-
compiling SQ tests is the next concern after the parse forms.)

