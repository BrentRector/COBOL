      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1421 — GO TO ... DEPENDING ON identifier-1 admits a function-identifier (ISO/IEC 1989:2023
      *> §8.4.3.1.2 Format 1; positive pb1421_goto_depending_function_identifier), and the function's temporary
      *> item (§8.4.3.2.4 GR1) is held to §14.9.17.3 SR1: "Identifier-1 shall reference a numeric elementary
      *> data item that is an integer." SQRT is a NUMERIC function (§15.2 type: Num), and §8.4.3.2.3 SR11 says
      *> "A numeric function shall not be specified where an integer operand is required, even though a
      *> particular reference of the numeric function might yield an integer value" — SQRT(4) is 2, and is
      *> still refused (COBOLNET2324, the GO TO DEPENDING operand-class row).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1421NG.
       PROCEDURE DIVISION.
       MAIN-P.
           GO TO P1 P2 DEPENDING ON FUNCTION SQRT(4).
           STOP RUN.
       P1.
           STOP RUN.
       P2.
           STOP RUN.
