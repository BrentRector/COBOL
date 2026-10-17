*>--- conformance-case
*> id: l1_sync_sign_position
*> rules: [GR-13.18.55.4-7]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.55.4 GR7 — with ANY form of the SYNCHRONIZED clause, the
      *> operational sign still appears in the position the SIGN clause
      *> specifies, explicitly or implicitly.
      *>
      *> THE RULE. §13.18.55.4 GR7: "If the data description of an item
      *> contains an operational sign and any form of the SYNCHRONIZED clause,
      *> the sign of the item appears in the sign position explicitly or
      *> implicitly specified by the SIGN clause."
      *> "Any form" is GR3/GR4/GR5's three: bare SYNCHRONIZED, SYNCHRONIZED
      *> LEFT, SYNCHRONIZED RIGHT — all three name a natural boundary, and
      *> GR7 says none of them may move the sign off the position §13.18.52
      *> put it on.
      *>
      *> THE EXPLICIT HALF, DERIVED FROM §13.18.52.4 GR6 ALONE.
      *>   GR6 a) "The operational sign is presumed to be the leading (or,
      *>          respectively, trailing) character position of the data item
      *>          to which it applies; this character position is not a digit
      *>          position."
      *>   GR6 b) "The operational signs for positive and negative are the
      *>          basic special characters '+' and '-', respectively." (The
      *>          printed standard sets that second character as an en dash;
      *>          the basic special character it names is Table 1's "minus
      *>          sign (hyphen)", §8.1.3.1 — U+002D HYPHEN-MINUS.)
      *> So PIC S9(4) SIGN IS ... SEPARATE is 5 character positions, at one
      *> byte each (docs/CONFORMANCE.md DOC-A.1-209), and VALUE -42 gives the
      *> digit string "0042":
      *>   P1V  LEADING SEPARATE  SYNC        -42 -> [-0042]
      *>   P2V  TRAILING SEPARATE SYNC        -42 -> [0042-]
      *>   P3V  LEADING SEPARATE  SYNC LEFT   +42 -> [+0042]
      *>   P4V  TRAILING SEPARATE SYNC RIGHT  +42 -> [0042+]
      *> Nothing here depends on an implementor determination: the SEPARATE
      *> form's position AND representation are both spec-defined.
      *> CTRL is the same description as P1V without the clause; GR7 makes the
      *> two images identical, so a SYNCHRONIZED form that shifted the sign
      *> would break CTRL even if it kept a self-consistent layout.
      *>
      *> THE IMPLICIT HALF — WHICH IS NOT THE NO-SIGN-CLAUSE CASE.
      *> §13.18.52.4 GR4 removes that case from this rule's reach entirely:
      *> "A numeric item whose picture character-string contains the symbol
      *> 'S', and to which no SIGN clause applies ... The implementor shall
      *> specify the position and representation of the operational sign.
      *> General rules 5 and 6 do not apply to such signed numeric items."
      *> With no SIGN clause applying there is no "sign position ... specified
      *> by the SIGN clause" for GR7's consequent to name, so a bare
      *> PIC S9(4) SYNC with no SIGN clause anywhere would not exercise GR7 at
      *> all. The position a SIGN clause specifies IMPLICITLY is the one it
      *> applies by INHERITANCE from a containing group — §13.18.52.4 GR1,
      *> "or for each numeric item subordinate to the group to which it
      *> applies" — and, secondarily, the position a written clause fixes
      *> WITHOUT the SEPARATE CHARACTER phrase (GR5 a) rather than with it.
      *> This golden takes both arms:
      *>   IMPL / IMPLEQ — I1V carries no SIGN clause of its own; the clause
      *>     sits on the containing group `01 I1 SIGN IS TRAILING SEPARATE`
      *>     (legal by §13.18.52.3 SR1's third bullet, "an alphanumeric group
      *>     item"), and GR1 applies it to I1V. That inherited position is
      *>     fully spec-defined by GR6 a/b, so the image is printed
      *>     character-exact — [0042-] — and IMPLEQ pairs it against the
      *>     identical group I2 whose item carries no SYNC, which GR7
      *>     requires to agree.
      *>   EMBED — the written-but-not-SEPARATE arm: J1V PIC S9(4) SIGN IS
      *>     TRAILING SYNC against the identical J2V without SYNC. GR5 a)
      *>     fixes the POSITION ("associated with the leading (or,
      *>     respectively, trailing) digit position"), so the sign occupies an
      *>     EXISTING digit position and J1V is 4 bytes; but GR5 b) leaves
      *>     "what constitutes valid signs" to the implementor and
      *>     docs/CONFORMANCE.md has no DOC-A.1-178 row, so this arm asserts
      *>     only the EQUALITY GR7 actually claims and never prints either
      *>     image.
      *> IMPLEN pins the width the inherited arm rests on: I1V is 5 character
      *> positions by GR6 a) (the separate sign position "is not a digit
      *> position") at one byte each (DOC-A.1-209), so 01 I1 is 1 + 5 = 6 both
      *> with the SYNC clause and without it.
      *>
      *> Every image is read through a GROUP move to an alphanumeric item,
      *> which §14.9.25.4 GR4 makes a byte transfer rather than a numeric
      *> conversion; the one-byte prefix in each group is what gives a
      *> boundary adjustment something to do, and the printed slice skips it.
      *> The prefixes of the compared groups carry a VALUE so that no
      *> undefined byte ever enters an equality.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SYN03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P1.
          05 P1A PIC X.
          05 P1V PIC S9(4) SIGN IS LEADING SEPARATE SYNC VALUE -42.
       01 P2.
          05 P2A PIC X.
          05 P2V PIC S9(4) SIGN IS TRAILING SEPARATE SYNC
                  VALUE -42.
       01 P3.
          05 P3A PIC X.
          05 P3V PIC S9(4) SIGN IS LEADING SEPARATE SYNC LEFT
                  VALUE +42.
       01 P4.
          05 P4A PIC X.
          05 P4V PIC S9(4) SIGN IS TRAILING SEPARATE SYNC RIGHT
                  VALUE +42.
       01 P5.
          05 P5A PIC X.
          05 P5V PIC S9(4) SIGN IS LEADING SEPARATE VALUE -42.
       01 I1 SIGN IS TRAILING SEPARATE.
          05 I1A PIC X VALUE "p".
          05 I1V PIC S9(4) SYNC VALUE -42.
       01 I2 SIGN IS TRAILING SEPARATE.
          05 I2A PIC X VALUE "p".
          05 I2V PIC S9(4) VALUE -42.
       01 J1.
          05 J1A PIC X VALUE "p".
          05 J1V PIC S9(4) SIGN IS TRAILING SYNC VALUE -42.
       01 J2.
          05 J2A PIC X VALUE "p".
          05 J2V PIC S9(4) SIGN IS TRAILING VALUE -42.
       01 W1 PIC X(6).
       01 W2 PIC X(6).
       01 W3 PIC X(6).
       01 W4 PIC X(6).
       01 W5 PIC X(6).
       01 V1 PIC X(6).
       01 V2 PIC X(6).
       01 U1 PIC X(5).
       01 U2 PIC X(5).
       01 CFLAG PIC X(3).
       01 IFLAG PIC X(3).
       01 EFLAG PIC X(3).
       PROCEDURE DIVISION.
       MAIN.
           MOVE P1 TO W1
           MOVE P2 TO W2
           MOVE P3 TO W3
           MOVE P4 TO W4
           MOVE P5 TO W5
           DISPLAY "EXPL=[" W1(2:5) "][" W2(2:5) "][" W3(2:5)
               "][" W4(2:5) "]"
           MOVE "NO" TO CFLAG
           IF W1(2:5) = W5(2:5)
               MOVE "YES" TO CFLAG
           END-IF
           DISPLAY "CTRL=" CFLAG
           MOVE I1 TO V1
           MOVE I2 TO V2
           MOVE "NO" TO IFLAG
           IF V1 = V2
               MOVE "YES" TO IFLAG
           END-IF
           DISPLAY "IMPL=[" V1(2:5) "]"
           DISPLAY "IMPLEQ=" IFLAG
           MOVE J1 TO U1
           MOVE J2 TO U2
           MOVE "NO" TO EFLAG
           IF U1 = U2
               MOVE "YES" TO EFLAG
           END-IF
           DISPLAY "EMBED=" EFLAG
           DISPLAY "IMPLEN=" FUNCTION BYTE-LENGTH(I1)
               " " FUNCTION BYTE-LENGTH(I2)
           STOP RUN.
