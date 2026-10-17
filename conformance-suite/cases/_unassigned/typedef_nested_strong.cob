*>--- conformance-case
*> id: typedef_nested_strong
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> Nested STRONG TYPEDEF (data-model D17; review DEVLOG 664 fixes #5/#6). A strong type referenced INSIDE another
      *> strong type (13.18.57.3 SR6 second arm) is legal; a nested strong subgroup is the SAME type as a standalone
      *> item of that type (8.5.3 by NEAREST TYPE anchor), so MOVE/compare between them is allowed. Before the fixes
      *> this tripped a false COBOLNET1532 (SR6 ordering) and false COBOLNET1533 (outermost-root type mismatch).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TYPEDEF-NESTED-STRONG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 INNER-T TYPEDEF STRONG.
          05 IA PIC 9(4).
          05 IB PIC 9(4).
       01 OUTER-T TYPEDEF STRONG.
          05 SUB TYPE INNER-T.
          05 OC PIC 9(4).
       01 R1 TYPE OUTER-T.
       01 S1 TYPE INNER-T.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 12 TO IA OF SUB OF R1.
           MOVE 34 TO IB OF SUB OF R1.
           MOVE SUB OF R1 TO S1.
           DISPLAY "S1=[" IA OF S1 "][" IB OF S1 "]".
           IF SUB OF R1 = S1
               DISPLAY "SAME"
           ELSE
               DISPLAY "DIFF"
           END-IF.
           STOP RUN.
