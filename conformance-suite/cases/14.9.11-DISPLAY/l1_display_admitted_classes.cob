*>--- conformance-case
*> id: l1_display_admitted_classes
*> rules: [SR-14.9.11.3-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.11.3 SR1 — "Identifier-1 shall not reference a data item of class message-tag, object,
      *> or pointer." (cite.py --check 14.9.11.3 "Identifier-1 shall not reference a data item of class
      *> message-tag, object, or pointer." -> OK, §14.9.11.3 rule 1.)
      *>
      *> THE ADMIT SIDE OF A CLOSED EXCLUSION LIST. SR1 names THREE classes and no others, so every other
      *> §8.5.2.1 Table 2 class is a legal identifier-1 and §14.9.11.4 GR1 transfers its content to the
      *> device. This golden exists so that the operand-CLASS gate kb/Work PB148 installed (and any later
      *> widening of it) cannot quietly over-reject: the three negatives
      *> conformance:negative/l1-display-{pointer,program-pointer,object-reference}-operand are green when
      *> the screen rejects too MUCH as well as when it rejects exactly right, and only this file tells
      *> them apart.
      *>
      *> DERIVED EXPECTATIONS, each from Table 2 plus §14.9.11.4 GR1 ("the content of each operand …
      *> transferred to the device in the order listed"):
      *>   ALNUM=HELLO   — PIC X(5), class alphanumeric.
      *>   ALPHA=ABC     — PIC A(3), class ALPHABETIC (Table 2's own first row; a class SR1 does not name).
      *>   NUM=0042      — PIC 9(4), class numeric; the device image is the four stored digits.
      *>   EDIT=  42.00  — PIC ZZ9.99 is category numeric-edited, which Table 2 places in class
      *>                   ALPHANUMERIC; MOVE 42 aligns on the decimal point (042.00) and the two Z
      *>                   positions suppress the leading zero, giving " 42.00" with ONE leading space.
      *>   NAT=NAT       — PIC N(3), class national.
      *>   GRP=GHIJ      — an alphanumeric group is class alphanumeric (§8.5.2.1); its content is the concatenation
      *>                   of its subordinates.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1DSPADM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-ALNUM PIC X(5) VALUE "HELLO".
       01 WS-ALPHA PIC A(3) VALUE "ABC".
       01 WS-NUM   PIC 9(4) VALUE 42.
       01 WS-EDIT  PIC ZZ9.99.
       01 WS-NAT   PIC N(3) VALUE N"NAT".
       01 WS-GRP.
          05 WS-G1 PIC X(2) VALUE "GH".
          05 WS-G2 PIC X(2) VALUE "IJ".
       PROCEDURE DIVISION.
       MAIN.
           MOVE 42 TO WS-EDIT
           DISPLAY "ALNUM=" WS-ALNUM
           DISPLAY "ALPHA=" WS-ALPHA
           DISPLAY "NUM=" WS-NUM
           DISPLAY "EDIT=" WS-EDIT
           DISPLAY "NAT=" WS-NAT
           DISPLAY "GRP=" WS-GRP
           STOP RUN.
