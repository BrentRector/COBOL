      *> kb/Work PB1781 - a GROUP argument crosses an INVOKE through a UNIVERSAL object reference exactly as it
      *> crosses through a typed one: the callee sees the group's characters and what it stores is visible in the
      *> caller's group after the call. Before the fix every program below was a backend CS0029 ("Cannot implicitly
      *> convert type 'string' to <the group record struct>") - the universal lane boxed the group's record struct
      *> but copied the box back as a string.
      *> ISO 14.9.23.3 SR6: "If identifier-1 references a universal object reference, neither the BY CONTENT nor the BY
      *>   VALUE phrase shall be specified and the BY REFERENCE phrase, if not specified explicitly, is assumed
      *>   implicitly" (cite.py --check 14.9.23.3 -> OK 14.9.23.3 6)).
      *> ISO 14.2.3 GR8: "If the argument is passed by reference, the activated runtime element operates as if the
      *>   formal parameter occupies the same storage area as the argument" (cite.py --check 14.2.3 -> OK 14.2.3 8)),
      *>   so a MOVE to the formal in the method is a store into the caller's group.
      *> ISO 9.3.6 match rule 3 c) - the argument and the formal "is the same class and category" (cite.py --check
      *>   9.3.6 -> OK 9.3.6 3) c)) - and 14.8.2.2 rule 1 (an alphanumeric group formal of the SAME size as the
      *>   argument) make the method MATCH, so it is the one invoked.
      *> ISO 14.9.23.4 GR8: "If a RETURNING phrase is specified, the result of the activated method is placed into
      *>   identifier-4" (cite.py --check 14.9.23.4 -> OK 14.9.23.4 8)).
      *> DERIVATION:
      *>   TG  group G = "ABCD" crosses; the method shows TG:ABCD and stores "WXYZ"           -> a=WXYZ
      *>   TN  a NATIONAL group (as-if PICTURE N(3), 13.18.29.4 GR2 b) crosses as its 3 positions;
      *>       the method shows TN:ABC and stores N"XYZ"                                       -> b=XYZ
      *>   TR  a group delivered through RETURNING: the method's returning item "RR42"         -> c=RR42
      *> An 8-character group into a 4-character group formal (14.8.2.2 rule 1's prefix) needs the MATCH relation of
      *> kb/Work PB480 and is not pinned here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1781A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C1781A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U USAGE OBJECT REFERENCE.
       01 G.
          05 G1 PIC X(4) VALUE "ABCD".
       01 NG GROUP-USAGE NATIONAL.
          05 NG1 PIC N(3) VALUE N"ABC".
       01 GR.
          05 GR1 PIC X(2) VALUE "..".
          05 GR2 PIC 9(2) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE C1781A "NEW" RETURNING U
           INVOKE U "TG" USING G
           DISPLAY "a=" G
           INVOKE U "TN" USING NG
           DISPLAY "b=" NG
           INVOKE U "TR" RETURNING GR
           DISPLAY "c=" GR
           STOP RUN.
       END PROGRAM PB1781A.

       IDENTIFICATION DIVISION.
       CLASS-ID. C1781A INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TG.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LG.
          05 L1 PIC X(4).
       PROCEDURE DIVISION USING LG.
           DISPLAY "TG:" LG
           MOVE "WXYZ" TO LG.
       END METHOD TG.
       METHOD-ID. TN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LN GROUP-USAGE NATIONAL.
          05 LN1 PIC N(3).
       PROCEDURE DIVISION USING LN.
           DISPLAY "TN:" LN
           MOVE N"XYZ" TO LN.
       END METHOD TN.
       METHOD-ID. TR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR.
          05 LR1 PIC X(2).
          05 LR2 PIC 9(2).
       PROCEDURE DIVISION RETURNING LR.
           MOVE "RR" TO LR1
           MOVE 42 TO LR2.
       END METHOD TR.
       END OBJECT.
       END CLASS C1781A.
