      *> reject-at: 2002 2014 2023
      *> kb/Work PB1063 -- ISO 14.8.2.3.3 2): "If the formal parameter is
      *> of class pointer or an object reference described without the
      *> ACTIVE-CLASS phrase, the conformance rules shall be the same as
      *> if a SET statement were performed" with the argument as the
      *> sending operand. DP is a data-pointer restricted to REC-T
      *> (13.18.60.4 GR23) and LP is unrestricted; 14.9.39.3 SR19 refuses
      *> storing a restricted data-pointer into a receiver not restricted
      *> to the same type, so the INVOKE BY CONTENT is COBOLNET0828. (The
      *> BY REFERENCE twin was already refused by 14.8.2.3.2; before the
      *> fix this BY CONTENT form compiled and printed IN-M.)
       IDENTIFICATION DIVISION.
       CLASS-ID. N1063K INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP USAGE POINTER.
       PROCEDURE DIVISION USING BY REFERENCE LP.
           DISPLAY "IN-M"
           GOBACK.
       END METHOD M.
       END OBJECT.
       END CLASS N1063K.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N1063M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS N1063K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 REC-T IS TYPEDEF STRONG.
          05 A PIC X(2).
       01 DPT IS TYPEDEF USAGE POINTER TO REC-T.
       01 W TYPE REC-T.
       01 DP TYPE DPT.
       01 O USAGE OBJECT REFERENCE N1063K.
       PROCEDURE DIVISION.
           SET DP TO ADDRESS OF W
           INVOKE N1063K "NEW" RETURNING O
           INVOKE O "M" USING BY CONTENT DP
           STOP RUN.
       END PROGRAM N1063M.
