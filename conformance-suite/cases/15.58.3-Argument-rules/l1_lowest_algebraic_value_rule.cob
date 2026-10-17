*>--- conformance-case
*> id: l1_lowest_algebraic_value_rule
*> rules: [AR-15.58.3-1, AR-15.58.3-2, RV-15.58.4-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.58.4 rule 2 — the LOWEST-ALGEBRAIC returned value, over the shapes the clause's own NOTE
      *> enumerates, plus the §15.58.3 rule 2 ADMIT side (that bar is CONDITIONAL on standard-decimal).
      *>
      *> THE RULE: "The value returned is equal to the lowest finite algebraic value that may be represented
      *> in argument-1." (§15.58.4 r2). Its NOTE tabulates seven argument shapes; the first seven DISPLAYs
      *> below reproduce that table, each value re-derived from the description rather than copied:
      *>   S999                  signed, 3 digit positions, scale 0 -> the all-nines magnitude negated: -999
      *>   S9(4) USAGE BINARY    §13.18.60.4 GR4 sizes USAGE BINARY to "the maximum range of values implied by
      *>                         the associated decimal picture character-string", so the PICTURE's 4 digits
      *>                         bound it (not the 2-byte container): -9999
      *>   99V9(3)               UNSIGNED — no negative value is representable, so the lowest IS zero, carried
      *>                         at the item's own scale: 0.000
      *>   $**,**9.99BCR         numeric-edited: 5 integer digit positions (4 '*' + '9') and 2 fraction ones;
      *>                         '$' is a FIXED insertion (one occurrence) and 'B' an insertion, neither a
      *>                         digit position; 'CR' makes the mask sign-representable: -99999.99
      *>   $**,**9.99            the same capacity with NO sign symbol anywhere in the mask, so no negative
      *>                         value may be represented: 0
      *>   BINARY-CHAR SIGNED    §13.18.60.4 GR21 leaves the representation and length implementor-defined;
      *>                         COBOL.NET documents 1 byte, two's complement (CONFORMANCE.md A.1 items
      *>                         206/207), which is exactly the NOTE's "assuming an 8-bit twos-complement
      *>                         representation": -128
      *>   BINARY-CHAR UNSIGNED  the same container unsigned — §13.18.60.4 GR12's range for it starts at 0
      *>                         and no negative value is representable: 0
      *>
      *> THE EIGHTH LINE IS §15.58.3 RULE 2's ADMIT SIDE. Rule 2 bars a standard binary floating-point
      *> argument-1 only "If standard-decimal arithmetic is in effect"; no ARITHMETIC clause is written here,
      *> so §11.9.5.2 GR4 makes NATIVE arithmetic effective and the FLOAT-BINARY-64 item is LEGAL source.
      *> §13.18.60.4 GR15 pins that usage to ISO/IEC 60559 binary64, whose lowest FINITE value is
      *> -(2 - 2**-52) x 2**1023 = -1.7976931348623157E+308. The bound is one-sided on purpose: any literal
      *> farther from zero than the carrier's own maximum (1.798E+308, say) is not a representable binary64
      *> literal, so a two-sided test cannot be written under this mode. The REJECT side of rule 2 is
      *> conformance:cases/15.58.3-Argument-rules/l1-lowest-algebraic-std-decimal-binary-float — the pair is what proves the bar
      *> is mode-conditional rather than a blanket refusal of floating-point arguments.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1LOWRV2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A-S999    PIC S999.
       01 B-S9KBIN  PIC S9(4) USAGE BINARY.
       01 C-99V999  PIC 99V9(3).
       01 D-EDCR    PIC $**,**9.99BCR.
       01 E-EDPLAIN PIC $**,**9.99.
       01 F-BCS     USAGE BINARY-CHAR SIGNED.
       01 G-BCU     USAGE BINARY-CHAR UNSIGNED.
       01 H-FB64    USAGE FLOAT-BINARY-64.
       01 SR        PIC -9(6).9(3).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION LOWEST-ALGEBRAIC(A-S999)    TO SR
           DISPLAY "S999=" SR
           MOVE FUNCTION LOWEST-ALGEBRAIC(B-S9KBIN)  TO SR
           DISPLAY "S9K-BINARY=" SR
           MOVE FUNCTION LOWEST-ALGEBRAIC(C-99V999)  TO SR
           DISPLAY "U99V999=" SR
           MOVE FUNCTION LOWEST-ALGEBRAIC(D-EDCR)    TO SR
           DISPLAY "EDITED-CR=" SR
           MOVE FUNCTION LOWEST-ALGEBRAIC(E-EDPLAIN) TO SR
           DISPLAY "EDITED-PLAIN=" SR
           MOVE FUNCTION LOWEST-ALGEBRAIC(F-BCS)     TO SR
           DISPLAY "BINCHAR-SIGNED=" SR
           MOVE FUNCTION LOWEST-ALGEBRAIC(G-BCU)     TO SR
           DISPLAY "BINCHAR-UNSIGNED=" SR
           IF FUNCTION LOWEST-ALGEBRAIC(H-FB64) < -1.79E+308
               DISPLAY "FB64-NATIVE=OK"
           ELSE
               DISPLAY "FB64-NATIVE=BAD"
           END-IF
           STOP RUN.
       END PROGRAM L1LOWRV2.
