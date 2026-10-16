      *> reject-at: 2023
      *> kb/Work PB780 - a constant-name is admitted where the OPTIONS INITIALIZE clause writes literal-1 (13.10.3
      *> SR2) and then meets the rules of the literal it stands for (13.10.4 GR1 "as if literal-1 ... were
      *> written"): 11.9.10.3 SR1 "Literal-1 shall specify a one-byte hexadecimal-alphanumeric literal", and KZ
      *> stands for the FORMAT-1 literal "Z", which is not one (the written twin is
      *> pb152-options-initialize-fill-literal-not-hex). The conforming X"nn" form is
      *> conformance/2023/pb780_constant_name_options_fill.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB780FZ.
       OPTIONS.
           INITIALIZE WORKING-STORAGE SECTION TO KZ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 KZ CONSTANT AS "Z".
       01 S PIC X(3).
       PROCEDURE DIVISION.
           DISPLAY S.
           STOP RUN.
