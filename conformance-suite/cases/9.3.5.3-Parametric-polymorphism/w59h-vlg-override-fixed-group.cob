*>--- conformance-case
*> id: w59h-vlg-override-fixed-group
*> rules: [GR-9.3.5.3-7]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1519 - GR-9.3.5.3-7's variable-length-group component of
      *> the method resolution signature, the REJECT half.
      *>   cite.py --check 9.3.5.3 "If the parameter is a variable-length
      *>     group, sufficient information to determine if this group would
      *>     match the group is specified in the invoke statement." -> OK
      *>     9.3.5.3 7)
      *>   cite.py --check 11.7.3 "If the OVERRIDE phrase is specified,
      *>     there shall be a method with the same method resolution
      *>     signature" -> OK 11.7.3 3)
      *> The overridden TAKE's formal is a VARIABLE-LENGTH group (a
      *> dynamic-capacity table); the overriding TAKE's is a FIXED-length
      *> group whose OCCURS 1 table has the same element and the same
      *> collapsed width. 8.5.1.12.3 would call the two groups compatible,
      *> but a fixed-length group carries no variable-length-group signature
      *> information, so no superclass method has the SAME signature and
      *> 11.7.3 SR3 is violated: COBOLNET0829. Before wave 59 H this program
      *> passed the binder and died in Roslyn (CS0115, "no suitable method
      *> found to override"). The positive twin is
      *> 2014/w59h_vlg_override_signature.
       IDENTIFICATION DIVISION.
       CLASS-ID. W59HNB INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TAKE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-G.
          05 LK-P PIC X(2).
          05 LK-T PIC X(2) OCCURS DYNAMIC CAPACITY IN LK-C FROM 1.
       PROCEDURE DIVISION USING LK-G.
       MAIN.
           DISPLAY "BASE C=" LK-C.
       END METHOD TAKE.
       END OBJECT.
       END CLASS W59HNB.

       IDENTIFICATION DIVISION.
       CLASS-ID. W59HNS INHERITS FROM W59HNB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS W59HNB.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TAKE OVERRIDE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-G.
          05 LK-P PIC X(2).
          05 LK-T PIC X(2) OCCURS 1.
       PROCEDURE DIVISION USING LK-G.
       MAIN.
           DISPLAY "SUB P=" LK-P " T1=" LK-T(1) .
       END METHOD TAKE.
       END OBJECT.
       END CLASS W59HNS.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. W59HNM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS W59HNB
           CLASS W59HNS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE W59HNB.
       01 G.
          05 P PIC X(2).
          05 T PIC X(2) OCCURS DYNAMIC CAPACITY IN C FROM 1.
       PROCEDURE DIVISION.
           MOVE "PP" TO P
           MOVE "aa" TO T(1)
           MOVE "bb" TO T(2)
           INVOKE W59HNS "NEW" RETURNING O
           INVOKE O "TAKE" USING G
           STOP RUN.
       END PROGRAM W59HNM.
