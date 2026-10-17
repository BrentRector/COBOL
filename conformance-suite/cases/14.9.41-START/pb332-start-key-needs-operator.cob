*>--- conformance-case
*> id: pb332-start-key-needs-operator
*> rules: [FMT-14.9.41.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.41.2 prints the KEY phrase as `KEY relational-operator { data-name-1 |
      *> record-key-name-1 }`. relational-operator carries NO brackets - verified on the 600 dpi render
      *> of printed folio 754 - so writing the KEY phrase without an operator is not a spelling of this
      *> format. 14.9.41.4 GR15 supplies the implied comparison only for the OTHER case: "If the KEY
      *> phrase is not specified, the behavior is the same as if KEY IS EQUAL TO data-name-1 or
      *> record-key-name-1 had been specified" - the whole phrase omitted, never the operator alone.
      *> The spelling below compiled and RAN until kb/Work PB332, positioning as though EQUAL had been
      *> written; an under-rejection is the half of a format defect no golden ever notices, because the
      *> program that provokes it is the one nobody writes on purpose.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB332N3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "pb332n3.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               FILE STATUS IS ST1.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-KEY PIC X(4).
          05 IX-VAL PIC X(4).
       WORKING-STORAGE SECTION.
       01 ST1 PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT IXF
           MOVE "AA01" TO IX-KEY
           START IXF KEY IS IX-KEY
               INVALID KEY DISPLAY "INVALID"
           END-START
           CLOSE IXF
           STOP RUN.
