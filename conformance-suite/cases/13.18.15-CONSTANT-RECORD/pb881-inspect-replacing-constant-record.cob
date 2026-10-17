*>--- conformance-case
*> id: pb881-inspect-replacing-constant-record
*> rules: [SR-13.18.15.3-2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB881 — ISO 13.18.15.3 SR2: "Neither the data item described by the subject of the entry nor any
*> data item subordinate to the subject of the entry shall be specified as a receiving data item." INSPECT's
*> identifier-1 is a receiving data item whenever REPLACING or CONVERTING stores into it (14.9.22.3 SR8 makes it
*> a SENDING operand only in Format 1, TALLYING alone). Before PB881 INSPECT resolved identifier-1 with the
*> plain reference resolver, so the constant was REWRITTEN at run time with no diagnostic while the identical
*> MOVE drew COBOLNET1548. Both statements below are refused; the CONSTANT RECORD clause is COBOL-2002, so 85
*> is not a reject edition for THIS rule (below 2002 the clause itself is refused, a different rule).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB881NIR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CA CONSTANT RECORD PIC X(5) VALUE "aaaaa".
       PROCEDURE DIVISION.
       MAIN.
           INSPECT CA REPLACING ALL "a" BY "b"
           INSPECT CA CONVERTING "a" TO "z"
           STOP RUN.
