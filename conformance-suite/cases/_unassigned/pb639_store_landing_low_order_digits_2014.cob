      *>--- conformance-case
      *> id: pb639_store_landing_low_order_digits_2014
      *> rules: []
      *> accept: [2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB639 - the INLINE arithmetic / MOVE store aligned the value to
      *> the receiver's scale with the WRAPPING CobolNum.Rescale, so a
      *> widening past the Int128 carrier wrapped modulo 2^128 BEFORE
      *> the receiver's own capacity rule ran.
      *>
      *> ISO 14.7.5 case 3: "if, after radix point alignment and any
      *> applicable rounding specifications, the result of an arithmetic
      *> statement is further from zero than permitted for the
      *> associated resultant data item" - the size error condition
      *> exists. With NO SIZE ERROR phrase and EC-SIZE checking off, no-
      *> phrase rule 4 sets EC-SIZE-TRUNCATION and 14.6.13.1.3 item 8
      *> hands the disposition to the implementor; CONFORMANCE.md
      *> DOC-A.1-70 documents it as "execution continues and the
      *> resultant identifier receives the LOW-ORDER digits of the
      *> result aligned at its scale". A MOVE lands the same digits
      *> under 14.6.8.2 r4 - "the data is aligned by decimal point and
      *> is transferred to the receiving digits with zero fill or
      *> truncation on either end". WITH the phrase, storing rule 2
      *> leaves that receiver UNCHANGED.
      *>
      *> EVERY EXPECTED VALUE IS COMPUTED FROM THE SPEC, never read off
      *> a run:
      *>
      *> NOPH / MOVE / EDIT / ROUND: 10**30 (31 digits - 8.3.3.3.2
      *> permits 1 through 31) aligned at scale 9 is 10**39; the
      *> receiver's 18 digit positions keep its LOW-ORDER 18, all of
      *> which are zero, so all four print +000000000.000000000. They
      *> MUST print the same characters: the no-phrase arithmetic store
      *> and the MOVE are one determination, and the numeric-edited
      *> receiver is that store with an edit applied. Pre-fix all four
      *> printed the low digits of 10**39 taken modulo 2^128 - the value
      *> -123822295.304634368.
      *>
      *> CMP5: a BinaryCapacity receiver's low-order digits are the
      *> CONTAINER's two's-complement residue, never a decimal cap:
      *> 10**39 mod 2**64 = 6873995514006732800, i.e.
      *> +6873995514.006732800 at scale 9. This row PINS the arm that
      *> must NOT change when the decimal arm does.
      *>
      *> FIT: 123.456 fits the receiver exactly, so the landing is not a
      *> blanket zero.
      *>
      *> SIZE / ECST: 340282366920938463463374607432 is ceil(2**128 /
      *> 10**9), so aligning it at scale 9 gives a 39-digit value -
      *> further from zero than PIC S9(9)V9(9) permits - and the phrase
      *> fires with WS unchanged at 7 (storing rule 2). THIS ROW IS THE
      *> DISCRIMINATOR FOR THE CHECKED ARM: the same alignment WRAPPED
      *> is 340282366920938463463374607432 * 10**9 - 2**128 = 231788544,
      *> i.e. +0.231788544, which sits comfortably inside the capacity -
      *> so the pre-fix compiler ran NOT ON SIZE ERROR and overwrote WS
      *> with it. ECST re-runs it under >>TURN EC-SIZE CHECKING ON:
      *> 14.7.5 says the phrase processes the condition and no-phrase
      *> rule 4 names it, so EXCEPTION-STATUS reports EC-SIZE-
      *> TRUNCATION.
      *>
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB639STORE14.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WC    PIC S9(9)V9(9) VALUE 7.
       01 WM    PIC S9(9)V9(9) VALUE 7.
       01 WR    PIC S9(9)V9(9) VALUE 7.
       01 WF    PIC S9(9)V9(9) VALUE 7.
       01 WS    PIC S9(9)V9(9) VALUE 7.
       01 WB    PIC S9(9)V9(9) COMP-5 VALUE 7.
       01 WZ    PIC Z(8)9.9(9) VALUE ZERO.
       01 SHOW  PIC +9(9).9(9).
       01 SHOWB PIC +9(10).9(9).
       PROCEDURE DIVISION.
           COMPUTE WC = 1000000000000000000000000000000.
           MOVE WC TO SHOW.
           DISPLAY "NOPH=" SHOW.
           MOVE 1000000000000000000000000000000 TO WM.
           MOVE WM TO SHOW.
           DISPLAY "MOVE=" SHOW.
           COMPUTE WZ = 1000000000000000000000000000000.
           DISPLAY "EDIT=" WZ.
           COMPUTE WR ROUNDED = 1000000000000000000000000000000.
           MOVE WR TO SHOW.
           DISPLAY "ROUND=" SHOW.
           COMPUTE WB = 1000000000000000000000000000000.
           MOVE WB TO SHOWB.
           DISPLAY "CMP5=" SHOWB.
           COMPUTE WF = 123.456.
           MOVE WF TO SHOW.
           DISPLAY "FIT=" SHOW.
           COMPUTE WS = 340282366920938463463374607432
               ON SIZE ERROR
                   MOVE WS TO SHOW
                   DISPLAY "SIZE=SE " SHOW
               NOT ON SIZE ERROR
                   MOVE WS TO SHOW
                   DISPLAY "SIZE=NOSE " SHOW
           END-COMPUTE.
       >>TURN EC-SIZE CHECKING ON
           COMPUTE WS = 340282366920938463463374607432
               ON SIZE ERROR DISPLAY "ECST=" FUNCTION EXCEPTION-STATUS
               NOT ON SIZE ERROR DISPLAY "ECST=NOSE"
           END-COMPUTE.
       >>TURN EC-SIZE CHECKING OFF
           STOP RUN.
       END PROGRAM PB639STORE14.
