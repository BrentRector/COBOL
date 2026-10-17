*>--- conformance-case
*> id: pb71_all_national_boolean_figurative
*> rules: [SR-8.3.3.6.3-2, GR-8.3.3.6.4-2, GR-8.3.3.6.4-3, GR-8.3.3.6.4-9, GR-14.9.25.4-7, FMT-8.3.3.6.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB71. ISO §8.3.3.6.3 SR2: for the figurative `ALL literal-1`,
      *> "Literal-1 shall be an alphanumeric, boolean, or national literal";
      *> §8.3.3.6.4 GR2 repeats it to the width of the associated item;
      *> §14.9.25.4 GR7 / Table 17 give the figurative literal-1's category, and
      *> §14.9.25.3 Table 16 admits National→National and Boolean→Boolean;
      *> §8.3.3.5.2 / §8.3.3.4.2 make NX"…" / BX"…" (Format 2, hexadecimal) the
      *> SAME class as N"…" / B"…". Before this: `ALL N"…"` had no grammar arm
      *> (COBOL0001), `ALL B"…"` parsed and DIED AT RUN TIME (the figurative
      *> binder had two arms of four), and a VALUE clause refused NX"…" / BX"…"
      *> and every ALL literal because it classified the literal from its raw
      *> first two characters. Expected values: 'Q' is U+0051; BX"5" is B"0101";
      *> BX"A" is B"1010"; a boolean receiver wider than the pattern repeats it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB71ALLNATBOOL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NR  PIC N(3).
       01 BR  PIC 1(6).
       01 AR  PIC X(4).
       01 NV  PIC N(3) VALUE ALL N"Z".
       01 NX2 PIC N(2) VALUE NX"00410042".
       01 BV  PIC 1(4) VALUE ALL B"1".
       01 BX2 PIC 1(4) VALUE BX"A".
       01 BX3 PIC 1(6) VALUE ALL BX"5".
       01 AR7 PIC X(7).
       01 NR5 PIC N(5).
       01 BR5 PIC 1(5).
       01 AV  PIC X(5) VALUE ALL "A" & "B".
       01 NV2 PIC N(4) VALUE ALL N"Q" & N"R".
       PROCEDURE DIVISION.
      *> 1-2 — the note's repro and its hexadecimal-national twin.
           MOVE ALL N"Q" TO NR.
           IF NR = N"QQQ" DISPLAY "T1 [" NR "]" ELSE DISPLAY "T1 WRONG [" NR "]" END-IF.
           MOVE ALL NX"0051" TO NR.
           IF NR = N"QQQ" DISPLAY "T2 [" NR "]" ELSE DISPLAY "T2 WRONG [" NR "]" END-IF.
      *> 3-4 — the boolean member (a run-time death before) and its hexadecimal twin.
           MOVE ALL B"10" TO BR.
           IF BR = B"101010" DISPLAY "T3 [" BR "]" ELSE DISPLAY "T3 WRONG [" BR "]" END-IF.
           MOVE ALL BX"5" TO BR.
           IF BR = B"010101" DISPLAY "T4 [" BR "]" ELSE DISPLAY "T4 WRONG [" BR "]" END-IF.
      *> 5 — the figurative as a relation operand (§8.8.4.2.2 Format 2, a boolean relation; §8.8.2 lists
      *> "the figurative constant ALL literal, where literal is a boolean literal" as a boolean expression).
           MOVE ALL B"1" TO BR.
           IF BR = ALL B"1" DISPLAY "T5 ALL-ONES" ELSE DISPLAY "T5 WRONG" END-IF.
           IF NR = ALL N"Q" DISPLAY "T6 ALL-Q" ELSE DISPLAY "T6 WRONG" END-IF.
      *> 7-11 — VALUE clauses (§13.18.63 SR5/SR10 — a national / boolean literal or a figurative constant).
           DISPLAY "T7 [" NV "]".
           DISPLAY "T8 [" NX2 "]".
           DISPLAY "T9 [" BV "]".
           DISPLAY "T10 [" BX2 "]".
           DISPLAY "T11 [" BX3 "]".
      *> 12 — a length-unspecified context takes the literal once (§8.3.3.6.4 GR3c).
           DISPLAY "T12 [" ALL N"Q" "][" ALL B"01" "]".
      *> 13 — the alphanumeric control (ALL X"…" — the PB4 arm) is untouched.
           MOVE ALL X"51" TO AR.
           DISPLAY "T13 [" AR "]".
      *> 14-18 — literal-1 "may be a concatenation expression" (§8.3.3.6.3 SR2): ALL binds the WHOLE
      *> concatenation (§8.8.3.3 GR2 folds it to one literal), in every class, in a MOVE, a VALUE and a relation.
           MOVE ALL "AB" & "C" TO AR7.
           DISPLAY "T14 [" AR7 "]".
           MOVE ALL N"Q" & N"R" TO NR5.
           DISPLAY "T15 [" NR5 "]".
           MOVE ALL B"1" & B"0" TO BR5.
           DISPLAY "T16 [" BR5 "]".
           DISPLAY "T17 [" AV "] [" NV2 "]".
           IF AR7 = ALL "AB" & "C" DISPLAY "T18 EQ" ELSE DISPLAY "T18 WRONG" END-IF.
           STOP RUN.
