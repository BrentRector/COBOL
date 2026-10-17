      *>--- conformance-case
      *> id: pb444_search_next_sentence
      *> rules: [SR-14.9.37.3-4, FMT-14.9.37.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB444 / PB446 - the NEXT SENTENCE alternative of a SEARCH WHEN phrase, in BOTH formats, written
      *> the only way ISO/IEC 1989:2023 permits it: as the WHOLE of the WHEN phrase's body (the printed brace
      *> `{ imperative-statement-2 | NEXT SENTENCE }`, 14.9.37.2) and with NO END-SEARCH phrase - 14.9.37.3 SR4:
      *> "If the END-SEARCH phrase is specified, the NEXT SENTENCE phrase shall not be specified." At --std 85,
      *> the earliest supported edition: both formats print the phrase in all four editions (ARCHAIC in 2023,
      *> COBOLNET0903), so there is no edition below its introduction to reject it.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC AND NOT FROM A RUN:
      *> 14.9.37.4 GR1 a) - "the search operation is terminated immediately; the index being varied by the search
      *>   operation remains set at the occurrence number that caused a WHEN condition to be satisfied; if the WHEN
      *>   phrase contains the NEXT SENTENCE phrase, control is transferred to an implicit CONTINUE statement
      *>   immediately preceding the next separator period".
      *> K(n) = n for n = 1..5 (the VALUE of the redefined area), ascending, so:
      *>   B - Format 1 from occurrence 1: WHEN K = 09 never holds, WHEN K = 02 holds at occurrence 2 and its body is
      *>       NEXT SENTENCE, so B-NINE never prints and the index stays at 2: "B-AFTER 02".
      *>   C - Format 1, no WHEN ever holds: the AT END phrase runs ("C-NONE"), then the sentence ends normally.
      *>   D - Format 2: the one WHEN (K = 04) holds at occurrence 4, NEXT SENTENCE, index stays at 4: "D-AFTER 04".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB444SRCHNS85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T-INIT PIC X(10) VALUE "0102030405".
       01  T REDEFINES T-INIT.
           05 E OCCURS 5 ASCENDING KEY K INDEXED BY IX.
              10 K PIC 9(2).
       PROCEDURE DIVISION.
       MAIN-P.
           SET IX TO 1
           SEARCH E AT END DISPLAY "B-NONE"
              WHEN K (IX) = 09 DISPLAY "B-NINE"
              WHEN K (IX) = 02 NEXT SENTENCE.
           DISPLAY "B-AFTER " K (IX).
           SET IX TO 1
           SEARCH E AT END DISPLAY "C-NONE"
              WHEN K (IX) = 09 NEXT SENTENCE.
           DISPLAY "C-DONE".
           SEARCH ALL E AT END DISPLAY "D-NONE"
              WHEN K (IX) = 04 NEXT SENTENCE.
           DISPLAY "D-AFTER " K (IX).
           STOP RUN.
