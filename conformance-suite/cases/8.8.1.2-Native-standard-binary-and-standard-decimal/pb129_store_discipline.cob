*>--- conformance-case
*> id: pb129_store_discipline
*> rules: [GR-8.8.1.2-1, GR-14.9.12.4-4, GR-14.9.12.4-5, GR-14.9.12.4-6]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB129 — the arithmetic store loop's size-error discipline, pinned from three sides.
      *> (1) 14.7.7 r4b: a receiver's PROHIBITED-inexact raise proceeds "to the next resulting data item to
      *> the right" — E (PIC 9V9, 1.0454.. inexact at scale 1) raises and stays 8.8, F (9V99) still stores
      *> 1.04 (the old single try abandoned it at 8.00), and 14.7.5 storing rule 2 keeps both dispositions.
      *> (2) 14.9.12.4 GR6c: 1000/3 into Q PIC 9 overflows -> the quotient's size error fires and identifier-4
      *> stores ONLY "if the size error condition is not raised" — R2 stays 9999 (it stored 0001 before).
      *> (3) 8.8.1.2 r1 / 14.7.4.3: a division under ** is NOT the final transfer — (A2/B2) ** 2 must equal
      *> 1 * (A2/B2) ** 2 (both 11.1111 via the D2 guard-scale technique; the leaked receiver context gave
      *> 11.1086 for the first — multiplying by one changed the value). The no-phrase twin golden
      *> pb129_remainder_no_phrase pins GR6c's capped subsidiary quotient (Q=3 low-digit, R2=991).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB129SD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9V99 VALUE 1.15.
       01 B PIC 9V99 VALUE 1.10.
       01 E PIC 9V9 VALUE 8.8.
       01 F PIC 9V99 VALUE 8.
       01 D PIC 9(4) VALUE 1000.
       01 V PIC 9 VALUE 3.
       01 Q PIC 9 VALUE 7.
       01 R2 PIC 9(4) VALUE 9999.
       01 X PIC 9(2)V9(4).
       01 A2 PIC 9(2) VALUE 10.
       01 B2 PIC 9 VALUE 3.
       PROCEDURE DIVISION.
       MAIN.
           DIVIDE A BY B GIVING E ROUNDED MODE IS PROHIBITED F
               ON SIZE ERROR DISPLAY "SE1"
           END-DIVIDE
           DISPLAY "E=" E " F=" F
           DIVIDE V INTO D GIVING Q REMAINDER R2
               ON SIZE ERROR DISPLAY "SE2"
           END-DIVIDE
           DISPLAY "Q=" Q " R2=" R2
           COMPUTE X = (A2 / B2) ** 2
           DISPLAY "X1=" X
           COMPUTE X = 1 * (A2 / B2) ** 2
           DISPLAY "X2=" X
           STOP RUN.
