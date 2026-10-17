*>--- conformance-case
*> id: pb177-redefines-dynamic-object
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.44.3 SR5 SENTENCE 1 again, on the OTHER OCCURS format: OCCURS DYNAMIC is Format 4 OF THE
      *> OCCURS CLAUSE (13.18.38), so "The data description entry for data-name-2 shall not contain an OCCURS
      *> clause" reaches the dynamic-capacity object exactly as it reaches the fixed one. This is the OBJECT half
      *> of what COBOLNET1525 used to cover with a storage-model rationale; the syntax rule is the better
      *> authority and 1525 is now narrowed to the SUBJECT half, where no syntax rule does name the case.
      *> Its sibling pb177-redefines-dynamic-subject.cob pins that other half - the two are the witnesses either
      *> side of the boundary, which previously had none.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB177NA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A.
          05 T PIC X(3) OCCURS DYNAMIC FROM 1 TO 5.
          05 R REDEFINES T PIC X(3).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "X"
           STOP RUN.
