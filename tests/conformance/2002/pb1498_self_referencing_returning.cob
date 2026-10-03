      *> kb/Work PB1498 - ISO 9.3.8.2.3: "If the description of the
      *> returning item of a method in interface-1 directly or indirectly
      *> references interface-2, the description of the returning item of
      *> the corresponding method in interface-2 shall not directly or
      *> indirectly reference interface-1." The sentence is about two
      *> DISTINCT interfaces ("If two interfaces are of the same interface,
      *> they conform to each other"), so a returning item that references
      *> its OWN class or interface is legal (owner decision kb/Work R63:
      *> self-references are exempt). Both pairs below conform:
      *>  1) SUBNODE's NEXT overrides NODE's NEXT; both return a NODE. The
      *>     override's returning item references NODE (interface-2), but
      *>     NODE's returning item reaches only NODE, never SUBNODE.
      *>  2) CNODE implements INODE, whose NEXT returns an INODE. CNODE's
      *>     NEXT references INODE (interface-2); INODE's reaches only INODE.
      *> EXPECTED: compiles; the overriding NEXT runs (S-NEXT), the
      *> implementing NEXT runs through the interface-typed reference
      *> (C-NEXT).
       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB1498I.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. NEXT-ONE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R USAGE OBJECT REFERENCE PB1498I.
       PROCEDURE DIVISION RETURNING R.
       END METHOD NEXT-ONE.
       END INTERFACE PB1498I.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1498N INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. CLASS BASE CLASS PB1498N.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. NEXT-ONE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R USAGE OBJECT REFERENCE PB1498N.
       PROCEDURE DIVISION RETURNING R.
           DISPLAY "N-NEXT"
           SET R TO SELF
           GOBACK.
       END METHOD NEXT-ONE.
       END OBJECT.
       END CLASS PB1498N.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1498S INHERITS FROM PB1498N.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. CLASS PB1498N CLASS PB1498S.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. NEXT-ONE OVERRIDE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R USAGE OBJECT REFERENCE PB1498N.
       PROCEDURE DIVISION RETURNING R.
           DISPLAY "S-NEXT"
           SET R TO SELF
           GOBACK.
       END METHOD NEXT-ONE.
       END OBJECT.
       END CLASS PB1498S.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1498C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. CLASS BASE CLASS PB1498C INTERFACE PB1498I.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS PB1498I.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. NEXT-ONE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R USAGE OBJECT REFERENCE PB1498I.
       PROCEDURE DIVISION RETURNING R.
           DISPLAY "C-NEXT"
           SET R TO SELF
           GOBACK.
       END METHOD NEXT-ONE.
       END OBJECT.
       END CLASS PB1498C.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1498R.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. CLASS PB1498N CLASS PB1498S CLASS PB1498C
           INTERFACE PB1498I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N USAGE OBJECT REFERENCE PB1498N.
       01 N2 USAGE OBJECT REFERENCE PB1498N.
       01 S USAGE OBJECT REFERENCE PB1498S.
       01 I USAGE OBJECT REFERENCE PB1498I.
       01 I2 USAGE OBJECT REFERENCE PB1498I.
       01 C USAGE OBJECT REFERENCE PB1498C.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1498S "NEW" RETURNING S
           SET N TO S
           INVOKE N "NEXT-ONE" RETURNING N2
           INVOKE PB1498C "NEW" RETURNING C
           SET I TO C
           INVOKE I "NEXT-ONE" RETURNING I2
           STOP RUN.
       END PROGRAM PB1498R.
