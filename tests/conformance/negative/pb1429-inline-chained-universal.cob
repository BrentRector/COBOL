      *> reject-at: 2002 2014 2023
      *> ISO §8.4.3.4.3 SR2: "Identifier-1 shall be of class object; neither
      *> the predefined object reference NULL nor a universal object reference
      *> shall be specified."  Identifier is defined recursively (§8.4.3.1.3
      *> SR1), so in `A1 :: "UNIV" :: "GETNAME"` the inline invocation
      *> `A1 :: "UNIV"` IS identifier-1 of the second invocation, and UNIV
      *> returns a UNIVERSAL object reference (§8.4.3.4.4 GR1 b): the temporary
      *> has the description of the RETURNING item).  SR2 refuses that receiver
      *> with the same named diagnostic, COBOLNET2138, that a written universal
      *> identifier-1 draws.  kb/Work PB1429: the chained segment's receiver was
      *> never screened and the compile ended in the COBOLNET2362 internal error.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1429N1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1429C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A1 USAGE OBJECT REFERENCE PB1429C.
       01 W  PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1429C "NEW" RETURNING A1.
           MOVE A1 :: "UNIV" :: "GETNAME" TO W.
           DISPLAY W.
           STOP RUN.
       END PROGRAM PB1429N1.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1429C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. UNIV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-U USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION RETURNING LK-U.
       MAIN.
           SET LK-U TO SELF.
       END METHOD UNIV.
       METHOD-ID. GETNAME.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-NAME PIC X(8).
       PROCEDURE DIVISION RETURNING LK-NAME.
       MAIN.
           MOVE "ACCOUNT" TO LK-NAME.
       END METHOD GETNAME.
       END OBJECT.
       END CLASS PB1429C.
