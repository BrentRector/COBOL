*>--- conformance-case
*> id: pb442_numeric_edited_national_class
*> rules: [GR-14.9.25.4-7]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB442 - a numeric-edited item of USAGE NATIONAL is CLASS NATIONAL (8.5.2.1 Table 2:
      *> "Numeric-edited (if usage is national)"), so a figurative constant against it is national and
      *> two of them compare in the national program collating sequence.
      *>
      *> THE RULES, and every expected value below derived from them (no observation):
      *>  14.9.25.4 GR7 Table 17 - HIGH-VALUE / LOW-VALUE / QUOTE moved to a Numeric-edited receiver are
      *>    of category Alphanumeric "if usage is display" and National "if usage is national".
      *>  8.3.3.6.4 GR1 - a figurative constant "used in a context requiring national characters"
      *>    represents a national character value; GR7 - LOW-VALUE is the character with the lowest
      *>    ordinal position in the program collating sequence of its class.
      *>  12.3.7 - the ALPHABET clause below makes "2" the FIRST alphanumeric character, so the
      *>    alphanumeric LOW-VALUE is "2"; no national program collating sequence is specified, so the
      *>    national LOW-VALUE is the native one, which is not N"2".
      *>  8.8.4.2.9 - two operands of class national compare in the national program collating sequence
      *>    (native: "1" before "2"); 8.8.4.2.7 - two of class alphanumeric in the alphanumeric one ("2"
      *>    before "1").
      *>  Table 17's Numeric row is Alphanumeric whatever the usage, so LOW-VALUE moved to PIC 9 and to
      *>    PIC 9 USAGE NATIONAL is the alphanumeric "2", and an alphanumeric sender is moved to a numeric
      *>    receiver as an unsigned integer (14.9.25.4 GR6): both hold 2.
      *>  8.4.3.3.4 GR6 c) - a reference-modified numeric item of usage national is class and category
      *>    national, so MOVE SPACE TO NR9(1:1) stores a space in its first character position and leaves
      *>    the other two: NR9 shows " 23".
      *>  14.9.22 INSPECT - a figurative in TALLYING takes identifier-1's class; LOW-VALUE (national) occurs
      *>    nowhere in the national digits "22", so the tally is 0.
      *>
      *> WHY EACH LEG CAN FAIL: each one read the CATEGORY numeric-edited, whose class for usage display is
      *> alphanumeric, instead of the CLASS: the national receiver took the alphanumeric LOW-VALUE "2"
      *> (NEN-ALPHANUMERIC), the national pair collated by the alphanumeric sequence (NAT-ORDER-PCS), and
      *> INSPECT counted the two "2"s (TALLY-2); and a national numeric receiver had no character image, so
      *> the LOW-VALUE fill stopped the run unit and the slice store was lost (REFMOD-[123]).
      *>
      *> EDITIONS: 2002 - USAGE NATIONAL's introducing edition; 2014 behaves the same. At 2023 the MOVE of
      *> LOW-VALUE to a numeric-edited item is removed (14.9.25.3 SR5, COBOLNET0902).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB442NEN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. PB442HOST PROGRAM COLLATING SEQUENCE IS ALPHA1.
       SPECIAL-NAMES. ALPHABET ALPHA1 IS "2" "1".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NEN PIC Z9 USAGE NATIONAL.
       01 NED PIC Z9.
       01 NEN1 PIC Z9 USAGE NATIONAL.
       01 NEN2 PIC Z9 USAGE NATIONAL.
       01 NED1 PIC Z9.
       01 NED2 PIC Z9.
       01 NEN3 PIC Z9 USAGE NATIONAL.
       01 CNT PIC 9 VALUE 0.
       01 N9 PIC 9.
       01 NN9 PIC 9 USAGE NATIONAL.
       01 NR9 PIC 9(3) USAGE NATIONAL VALUE 123.
       PROCEDURE DIVISION.
       M.
           MOVE LOW-VALUE TO NEN
           IF NEN(1:1) = N"2"
               DISPLAY "NEN-ALPHANUMERIC"
           ELSE
               DISPLAY "NEN-NATIONAL"
           END-IF
           MOVE LOW-VALUE TO NED
           IF NED(1:1) = "2"
               DISPLAY "NED-ALPHANUMERIC"
           ELSE
               DISPLAY "NED-NATIONAL"
           END-IF
           MOVE 1 TO NEN1 NED1
           MOVE 2 TO NEN2 NED2
           IF NEN1 < NEN2
               DISPLAY "NAT-ORDER-NATIONAL"
           ELSE
               DISPLAY "NAT-ORDER-PCS"
           END-IF
           IF NED1 < NED2
               DISPLAY "DSP-ORDER-NATIVE"
           ELSE
               DISPLAY "DSP-ORDER-PCS"
           END-IF
           MOVE 22 TO NEN3
           INSPECT NEN3 TALLYING CNT FOR ALL LOW-VALUE
           DISPLAY "TALLY-" CNT
           MOVE LOW-VALUE TO N9 NN9
           DISPLAY "NUM-" N9 " " NN9
           MOVE SPACE TO NR9(1:1)
           DISPLAY "REFMOD-[" NR9 "]"
           STOP RUN.
