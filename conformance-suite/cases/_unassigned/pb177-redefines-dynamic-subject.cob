*>--- conformance-case
*> id: pb177-redefines-dynamic-subject
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> COBOLNET1525's SURVIVING half - and the one side of this family for which "no syntax rule literally
      *> names it" is TRUE. The SUBJECT of the REDEFINES is itself a dynamic-capacity table: 13.18.38.3 carries
      *> no REDEFINES restriction, and 13.18.44.3 SR17 does not reach it either, because 8.5.1.12.1 defines
      *> "variable-length group" over items SUBORDINATE to the group - an elementary dynamic table is not one.
      *> What decides it is the storage model: 13.18.44.4 GR1 associates the subject with "an area sufficient to
      *> contain the number of bits required by the data item referenced by the subject of the entry", and
      *> 8.5.1.9.1 says a dynamic-capacity table's capacities "may vary during execution" - there is no fixed
      *> area to associate. A COBOL.NET storage-model rejection, honestly labelled.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB177NB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A.
          05 T PIC X(12).
          05 R REDEFINES T PIC X(3) OCCURS DYNAMIC FROM 1 TO 5.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "X"
           STOP RUN.
