      *>--- conformance-case
      *> id: l1c11_invalid_key_no_phrase_no_use
      *> rules: [GR-9.1.14-1, GR-9.1.14-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §9.1.14 1) and 4) — invalid key with no INVALID KEY phrase
      *>   and no applicable exception processing statement; phrase
      *>   ignored when no invalid key condition exists
      *> Rules: "The I-O status of the file connector associated with
      *>   the statement is set to a value indicating the invalid key
      *>   condition as described in 9.1.13.5" and "If the INVALID KEY
      *>   phrase is not specified in the input-output statement and
      *>   there are no applicable exception processing statements,
      *>   control is transferred to the end of the input-output
      *>   statement." and "If the invalid key condition does not exist
      *>   after the execution of the input-output operation specified
      *>   by an input-output statement, the INVALID KEY phrase is
      *>   ignored, if specified."
      *>   cite.py --check 9.1.14 "The I-O status of the file connector
      *>   associated with the statement is set to a value indicating
      *>   the invalid key condition" -> OK  §9.1.14 1)
      *>   cite.py --check 9.1.14 "If the INVALID KEY phrase is not
      *>   specified in the input-output statement and there are no
      *>   applicable exception processing statements, control is
      *>   transferred to the end of the input-output statement"
      *>   -> OK  §9.1.14 4)  (Invalid key condition)
      *>   cite.py --check 9.1.14 "If the invalid key condition does not
      *>   exist after the execution of the input-output operation
      *>   specified by an input-output statement, the INVALID KEY
      *>   phrase is ignored, if specified"
      *>   -> OK  §9.1.14 4)  (printed after item 4)
      *>   cite.py --check 9.1.14 "If the I-O status indicates a
      *>   successful completion, control is transferred to the end of
      *>   the input-output statement or to the imperative-statement
      *>   specified in the NOT INVALID KEY phrase if it is specified"
      *>   -> OK  §9.1.14 2)  (second list)
      *>   cite.py --check 9.1.13.5 "to write a record that would create
      *>   a duplicate prime record key in a physical indexed file"
      *>   -> OK  §9.1.13.5 2) b)   (status 22)
      *>   cite.py --check 9.1.13.5 "an attempt is made to randomly
      *>   access a record that does not exist in the physical file"
      *>   -> OK  §9.1.13.5 3) a)   (status 23)
      *> The program has NO DECLARATIVES and no exception-checking
      *> PERFORM, so no applicable exception processing statement
      *> exists (§9.1.12: cite.py --check 9.1.12 "Statements in a
      *> file-exception or exception-name format of a USE declarative"
      *> -> OK §9.1.12 2)). Each failing statement below has no phrase,
      *> so control reaches the DISPLAY that follows it.
      *> Derivation:
      *>   W1=00       first WRITE of key K1 succeeds.
      *>   W-DUP=22    second WRITE of K1: duplicate prime key, status
      *>               22, control to the next statement.
      *>   R-MISS=23   random READ of absent K9: status 23.
      *>   RW-MISS=23  REWRITE of absent K9: status 23.
      *>   D-MISS=23   DELETE of absent K9: status 23.
      *>   S-MISS=23   START KEY = absent K9: status 23.
      *>   W2=00       WRITE of K2 with INVALID KEY: no invalid key, so
      *>               the phrase is ignored ("W2-INV" never appears).
      *>   R-NOTINV    READ of K1 succeeds: the NOT INVALID KEY
      *>   R-OK=00     imperative runs, the INVALID KEY one does not.
      *>   R-VAL=K1ONE the record read.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C11J.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-IX ASSIGN TO "L1C11J.IDX"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               FILE STATUS IS IX-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F-IX.
       01 IX-REC.
          05 IX-KEY  PIC XX.
          05 IX-DATA PIC X(3).
       WORKING-STORAGE SECTION.
       01 IX-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F-IX.
           MOVE "K1ONE" TO IX-REC.
           WRITE IX-REC.
           DISPLAY "W1=" IX-ST.
           MOVE "K1DUP" TO IX-REC.
           WRITE IX-REC.
           DISPLAY "W-DUP=" IX-ST.
           CLOSE F-IX.
           OPEN I-O F-IX.
           MOVE "K9" TO IX-KEY.
           READ F-IX.
           DISPLAY "R-MISS=" IX-ST.
           MOVE "K9NEW" TO IX-REC.
           REWRITE IX-REC.
           DISPLAY "RW-MISS=" IX-ST.
           MOVE "K9" TO IX-KEY.
           DELETE F-IX RECORD.
           DISPLAY "D-MISS=" IX-ST.
           MOVE "K9" TO IX-KEY.
           START F-IX KEY IS EQUAL TO IX-KEY.
           DISPLAY "S-MISS=" IX-ST.
           MOVE "K2TWO" TO IX-REC.
           WRITE IX-REC
               INVALID KEY DISPLAY "W2-INV"
           END-WRITE.
           DISPLAY "W2=" IX-ST.
           MOVE "K1" TO IX-KEY.
           READ F-IX
               INVALID KEY DISPLAY "R-INV"
               NOT INVALID KEY DISPLAY "R-NOTINV"
           END-READ.
           DISPLAY "R-OK=" IX-ST.
           DISPLAY "R-VAL=" IX-REC.
           CLOSE F-IX.
           STOP RUN.
