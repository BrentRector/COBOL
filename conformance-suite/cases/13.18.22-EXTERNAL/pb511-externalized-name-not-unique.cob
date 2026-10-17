*>--- conformance-case
*> id: pb511-externalized-name-not-unique
*> rules: [SR-13.18.22.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB511 — §13.18.22.3 syntax rule 2: "In the same source element, the externalized name of the
      *> subject of the entry that includes the EXTERNAL clause shall not be the same as the externalized name
      *> of any other entry that includes the EXTERNAL clause."
      *> ⛔ THE RULE ONLY BECAME VIOLABLE WHEN THE AS PHRASE GAINED A GRAMMAR. Before it, every externalized
      *> name was §13.18.22.4 GR5's default — the subject's own data-name or file-name — and §8.4.2.2 already
      *> keeps those unique inside one source element. With literal-1 writable, two subjects can claim ONE
      *> run-unit cell, and the consequence is not a diagnostic-quality one: the cell is keyed by exactly this
      *> name, so a PIC X(3) record and a PIC X(9) record would silently alias one another's storage.
      *> COBOLNET2159, at every edition that has the phrase.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB511DUP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  DUP-A IS EXTERNAL AS "PB511DUP" PIC X(3).
       01  DUP-B IS EXTERNAL AS "PB511DUP" PIC X(9).
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
