      *>--- conformance-case
      *> id: l1c09_external_assign_consistency
      *> rules: [DOC-A.1-72]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.4.5.3 1) b) / Annex A item 72 — the documented
      *> consistency rules for the ASSIGN clause of an external file.
      *> Rule: "A consistent specification for data-name-1,
      *> device-name-1, and literal-1 in the ASSIGN clause. The
      *> implementor shall specify the consistency rules for
      *> data-name-1, device-name-1, and literal-1."
      *> cite.py: OK  §12.4.5.3 1) b)  (General rules)
      *> cite.py: OK  §14.8.4.4   (Correspondence of external file
      *>          control entries) "For each external file connector,
      *>          the rules specified in 12.4.5, File control entry
      *>          General rule 1 apply."
      *> cite.py: OK  §14.9.4.4 3)  (General rules) CALL e): "If one of
      *>          the rules listed above is violated and checking for it
      *>          is enabled ... in both the activated program and
      *>          activating runtime element, that exception is set to
      *>          exist, the program call is not successful"
      *> The documented choice (docs/CONFORMANCE.md row DOC-A.1-72): the
      *> TO phrase is compared by the operand that names the file - a
      *> leading device class is dropped, a literal by its value, a
      *> device-name by its spelling, case ignored, item 71's host-path
      *> mapping NOT applied; USING data-name-1 is compared by spelling
      *> with qualifiers, case ignored, IN = OF.
      *> Main L1C09E: XA ASSIGN TO "xadat"; XU ASSIGN USING FNAME OF G.
      *> Each sub describes one of them; checking is on everywhere, so
      *> an inconsistency makes the CALL unsuccessful (ON EXCEPTION)
      *> with EXCEPTION-STATUS EC-EXTERNAL-FILE-MISMATCH:
      *>   E1 TO XADAT        device-name, same spelling -> OK
      *>   E2 TO "XADAT"      literal value, case ignored -> OK
      *>   E3 TO DISK "xadat" device class dropped      -> OK
      *>   E4 USING FNAME IN G  IN = OF                   -> OK
      *>   E5 TO "xadat.txt"  no host mapping first      -> MISMATCH
      *>   E6 TO "xbdat"      different file target      -> MISMATCH
      *>   E7 USING FNAME     qualifier differs          -> MISMATCH
      *> An OK sub runs (it DISPLAYs "Ek RAN"); a mismatching one never
      *> runs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C09E.
       >>TURN EC-EXTERNAL-FILE-MISMATCH CHECKING ON
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XA ASSIGN TO "xadat".
           SELECT XU ASSIGN USING FNAME OF G.
       DATA DIVISION.
       FILE SECTION.
       FD XA IS EXTERNAL.
       01 XA-REC PIC X(10).
       FD XU IS EXTERNAL.
       01 XU-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 G.
          05 FNAME PIC X(20) VALUE "l1c09u.dat".
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "L1C09E1"
               ON EXCEPTION
                   DISPLAY "E1 MISMATCH " FUNCTION EXCEPTION-STATUS
               NOT ON EXCEPTION
                   DISPLAY "E1 OK"
           END-CALL
           CALL "L1C09E2"
               ON EXCEPTION
                   DISPLAY "E2 MISMATCH " FUNCTION EXCEPTION-STATUS
               NOT ON EXCEPTION
                   DISPLAY "E2 OK"
           END-CALL
           CALL "L1C09E3"
               ON EXCEPTION
                   DISPLAY "E3 MISMATCH " FUNCTION EXCEPTION-STATUS
               NOT ON EXCEPTION
                   DISPLAY "E3 OK"
           END-CALL
           CALL "L1C09E4"
               ON EXCEPTION
                   DISPLAY "E4 MISMATCH " FUNCTION EXCEPTION-STATUS
               NOT ON EXCEPTION
                   DISPLAY "E4 OK"
           END-CALL
           CALL "L1C09E5"
               ON EXCEPTION
                   DISPLAY "E5 MISMATCH " FUNCTION EXCEPTION-STATUS
               NOT ON EXCEPTION
                   DISPLAY "E5 OK"
           END-CALL
           CALL "L1C09E6"
               ON EXCEPTION
                   DISPLAY "E6 MISMATCH " FUNCTION EXCEPTION-STATUS
               NOT ON EXCEPTION
                   DISPLAY "E6 OK"
           END-CALL
           CALL "L1C09E7"
               ON EXCEPTION
                   DISPLAY "E7 MISMATCH " FUNCTION EXCEPTION-STATUS
               NOT ON EXCEPTION
                   DISPLAY "E7 OK"
           END-CALL
           DISPLAY "DONE"
           STOP RUN.
       END PROGRAM L1C09E.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C09E1.
       >>TURN EC-EXTERNAL-FILE-MISMATCH CHECKING ON
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XA ASSIGN TO XADAT.
       DATA DIVISION.
       FILE SECTION.
       FD XA IS EXTERNAL.
       01 XA-REC PIC X(10).
       PROCEDURE DIVISION.
       SUB-P.
           DISPLAY "E1 RAN"
           GOBACK.
       END PROGRAM L1C09E1.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C09E2.
       >>TURN EC-EXTERNAL-FILE-MISMATCH CHECKING ON
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XA ASSIGN TO "XADAT".
       DATA DIVISION.
       FILE SECTION.
       FD XA IS EXTERNAL.
       01 XA-REC PIC X(10).
       PROCEDURE DIVISION.
       SUB-P.
           DISPLAY "E2 RAN"
           GOBACK.
       END PROGRAM L1C09E2.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C09E3.
       >>TURN EC-EXTERNAL-FILE-MISMATCH CHECKING ON
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XA ASSIGN TO DISK "xadat".
       DATA DIVISION.
       FILE SECTION.
       FD XA IS EXTERNAL.
       01 XA-REC PIC X(10).
       PROCEDURE DIVISION.
       SUB-P.
           DISPLAY "E3 RAN"
           GOBACK.
       END PROGRAM L1C09E3.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C09E4.
       >>TURN EC-EXTERNAL-FILE-MISMATCH CHECKING ON
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XU ASSIGN USING FNAME IN G.
       DATA DIVISION.
       FILE SECTION.
       FD XU IS EXTERNAL.
       01 XU-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 G.
          05 FNAME PIC X(20) VALUE "l1c09u.dat".
       PROCEDURE DIVISION.
       SUB-P.
           DISPLAY "E4 RAN"
           GOBACK.
       END PROGRAM L1C09E4.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C09E5.
       >>TURN EC-EXTERNAL-FILE-MISMATCH CHECKING ON
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XA ASSIGN TO "xadat.txt".
       DATA DIVISION.
       FILE SECTION.
       FD XA IS EXTERNAL.
       01 XA-REC PIC X(10).
       PROCEDURE DIVISION.
       SUB-P.
           DISPLAY "E5 RAN"
           GOBACK.
       END PROGRAM L1C09E5.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C09E6.
       >>TURN EC-EXTERNAL-FILE-MISMATCH CHECKING ON
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XA ASSIGN TO "xbdat".
       DATA DIVISION.
       FILE SECTION.
       FD XA IS EXTERNAL.
       01 XA-REC PIC X(10).
       PROCEDURE DIVISION.
       SUB-P.
           DISPLAY "E6 RAN"
           GOBACK.
       END PROGRAM L1C09E6.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C09E7.
       >>TURN EC-EXTERNAL-FILE-MISMATCH CHECKING ON
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XU ASSIGN USING FNAME.
       DATA DIVISION.
       FILE SECTION.
       FD XU IS EXTERNAL.
       01 XU-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 G.
          05 FNAME PIC X(20) VALUE "l1c09u.dat".
       PROCEDURE DIVISION.
       SUB-P.
           DISPLAY "E7 RAN"
           GOBACK.
       END PROGRAM L1C09E7.
