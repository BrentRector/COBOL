      *>--- conformance-case
      *> id: l1c10_figurative_code_values
      *> rules: [GR-8.3.3.6.4-5, DOC-A.1-74]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.3.3.6.4 GR1 — documented code values of ZERO SPACE QUOTE
      *>
      *> Row pinned: DOC-A.1-74 (both coded character sets); also
      *> GR-8.3.3.6.4-5 (space) and GR-8.3.3.6.4-8 (quote, first
      *> sentence) in the national context.
      *>  GR1 "When a figurative constant is used in a context
      *>    requiring national characters, the figurative constant
      *>    represents a national character value. ... the character
      *>    value representation of the figurative constant ZERO
      *>    (ZEROS, ZEROES), SPACE (SPACES), and QUOTE (QUOTES) is the
      *>    value of the character '0', space, and '"', respectively
      *>    ... The implementor shall specify the unique
      *>    representation of ZERO, SPACE, and QUOTE in the computer's
      *>    alphanumeric and national coded character sets."
      *>  docs/CONFORMANCE.md DOC-A.1-74 specifies it: SPACE U+0020,
      *>  ZERO U+0030, QUOTE U+0022; alphanumeric image 0x20/0x30/
      *>  0x22, national code unit 0x0020/0x0030/0x0022; FUNCTION
      *>  ORD 33/49/35 for PIC X and PIC N alike (no PROGRAM
      *>  COLLATING SEQUENCE).
      *> cite.py --check:
      *>  OK  §8.3.3.6.4 1)  (General rules)
      *>  OK  §8.3.3.6.4 5)  (General rules)
      *>  OK  §8.3.3.6.4 8)  (General rules)
      *>  OK  §8.3.3.2.4 4)  (General rules)   X"hh" bit configuration
      *>  OK  §8.3.3.5.4 4)  (General rules)   NX"hhhh" bit config.
      *>  OK  §15.70.4 1)  (Returned value rules)  ORD alphanumeric
      *>  OK  §15.70.4 2)  (Returned value rules)  ORD national
      *>
      *> Derivation of each expected line (8.3.3.2.4 GR4 / 8.3.3.5.4
      *> GR4 make X"20" etc. exactly the documented bit patterns, so
      *> equality is true only for the documented representation):
      *>  AN-SP=Y AN-ZR=Y AN-QT=Y   X(3) after MOVE SPACE / ZERO /
      *>       QUOTE equals X"202020" / X"303030" / X"222222".
      *>  NA-SP=Y NA-ZR=Y NA-QT=Y   N(3) (national context, GR1)
      *>       equals NX"002000200020" / NX"0030..." / NX"0022...".
      *>  ORD-X=033 049 035          ORD of PIC X holding each.
      *>  ORD-N=033 049 035          ORD of PIC N holding each.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C10B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X3 PIC X(3).
       01 N3 PIC N(3).
       01 X1 PIC X.
       01 N1 PIC N.
       01 R1 PIC X VALUE "N".
       01 R2 PIC X VALUE "N".
       01 R3 PIC X VALUE "N".
       01 O1 PIC 999.
       01 O2 PIC 999.
       01 O3 PIC 999.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE SPACE TO X3.
           IF X3 = X"202020" MOVE "Y" TO R1.
           MOVE ZERO TO X3.
           IF X3 = X"303030" MOVE "Y" TO R2.
           MOVE QUOTE TO X3.
           IF X3 = X"222222" MOVE "Y" TO R3.
           DISPLAY "AN-SP=" R1 " AN-ZR=" R2 " AN-QT=" R3.
           MOVE "N" TO R1 R2 R3.
           MOVE SPACE TO N3.
           IF N3 = NX"002000200020" MOVE "Y" TO R1.
           MOVE ZERO TO N3.
           IF N3 = NX"003000300030" MOVE "Y" TO R2.
           MOVE QUOTE TO N3.
           IF N3 = NX"002200220022" MOVE "Y" TO R3.
           DISPLAY "NA-SP=" R1 " NA-ZR=" R2 " NA-QT=" R3.
           MOVE SPACE TO X1.
           MOVE FUNCTION ORD (X1) TO O1.
           MOVE ZERO TO X1.
           MOVE FUNCTION ORD (X1) TO O2.
           MOVE QUOTE TO X1.
           MOVE FUNCTION ORD (X1) TO O3.
           DISPLAY "ORD-X=" O1 " " O2 " " O3.
           MOVE SPACE TO N1.
           MOVE FUNCTION ORD (N1) TO O1.
           MOVE ZERO TO N1.
           MOVE FUNCTION ORD (N1) TO O2.
           MOVE QUOTE TO N1.
           MOVE FUNCTION ORD (N1) TO O3.
           DISPLAY "ORD-N=" O1 " " O2 " " O3.
           STOP RUN.
