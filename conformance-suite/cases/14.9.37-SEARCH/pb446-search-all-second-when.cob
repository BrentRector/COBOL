*>--- conformance-case
*> id: pb446-search-all-second-when
*> rules: [FMT-14.9.37.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 §14.9.37.2 Format 2 (rendered, PDF page 750): the ellipsis sits on the [ AND ... ] bracket, not
*> on the WHEN brace (§5.2.7), so a SEARCH ALL has exactly ONE WHEN phrase whose AND phrase repeats. The grammar
*> used to admit N WHEN phrases (measured: this program printed HIT3). Refused at every edition. COBOLNET2269
*> (kb/Work PB446).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB446SA2W.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T.
           05 E OCCURS 5 ASCENDING KEY K INDEXED BY IX.
              10 K PIC 9(2).
       PROCEDURE DIVISION.
           SEARCH ALL E AT END DISPLAY "NONE"
              WHEN K (IX) = 05 DISPLAY "HIT5"
              WHEN K (IX) = 03 DISPLAY "HIT3"
           END-SEARCH
           STOP RUN.
