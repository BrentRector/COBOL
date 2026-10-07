      *> kb/Work PB2093 - a data-name spelled like a member the compiler's own C# carries is legal COBOL.
      *> RULE: ISO 8.4.2.1, "In order to use a resource, a statement shall contain a reference that uniquely
      *> identifies that resource" (cite.py OK) - uniqueness bites on a REFERENCE, so two subordinates named F
      *> are legal while neither is referenced; and AsImage, Equals, GetHashCode, ToString, FromImage,
      *> PrintMembers, CurrentExtents and CloseFiles are ordinary user-defined words (8.3.2.1).
      *> Before PB2093 each of the group's members drew CS0102 in the record struct the group compiles to
      *> (the struct's own image and synthesized members carry those names), and the record CloseFiles
      *> collided with the program class's CloseFiles - a legal program refused.
      *> DERIVATION: R is the characters of its members in order, VALUE-initialized:
      *>   "ABCD" + "1" + "2" + "3" + "AI" + "EQ" + "GH" + "TS" + "FI" + "PM" + "CE" = ABCD123AIEQGHTSFIPMCE
      *> MOVE R TO T (a group move, 21 characters into the 21-character TYPE clone of RT, whose members are
      *> named alike) copies it; MOVE "XY" TO AsImage OF T then changes only T's first two characters:
      *>   T = XYCD123AIEQGHTSFIPMCE, AsImage OF T = XY, Equals OF T = CD.
      *> EDITION: TYPEDEF / TYPE are 2002; placed at 2002 (identical at 2014/2023).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2093NAMES.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CloseFiles PIC X(2) VALUE "CF".
       01 R.
          05 CUST PIC X(4) VALUE "ABCD".
          05 F PIC 9 VALUE 1.
          05 F PIC 9 VALUE 2.
          05 F-2 PIC 9 VALUE 3.
          05 AsImage PIC X(2) VALUE "AI".
          05 Equals PIC X(2) VALUE "EQ".
          05 GetHashCode PIC X(2) VALUE "GH".
          05 ToString PIC X(2) VALUE "TS".
          05 FromImage PIC X(2) VALUE "FI".
          05 PrintMembers PIC X(2) VALUE "PM".
          05 CurrentExtents PIC X(2) VALUE "CE".
       01 RT TYPEDEF.
          05 AsImage PIC X(2).
          05 Equals PIC X(2).
          05 FILLER PIC X(17).
       01 T TYPE RT.
       PROCEDURE DIVISION.
           DISPLAY CloseFiles.
           DISPLAY R.
           DISPLAY AsImage OF R " " Equals OF R " " GetHashCode " "
               ToString " " FromImage " " PrintMembers " "
               CurrentExtents " " F-2.
           MOVE R TO T.
           MOVE "XY" TO AsImage OF T.
           DISPLAY T.
           DISPLAY AsImage OF T " " Equals OF T.
           STOP RUN.
