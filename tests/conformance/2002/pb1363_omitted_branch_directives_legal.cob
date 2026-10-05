      *> ISO/IEC 1989:2023 7.2.1 (kb/Work PB1363): compiler directives shall be syntactically correct in the initial source
      *> text, including the false path of an IF directive; a LEGAL directive there is accepted and has no effect. With
      *> V = 1 the outer IF (V = 2) is FALSE, so every directive inside it is parsed and never evaluated: Q is not a
      *> compilation variable, and the operands that name it (Q = 2, Q IS DEFINED) would be evaluation errors in a compiled
      *> branch (7.3.8.4.3) but are only syntax here. The one line the program writes is the DISPLAY after the IF.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1363OM.
       PROCEDURE DIVISION.
       MAIN-PARA.
       >>DEFINE V AS 1
       >>IF V = 2
       >>EVALUATE V
       >>WHEN 1 THRU 3
           DISPLAY "NO1".
       >>WHEN 4
       >>WHEN OTHER
           DISPLAY "NO2".
       >>END-EVALUATE
       >>DEFINE Y AS V + 1 OVERRIDE
       >>DEFINE Z AS OFF
       >>EVALUATE TRUE
       >>WHEN V = 1 AND Q = 2
           DISPLAY "NO3".
       >>WHEN OTHER
       >>END-EVALUATE
       >>LISTING OFF
       >>IF Q IS DEFINED AND Y = 2
           DISPLAY "NO4".
       >>END-IF
       >>END-IF
           DISPLAY "A".
           STOP RUN.
