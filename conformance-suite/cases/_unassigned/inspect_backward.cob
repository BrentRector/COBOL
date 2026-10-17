*>--- conformance-case
*> id: inspect_backward
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.22 — INSPECT BACKWARD (COBOL-2023: Annex E.3 item 34 adds the BACKWARD context-sensitive
      *> word to INSPECT): inspection proceeds right-to-left. Re-editioned from 2002 at the W3 audit (DEVLOG 597).
      *> R: FIRST "A" backward = the RIGHTMOST "A" (forward FIRST would hit index 0).
      *>    "ABABA" -> "ABAB*".
      *> T: LEADING "0" backward counts the trailing run of zeros (2). Forward LEADING would be 0.
      *> C: CONVERTING "AB"->"XY" BEFORE INITIAL "-" backward converts only the segment to the RIGHT
      *>    of the rightmost "-" (the part scanned before reaching "-"): "AB-AB" -> "AB-XY".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INSBACK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC X(5) VALUE "ABABA".
       01 WS-B PIC X(5) VALUE "12300".
       01 WS-C PIC 9(2) VALUE 0.
       01 WS-D PIC X(5) VALUE "AB-AB".
       PROCEDURE DIVISION.
       MAIN.
           INSPECT BACKWARD WS-A REPLACING FIRST "A" BY "*".
           DISPLAY "R=" WS-A.
           INSPECT BACKWARD WS-B TALLYING WS-C FOR LEADING "0".
           DISPLAY "T=" WS-C.
           INSPECT BACKWARD WS-D CONVERTING "AB" TO "XY"
               BEFORE INITIAL "-".
           DISPLAY "C=" WS-D.
           STOP RUN.
