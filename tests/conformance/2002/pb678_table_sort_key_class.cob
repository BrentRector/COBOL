      *> ISO 14.9.40.4 GR5 - the SAME two-sequence rule, on the OTHER side of the format split and
      *> on the OTHER precedence step. gold/pb678_sort_national_key_collating pins GR5a (the
      *> statement phrase) for the FILE sort; this file pins GR5b (the PROGRAM collating sequences)
      *> for the FORMAT-2 TABLE sort, and the one place the two precedence steps meet: a statement
      *> phrase naming ONE class leaves the OTHER class on its program collating sequence.
      *>
      *> IT ALSO PINS THE CLASS GR5 NAMES NO SEQUENCE FOR THAT A KEY MAY BE. GR5 speaks only of "key
      *> data items of class alphabetic and class alphanumeric" and "key data items of class
      *> national"; GR19 sends the comparison itself to the relation-condition rules, where 8.8.4.2.4
      *> makes a numeric comparison algebraic - it may not be weighed. Before kb/Work PB678 the table
      *> sort applied the ONE resolved alphanumeric sequence to every non-numeric key; REV-AN below
      *> deliberately reorders the digits '1' and '0' so a numeric key read as characters would sort
      *> differently from its value. (A BOOLEAN key - an elementary bit item or a bit group - is not a
      *> legal key at all: 14.9.40.3 SR14 c) "Key data items shall not be of class boolean, object, or
      *> pointer", refused by negative/pb1173-sort-table-key-boolean; this file used to sort on one.)
      *>
      *> A NATIONAL GROUP KEY IS A NATIONAL OPERAND, NOT A BYTE IMAGE. 13.18.29.4 GR2b makes a
      *> GROUP-USAGE NATIONAL group "an elementary data item of usage national ... PICTURE N(m)",
      *> so its key value is its m national POSITIONS - the generated AsNat() - and it collates
      *> under the NATIONAL sequence. Read through AsImage() it would have been 2m UTF-16BE bytes
      *> under the ALPHANUMERIC sequence, the same defect one shape over (kb/Work PB327's channel).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB678TABNAT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. GENERIC-BOX
           PROGRAM COLLATING SEQUENCE
               FOR ALPHANUMERIC IS REV-AN
               FOR NATIONAL IS REV-NAT.
       SPECIAL-NAMES.
           ALPHABET REV-AN IS "CBA10"
           ALPHABET REV-NAT FOR NATIONAL IS N"BAC"
           ALPHABET NAT-PLAIN FOR NATIONAL IS NATIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
      *> One table per leg: the element storage of a Format-2 sort is the TYPED record struct, and
      *> keeping the classes apart keeps each expected order derivable from one rule.
       01 TN-TAB.
          05 EN OCCURS 3 TIMES.
             10 TN PIC N(1).
             10 NP PIC X(3).
       01 TA-TAB.
          05 EA OCCURS 3 TIMES.
             10 TA PIC X(1).
             10 AP PIC X(3).
       01 TC-TAB.
          05 ENM OCCURS 3 TIMES.
             10 TC PIC 9(2).
             10 CP PIC X(3).
       01 TG-TAB.
          05 EG OCCURS 3 TIMES.
             10 TG GROUP-USAGE NATIONAL.
                15 TG1 PIC N(1).
             10 GP PIC X(3).
       PROCEDURE DIVISION.
       MAIN.
      *> LEG 1 - GR5b: no statement phrase, so the NATIONAL key takes the PROGRAM national
      *> collating sequence REV-NAT (N"BAC"): B < A < C.
           MOVE N"A" TO TN(1). MOVE "AAA" TO NP(1).
           MOVE N"B" TO TN(2). MOVE "BBB" TO NP(2).
           MOVE N"C" TO TN(3). MOVE "CCC" TO NP(3).
           SORT EN ON ASCENDING KEY TN.
           DISPLAY "T1=" NP(1) " " NP(2) " " NP(3).
      *> LEG 2 - GR5a OVERRIDES GR5b for the class it names: alphabet-name-2 NAT-PLAIN is the
      *> NATIVE national sequence, so this sort ignores the program's REV-NAT: A < B < C.
           MOVE N"A" TO TN(1). MOVE "AAA" TO NP(1).
           MOVE N"B" TO TN(2). MOVE "BBB" TO NP(2).
           MOVE N"C" TO TN(3). MOVE "CCC" TO NP(3).
           SORT EN ON ASCENDING KEY TN
               COLLATING SEQUENCE FOR NATIONAL IS NAT-PLAIN.
           DISPLAY "T2=" NP(1) " " NP(2) " " NP(3).
      *> LEG 3 - THE CONTROL for the alphanumeric half of the PROGRAM sequence: REV-AN ("CBA10")
      *> IS live and DOES apply to a key of class alphanumeric: C < B < A.
           MOVE "A" TO TA(1). MOVE "AAA" TO AP(1).
           MOVE "B" TO TA(2). MOVE "BBB" TO AP(2).
           MOVE "C" TO TA(3). MOVE "CCC" TO AP(3).
           SORT EA ON ASCENDING KEY TA.
           DISPLAY "T3=" AP(1) " " AP(2) " " AP(3).
      *> LEG 4 - a NUMERIC key takes NO sequence (GR5 names no sequence for class numeric; 8.8.4.2.4
      *> compares algebraic VALUES). Value order is 05 < 11 < 20, i.e. ONE < TWO < FOR. Under REV-AN
      *> the digit '1' sits at position 4 and '0' at position 5 (every other digit after them), so the
      *> keys read as CHARACTERS would order 11 < 05 < 20 - TWO ONE FOR - which is what a sort that
      *> weighed the key would answer.
           MOVE 05 TO TC(1). MOVE "ONE" TO CP(1).
           MOVE 20 TO TC(2). MOVE "FOR" TO CP(2).
           MOVE 11 TO TC(3). MOVE "TWO" TO CP(3).
           SORT ENM ON ASCENDING KEY TC.
           DISPLAY "T4=" CP(1) " " CP(2) " " CP(3).
      *> LEG 5 - a NATIONAL GROUP key (13.18.29.4 GR2b) under the program national sequence
      *> REV-NAT: B < A < C, the same answer LEG 1 gives for the elementary national key. The
      *> agreement IS the requirement - one operand rule, two spellings of the same operand.
           MOVE N"A" TO TG1(1). MOVE "AAA" TO GP(1).
           MOVE N"B" TO TG1(2). MOVE "BBB" TO GP(2).
           MOVE N"C" TO TG1(3). MOVE "CCC" TO GP(3).
           SORT EG ON ASCENDING KEY TG.
           DISPLAY "T5=" GP(1) " " GP(2) " " GP(3).
           STOP RUN.
