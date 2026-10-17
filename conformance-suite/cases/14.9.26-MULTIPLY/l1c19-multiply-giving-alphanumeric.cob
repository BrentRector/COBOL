      *>--- conformance-case
      *> id: l1c19-multiply-giving-alphanumeric
      *> rules: [SR-14.9.26.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.26.3 2) — MULTIPLY GIVING into an alphanumeric item
      *>   is refused
      *> "Identifier-3 shall reference a data item of category numeric
      *>   or
      *> numeric-edited."
      *> OK  §14.9.26.3 2)  (Syntax rules)
      *> Identifier-3 is the GIVING resultant (format 2). R-X is PIC
      *>   X(6),
      *> category alphanumeric, so the source shall be rejected in every
      *>   edition.
      *> Everything else is valid: A and B are numeric, the GIVING into
      *>   R-N
      *> (numeric) and R-E (numeric-edited) are the two admitted
      *>   categories.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C19B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(2) VALUE 3.
       01 B PIC 9(2) VALUE 4.
       01 R-N PIC 9(4).
       01 R-E PIC ZZZ9.
       01 R-X PIC X(6).
       PROCEDURE DIVISION.
           MULTIPLY A BY B GIVING R-N
           MULTIPLY A BY B GIVING R-E
           MULTIPLY A BY B GIVING R-X
           STOP RUN.
