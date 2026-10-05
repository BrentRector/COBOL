      *> kb/Work PB480 - a VARIABLE-LENGTH group and a FIXED-LENGTH group crossing an INVOKE through a UNIVERSAL
      *> object reference. 9.3.6 match rule 3 e) lists clauses a group carries none of, so the two groups MATCH;
      *> the bound method's conformance is 14.8.2.2: "If either the formal parameter or the argument is a variable
      *> length group, the formal parameter and the argument shall be compatible, as described in 8.5.1.12,
      *> Variable-length groups" (cite.py --check 14.8.2.2 -> OK 14.8.2.2 2)), and 8.5.1.12.2 / 8.5.1.12.3 make a
      *> dynamic-capacity table correspond to a FIXED table at the same position: "Two tables correspond if at least
      *> one of them is a dynamic-capacity table and they occupy the same relative byte positions within their
      *> groups" (OK 8.5.1.12.2); "If one of the corresponding tables is not a dynamic-capacity table, that table is
      *> treated as though it were a dynamic-capacity table" (OK 8.5.1.12.3). 14.2.3 GR8: the formal "occupies the
      *> same storage area as the argument" (OK 14.2.3 8)), so the method's store is seen by the caller. Before the
      *> fix the universal descriptor of a variable-length group was a signature STRING that no fixed group
      *> equalled, so both invocations ended in EC-OO-METHOD.
      *> DERIVATION:
      *>   TF  VG (HH + 3 dynamic elements AA BB CC) into a fixed formal with OCCURS 3: the formal sees HHAABBCC;
      *>       it stores XX into its second element                    -> TF:HHAABBCC, VG=HHAAXXCC
      *>   TV  FG (hh + aa bb cc, OCCURS 3) into a formal with a dynamic-capacity table: capacity 3, the elements
      *>       aa bb cc; it stores YY into its first element -> TV:hhaabbcc 0000000003, FG=hhYYbbcc
       >>TURN EC-OO CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB480V.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C480V.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U  USAGE OBJECT REFERENCE.
       01 VG.
          05 VH PIC X(2) VALUE "HH".
          05 VT PIC X(2) OCCURS DYNAMIC CAPACITY IN VCAP FROM 1 TO 5.
       01 FG.
          05 FH PIC X(2) VALUE "hh".
          05 FT PIC X(2) OCCURS 3.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-OO.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           INVOKE C480V "NEW" RETURNING U
           SET VCAP TO 3
           MOVE "AA" TO VT(1) MOVE "BB" TO VT(2) MOVE "CC" TO VT(3)
           INVOKE U "TF" USING VG
           DISPLAY "VG=" VH VT(1) VT(2) VT(3)
           MOVE "aa" TO FT(1) MOVE "bb" TO FT(2) MOVE "cc" TO FT(3)
           INVOKE U "TV" USING FG
           DISPLAY "FG=" FG
           DISPLAY "END".
           STOP RUN.
       END PROGRAM PB480V.

       IDENTIFICATION DIVISION.
       CLASS-ID. C480V INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TF.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LF.
          05 LH PIC X(2).
          05 LT PIC X(2) OCCURS 3.
       PROCEDURE DIVISION USING LF.
           DISPLAY "TF:" LF
           MOVE "XX" TO LT(2).
       END METHOD TF.
       METHOD-ID. TV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LV.
          05 LH2 PIC X(2).
          05 LT2 PIC X(2) OCCURS DYNAMIC CAPACITY IN LCAP FROM 1 TO 5.
       PROCEDURE DIVISION USING LV.
           DISPLAY "TV:" LH2 LT2(1) LT2(2) LT2(3) " " LCAP
           MOVE "YY" TO LT2(1).
       END METHOD TV.
       END OBJECT.
       END CLASS C480V.
