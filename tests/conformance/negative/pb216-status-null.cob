      *> reject-at: 2002 2014 2023
      *> ISO 14.9.42.2 writes the STOP RUN status slot { identifier-1 |
      *> literal-1 }. NULL is neither: 8.3.3.6.2 lists no NULL format among the
      *> seven figurative constants, and 8.4.3.10.1 makes it "a predefined
      *> address of class pointer or a predefined content of class message-tag",
      *> whose 8.4.3.10.3 SR1 admits it "only as a sending operand in an
      *> INITIALIZE or a SET statement; as an argument in a program-prototype
      *> format CALL statement, a function-prototype format function activation,
      *> or a method invocation; or in a pointer-or-object-reference relation
      *> condition". The termination-status phrase is none of those.
      *> It once parsed as a figurative constant and reached the GR5 renderer
      *> (kb/Work PB216); NULL is now its own identifier rule, refused here by
      *> the one 8.4.3.10.3 SR1 context screen, COBOLNET2576 (kb/Work PB1427).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB216N1.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN WITH ERROR STATUS NULL.
