*>--- conformance-case
*> id: oo_crossing_byte_form_window
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB187 -- a float / binary formal that is a WINDOW (a member of a REDEFINES class) crosses an OO
      *> method boundary with its plain twin on the other side: the invoker's argument, the interface prototype,
      *> and the universal-dispatch box. ISO 14.2.2 SR1 bars REDEFINES on a formal's own entry, and its NOTE 2
      *> says "a data item defined subsequently in the linkage section can specify REDEFINES data-name-1" -- so
      *> each formal here is legal, and each argument (itself REDEFINED in the caller) is legal too.
      *> 14.2.3 GR8: "the formal parameter occupies the same storage area as the argument", so every ADD 1 is seen by
      *> the invoker. Starting from V = 2.5 and B = -12, three activations (universal, interface-typed, and a
      *> method whose formals are NOT windows) each display the values they received and add 1 to both:
      *>   M1 2.5 -0012 / 3.5 -0011 / M1 3.5 -0011 / 4.5 -0010 / M2 4.5 -0010 / 5.5 -0009
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB187XNG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB187C
           INTERFACE PB187I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE.
       01 I USAGE OBJECT REFERENCE PB187I.
       01 V USAGE FLOAT-LONG VALUE 2.5.
       01 VR REDEFINES V PIC X(8).
       01 B PIC S9(4) BINARY VALUE -12.
       01 BR REDEFINES B PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB187C "NEW" RETURNING O.
           INVOKE O "M1" USING V B.
           DISPLAY V " " B.
           INVOKE PB187C "NEW" RETURNING I.
           INVOKE I "M1" USING V B.
           DISPLAY V " " B.
           INVOKE O "M2" USING V B.
           DISPLAY V " " B.
           STOP RUN.
       END PROGRAM PB187XNG.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB187I.
       PROCEDURE DIVISION.
       METHOD-ID. M1.
       DATA DIVISION.
       LINKAGE SECTION.
       01  F USAGE FLOAT-LONG.
       01  G PIC S9(4) BINARY.
       PROCEDURE DIVISION USING F G.
       END METHOD M1.
       END INTERFACE PB187I.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB187C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE PB187I.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS PB187I.
       PROCEDURE DIVISION.
       METHOD-ID. M1.
       DATA DIVISION.
       LINKAGE SECTION.
       01  F USAGE FLOAT-LONG.
       01  FR REDEFINES F PIC X(8).
       01  G PIC S9(4) BINARY.
       01  GR REDEFINES G PIC X(2).
       PROCEDURE DIVISION USING F G.
           DISPLAY "M1 " F " " G.
           ADD 1 TO F G.
       END METHOD M1.
       METHOD-ID. M2.
       DATA DIVISION.
       LINKAGE SECTION.
       01  F USAGE FLOAT-LONG.
       01  G PIC S9(4) BINARY.
       PROCEDURE DIVISION USING F G.
           DISPLAY "M2 " F " " G.
           ADD 1 TO F G.
       END METHOD M2.
       END OBJECT.
       END CLASS PB187C.
