      *>--- conformance-case
      *> id: l1_picture_abbreviation
      *> rules: [SR-13.18.40.3-5]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.40.3 SR5 PICTURE clause — "PIC is an abbreviation
      *> for PICTURE."
      *>   python scripts/spec/cite.py --check 13.18.40.3 "PIC is an
      *>   abbreviation for PICTURE."
      *>   -> OK  §13.18.40.3   (Syntax rules)  [5.]
      *>
      *> DERIVATION. An abbreviation is the SAME word, so the clause is
      *> the same clause however it is spelled and the two spellings
      *> shall be interchangeable in every position the §13.18.40.2
      *> general format allows — including before the optional word IS.
      *> Four entries are written: PIC and PICTURE over one
      *> character-string, then PIC IS and PICTURE IS over another. If
      *> the two spellings are one clause, the pairs are identical data
      *> items, and identity is observable three ways, all displayed:
      *>   size — FUNCTION LENGTH of WS-A and of WS-B is 4 for both,
      *>          because §13.18.40.4 GR4 sizes an item by the number
      *>          of symbols in character-string-1 and both entries
      *>          carry the same character-string;
      *>   category and alignment — WS-A and WS-B are alphanumeric, so
      *>          §14.6.8.5 aligns "AB" at the leftmost character
      *>          position with space fill to the right: [AB  ];
      *>   editing — WS-C and WS-D are numeric-edited, so §14.6.8.2
      *>          rule 5 hands the transfer to §13.18.40's editing
      *>          rules. ZZ9.99 has three integer digit positions and
      *>          two fractional ones; 12.34 aligns on the decimal
      *>          point as 012.34; editing rule 7a replaces the leading
      *>          zero of the Z string with a space and stops at the
      *>          first nonzero numeric character, giving [ 12.34].
      *> A compiler that knew only PICTURE would fail to compile the
      *> WS-A / WS-C entries; one that knew only PIC would fail on
      *> WS-B / WS-D; one that accepted the abbreviation but bound it
      *> to a different clause would print a different length or a
      *> different edited image on one side of a pair.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1PICAB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC X(4).
       01 WS-B PICTURE X(4).
       01 WS-C PIC IS ZZ9.99.
       01 WS-D PICTURE IS ZZ9.99.
       01 WS-L PIC 9.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "AB" TO WS-A.
           MOVE "AB" TO WS-B.
           MOVE 12.34 TO WS-C.
           MOVE 12.34 TO WS-D.
           DISPLAY "A=[" WS-A "]".
           DISPLAY "B=[" WS-B "]".
           DISPLAY "C=[" WS-C "]".
           DISPLAY "D=[" WS-D "]".
           MOVE FUNCTION LENGTH(WS-A) TO WS-L.
           DISPLAY "LA=" WS-L.
           MOVE FUNCTION LENGTH(WS-B) TO WS-L.
           DISPLAY "LB=" WS-L.
           STOP RUN.
