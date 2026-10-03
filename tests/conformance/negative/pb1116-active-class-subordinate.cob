      *> reject-at: 2002 2014 2023
      *> kb/Work PB1116 - ISO 14.9.23.3 SR13: "If Identifier-3,
      *> identifier-4, or identifier-5 references a group item, there
      *> shall not be an item subordinate to that group item that is an
      *> object reference described with the ACTIVE-CLASS phrase." G is a
      *> strongly-typed group (the only legal home of a subordinate object
      *> reference, 13.18.60.3) whose TA is ACTIVE-CLASS, passed BY
      *> REFERENCE and BY CONTENT to TAKEG: COBOLNET2728 naming SR13 on
      *> each. (Before the fix both were refused only as the "Tier-C byte
      *> island" - a carrier limit, not the rule.)
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1116A INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. CLASS BASE CLASS PB1116A.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T TYPEDEF STRONG.
          05 TA USAGE OBJECT REFERENCE ACTIVE-CLASS.
          05 TX PIC X(3).
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. RUNIT.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 G TYPE T.
       PROCEDURE DIVISION.
           INVOKE SELF "TAKEG" USING G
           INVOKE SELF "TAKEG" USING BY CONTENT G
           GOBACK.
       END METHOD RUNIT.
       IDENTIFICATION DIVISION.
       METHOD-ID. TAKEG.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LG TYPE T.
       PROCEDURE DIVISION USING LG.
           DISPLAY "LX=" TX OF LG
           GOBACK.
       END METHOD TAKEG.
       END OBJECT.
       END CLASS PB1116A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1116M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. CLASS PB1116A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB1116A.
       PROCEDURE DIVISION.
           INVOKE PB1116A "NEW" RETURNING O
           INVOKE O "RUNIT"
           STOP RUN.
       END PROGRAM PB1116M.
