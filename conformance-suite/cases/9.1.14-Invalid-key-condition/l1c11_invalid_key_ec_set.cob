      *>--- conformance-case
      *> id: l1c11_invalid_key_ec_set
      *> rules: [GR-9.1.14-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §9.1.14 1) — invalid key: I-O status set and, if enabled,
      *>   EC-I-O-INVALID-KEY set to exist
      *> Rule: "The I-O status of the file connector associated with the
      *>   statement is set to a value indicating the invalid key
      *>   condition as described in 9.1.13.5, Invalid key condition
      *>   with unsuccessful completion, and, if enabled, the
      *>   EC-I-O-INVALID-KEY exception condition is set to exist."
      *>   cite.py --check 9.1.14 "and, if enabled, the
      *>   EC-I-O-INVALID-KEY exception condition is set to exist"
      *>   -> OK  §9.1.14 1)  (Invalid key condition)
      *>   cite.py --check 9.1.14 "If the INVALID KEY phrase is
      *>   specified in the input-output statement, any applicable
      *>   exception processing statements are not executed, and
      *>   control is transferred to the imperative-statement specified
      *>   in the INVALID KEY phrase" -> OK  §9.1.14 2)
      *>   cite.py --check 9.1.14 "If the INVALID KEY phrase is not
      *>   specified in the input-output statement, any applicable
      *>   exception processing statements are executed"
      *>   -> OK  §9.1.14 3)
      *>   cite.py --check 14.6.13.1.1 "the last exception status is
      *>   set to indicate that exception condition"
      *>   -> OK  §14.6.13.1.1   (General)
      *>   cite.py --check 15.33.3 "A 31-character, left-justified,
      *>   alphanumeric character string that is the exception-name"
      *>   -> OK  §15.33.3 1)  (Returned value rule)
      *> Checking for EC-I-O-INVALID-KEY is enabled by the >>TURN below.
      *> Derivation (the name is 18 characters, padded to 31):
      *>   W1=00                first WRITE of K1 succeeds.
      *>   PHRASE ST=22         duplicate WRITE, INVALID KEY written:
      *>   PHRASE EC=[...]      status 22 and, before the phrase runs,
      *>                        EC-I-O-INVALID-KEY exists, so it is the
      *>                        last exception status; the USE
      *>                        procedure is NOT executed (item 2).
      *>   USE ST=23            random READ of absent K9, no phrase:
      *>   USE EC=[...]         status 23, the condition exists, and the
      *>                        applicable USE procedure runs (item 3).
      *>   AFTER-READ           control returns to the end of the READ.
       >>TURN EC-I-O-INVALID-KEY CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C11K.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-IX ASSIGN TO "L1C11K.IDX"
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
       DECLARATIVES.
       IX-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F-IX.
       IX-ERR-P.
           DISPLAY "USE ST=" IX-ST.
           DISPLAY "USE EC=[" FUNCTION EXCEPTION-STATUS "]".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT F-IX.
           MOVE "K1ONE" TO IX-REC.
           WRITE IX-REC.
           DISPLAY "W1=" IX-ST.
           MOVE "K1DUP" TO IX-REC.
           WRITE IX-REC
               INVALID KEY
                   DISPLAY "PHRASE ST=" IX-ST
                   DISPLAY "PHRASE EC=[" FUNCTION EXCEPTION-STATUS "]"
           END-WRITE.
           CLOSE F-IX.
           OPEN INPUT F-IX.
           MOVE "K9" TO IX-KEY.
           READ F-IX.
           DISPLAY "AFTER-READ".
           CLOSE F-IX.
           STOP RUN.
