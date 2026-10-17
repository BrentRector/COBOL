*>--- conformance-case
*> id: pb649_national_figurative_collation
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.8.4.2.6, comparison of alphanumeric and national operands: "Two operands, one class
      *> alphanumeric and one class national, may be compared. The alphanumeric operand is treated as though
      *> it were converted and moved in accordance with the rules of the MOVE statement from an alphanumeric
      *> elementary data item to a temporary elementary data item of class national", after which 8.8.4.2.9
      *> governs - "the collating sequence of characters specified for the current national program
      *> collating sequence". The ALPHANUMERIC program collating sequence applies to 8.8.4.2.7's pair only.
      *>
      *> 8.3.3.6.3 SR2 gives `ALL literal-1` its literal's own class ("Literal-1 shall be an alphanumeric,
      *> boolean, or national literal"), so `ALL N"AB"` is class NATIONAL and the two lines below are the
      *> SAME comparison: one class-national operand and one class-alphanumeric operand. They answered
      *> OPPOSITELY (kb/Work PB649) because the figurative branch of the relation renderer derived the
      *> comparison class from ONE operand - the non-figurative anchor - and then handed that answer back to
      *> itself as the figurative's category, so the pair rule was asked a two-operand question with one
      *> operand's answer twice. The decision is now made once, from both operands, and both branches read it.
      *>
      *> POSITIONS. `ALPHABET AL IS "ZYXWVUTSRQPONMLKJIHGFEDCBA"` - 12.3.7.4 GR7 k) 2.: a literal list
      *> assigns "a successive ascending position in the collating sequence being specified" in the order
      *> written, so Z=1, Y=2 ... B=25, A=26. Under AL, "AB" collates AFTER "AC" (position 2: B=25 > C=24).
      *> The NATIONAL sequence is untouched by it (12.3.6 GR11 names a FOR NATIONAL alphabet separately and
      *> none is declared), so nationally "AB" < "AC" by the native order.
      *>
      *> EXPECTED VALUES, FROM THE RULES:
      *>   FIG-LT  - ALL N"AB" (national) vs XA "AC" (alphanumeric): 8.8.4.2.6 -> national -> "AB" < "AC" => 1
      *>   ITEM-LT - NB (national item, same value) vs XA: the identical rule and answer                 => 1
      *>   FIG-NAT - ALL N"AB" vs NB: both national, equal values, so NOT less                           => 0
      *>   AN-LT   - XB "AB" vs XA "AC": both alphanumeric, so 8.8.4.2.7 under AL, where "AB" > "AC"      => 0
      *>             (the CONTROL that proves the alphanumeric PCS is still applied where it belongs)
      *>   FIG-EQ  - ALL N"AB" = NB: equal under the national rules                                      => 1
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB649NAT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XX PROGRAM COLLATING SEQUENCE AL.
       SPECIAL-NAMES. ALPHABET AL IS "ZYXWVUTSRQPONMLKJIHGFEDCBA".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XA PIC X(2) VALUE "AC".
       01 XB PIC X(2) VALUE "AB".
       01 NB PIC N(2) VALUE N"AB".
       PROCEDURE DIVISION.
       MAIN.
           IF ALL N"AB" < XA
               DISPLAY "FIG-LT=1" ELSE DISPLAY "FIG-LT=0" END-IF
           IF NB < XA
               DISPLAY "ITEM-LT=1" ELSE DISPLAY "ITEM-LT=0" END-IF
           IF ALL N"AB" < NB
               DISPLAY "FIG-NAT=1" ELSE DISPLAY "FIG-NAT=0" END-IF
           IF XB < XA
               DISPLAY "AN-LT=1" ELSE DISPLAY "AN-LT=0" END-IF
           IF ALL N"AB" = NB
               DISPLAY "FIG-EQ=1" ELSE DISPLAY "FIG-EQ=0" END-IF
           STOP RUN.
