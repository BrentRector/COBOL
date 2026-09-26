      *> reject-at: 2002 2014 2023
      *> kb/Work PB1166 - the INVOKE lane (§14.8.2.3.2 BY REFERENCE and
      *> §14.8.3.3 RETURNING), the REJECT half.
      *>   cite.py --check 14.8.3.3 "Currency symbols match if and only if
      *>     the corresponding currency strings are the same"      OK 1)
      *>   cite.py --check 14.8.3.3 "Period picture symbols match if and
      *>     only if"                                               OK 2)
      *>   cite.py --check 14.8.2.3.2 "Comma picture symbols match if and
      *>     only if the DECIMAL-POINT IS COMMA clause is in effect for
      *>     both the activating and the activated runtime elements or
      *>     for neither of them"                                   OK 2) b)
      *> The class says CURRENCY "EUR" under the symbol $ and DECIMAL-POINT
      *> IS COMMA; this program says neither. Every pair below has the SAME
      *> character-string on both sides, so each refusal can only come from
      *> the axis it names:
      *>   GETC  RETURNING $$$9.99 - the symbols stand for "EUR" vs "$".
      *>   GETE  RETURNING Z9,99   - DPC in effect for one element only;
      *>         before PB1166 it ran and the receiver read 1234 for 12,34.
      *>   TAKE  USING BY REFERENCE Z9.99 - the same, the argument lane.
      *>   TAKEA USING BY REFERENCE A(5) into X(5) - two PICTURE clauses.
      *> Each is the §14.8.2.3.2 / §14.8.3.3 conformance diagnostic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W61BNEGINV.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS W61BNEGINVC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O   USAGE OBJECT REFERENCE W61BNEGINVC.
       01 RCU PIC $$$9.99.
       01 RDP PIC Z9,99.
       01 ADP PIC Z9.99.
       01 AAL PIC A(5).
       PROCEDURE DIVISION.
       MAIN.
           INVOKE W61BNEGINVC "NEW" RETURNING O
           INVOKE O "GETC" RETURNING RCU
           INVOKE O "GETE" RETURNING RDP
           INVOKE O "TAKE" USING ADP
           INVOKE O "TAKEA" USING AAL
           STOP RUN.
       END PROGRAM W61BNEGINV.
       IDENTIFICATION DIVISION.
       CLASS-ID. W61BNEGINVC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "EUR" WITH PICTURE SYMBOL "$"
           DECIMAL-POINT IS COMMA.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GETC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LC PIC $$$9.99.
       PROCEDURE DIVISION RETURNING LC.
           MOVE 1 TO LC.
       END METHOD GETC.
       METHOD-ID. GETE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LE PIC Z9,99.
       PROCEDURE DIVISION RETURNING LE.
           MOVE 12,34 TO LE.
       END METHOD GETE.
       METHOD-ID. TAKE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LT PIC Z9.99.
       PROCEDURE DIVISION USING LT.
           MOVE 1 TO LT.
       END METHOD TAKE.
       METHOD-ID. TAKEA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LX PIC X(5).
       PROCEDURE DIVISION USING LX.
           MOVE "A" TO LX.
       END METHOD TAKEA.
       END OBJECT.
       END CLASS W61BNEGINVC.
