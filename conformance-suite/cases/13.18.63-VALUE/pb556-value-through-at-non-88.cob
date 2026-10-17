*>--- conformance-case
*> id: pb556-value-through-at-non-88
*> rules: [SR-13.18.63.3-30, SR-13.18.63.3-33]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.63.3 SR33 - "Formats 3 and 5 may be specified only when the level-number of the
*> subject of the entry is 88." The THROUGH phrase is written only in those two formats (13.18.63.2), so a
*> level-01 entry carrying one is nonconforming source and shall be diagnosed at compile time (COBOLNET2167).
*> No edition changes this: Annex E carries no 2023 change to SR33 and the screen has no version predicate,
*> so all four editions reject.
*> MEASURED BEFORE the screen: the grammar admits every VALUE format through one rule (formats 3 and 5 share
*> their literal/THROUGH list), and the binder took `valueItem.GetText()` - so this program reached the code
*> generator with the raw text `1THRU5` as a NUMERIC initializer and failed the whole compilation with Roslyn
*> `CS1002: ; expected` plus two CS1519s. A syntax-rule violation is a compile-time reject, never a backend
*> failure. Its silent twin is pb556-value-through-at-non-88-alphanumeric.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB556THRU01.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 X PIC 9 VALUE 1 THRU 5.
PROCEDURE DIVISION.
MAIN.
    DISPLAY X
    STOP RUN.
