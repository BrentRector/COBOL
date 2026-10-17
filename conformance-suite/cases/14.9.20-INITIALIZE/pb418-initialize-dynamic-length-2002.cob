*>--- conformance-case
*> id: pb418-initialize-dynamic-length-2002
*> rules: [GR-14.9.20.4-7]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB418 — the edition floor under the D-DL1 determination's positive witness
      *> (conformance:2014/pb418_initialize_dynamic_length_senders). ISO 14.9.20.4 GR7's subject is a
      *> "dynamic-length elementary item", which 8.5.1.10 / 13.18.19 introduce in COBOL-2014, so at COBOL-85 and
      *> COBOL-2002 the rule has no population at all and the DECLARATION is what has to be refused — before any
      *> question about which sending-operand arm GR7 reaches can be asked. COBOLNET0900 is the version gate.
      *> The INITIALIZE ... ALL TO VALUE below is itself COBOL-2002 (COBOLNET0831 covers it at 85), which is why
      *> both editions are named: the DYNAMIC LENGTH clause is what neither of them admits.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB418N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 DV PIC X DYNAMIC LENGTH LIMIT IS 30 VALUE "hello".
       PROCEDURE DIVISION.
           INITIALIZE DV ALL TO VALUE
           DISPLAY "[" DV "]"
           STOP RUN.
