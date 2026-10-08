      *> kb/Work PB1454 - a report-section SUM counter is qualified by the report group entries above it and by its
      *> report-name. ISO/IEC 1989:2023 section 8.4.2.2.3 SR4: "Each data-name-2 shall be the name associated with
      *> a level number to which the item being qualified is subordinate."
      *>   cite.py: OK  8.4.2.2.3 4)  (Syntax rules)
      *> SR2: "if there is more than one combination of qualifiers that ensures uniqueness, then any such set may
      *> be used."
      *>   cite.py: OK  8.4.2.2.3 2)  (Syntax rules)
      *> Section 13.18.54.4 GR5: "If a data-name immediately follows the level number in the entry containing the
      *> SUM clause, the data-name is the name of the sum counter, not the name of the associated printable item".
      *>   cite.py: OK  13.18.54.4 5)  (General rules)
      *>
      *> DERIVATION. WX is 7. R1 is generated for D1 twice, so each of its counters that sums WX UPON D1 holds 14
      *> before TERMINATE resets it: CF1's TOT1 and TOT2 (PIC 99, shown 14) and the report footing's TOT1
      *> (PIC 999, shown 014). R2 is generated for D2 once, so its CF2.TOT1 holds 07. SR4 makes every named level
      *> above a counter a qualifier, so TOT1 OF CF1 and TOT1 OF RF1 name DIFFERENT counters of one report (the bare
      *> TOT1 would be ambiguous), TOT2 OF CF1 OF R1 uses the group and the report together, and TOT1 IN R2 still
      *> reaches the only counter of that name in R2. Before the fix, every form with a group qualifier was refused.
      *> A sum counter is a signed USAGE DISPLAY item (docs/CONFORMANCE.md A.4.11, kb/Work PB1943: 13.18.54.4 GR1
      *> gives it no usage and a sign), so DISPLAY shows the digits with the operational sign on the last one, as
      *> for any signed DISPLAY item: positive 14 is "1D", 014 is "01D" and 07 is "0G" in the default IBM
      *> overpunch convention (--sign-encoding ibm); the values are the derivation's 14, 014 and 07.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1454Q.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1454a.txt".
           SELECT PRT2 ASSIGN TO "pb1454b.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       FD  PRT2 REPORT IS R2.
       WORKING-STORAGE SECTION.
       01  WX PIC 99 VALUE 7.
       REPORT SECTION.
       RD  R1 CONTROL IS FINAL PAGE LIMIT 20 LINES FOOTING 18.
       01  D1 TYPE DE LINE PLUS 1.
           02 COLUMN 1 PIC 99 SOURCE WX.
       01  CF1 TYPE CF FINAL.
           02 LINE PLUS 1.
              03 TOT1 COLUMN 1 PIC 99 SUM WX UPON D1.
              03 TOT2 COLUMN 5 PIC 99 SUM WX UPON D1.
       01  RF1 TYPE REPORT FOOTING.
           02 LINE PLUS 1.
              03 TOT1 COLUMN 1 PIC 999 SUM WX UPON D1.
       RD  R2 CONTROL IS FINAL PAGE LIMIT 20 LINES.
       01  D2 TYPE DE LINE PLUS 1.
           02 COLUMN 1 PIC 99 SOURCE WX.
       01  CF2 TYPE CF FINAL.
           02 LINE PLUS 1.
              03 TOT1 COLUMN 1 PIC 99 SUM WX UPON D2.
       PROCEDURE DIVISION.
           OPEN OUTPUT PRT PRT2.
           INITIATE R1 R2.
           GENERATE D1.
           GENERATE D1.
           GENERATE D2.
           DISPLAY "A=" TOT2 " " TOT2 IN R1 " " TOT2 OF CF1
               " " TOT2 OF CF1 OF R1.
           DISPLAY "B=" TOT1 OF CF1 " " TOT1 OF RF1 " "
               TOT1 OF CF1 OF R1 " " TOT1 OF RF1 OF R1.
           DISPLAY "C=" TOT1 OF CF2 " " TOT1 OF CF2 OF R2
               " " TOT1 IN R2.
           TERMINATE R1 R2.
           CLOSE PRT PRT2.
           STOP RUN.
