*>--- conformance-case
*> id: alphabet_national
*> rules: [GR-12.3.6.4-9, GR-12.3.7.4-7, RV-15.16.4-1, RV-15.70.4-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 12.3.7 ALPHABET ... FOR NATIONAL + 12.3.6 PROGRAM COLLATING SEQUENCE FOR NATIONAL:
      *> a custom national literal-phrase alphabet (REV-NAT: C < B < A) visibly REORDERS national
      *> comparisons vs the native UTF-16 code-unit order (D-N3). Unspecified characters keep their
      *> native relative order ABOVE the specified block (12.3.7 GR7 k3). CHAR-NATIONAL (15.16.4)
      *> and ORD over a national argument (15.70.4 r2) read the same sequence; national HIGH-VALUE/
      *> LOW-VALUE are the sequence's extremes (12.3.7 GR8/GR9 - LOW = position 0 = "C"; HIGH = the
      *> largest UNSPECIFIED code unit, U+FFFF). The FOR ALPHANUMERIC leg names a NATIVE alphabet
      *> (identity - alphanumeric comparisons stay native, 12.3.6 GR9). UCS-4 names the ISO 10646
      *> collating sequence, which on the one-UTF-16-code-unit-per-position substrate IS the native
      *> order (8.5.1.4 - no surrogate-pair recognition), and UTF-16 names a coded character set
      *> only (12.3.7 GR7 Table 6) - both declarations bind here as parse+bind proof.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ALPNATP10AN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. GENERIC-BOX
           PROGRAM COLLATING SEQUENCE
               FOR ALPHANUMERIC IS STD-SEQ
               FOR NATIONAL IS REV-NAT.
       SPECIAL-NAMES.
           ALPHABET STD-SEQ IS NATIVE
           ALPHABET REV-NAT FOR NATIONAL IS N"CBA"
           ALPHABET UNI FOR NATIONAL IS UCS-4
           ALPHABET WIDE-SET FOR NATIONAL IS UTF-16.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N-A     PIC N VALUE N"A".
       01 N-B     PIC N VALUE N"B".
       01 N-C     PIC N VALUE N"C".
       01 N-X     PIC N.
       01 N-FLAG  PIC N VALUE N"B".
          88 IS-BEE VALUE N"B".
       01 ORD-R   PIC 9(5).
       PROCEDURE DIVISION.
       MAIN.
      *> Under REV-NAT: C(position 0) < B(1) < A(2) - the native order is REVERSED.
           IF N-A > N-B DISPLAY "A-GT-B=YES" ELSE DISPLAY "A-GT-B=NO".
           IF N-C < N-B DISPLAY "C-LT-B=YES" ELSE DISPLAY "C-LT-B=NO".
      *> An UNSPECIFIED character sits above every specified one (GR7 k3): D > A.
           IF N"D" > N-A DISPLAY "D-GT-A=YES" ELSE DISPLAY "D-GT-A=NO".
      *> CHAR-NATIONAL(1) = the character at position 0 of the national PCS = "C" (15.16.4).
           MOVE FUNCTION CHAR-NATIONAL(1) TO N-X.
           IF N-X = N"C" DISPLAY "CN1=C" ELSE DISPLAY "CN1=OTHER".
      *> ORD reads the national PCS for a national argument (15.70.4 r2): A is at position 2.
           MOVE FUNCTION ORD(N-A) TO ORD-R.
           DISPLAY "ORD-A=" ORD-R.
      *> ORD of unspecified SPACE (U+0020): NextFree(3) + (32 - 0 specified below) = 35 -> 36.
           MOVE FUNCTION ORD(N" ") TO ORD-R.
           DISPLAY "ORD-SP=" ORD-R.
      *> National LOW-VALUE = the position-0 character "C" (12.3.7 GR9).
           MOVE LOW-VALUE TO N-X.
           IF N-X = N"C" DISPLAY "LOW=C" ELSE DISPLAY "LOW=OTHER".
      *> National HIGH-VALUE = the largest unspecified code unit (GR8 + GR7 k3) - above A.
           MOVE HIGH-VALUE TO N-X.
           IF N-X > N-A DISPLAY "HIGH-GT-A=YES" ELSE DISPLAY "HIGH-GT-A=NO".
      *> A condition-name over a national variable compares under the same sequence (8.8.4.5 GR2).
           IF IS-BEE DISPLAY "88=YES" ELSE DISPLAY "88=NO".
           STOP RUN.
