      *> reject-at: 2002 2014 2023
      *> kb/Work PB1784 / PB745 - ISO/IEC 1989:2023 section 9.3.5.3: "Parametric polymorphism is an optional
      *> feature" (Annex A.4.10 item 3). Two method prototypes of one name in an interface are the declined
      *> optional element, refused by its catalog descriptor COBOLNET0822 (Annex A4, section 4.2.7), the
      *> same decline a class's second method of one name draws.
      *>   cite.py: OK  9.3.5.3 7)
       IDENTIFICATION DIVISION.
       INTERFACE-ID. I1784P.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       END METHOD PING.
       METHOD-ID. PING.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X PIC X.
       PROCEDURE DIVISION USING X.
       END METHOD PING.
       END INTERFACE I1784P.
