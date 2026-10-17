*>--- conformance-case
*> id: pb399-evaluate-fewer-objects-than-subjects
*> rules: [SR-14.9.13.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.13.3 SR2: "The number of selection objects within each set of selection objects shall be
      *> equal to the number of selection subjects."  TWO selection subjects, ONE selection object.
      *>
      *> THE OTHER DIRECTION, AND THE ONE WITH NO DIAGNOSTIC AT ANY STAGE (kb/Work PB399).  The pairing loop
      *> ran to the number of objects WRITTEN, so the surplus subjects were never paired with anything and
      *> the WHEN phrase matched on the strict subset of pairs that happened to appear: illegal source
      *> silently branching on part of its own selection set.  SR2 is an EQUALITY, so both directions are
      *> violations; an index guard can only ever see one of them.
      *> SR7 c) is the legal way to write "this position does not matter": "The word ANY may correspond to a
      *> selection subject of any type."
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB399FEW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9 VALUE 1.
       01 WS-B PIC 9 VALUE 9.
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE WS-A ALSO WS-B
               WHEN 1
                   DISPLAY "MATCHED-ONE-OBJECT"
               WHEN OTHER
                   DISPLAY "OTHER"
           END-EVALUATE.
           STOP RUN.
