*>--- conformance-case
*> id: pb415_initialize_national_group_sender
*> rules: [SR-14.9.20.3-8, GR-14.9.20.4-1, FMT-14.9.20.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB415 — ISO §14.9.20.4 GR1, SECOND SENTENCE: "When identifier-2 references a bit group item or
      *> a national group item, identifier-2 is processed as an elementary data item."
      *>
      *> ⛔ THIS SENTENCE WAS UNREACHABLE, NOT UNIMPLEMENTED. §14.9.20.3 SR4 admits identifier-2 only where a
      *> MOVE (or SET) from it to an item of the named category would be valid, so a NATIONAL GROUP sender needs
      *> the category-name NATIONAL — and NATIONAL was one of the eight of §14.9.20.2's thirteen category names
      *> the grammar could not spell. With the words landed the rule can be measured for the first time.
      *>
      *> EXPECTED, DERIVED BEFORE THE RUN:
      *>   NG is `GROUP-USAGE NATIONAL` with two national children holding N"ab" and N"cde" — five national
      *>   character positions in total. GR1 sentence 2 makes it ONE elementary national sending item, so the
      *>   implicit MOVE of §14.9.20.4 GR4 moves all five positions into N1 (PIC N(5)) → "abcde". A1 is category
      *>   alphanumeric, names no category in the phrase, and GR5c therefore leaves it at "xyz".
      *>   (GR1 sentence 1 — identifier-1 as a bit/national GROUP is processed as a GROUP — is pinned elsewhere;
      *>   this file is the sentence-2 witness.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB415NGSND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NG GROUP-USAGE NATIONAL.
          05 SA PIC N(2).
          05 SB PIC N(3).
       01 G.
          05 N1 PIC N(5) VALUE N"zzzzz".
          05 A1 PIC X(3) VALUE "xyz".
       PROCEDURE DIVISION.
       MAIN.
           MOVE N"ab" TO SA.
           MOVE N"cde" TO SB.
           INITIALIZE G REPLACING NATIONAL DATA BY NG.
           DISPLAY "N1=[" N1 "] A1=[" A1 "]".
           STOP RUN.
