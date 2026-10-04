      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1507 - 10.6.1: the program-definition format ends with `END PROGRAM program-name-1.`; END FUNCTION
      *>   closes only a function-definition or a function-prototype. COBOLNET2274.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EFP1507.
       PROCEDURE DIVISION.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
       END FUNCTION EFP1507.
