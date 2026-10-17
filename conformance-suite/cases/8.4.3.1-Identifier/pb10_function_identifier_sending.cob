*>--- conformance-case
*> id: pb10_function_identifier_sending
*> rules: [SR-8.4.3.1.3-2, SR-14.9.20.3-8, SR-14.9.32.3-2,
*>    SR-14.9.51.3-11]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB10 - a FUNCTION-IDENTIFIER in the identifier-N SENDING positions that rejected it outright.
      *> 8.4.3.1.2 Format 1 makes a function-identifier an IDENTIFIER, and 8.4.3.2.3 SR1 bars one ONLY from a
      *> RECEIVING operand - so every SENDING position admits one. The grammar reached functionCall from just
      *> four rules, so these were COBOL0001 "no viable alternative" on legal source.
      *>
      *> Each position below is one the STANDARD ITSELF defines as a MOVE sending item, which is why they are
      *> fixed together and why the operand admits exactly what a MOVE sender admits:
      *>   14.9.51.4 GR5a - WRITE   ... FROM identifier-1  ==  MOVE identifier-1 TO record-name-1
      *>   14.9.35.4      - REWRITE ... FROM identifier-1  ==  the same MOVE
      *>   14.9.32.4      - RELEASE ... FROM identifier-1  ==  the same MOVE
      *>   14.9.20.3 SR4  - INITIALIZE ... REPLACING ... BY identifier-2, "the SENDING item" of a MOVE
      *>
      *> All four bind through the ONE WriteSource / operand helper rather than four call sites, so the next
      *> FROM phrase inherits the arm instead of re-deriving it.
      *>
      *> ⛔ INSPECT IS THE ONE POSITION THE GRAMMAR CANNOT DECIDE ALONE, and it is now covered - identifier-1
      *> is a SENDING operand only in Format 1 (TALLYING). Derived rather than assumed: 14.9.22.4 GR1 concedes
      *> only that "for purposes of determining its length, identifier-1 is treated as a sending data item" - a
      *> SCOPED concession that would be unnecessary if it were generally sending; GR7 has each match "tallied
      *> (format 1) or replaced by literal-3 (format 2)"; and GR20 makes Format 4 execute AS a Format 2 over the
      *> same identifier-1. So Formats 2/3/4 MODIFY it and 8.4.3.2.3 SR1 bars a function-identifier there.
      *> The grammar admits and the BINDER screens per format (COBOLNET1632); the three rejections are pinned by
      *> pb10-inspect-fn-replacing / -converting / -tallying-replacing.
      *> ⚠ THE THIRD OF THOSE IS THE ONE THAT MATTERS: a screen keyed on "TALLYING present => Format 1" would
      *> wrongly ACCEPT Format 3. The screen keys on REPLACING-or-CONVERTING present, which is also exactly the
      *> emitter's existing `mutated` flag - one fact, not two representations of it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB10FNSEND.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb10_fn.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
           SELECT SF ASSIGN TO "pb10_fn_sort.tmp".
           SELECT OF1 ASSIGN TO "pb10_fn_out.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 FREC PIC X(8).
       SD SF.
       01 SREC PIC X(8).
       FD OF1.
       01 OREC PIC X(8).
       WORKING-STORAGE SECTION.
       01 A PIC X(8) VALUE "abcdefgh".
       01 T PIC X(8).
       01 CNT PIC 9(3) VALUE 0.
       PROCEDURE DIVISION.
      *> WRITE ... FROM - 15.97.3 r1 upper-cases "abcdefgh" to "ABCDEFGH", and GR5a moves that to the record.
           OPEN OUTPUT F
           WRITE FREC FROM FUNCTION UPPER-CASE(A)
           CLOSE F
           OPEN INPUT F
           READ F INTO T
           CLOSE F
           DISPLAY "WRITE-FROM=" T

      *> INITIALIZE ... REPLACING ... BY - the same sending operand, via 14.9.20.3 SR4.
           MOVE SPACES TO T
           INITIALIZE T REPLACING ALPHANUMERIC BY FUNCTION UPPER-CASE(A)
           DISPLAY "INIT-BY=" T

      *> INSPECT identifier-1, FORMAT 1 (TALLYING) - the SENDING format, so a function-identifier is legal.
      *> 15.97.3 r1 upper-cases "abcdefgh"; TALLYING counts the two characters "C" and "D" in "ABCDEFGH", which
      *> the lower-case source would NOT match - so this also proves the function is actually evaluated rather
      *> than the raw item being inspected.
           MOVE 0 TO CNT
           INSPECT FUNCTION UPPER-CASE(A) TALLYING CNT FOR ALL "C"
           INSPECT FUNCTION UPPER-CASE(A) TALLYING CNT FOR ALL "D"
           DISPLAY "INSPECT-TALLY=" CNT

      *> RELEASE ... FROM - the SORT path, which reaches the same helper through a different binder.
           SORT SF ASCENDING KEY SREC
               INPUT PROCEDURE IS FEED-SORT
               GIVING OF1
           OPEN INPUT OF1
           READ OF1 INTO T
           CLOSE OF1
           DISPLAY "RELEASE-FROM=" T
           STOP RUN.

       FEED-SORT.
           RELEASE SREC FROM FUNCTION LOWER-CASE("ABCDEFGH").
