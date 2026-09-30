      *> PB1689 control - un-reserving END-INVOKE must not break the vendor INVOKE terminator
      *>   after USING ... RETURNING (pb204_vlg_invoke with the terminator added).
      *> kb/Work PB204 - A VARIABLE-LENGTH GROUP ACROSS AN INVOKE BOUNDARY, both directions.
      *>
      *> 14.9.23.3 (INVOKE syntax rules) contains NO prohibition on a variable-length group operand - the one
      *> that exists, 14.9.4.3 SR12, is CALL FORMAT 1's and names identifier-2 only. What governs an INVOKE
      *> argument is 14.8.2.2: "If either the formal parameter or the argument is a variable length group, the
      *> formal parameter and the argument shall be compatible, as described in 8.5.1.12, Variable-length
      *> groups", and what governs the result is 14.8.3.2's identical sentence over the RETURNING pair. Both
      *> legs here are conforming source; both used to draw COBOLNET1688 at bind, and a method merely
      *> DECLARING such a formal used to emit the Tier-C loud in a body nobody could invoke.
      *>
      *> THE ARGUMENT'S PREFIX IS SPLIT 2+1 AGAINST THE FORMAL'S X(3) so the crossing is proved to line up by
      *> RELATIVE BYTE POSITION (8.5.1.12.2) rather than by member name or member count.
      *>
      *> EXPECTED VALUES, DERIVED:
      *>   M1 - the argument's fixed run is GP1+GP2 = "PF"+"X" and its dynamic component is GD, so the
      *>        formal's LGP takes "PFX" whole and LGD takes "abcde".
      *>   H1 - 14.9.23.4 GR8 delivers the returning item to identifier-4: the method built "RET"/"vwxyz".
      *>   G1 - 14.2.3 GR8: the method's store into its BY REFERENCE formal is visible in the argument.
      *>        LGP := "ZZZ" splices back across GP1 (2 positions) and GP2 (1), so GP1="ZZ" and GP2="Z";
      *>        LGD := "uv" replaces GD's whole current content.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB204VIN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB204C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB204C.
       01 G.
          05 GP1 PIC X(2).
          05 GP2 PIC X.
          05 GD PIC X DYNAMIC LENGTH.
       01 H.
          05 HP PIC X(3).
          05 HD PIC X DYNAMIC LENGTH.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB204C "NEW" RETURNING O
           MOVE "PF" TO GP1
           MOVE "X" TO GP2
           MOVE "abcde" TO GD
           INVOKE O "XFER" USING G RETURNING H END-INVOKE
           DISPLAY "H1=[" HP "][" HD "]"
           DISPLAY "G1=[" GP1 "][" GP2 "][" GD "]"
           STOP RUN.
       END PROGRAM PB204VIN.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB204C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.

       METHOD-ID. XFER.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LG.
          05 LGP PIC X(3).
          05 LGD PIC X DYNAMIC LENGTH.
       01 LH.
          05 LHP PIC X(3).
          05 LHD PIC X DYNAMIC LENGTH.
       PROCEDURE DIVISION USING LG RETURNING LH.
       MAIN-P.
           DISPLAY "M1=[" LGP "][" LGD "]"
           MOVE "RET" TO LHP
           MOVE "vwxyz" TO LHD
           MOVE "ZZZ" TO LGP
           MOVE "uv" TO LGD.
       END METHOD XFER.

       END OBJECT.
       END CLASS PB204C.
