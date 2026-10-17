*>--- conformance-case
*> id: l1-divide-arithmetic-standard-binary
*> rules: [GR-14.9.12.4-1, GR-14.9.12.4-7]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.12.4 GR1 and GR7 each name THREE arithmetic modes: "When native arithmetic is in
      *> effect …  When standard-decimal arithmetic, or standard-binary arithmetic is in effect …".
      *> (cite.py --check 14.9.12.4 "When native arithmetic is in effect, the quotient is the result of
      *> dividing the dividend by the divisor" -> OK, General rules 1.)
      *>
      *> THE THIRD MODE IS DECLINED, AND THE DECLINE IS WHAT THIS FIXTURE PINS. ARITHMETIC IS
      *> STANDARD-BINARY is a processor-dependent language element (Annex A.3 item 2) for which §4.2.6
      *> permits an implementor not to claim support, and docs/CONFORMANCE.md §2 row 2 records it "Not
      *> claimed"; §8.8.1.4.1 NOTE 1 / Annex F.2 item 3 also make it obsolete. The requirement a decline
      *> carries is that it be LOUD: an unclaimed mode must be refused by name, never accepted and
      *> silently computed in some other mode. So this program shall be REJECTED with COBOLNET0806 at
      *> every edition where the clause exists (2014+; arithmetic-standard-binary-2014).
      *>
      *> WITHOUT THIS FIXTURE the standard-binary sentence of GR1 and GR7 would be neither implemented nor
      *> witnessed — the shape that keeps a row PARTIAL forever. With it, the sentence is UNREACHABLE
      *> rather than unverified, which is the same disposition GR-8.8.1.2-6 already carries.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1DIVSB.
       OPTIONS.
           ARITHMETIC IS STANDARD-BINARY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-QC PIC 9(3).
       01 W-RC PIC 9(3).
       PROCEDURE DIVISION.
       MAIN.
           DIVIDE 7 INTO 100 GIVING W-QC REMAINDER W-RC
           DISPLAY "C=" W-QC " " W-RC
           STOP RUN.
