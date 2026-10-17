*>--- conformance-case
*> id: pb345-sd-no-record
*> rules: [SR-13.4.6.3-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 §13.4.6.3 syntax rule 2: "One or more record description entries
*> shall be associated with the sort-merge file description entry."
*> The sort-merge arm never had §13.4.5.3 SR3's permission -- that is a FORMATS 1 AND 2
*> rule of the FILE description entry -- and §14.9.40.3 SR6 a) says why: "The data items
*> identified by key data-names shall be described in records associated with file-name-1."
*> Until kb/Work PB345 the ONLY diagnostic this program drew was the compiler's own
*> deferral warning, "SORT 'S1' without an SD record - not implemented" (COBOLNET1756),
*> followed by a run-unit abort -- the compiler apologising for the source's error.
*> kb/Work PB345 -> COBOLNET1837.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB345N4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S1 ASSIGN TO "pb345n4.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD  S1 RECORD CONTAINS 10 CHARACTERS.
       WORKING-STORAGE SECTION.
       01  W PIC X(10).
       PROCEDURE DIVISION.
           SORT S1 ON ASCENDING KEY W
               INPUT PROCEDURE IS P1
               OUTPUT PROCEDURE IS P2.
           STOP RUN.
       P1.
           MOVE "AAAA" TO W.
       P2.
           DISPLAY "UNREACHABLE".
