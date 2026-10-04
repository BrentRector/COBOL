000100 IDENTIFICATION DIVISION.                                         RL2014.2
000200 PROGRAM-ID.                                                      RL2014.2
000300     RL201A.                                                      RL2014.2
000400****************************************************************  RL2014.2
000500*                                                              *  RL2014.2
000600*    VALIDATION FOR:-                                          *  RL2014.2
000700*                                                              *  RL2014.2
000800*    "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".RL2014.2
000900*                                                              *  RL2014.2
001000*    "COBOL 85 VERSION 4.2, Apr  1993 SSVG                      ".RL2014.2
001100*                                                              *  RL2014.2
001200****************************************************************  RL2014.2
001300*GENERAL:    THIS RUN UNIT IS THE FIRST OF A SERIES WHICH         RL2014.2
001400*            PROCESSES A RELATIVE I-O FILE.  THE FUNCTION OF THIS RL2014.2
001500*            PROGRAM IS TO CREATE A RELATIVE FILE SEQUENTIALLY    RL2014.2
001600*            (ACCESS MODE SEQUENTIAL) AND VERIFY THAT IT WAS      RL2014.2
001700*            CREATED CORRECTLY.  THE FILE IS IDENTIFED AS "RL-FS1"RL2014.2
001800*            AND IS PASSED TO SUBSEQUENT RUN UNITS FOR PROCESSING.RL2014.2
001900*                                                                 RL2014.2
002000*            X-CARD PARAMETERS WHICH MUST BE SUPPLIED FOR THIS    RL2014.2
002100*            PROGRAM ARE:                                         RL2014.2
002200*                                                                 RL2014.2
002300*                 X-21   IMPLEMENTOR-NAME IN ASSIGN TO CLAUSE FOR RL2014.2
002400*                         RELATIVE  I-O DATA FILE                 RL2014.2
002500*                 X-55   SYSTEM PRINTER                           RL2014.2
002600*                 X-69   ADDITIONAL VALUE OF CLAUSES              RL2014.2
002700*                 X-74   VALUE OF IMPLEMENTOR-NAME                RL2014.2
002800*                 X-75   OBJECT OF VALUE OF CLAUSE                RL2014.2
002900*                 X-82   SOURCE-COMPUTER                          RL2014.2
003000*                 X-83   OBJECT-COMPUTER.                         RL2014.2
003100*                                                                 RL2014.2
003200****************************************************************  RL2014.2
003300 ENVIRONMENT DIVISION.                                            RL2014.2
003400 CONFIGURATION SECTION.                                           RL2014.2
003500 SOURCE-COMPUTER.                                                 RL2014.2
003600     XXXXX082.                                                    RL2014.2
003700 OBJECT-COMPUTER.                                                 RL2014.2
003800     XXXXX083.                                                    RL2014.2
003900 INPUT-OUTPUT SECTION.                                            RL2014.2
004000 FILE-CONTROL.                                                    RL2014.2
004100     SELECT PRINT-FILE ASSIGN TO                                  RL2014.2
004200     XXXXX055.                                                    RL2014.2
004300     SELECT   RL-FS1 ASSIGN TO                                    RL2014.2
004400     XXXXP021                                                     RL2014.2
004500             ORGANIZATION IS RELATIVE.                            RL2014.2
004600*    ABSENCE OF THE ACCESS CLAUSE IS TREATED AS THOUGH            RL2014.2
004700*     SEQUENTIAL HAD BEEN SPECIFIED.                              RL2014.2
004800 DATA DIVISION.                                                   RL2014.2
004900 FILE SECTION.                                                    RL2014.2
005000 FD  PRINT-FILE.                                                  RL2014.2
005100 01  PRINT-REC PICTURE X(120).                                    RL2014.2
005200 01  DUMMY-RECORD PICTURE X(120).                                 RL2014.2
005300 FD  RL-FS1                                                       RL2014.2
005400     LABEL RECORDS STANDARD                                       RL2014.2
005500C    VALUE OF                                                     RL2014.2
005600C    XXXXX074                                                     RL2014.2
005700C    IS                                                           RL2014.2
005800C    XXXXX075                                                     RL2014.2
005900G    XXXXX069                                                     RL2014.2
006000     BLOCK CONTAINS 1 RECORDS                                     RL2014.2
006100     RECORD CONTAINS 120 CHARACTERS.                              RL2014.2
006200 01  RL-FS1R1-F-G-120.                                            RL2014.2
006300     02 FILLER PIC X(120).                                        RL2014.2
006400 WORKING-STORAGE SECTION.                                         RL2014.2
006500 01  WRK-CS-09V00 PIC S9(9) USAGE COMP VALUE ZERO.                RL2014.2
006600 01  FILE-RECORD-INFORMATION-REC.                                 RL2014.2
006700     03 FILE-RECORD-INFO-SKELETON.                                RL2014.2
006800        05 FILLER                 PICTURE X(48)       VALUE       RL2014.2
006900             "FILE=      ,RECORD=      /0,RECNO=000000,UPDT=00".  RL2014.2
007000        05 FILLER                 PICTURE X(46)       VALUE       RL2014.2
007100             ",ODO=0000,PGM=     ,LRECL=000000,BLKSIZ  =0000".    RL2014.2
007200        05 FILLER                 PICTURE X(26)       VALUE       RL2014.2
007300             ",LFIL=000000,ORG=  ,LBLR= ".                        RL2014.2
007400        05 FILLER                 PICTURE X(37)       VALUE       RL2014.2
007500             ",RECKEY=                             ".             RL2014.2
007600        05 FILLER                 PICTURE X(38)       VALUE       RL2014.2
007700             ",ALTKEY1=                             ".            RL2014.2
007800        05 FILLER                 PICTURE X(38)       VALUE       RL2014.2
007900             ",ALTKEY2=                             ".            RL2014.2
008000        05 FILLER                 PICTURE X(7)        VALUE SPACE.RL2014.2
008100     03 FILE-RECORD-INFO          OCCURS  10  TIMES.              RL2014.2
008200        05 FILE-RECORD-INFO-P1-120.                               RL2014.2
008300           07 FILLER              PIC X(5).                       RL2014.2
008400           07 XFILE-NAME           PIC X(6).                      RL2014.2
008500           07 FILLER              PIC X(8).                       RL2014.2
008600           07 XRECORD-NAME         PIC X(6).                      RL2014.2
008700           07 FILLER              PIC X(1).                       RL2014.2
008800           07 REELUNIT-NUMBER     PIC 9(1).                       RL2014.2
008900           07 FILLER              PIC X(7).                       RL2014.2
009000           07 XRECORD-NUMBER       PIC 9(6).                      RL2014.2
009100           07 FILLER              PIC X(6).                       RL2014.2
009200           07 UPDATE-NUMBER       PIC 9(2).                       RL2014.2
009300           07 FILLER              PIC X(5).                       RL2014.2
009400           07 ODO-NUMBER          PIC 9(4).                       RL2014.2
009500           07 FILLER              PIC X(5).                       RL2014.2
009600           07 XPROGRAM-NAME        PIC X(5).                      RL2014.2
009700           07 FILLER              PIC X(7).                       RL2014.2
009800           07 XRECORD-LENGTH       PIC 9(6).                      RL2014.2
009900           07 FILLER              PIC X(7).                       RL2014.2
010000           07 CHARS-OR-RECORDS    PIC X(2).                       RL2014.2
010100           07 FILLER              PIC X(1).                       RL2014.2
010200           07 XBLOCK-SIZE          PIC 9(4).                      RL2014.2
010300           07 FILLER              PIC X(6).                       RL2014.2
010400           07 RECORDS-IN-FILE     PIC 9(6).                       RL2014.2
010500           07 FILLER              PIC X(5).                       RL2014.2
010600           07 XFILE-ORGANIZATION   PIC X(2).                      RL2014.2
010700           07 FILLER              PIC X(6).                       RL2014.2
010800           07 XLABEL-TYPE          PIC X(1).                      RL2014.2
010900        05 FILE-RECORD-INFO-P121-240.                             RL2014.2
011000           07 FILLER              PIC X(8).                       RL2014.2
011100           07 XRECORD-KEY          PIC X(29).                     RL2014.2
011200           07 FILLER              PIC X(9).                       RL2014.2
011300           07 ALTERNATE-KEY1      PIC X(29).                      RL2014.2
011400           07 FILLER              PIC X(9).                       RL2014.2
011500           07 ALTERNATE-KEY2      PIC X(29).                      RL2014.2
011600           07 FILLER              PIC X(7).                       RL2014.2
011700 01  TEST-RESULTS.                                                RL2014.2
011800     02 FILLER                   PIC X      VALUE SPACE.          RL2014.2
011900     02 FEATURE                  PIC X(20)  VALUE SPACE.          RL2014.2
012000     02 FILLER                   PIC X      VALUE SPACE.          RL2014.2
012100     02 P-OR-F                   PIC X(5)   VALUE SPACE.          RL2014.2
012200     02 FILLER                   PIC X      VALUE SPACE.          RL2014.2
012300     02  PAR-NAME.                                                RL2014.2
012400       03 FILLER                 PIC X(19)  VALUE SPACE.          RL2014.2
012500       03  PARDOT-X              PIC X      VALUE SPACE.          RL2014.2
012600       03 DOTVALUE               PIC 99     VALUE ZERO.           RL2014.2
012700     02 FILLER                   PIC X(8)   VALUE SPACE.          RL2014.2
012800     02 RE-MARK                  PIC X(61).                       RL2014.2
012900 01  TEST-COMPUTED.                                               RL2014.2
013000     02 FILLER                   PIC X(30)  VALUE SPACE.          RL2014.2
013100     02 FILLER                   PIC X(17)  VALUE                 RL2014.2
013200            "       COMPUTED=".                                   RL2014.2
013300     02 COMPUTED-X.                                               RL2014.2
013400     03 COMPUTED-A               PIC X(20)  VALUE SPACE.          RL2014.2
013500     03 COMPUTED-N               REDEFINES COMPUTED-A             RL2014.2
013600                                 PIC -9(9).9(9).                  RL2014.2
013700     03 COMPUTED-0V18 REDEFINES COMPUTED-A   PIC -.9(18).         RL2014.2
013800     03 COMPUTED-4V14 REDEFINES COMPUTED-A   PIC -9(4).9(14).     RL2014.2
013900     03 COMPUTED-14V4 REDEFINES COMPUTED-A   PIC -9(14).9(4).     RL2014.2
014000     03       CM-18V0 REDEFINES COMPUTED-A.                       RL2014.2
014100         04 COMPUTED-18V0                    PIC -9(18).          RL2014.2
014200         04 FILLER                           PIC X.               RL2014.2
014300     03 FILLER PIC X(50) VALUE SPACE.                             RL2014.2
014400 01  TEST-CORRECT.                                                RL2014.2
014500     02 FILLER PIC X(30) VALUE SPACE.                             RL2014.2
014600     02 FILLER PIC X(17) VALUE "       CORRECT =".                RL2014.2
014700     02 CORRECT-X.                                                RL2014.2
014800     03 CORRECT-A                  PIC X(20) VALUE SPACE.         RL2014.2
014900     03 CORRECT-N    REDEFINES CORRECT-A     PIC -9(9).9(9).      RL2014.2
015000     03 CORRECT-0V18 REDEFINES CORRECT-A     PIC -.9(18).         RL2014.2
015100     03 CORRECT-4V14 REDEFINES CORRECT-A     PIC -9(4).9(14).     RL2014.2
015200     03 CORRECT-14V4 REDEFINES CORRECT-A     PIC -9(14).9(4).     RL2014.2
015300     03      CR-18V0 REDEFINES CORRECT-A.                         RL2014.2
015400         04 CORRECT-18V0                     PIC -9(18).          RL2014.2
015500         04 FILLER                           PIC X.               RL2014.2
015600     03 FILLER PIC X(2) VALUE SPACE.                              RL2014.2
015700     03 COR-ANSI-REFERENCE             PIC X(48) VALUE SPACE.     RL2014.2
015800 01  CCVS-C-1.                                                    RL2014.2
015900     02 FILLER  PIC IS X(99)    VALUE IS " FEATURE              PARL2014.2
016000-    "SS  PARAGRAPH-NAME                                          RL2014.2
016100-    "       REMARKS".                                            RL2014.2
016200     02 FILLER                     PIC X(20)    VALUE SPACE.      RL2014.2
016300 01  CCVS-C-2.                                                    RL2014.2
016400     02 FILLER                     PIC X        VALUE SPACE.      RL2014.2
016500     02 FILLER                     PIC X(6)     VALUE "TESTED".   RL2014.2
016600     02 FILLER                     PIC X(15)    VALUE SPACE.      RL2014.2
016700     02 FILLER                     PIC X(4)     VALUE "FAIL".     RL2014.2
016800     02 FILLER                     PIC X(94)    VALUE SPACE.      RL2014.2
016900 01  REC-SKL-SUB                   PIC 9(2)     VALUE ZERO.       RL2014.2
017000 01  REC-CT                        PIC 99       VALUE ZERO.       RL2014.2
017100 01  DELETE-COUNTER                PIC 999      VALUE ZERO.       RL2014.2
017200 01  ERROR-COUNTER                 PIC 999      VALUE ZERO.       RL2014.2
017300 01  INSPECT-COUNTER               PIC 999      VALUE ZERO.       RL2014.2
017400 01  PASS-COUNTER                  PIC 999      VALUE ZERO.       RL2014.2
017500 01  TOTAL-ERROR                   PIC 999      VALUE ZERO.       RL2014.2
017600 01  ERROR-HOLD                    PIC 999      VALUE ZERO.       RL2014.2
017700 01  DUMMY-HOLD                    PIC X(120)   VALUE SPACE.      RL2014.2
017800 01  RECORD-COUNT                  PIC 9(5)     VALUE ZERO.       RL2014.2
017900 01  ANSI-REFERENCE                PIC X(48)    VALUE SPACES.     RL2014.2
018000 01  CCVS-H-1.                                                    RL2014.2
018100     02  FILLER                    PIC X(39)    VALUE SPACES.     RL2014.2
018200     02  FILLER                    PIC X(42)    VALUE             RL2014.2
018300     "OFFICIAL COBOL COMPILER VALIDATION SYSTEM".                 RL2014.2
018400     02  FILLER                    PIC X(39)    VALUE SPACES.     RL2014.2
018500 01  CCVS-H-2A.                                                   RL2014.2
018600   02  FILLER                        PIC X(40)  VALUE SPACE.      RL2014.2
018700   02  FILLER                        PIC X(7)   VALUE "CCVS85 ".  RL2014.2
018800   02  FILLER                        PIC XXXX   VALUE             RL2014.2
018900     "4.2 ".                                                      RL2014.2
019000   02  FILLER                        PIC X(28)  VALUE             RL2014.2
019100            " COPY - NOT FOR DISTRIBUTION".                       RL2014.2
019200   02  FILLER                        PIC X(41)  VALUE SPACE.      RL2014.2
019300                                                                  RL2014.2
019400 01  CCVS-H-2B.                                                   RL2014.2
019500   02  FILLER                        PIC X(15)  VALUE             RL2014.2
019600            "TEST RESULT OF ".                                    RL2014.2
019700   02  TEST-ID                       PIC X(9).                    RL2014.2
019800   02  FILLER                        PIC X(4)   VALUE             RL2014.2
019900            " IN ".                                               RL2014.2
020000   02  FILLER                        PIC X(12)  VALUE             RL2014.2
020100     " HIGH       ".                                              RL2014.2
020200   02  FILLER                        PIC X(22)  VALUE             RL2014.2
020300            " LEVEL VALIDATION FOR ".                             RL2014.2
020400   02  FILLER                        PIC X(58)  VALUE             RL2014.2
020500     "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".RL2014.2
020600 01  CCVS-H-3.                                                    RL2014.2
020700     02  FILLER                      PIC X(34)  VALUE             RL2014.2
020800            " FOR OFFICIAL USE ONLY    ".                         RL2014.2
020900     02  FILLER                      PIC X(58)  VALUE             RL2014.2
021000     "COBOL 85 VERSION 4.2, Apr  1993 SSVG                      ".RL2014.2
021100     02  FILLER                      PIC X(28)  VALUE             RL2014.2
021200            "  COPYRIGHT   1985 ".                                RL2014.2
021300 01  CCVS-E-1.                                                    RL2014.2
021400     02 FILLER                       PIC X(52)  VALUE SPACE.      RL2014.2
021500     02 FILLER  PIC X(14) VALUE IS "END OF TEST-  ".              RL2014.2
021600     02 ID-AGAIN                     PIC X(9).                    RL2014.2
021700     02 FILLER                       PIC X(45)  VALUE SPACES.     RL2014.2
021800 01  CCVS-E-2.                                                    RL2014.2
021900     02  FILLER                      PIC X(31)  VALUE SPACE.      RL2014.2
022000     02  FILLER                      PIC X(21)  VALUE SPACE.      RL2014.2
022100     02 CCVS-E-2-2.                                               RL2014.2
022200         03 ERROR-TOTAL              PIC XXX    VALUE SPACE.      RL2014.2
022300         03 FILLER                   PIC X      VALUE SPACE.      RL2014.2
022400         03 ENDER-DESC               PIC X(44)  VALUE             RL2014.2
022500            "ERRORS ENCOUNTERED".                                 RL2014.2
022600 01  CCVS-E-3.                                                    RL2014.2
022700     02  FILLER                      PIC X(22)  VALUE             RL2014.2
022800            " FOR OFFICIAL USE ONLY".                             RL2014.2
022900     02  FILLER                      PIC X(12)  VALUE SPACE.      RL2014.2
023000     02  FILLER                      PIC X(58)  VALUE             RL2014.2
023100     "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".RL2014.2
023200     02  FILLER                      PIC X(13)  VALUE SPACE.      RL2014.2
023300     02 FILLER                       PIC X(15)  VALUE             RL2014.2
023400             " COPYRIGHT 1985".                                   RL2014.2
023500 01  CCVS-E-4.                                                    RL2014.2
023600     02 CCVS-E-4-1                   PIC XXX    VALUE SPACE.      RL2014.2
023700     02 FILLER                       PIC X(4)   VALUE " OF ".     RL2014.2
023800     02 CCVS-E-4-2                   PIC XXX    VALUE SPACE.      RL2014.2
023900     02 FILLER                       PIC X(40)  VALUE             RL2014.2
024000      "  TESTS WERE EXECUTED SUCCESSFULLY".                       RL2014.2
024100 01  XXINFO.                                                      RL2014.2
024200     02 FILLER                       PIC X(19)  VALUE             RL2014.2
024300            "*** INFORMATION ***".                                RL2014.2
024400     02 INFO-TEXT.                                                RL2014.2
024500       04 FILLER                     PIC X(8)   VALUE SPACE.      RL2014.2
024600       04 XXCOMPUTED                 PIC X(20).                   RL2014.2
024700       04 FILLER                     PIC X(5)   VALUE SPACE.      RL2014.2
024800       04 XXCORRECT                  PIC X(20).                   RL2014.2
024900     02 INF-ANSI-REFERENCE           PIC X(48).                   RL2014.2
025000 01  HYPHEN-LINE.                                                 RL2014.2
025100     02 FILLER  PIC IS X VALUE IS SPACE.                          RL2014.2
025200     02 FILLER  PIC IS X(65)    VALUE IS "************************RL2014.2
025300-    "*****************************************".                 RL2014.2
025400     02 FILLER  PIC IS X(54)    VALUE IS "************************RL2014.2
025500-    "******************************".                            RL2014.2
025600 01  CCVS-PGM-ID                     PIC X(9)   VALUE             RL2014.2
025700     "RL201A".                                                    RL2014.2
025800 PROCEDURE DIVISION.                                              RL2014.2
025900 CCVS1 SECTION.                                                   RL2014.2
026000 OPEN-FILES.                                                      RL2014.2
026100     OPEN    OUTPUT PRINT-FILE.                                   RL2014.2
026200     MOVE  CCVS-PGM-ID TO TEST-ID. MOVE CCVS-PGM-ID TO ID-AGAIN.  RL2014.2
026300     MOVE    SPACE TO TEST-RESULTS.                               RL2014.2
026400     PERFORM HEAD-ROUTINE THRU COLUMN-NAMES-ROUTINE.              RL2014.2
026500     MOVE    ZERO TO REC-SKL-SUB.                                 RL2014.2
026600     PERFORM CCVS-INIT-FILE 9 TIMES.                              RL2014.2
026700 CCVS-INIT-FILE.                                                  RL2014.2
026800     ADD     1 TO REC-SKL-SUB.                                    RL2014.2
026900     MOVE    FILE-RECORD-INFO-SKELETON                            RL2014.2
027000          TO FILE-RECORD-INFO (REC-SKL-SUB).                      RL2014.2
027100 CCVS-INIT-EXIT.                                                  RL2014.2
027200     GO TO CCVS1-EXIT.                                            RL2014.2
027300 CLOSE-FILES.                                                     RL2014.2
027400     PERFORM END-ROUTINE THRU END-ROUTINE-13. CLOSE PRINT-FILE.   RL2014.2
027500 TERMINATE-CCVS.                                                  RL2014.2
027600S    EXIT PROGRAM.                                                RL2014.2
027700STERMINATE-CALL.                                                  RL2014.2
027800     STOP     RUN.                                                RL2014.2
027900 INSPT. MOVE "INSPT" TO P-OR-F. ADD 1 TO INSPECT-COUNTER.         RL2014.2
028000 PASS.  MOVE "PASS " TO P-OR-F.  ADD 1 TO PASS-COUNTER.           RL2014.2
028100 FAIL.  MOVE "FAIL*" TO P-OR-F.  ADD 1 TO ERROR-COUNTER.          RL2014.2
028200 DE-LETE.  MOVE "*****" TO P-OR-F.  ADD 1 TO DELETE-COUNTER.      RL2014.2
028300     MOVE "****TEST DELETED****" TO RE-MARK.                      RL2014.2
028400 PRINT-DETAIL.                                                    RL2014.2
028500     IF REC-CT NOT EQUAL TO ZERO                                  RL2014.2
028600             MOVE "." TO PARDOT-X                                 RL2014.2
028700             MOVE REC-CT TO DOTVALUE.                             RL2014.2
028800     MOVE     TEST-RESULTS TO PRINT-REC. PERFORM WRITE-LINE.      RL2014.2
028900     IF P-OR-F EQUAL TO "FAIL*"  PERFORM WRITE-LINE               RL2014.2
029000        PERFORM FAIL-ROUTINE THRU FAIL-ROUTINE-EX                 RL2014.2
029100          ELSE PERFORM BAIL-OUT THRU BAIL-OUT-EX.                 RL2014.2
029200     MOVE SPACE TO P-OR-F. MOVE SPACE TO COMPUTED-X.              RL2014.2
029300     MOVE SPACE TO CORRECT-X.                                     RL2014.2
029400     IF     REC-CT EQUAL TO ZERO  MOVE SPACE TO PAR-NAME.         RL2014.2
029500     MOVE     SPACE TO RE-MARK.                                   RL2014.2
029600 HEAD-ROUTINE.                                                    RL2014.2
029700     MOVE CCVS-H-1  TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.  RL2014.2
029800     MOVE CCVS-H-2A TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.  RL2014.2
029900     MOVE CCVS-H-2B TO DUMMY-RECORD. PERFORM WRITE-LINE 3 TIMES.  RL2014.2
030000     MOVE CCVS-H-3  TO DUMMY-RECORD. PERFORM WRITE-LINE 3 TIMES.  RL2014.2
030100 COLUMN-NAMES-ROUTINE.                                            RL2014.2
030200     MOVE CCVS-C-1 TO DUMMY-RECORD. PERFORM WRITE-LINE.           RL2014.2
030300     MOVE CCVS-C-2 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   RL2014.2
030400     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE.        RL2014.2
030500 END-ROUTINE.                                                     RL2014.2
030600     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE 5 TIMES.RL2014.2
030700 END-RTN-EXIT.                                                    RL2014.2
030800     MOVE CCVS-E-1 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   RL2014.2
030900 END-ROUTINE-1.                                                   RL2014.2
031000      ADD ERROR-COUNTER TO ERROR-HOLD ADD INSPECT-COUNTER TO      RL2014.2
031100      ERROR-HOLD. ADD DELETE-COUNTER TO ERROR-HOLD.               RL2014.2
031200      ADD PASS-COUNTER TO ERROR-HOLD.                             RL2014.2
031300*     IF PASS-COUNTER EQUAL TO ERROR-HOLD GO TO END-ROUTINE-12.   RL2014.2
031400      MOVE PASS-COUNTER TO CCVS-E-4-1.                            RL2014.2
031500      MOVE ERROR-HOLD TO CCVS-E-4-2.                              RL2014.2
031600      MOVE CCVS-E-4 TO CCVS-E-2-2.                                RL2014.2
031700      MOVE CCVS-E-2 TO DUMMY-RECORD PERFORM WRITE-LINE.           RL2014.2
031800  END-ROUTINE-12.                                                 RL2014.2
031900      MOVE "TEST(S) FAILED" TO ENDER-DESC.                        RL2014.2
032000     IF       ERROR-COUNTER IS EQUAL TO ZERO                      RL2014.2
032100         MOVE "NO " TO ERROR-TOTAL                                RL2014.2
032200         ELSE                                                     RL2014.2
032300         MOVE ERROR-COUNTER TO ERROR-TOTAL.                       RL2014.2
032400     MOVE     CCVS-E-2 TO DUMMY-RECORD.                           RL2014.2
032500     PERFORM WRITE-LINE.                                          RL2014.2
032600 END-ROUTINE-13.                                                  RL2014.2
032700     IF DELETE-COUNTER IS EQUAL TO ZERO                           RL2014.2
032800         MOVE "NO " TO ERROR-TOTAL  ELSE                          RL2014.2
032900         MOVE DELETE-COUNTER TO ERROR-TOTAL.                      RL2014.2
033000     MOVE "TEST(S) DELETED     " TO ENDER-DESC.                   RL2014.2
033100     MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.           RL2014.2
033200      IF   INSPECT-COUNTER EQUAL TO ZERO                          RL2014.2
033300          MOVE "NO " TO ERROR-TOTAL                               RL2014.2
033400      ELSE MOVE INSPECT-COUNTER TO ERROR-TOTAL.                   RL2014.2
033500      MOVE "TEST(S) REQUIRE INSPECTION" TO ENDER-DESC.            RL2014.2
033600      MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.          RL2014.2
033700     MOVE CCVS-E-3 TO DUMMY-RECORD. PERFORM WRITE-LINE.           RL2014.2
033800 WRITE-LINE.                                                      RL2014.2
033900     ADD 1 TO RECORD-COUNT.                                       RL2014.2
034000Y    IF RECORD-COUNT GREATER 50                                   RL2014.2
034100Y        MOVE DUMMY-RECORD TO DUMMY-HOLD                          RL2014.2
034200Y        MOVE SPACE TO DUMMY-RECORD                               RL2014.2
034300Y        WRITE DUMMY-RECORD AFTER ADVANCING PAGE                  RL2014.2
034400Y        MOVE CCVS-C-1 TO DUMMY-RECORD PERFORM WRT-LN             RL2014.2
034500Y        MOVE CCVS-C-2 TO DUMMY-RECORD PERFORM WRT-LN 2 TIMES     RL2014.2
034600Y        MOVE HYPHEN-LINE TO DUMMY-RECORD PERFORM WRT-LN          RL2014.2
034700Y        MOVE DUMMY-HOLD TO DUMMY-RECORD                          RL2014.2
034800Y        MOVE ZERO TO RECORD-COUNT.                               RL2014.2
034900     PERFORM WRT-LN.                                              RL2014.2
035000 WRT-LN.                                                          RL2014.2
035100     WRITE    DUMMY-RECORD AFTER ADVANCING 1 LINES.               RL2014.2
035200     MOVE SPACE TO DUMMY-RECORD.                                  RL2014.2
035300 BLANK-LINE-PRINT.                                                RL2014.2
035400     PERFORM WRT-LN.                                              RL2014.2
035500 FAIL-ROUTINE.                                                    RL2014.2
035600     IF     COMPUTED-X NOT EQUAL TO SPACE                         RL2014.2
035700            GO TO   FAIL-ROUTINE-WRITE.                           RL2014.2
035800     IF     CORRECT-X NOT EQUAL TO SPACE GO TO FAIL-ROUTINE-WRITE.RL2014.2
035900     MOVE   ANSI-REFERENCE TO INF-ANSI-REFERENCE.                 RL2014.2
036000     MOVE  "NO FURTHER INFORMATION, SEE PROGRAM." TO INFO-TEXT.   RL2014.2
036100     MOVE   XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   RL2014.2
036200     MOVE   SPACES TO INF-ANSI-REFERENCE.                         RL2014.2
036300     GO TO  FAIL-ROUTINE-EX.                                      RL2014.2
036400 FAIL-ROUTINE-WRITE.                                              RL2014.2
036500     MOVE   TEST-COMPUTED TO PRINT-REC PERFORM WRITE-LINE         RL2014.2
036600     MOVE   ANSI-REFERENCE TO COR-ANSI-REFERENCE.                 RL2014.2
036700     MOVE   TEST-CORRECT TO PRINT-REC PERFORM WRITE-LINE 2 TIMES. RL2014.2
036800     MOVE   SPACES TO COR-ANSI-REFERENCE.                         RL2014.2
036900 FAIL-ROUTINE-EX. EXIT.                                           RL2014.2
037000 BAIL-OUT.                                                        RL2014.2
037100     IF     COMPUTED-A NOT EQUAL TO SPACE GO TO BAIL-OUT-WRITE.   RL2014.2
037200     IF     CORRECT-A EQUAL TO SPACE GO TO BAIL-OUT-EX.           RL2014.2
037300 BAIL-OUT-WRITE.                                                  RL2014.2
037400     MOVE CORRECT-A TO XXCORRECT. MOVE COMPUTED-A TO XXCOMPUTED.  RL2014.2
037500     MOVE   ANSI-REFERENCE TO INF-ANSI-REFERENCE.                 RL2014.2
037600     MOVE   XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   RL2014.2
037700     MOVE   SPACES TO INF-ANSI-REFERENCE.                         RL2014.2
037800 BAIL-OUT-EX. EXIT.                                               RL2014.2
037900 CCVS1-EXIT.                                                      RL2014.2
038000     EXIT.                                                        RL2014.2
038100 SECT-RL201-001 SECTION.                                          RL2014.2
038200 REL-INIT-001.                                                    RL2014.2
038300     MOVE     "FILE CREATE RL-FS1" TO FEATURE.                    RL2014.2
038400     OPEN     OUTPUT    RL-FS1.                                   RL2014.2
038500     MOVE     "RL-FS1" TO XFILE-NAME (1).                         RL2014.2
038600     MOVE     "R1-F-G" TO XRECORD-NAME (1).                       RL2014.2
038700     MOVE CCVS-PGM-ID      TO XPROGRAM-NAME (1).                  RL2014.2
038800     MOVE     000120   TO XRECORD-LENGTH (1).                     RL2014.2
038900     MOVE     "RC"     TO CHARS-OR-RECORDS (1).                   RL2014.2
039000     MOVE     0001     TO XBLOCK-SIZE (1).                        RL2014.2
039100     MOVE     000500   TO RECORDS-IN-FILE (1).                    RL2014.2
039200     MOVE     "RL"     TO XFILE-ORGANIZATION (1).                 RL2014.2
039300     MOVE     "S"      TO XLABEL-TYPE (1).                        RL2014.2
039400     MOVE     000001   TO XRECORD-NUMBER (1).                     RL2014.2
039500 REL-TEST-001.                                                    RL2014.2
039600     MOVE     FILE-RECORD-INFO-P1-120 (1) TO RL-FS1R1-F-G-120.    RL2014.2
039700     WRITE    RL-FS1R1-F-G-120                                    RL2014.2
039800              INVALID KEY GO TO REL-FAIL-001.                     RL2014.2
039900     IF      XRECORD-NUMBER (1) EQUAL TO 500                      RL2014.2
040000             GO TO REL-WRITE-001.                                 RL2014.2
040100     ADD      000001 TO XRECORD-NUMBER (1).                       RL2014.2
040200     GO       TO REL-TEST-001.                                    RL2014.2
040300 REL-DELETE-001.                                                  RL2014.2
040400     PERFORM   DE-LETE.                                           RL2014.2
040500     GO TO REL-WRITE-001.                                         RL2014.2
040600 REL-FAIL-001.                                                    RL2014.2
040700     PERFORM   FAIL.                                              RL2014.2
040800     MOVE    "BOUNDARY VIOLATION"  TO RE-MARK.                    RL2014.2
040900 REL-WRITE-001.                                                   RL2014.2
041000     MOVE     "REL-TEST-001" TO   PAR-NAME                        RL2014.2
041100     MOVE     "FILE CREATED, LFILE "  TO COMPUTED-A.              RL2014.2
041200     MOVE    XRECORD-NUMBER (1) TO CORRECT-18V0.                  RL2014.2
041300     PERFORM  PRINT-DETAIL.                                       RL2014.2
041400     CLOSE    RL-FS1.                                             RL2014.2
041500 REL-INIT-002.                                                    RL2014.2
041600     OPEN     INPUT     RL-FS1.                                   RL2014.2
041700     MOVE     ZERO      TO WRK-CS-09V00.                          RL2014.2
041800 REL-TEST-002.                                                    RL2014.2
041900     READ     RL-FS1                                              RL2014.2
042000              AT END GO TO REL-TEST-002-1.                        RL2014.2
042100     MOVE     RL-FS1R1-F-G-120 TO FILE-RECORD-INFO-P1-120 (1).    RL2014.2
042200     ADD      1 TO WRK-CS-09V00.                                  RL2014.2
042300     IF       WRK-CS-09V00 GREATER 500                            RL2014.2
042400             MOVE "MORE THAN 500 RECORDS" TO RE-MARK              RL2014.2
042500              GO TO REL-TEST-002-1.                               RL2014.2
042600     GO       TO REL-TEST-002.                                    RL2014.2
042700 REL-DELETE-002.                                                  RL2014.2
042800     PERFORM     DE-LETE.                                         RL2014.2
042900     PERFORM    PRINT-DETAIL.                                     RL2014.2
043000 REL-TEST-002-1.                                                  RL2014.2
043100     IF       XRECORD-NUMBER (1) NOT EQUAL TO 500                 RL2014.2
043200              PERFORM FAIL                                        RL2014.2
043300              ELSE                                                RL2014.2
043400              PERFORM PASS.                                       RL2014.2
043500     GO       TO REL-WRITE-002.                                   RL2014.2
043600 REL-WRITE-002.                                                   RL2014.2
043700     MOVE     "REL-TEST-002" TO PAR-NAME.                         RL2014.2
043800     MOVE     "FILE VERIFIED, LFILE" TO COMPUTED-A.               RL2014.2
043900     MOVE    XRECORD-NUMBER (1) TO CORRECT-18V0.                  RL2014.2
044000     PERFORM  PRINT-DETAIL.                                       RL2014.2
044100     CLOSE   RL-FS1.                                              RL2014.2
044200 CCVS-EXIT SECTION.                                               RL2014.2
044300 CCVS-999999.                                                     RL2014.2
044400     GO TO CLOSE-FILES.                                           RL2014.2
       END PROGRAM RL201A.
000100 IDENTIFICATION DIVISION.                                         RL2024.2
000200 PROGRAM-ID.                                                      RL2024.2
000300     RL202A.                                                      RL2024.2
000400****************************************************************  RL2024.2
000500*                                                              *  RL2024.2
000600*    VALIDATION FOR:-                                          *  RL2024.2
000700*                                                              *  RL2024.2
000800*    "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".RL2024.2
000900*                                                              *  RL2024.2
001000*    "COBOL 85 VERSION 4.2, Apr  1993 SSVG                      ".RL2024.2
001100*                                                              *  RL2024.2
001200****************************************************************  RL2024.2
001300*GENERAL:    THE FUNCTION OF THIS PROGRAM IS TO PROCESS A RELATIVERL2024.2
001400*            I-O FILE RANDOMLY (ACCESS MODE IS DYNAMIC). THE FILE RL2024.2
001500*            USED AS INPUT IS THAT FILE CREATED BY RL201.         RL2024.2
001600*                                                                 RL2024.2
001700*            FIRST THE FILE IS VERIFED AS TO THE EXISTANCE AND    RL2024.2
001800*            ACCURACY OF THE 500  RECORDS CREATED IN THE FIRST    RL2024.2
001900*            PROGRAM.  SECONDLY, RECORDS OF THE FILE ARE SEL-     RL2024.2
002000*            ECTIVELY UPDATED; AND THIRDLY, THE ACCURACY OF EACH  RL2024.2
002100*            RECORD IN THE FILE IS AGAIN VERIFIED.                RL2024.2
002200*                                                                 RL2024.2
002300*            X-CARD PARAMETERS WHICH MUST BE SUPPLIED FOR THIS    RL2024.2
002400*            PROGRAM ARE:                                         RL2024.2
002500*                                                                 RL2024.2
002600*                 X-21   IMPLEMENTOR-NAME IN ASSIGN TO CLAUSE FOR RL2024.2
002700*                         RELATIVE  I-O DATA FILE                 RL2024.2
002800*                 X-55   SYSTEM PRINTER                           RL2024.2
002900*                 X-69   ADDITIONAL VALUE OF CLAUSES              RL2024.2
003000*                 X-74   VALUE OF IMPLEMENTOR-NAME                RL2024.2
003100*                 X-75   OBJECT OF VALUE OF CLAUSE                RL2024.2
003200*                 X-82   SOURCE-COMPUTER                          RL2024.2
003300*                 X-83   OBJECT-COMPUTER.                         RL2024.2
003400*                                                                 RL2024.2
003500***************************************************               RL2024.2
003600 ENVIRONMENT DIVISION.                                            RL2024.2
003700 CONFIGURATION SECTION.                                           RL2024.2
003800 SOURCE-COMPUTER.                                                 RL2024.2
003900     XXXXX082.                                                    RL2024.2
004000 OBJECT-COMPUTER.                                                 RL2024.2
004100     XXXXX083.                                                    RL2024.2
004200 INPUT-OUTPUT SECTION.                                            RL2024.2
004300 FILE-CONTROL.                                                    RL2024.2
004400     SELECT PRINT-FILE ASSIGN TO                                  RL2024.2
004500     XXXXX055.                                                    RL2024.2
004600     SELECT  RL-FD1 ASSIGN TO                                     RL2024.2
004700     XXXXP021                                                     RL2024.2
004800             ORGANIZATION IS RELATIVE                             RL2024.2
004900             ACCESS  MODE IS DYNAMIC                              RL2024.2
005000             RELATIVE KEY RL-FD1-KEY.                             RL2024.2
005100 DATA DIVISION.                                                   RL2024.2
005200 FILE SECTION.                                                    RL2024.2
005300 FD  PRINT-FILE.                                                  RL2024.2
005400 01  PRINT-REC PICTURE X(120).                                    RL2024.2
005500 01  DUMMY-RECORD PICTURE X(120).                                 RL2024.2
005600 FD  RL-FD1                                                       RL2024.2
005700     LABEL RECORDS STANDARD                                       RL2024.2
005800C    VALUE OF                                                     RL2024.2
005900C    XXXXX074                                                     RL2024.2
006000C    IS                                                           RL2024.2
006100C    XXXXX075                                                     RL2024.2
006200G    XXXXX069                                                     RL2024.2
006300     BLOCK CONTAINS 1 RECORDS                                     RL2024.2
006400     RECORD CONTAINS 120 CHARACTERS.                              RL2024.2
006500 01  RL-FD1R1-F-G-120.                                            RL2024.2
006600     02 FILLER PICTURE X(120).                                    RL2024.2
006700 WORKING-STORAGE SECTION.                                         RL2024.2
006800 01  WRK-CS-09V00 PIC S9(09)      USAGE COMP VALUE ZERO.          RL2024.2
006900 01  RL-FD1-KEY        PIC 9(09)  USAGE COMP VALUE ZERO.          RL2024.2
007000 01  WRK-DS-09V00-002 PIC S9(9) VALUE ZERO.                       RL2024.2
007100 01  WRK-CS-09V00-002 PIC S9(09)       USAGE COMP VALUE ZERO.     RL2024.2
007200 01  WRK-CS-09V00-003 PIC S9(09)       USAGE COMP VALUE ZERO.     RL2024.2
007300 01  I-O-ERROR-RL-FD1 PIC X(3) VALUE "NO ".                       RL2024.2
007400 01  WRK-CS-09V00-001 PIC S9(09)       USAGE COMP VALUE ZERO.     RL2024.2
007500 01  WRK-CS-09V00-004 PIC S9(09)       USAGE COMP VALUE ZERO.     RL2024.2
007600 01  WRK-CS-09V00-005 PIC S9(09)       USAGE COMP VALUE ZERO.     RL2024.2
007700 01  WRK-DS-09V00-001 PIC S9(09)      VALUE ZERO.                 RL2024.2
007800 01  FILE-RECORD-INFORMATION-REC.                                 RL2024.2
007900     03 FILE-RECORD-INFO-SKELETON.                                RL2024.2
008000        05 FILLER                 PICTURE X(48)       VALUE       RL2024.2
008100             "FILE=      ,RECORD=      /0,RECNO=000000,UPDT=00".  RL2024.2
008200        05 FILLER                 PICTURE X(46)       VALUE       RL2024.2
008300             ",ODO=0000,PGM=     ,LRECL=000000,BLKSIZ  =0000".    RL2024.2
008400        05 FILLER                 PICTURE X(26)       VALUE       RL2024.2
008500             ",LFIL=000000,ORG=  ,LBLR= ".                        RL2024.2
008600        05 FILLER                 PICTURE X(37)       VALUE       RL2024.2
008700             ",RECKEY=                             ".             RL2024.2
008800        05 FILLER                 PICTURE X(38)       VALUE       RL2024.2
008900             ",ALTKEY1=                             ".            RL2024.2
009000        05 FILLER                 PICTURE X(38)       VALUE       RL2024.2
009100             ",ALTKEY2=                             ".            RL2024.2
009200        05 FILLER                 PICTURE X(7)        VALUE SPACE.RL2024.2
009300     03 FILE-RECORD-INFO          OCCURS  10  TIMES.              RL2024.2
009400        05 FILE-RECORD-INFO-P1-120.                               RL2024.2
009500           07 FILLER              PIC X(5).                       RL2024.2
009600           07 XFILE-NAME           PIC X(6).                      RL2024.2
009700           07 FILLER              PIC X(8).                       RL2024.2
009800           07 XRECORD-NAME         PIC X(6).                      RL2024.2
009900           07 FILLER              PIC X(1).                       RL2024.2
010000           07 REELUNIT-NUMBER     PIC 9(1).                       RL2024.2
010100           07 FILLER              PIC X(7).                       RL2024.2
010200           07 XRECORD-NUMBER       PIC 9(6).                      RL2024.2
010300           07 FILLER              PIC X(6).                       RL2024.2
010400           07 UPDATE-NUMBER       PIC 9(2).                       RL2024.2
010500           07 FILLER              PIC X(5).                       RL2024.2
010600           07 ODO-NUMBER          PIC 9(4).                       RL2024.2
010700           07 FILLER              PIC X(5).                       RL2024.2
010800           07 XPROGRAM-NAME        PIC X(5).                      RL2024.2
010900           07 FILLER              PIC X(7).                       RL2024.2
011000           07 XRECORD-LENGTH       PIC 9(6).                      RL2024.2
011100           07 FILLER              PIC X(7).                       RL2024.2
011200           07 CHARS-OR-RECORDS    PIC X(2).                       RL2024.2
011300           07 FILLER              PIC X(1).                       RL2024.2
011400           07 XBLOCK-SIZE          PIC 9(4).                      RL2024.2
011500           07 FILLER              PIC X(6).                       RL2024.2
011600           07 RECORDS-IN-FILE     PIC 9(6).                       RL2024.2
011700           07 FILLER              PIC X(5).                       RL2024.2
011800           07 XFILE-ORGANIZATION   PIC X(2).                      RL2024.2
011900           07 FILLER              PIC X(6).                       RL2024.2
012000           07 XLABEL-TYPE          PIC X(1).                      RL2024.2
012100        05 FILE-RECORD-INFO-P121-240.                             RL2024.2
012200           07 FILLER              PIC X(8).                       RL2024.2
012300           07 XRECORD-KEY          PIC X(29).                     RL2024.2
012400           07 FILLER              PIC X(9).                       RL2024.2
012500           07 ALTERNATE-KEY1      PIC X(29).                      RL2024.2
012600           07 FILLER              PIC X(9).                       RL2024.2
012700           07 ALTERNATE-KEY2      PIC X(29).                      RL2024.2
012800           07 FILLER              PIC X(7).                       RL2024.2
012900 01  TEST-RESULTS.                                                RL2024.2
013000     02 FILLER                   PIC X      VALUE SPACE.          RL2024.2
013100     02 FEATURE                  PIC X(20)  VALUE SPACE.          RL2024.2
013200     02 FILLER                   PIC X      VALUE SPACE.          RL2024.2
013300     02 P-OR-F                   PIC X(5)   VALUE SPACE.          RL2024.2
013400     02 FILLER                   PIC X      VALUE SPACE.          RL2024.2
013500     02  PAR-NAME.                                                RL2024.2
013600       03 FILLER                 PIC X(19)  VALUE SPACE.          RL2024.2
013700       03  PARDOT-X              PIC X      VALUE SPACE.          RL2024.2
013800       03 DOTVALUE               PIC 99     VALUE ZERO.           RL2024.2
013900     02 FILLER                   PIC X(8)   VALUE SPACE.          RL2024.2
014000     02 RE-MARK                  PIC X(61).                       RL2024.2
014100 01  TEST-COMPUTED.                                               RL2024.2
014200     02 FILLER                   PIC X(30)  VALUE SPACE.          RL2024.2
014300     02 FILLER                   PIC X(17)  VALUE                 RL2024.2
014400            "       COMPUTED=".                                   RL2024.2
014500     02 COMPUTED-X.                                               RL2024.2
014600     03 COMPUTED-A               PIC X(20)  VALUE SPACE.          RL2024.2
014700     03 COMPUTED-N               REDEFINES COMPUTED-A             RL2024.2
014800                                 PIC -9(9).9(9).                  RL2024.2
014900     03 COMPUTED-0V18 REDEFINES COMPUTED-A   PIC -.9(18).         RL2024.2
015000     03 COMPUTED-4V14 REDEFINES COMPUTED-A   PIC -9(4).9(14).     RL2024.2
015100     03 COMPUTED-14V4 REDEFINES COMPUTED-A   PIC -9(14).9(4).     RL2024.2
015200     03       CM-18V0 REDEFINES COMPUTED-A.                       RL2024.2
015300         04 COMPUTED-18V0                    PIC -9(18).          RL2024.2
015400         04 FILLER                           PIC X.               RL2024.2
015500     03 FILLER PIC X(50) VALUE SPACE.                             RL2024.2
015600 01  TEST-CORRECT.                                                RL2024.2
015700     02 FILLER PIC X(30) VALUE SPACE.                             RL2024.2
015800     02 FILLER PIC X(17) VALUE "       CORRECT =".                RL2024.2
015900     02 CORRECT-X.                                                RL2024.2
016000     03 CORRECT-A                  PIC X(20) VALUE SPACE.         RL2024.2
016100     03 CORRECT-N    REDEFINES CORRECT-A     PIC -9(9).9(9).      RL2024.2
016200     03 CORRECT-0V18 REDEFINES CORRECT-A     PIC -.9(18).         RL2024.2
016300     03 CORRECT-4V14 REDEFINES CORRECT-A     PIC -9(4).9(14).     RL2024.2
016400     03 CORRECT-14V4 REDEFINES CORRECT-A     PIC -9(14).9(4).     RL2024.2
016500     03      CR-18V0 REDEFINES CORRECT-A.                         RL2024.2
016600         04 CORRECT-18V0                     PIC -9(18).          RL2024.2
016700         04 FILLER                           PIC X.               RL2024.2
016800     03 FILLER PIC X(2) VALUE SPACE.                              RL2024.2
016900     03 COR-ANSI-REFERENCE             PIC X(48) VALUE SPACE.     RL2024.2
017000 01  CCVS-C-1.                                                    RL2024.2
017100     02 FILLER  PIC IS X(99)    VALUE IS " FEATURE              PARL2024.2
017200-    "SS  PARAGRAPH-NAME                                          RL2024.2
017300-    "       REMARKS".                                            RL2024.2
017400     02 FILLER                     PIC X(20)    VALUE SPACE.      RL2024.2
017500 01  CCVS-C-2.                                                    RL2024.2
017600     02 FILLER                     PIC X        VALUE SPACE.      RL2024.2
017700     02 FILLER                     PIC X(6)     VALUE "TESTED".   RL2024.2
017800     02 FILLER                     PIC X(15)    VALUE SPACE.      RL2024.2
017900     02 FILLER                     PIC X(4)     VALUE "FAIL".     RL2024.2
018000     02 FILLER                     PIC X(94)    VALUE SPACE.      RL2024.2
018100 01  REC-SKL-SUB                   PIC 9(2)     VALUE ZERO.       RL2024.2
018200 01  REC-CT                        PIC 99       VALUE ZERO.       RL2024.2
018300 01  DELETE-COUNTER                PIC 999      VALUE ZERO.       RL2024.2
018400 01  ERROR-COUNTER                 PIC 999      VALUE ZERO.       RL2024.2
018500 01  INSPECT-COUNTER               PIC 999      VALUE ZERO.       RL2024.2
018600 01  PASS-COUNTER                  PIC 999      VALUE ZERO.       RL2024.2
018700 01  TOTAL-ERROR                   PIC 999      VALUE ZERO.       RL2024.2
018800 01  ERROR-HOLD                    PIC 999      VALUE ZERO.       RL2024.2
018900 01  DUMMY-HOLD                    PIC X(120)   VALUE SPACE.      RL2024.2
019000 01  RECORD-COUNT                  PIC 9(5)     VALUE ZERO.       RL2024.2
019100 01  ANSI-REFERENCE                PIC X(48)    VALUE SPACES.     RL2024.2
019200 01  CCVS-H-1.                                                    RL2024.2
019300     02  FILLER                    PIC X(39)    VALUE SPACES.     RL2024.2
019400     02  FILLER                    PIC X(42)    VALUE             RL2024.2
019500     "OFFICIAL COBOL COMPILER VALIDATION SYSTEM".                 RL2024.2
019600     02  FILLER                    PIC X(39)    VALUE SPACES.     RL2024.2
019700 01  CCVS-H-2A.                                                   RL2024.2
019800   02  FILLER                        PIC X(40)  VALUE SPACE.      RL2024.2
019900   02  FILLER                        PIC X(7)   VALUE "CCVS85 ".  RL2024.2
020000   02  FILLER                        PIC XXXX   VALUE             RL2024.2
020100     "4.2 ".                                                      RL2024.2
020200   02  FILLER                        PIC X(28)  VALUE             RL2024.2
020300            " COPY - NOT FOR DISTRIBUTION".                       RL2024.2
020400   02  FILLER                        PIC X(41)  VALUE SPACE.      RL2024.2
020500                                                                  RL2024.2
020600 01  CCVS-H-2B.                                                   RL2024.2
020700   02  FILLER                        PIC X(15)  VALUE             RL2024.2
020800            "TEST RESULT OF ".                                    RL2024.2
020900   02  TEST-ID                       PIC X(9).                    RL2024.2
021000   02  FILLER                        PIC X(4)   VALUE             RL2024.2
021100            " IN ".                                               RL2024.2
021200   02  FILLER                        PIC X(12)  VALUE             RL2024.2
021300     " HIGH       ".                                              RL2024.2
021400   02  FILLER                        PIC X(22)  VALUE             RL2024.2
021500            " LEVEL VALIDATION FOR ".                             RL2024.2
021600   02  FILLER                        PIC X(58)  VALUE             RL2024.2
021700     "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".RL2024.2
021800 01  CCVS-H-3.                                                    RL2024.2
021900     02  FILLER                      PIC X(34)  VALUE             RL2024.2
022000            " FOR OFFICIAL USE ONLY    ".                         RL2024.2
022100     02  FILLER                      PIC X(58)  VALUE             RL2024.2
022200     "COBOL 85 VERSION 4.2, Apr  1993 SSVG                      ".RL2024.2
022300     02  FILLER                      PIC X(28)  VALUE             RL2024.2
022400            "  COPYRIGHT   1985 ".                                RL2024.2
022500 01  CCVS-E-1.                                                    RL2024.2
022600     02 FILLER                       PIC X(52)  VALUE SPACE.      RL2024.2
022700     02 FILLER  PIC X(14) VALUE IS "END OF TEST-  ".              RL2024.2
022800     02 ID-AGAIN                     PIC X(9).                    RL2024.2
022900     02 FILLER                       PIC X(45)  VALUE SPACES.     RL2024.2
023000 01  CCVS-E-2.                                                    RL2024.2
023100     02  FILLER                      PIC X(31)  VALUE SPACE.      RL2024.2
023200     02  FILLER                      PIC X(21)  VALUE SPACE.      RL2024.2
023300     02 CCVS-E-2-2.                                               RL2024.2
023400         03 ERROR-TOTAL              PIC XXX    VALUE SPACE.      RL2024.2
023500         03 FILLER                   PIC X      VALUE SPACE.      RL2024.2
023600         03 ENDER-DESC               PIC X(44)  VALUE             RL2024.2
023700            "ERRORS ENCOUNTERED".                                 RL2024.2
023800 01  CCVS-E-3.                                                    RL2024.2
023900     02  FILLER                      PIC X(22)  VALUE             RL2024.2
024000            " FOR OFFICIAL USE ONLY".                             RL2024.2
024100     02  FILLER                      PIC X(12)  VALUE SPACE.      RL2024.2
024200     02  FILLER                      PIC X(58)  VALUE             RL2024.2
024300     "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".RL2024.2
024400     02  FILLER                      PIC X(13)  VALUE SPACE.      RL2024.2
024500     02 FILLER                       PIC X(15)  VALUE             RL2024.2
024600             " COPYRIGHT 1985".                                   RL2024.2
024700 01  CCVS-E-4.                                                    RL2024.2
024800     02 CCVS-E-4-1                   PIC XXX    VALUE SPACE.      RL2024.2
024900     02 FILLER                       PIC X(4)   VALUE " OF ".     RL2024.2
025000     02 CCVS-E-4-2                   PIC XXX    VALUE SPACE.      RL2024.2
025100     02 FILLER                       PIC X(40)  VALUE             RL2024.2
025200      "  TESTS WERE EXECUTED SUCCESSFULLY".                       RL2024.2
025300 01  XXINFO.                                                      RL2024.2
025400     02 FILLER                       PIC X(19)  VALUE             RL2024.2
025500            "*** INFORMATION ***".                                RL2024.2
025600     02 INFO-TEXT.                                                RL2024.2
025700       04 FILLER                     PIC X(8)   VALUE SPACE.      RL2024.2
025800       04 XXCOMPUTED                 PIC X(20).                   RL2024.2
025900       04 FILLER                     PIC X(5)   VALUE SPACE.      RL2024.2
026000       04 XXCORRECT                  PIC X(20).                   RL2024.2
026100     02 INF-ANSI-REFERENCE           PIC X(48).                   RL2024.2
026200 01  HYPHEN-LINE.                                                 RL2024.2
026300     02 FILLER  PIC IS X VALUE IS SPACE.                          RL2024.2
026400     02 FILLER  PIC IS X(65)    VALUE IS "************************RL2024.2
026500-    "*****************************************".                 RL2024.2
026600     02 FILLER  PIC IS X(54)    VALUE IS "************************RL2024.2
026700-    "******************************".                            RL2024.2
026800 01  CCVS-PGM-ID                     PIC X(9)   VALUE             RL2024.2
026900     "RL202A".                                                    RL2024.2
027000 PROCEDURE DIVISION.                                              RL2024.2
027100 CCVS1 SECTION.                                                   RL2024.2
027200 OPEN-FILES.                                                      RL2024.2
027300     OPEN    OUTPUT PRINT-FILE.                                   RL2024.2
027400     MOVE  CCVS-PGM-ID TO TEST-ID. MOVE CCVS-PGM-ID TO ID-AGAIN.  RL2024.2
027500     MOVE    SPACE TO TEST-RESULTS.                               RL2024.2
027600     PERFORM HEAD-ROUTINE THRU COLUMN-NAMES-ROUTINE.              RL2024.2
027700     MOVE    ZERO TO REC-SKL-SUB.                                 RL2024.2
027800     PERFORM CCVS-INIT-FILE 9 TIMES.                              RL2024.2
027900 CCVS-INIT-FILE.                                                  RL2024.2
028000     ADD     1 TO REC-SKL-SUB.                                    RL2024.2
028100     MOVE    FILE-RECORD-INFO-SKELETON                            RL2024.2
028200          TO FILE-RECORD-INFO (REC-SKL-SUB).                      RL2024.2
028300 CCVS-INIT-EXIT.                                                  RL2024.2
028400     GO TO CCVS1-EXIT.                                            RL2024.2
028500 CLOSE-FILES.                                                     RL2024.2
028600     PERFORM END-ROUTINE THRU END-ROUTINE-13. CLOSE PRINT-FILE.   RL2024.2
028700 TERMINATE-CCVS.                                                  RL2024.2
028800S    EXIT PROGRAM.                                                RL2024.2
028900STERMINATE-CALL.                                                  RL2024.2
029000     STOP     RUN.                                                RL2024.2
029100 INSPT. MOVE "INSPT" TO P-OR-F. ADD 1 TO INSPECT-COUNTER.         RL2024.2
029200 PASS.  MOVE "PASS " TO P-OR-F.  ADD 1 TO PASS-COUNTER.           RL2024.2
029300 FAIL.  MOVE "FAIL*" TO P-OR-F.  ADD 1 TO ERROR-COUNTER.          RL2024.2
029400 DE-LETE.  MOVE "*****" TO P-OR-F.  ADD 1 TO DELETE-COUNTER.      RL2024.2
029500     MOVE "****TEST DELETED****" TO RE-MARK.                      RL2024.2
029600 PRINT-DETAIL.                                                    RL2024.2
029700     IF REC-CT NOT EQUAL TO ZERO                                  RL2024.2
029800             MOVE "." TO PARDOT-X                                 RL2024.2
029900             MOVE REC-CT TO DOTVALUE.                             RL2024.2
030000     MOVE     TEST-RESULTS TO PRINT-REC. PERFORM WRITE-LINE.      RL2024.2
030100     IF P-OR-F EQUAL TO "FAIL*"  PERFORM WRITE-LINE               RL2024.2
030200        PERFORM FAIL-ROUTINE THRU FAIL-ROUTINE-EX                 RL2024.2
030300          ELSE PERFORM BAIL-OUT THRU BAIL-OUT-EX.                 RL2024.2
030400     MOVE SPACE TO P-OR-F. MOVE SPACE TO COMPUTED-X.              RL2024.2
030500     MOVE SPACE TO CORRECT-X.                                     RL2024.2
030600     IF     REC-CT EQUAL TO ZERO  MOVE SPACE TO PAR-NAME.         RL2024.2
030700     MOVE     SPACE TO RE-MARK.                                   RL2024.2
030800 HEAD-ROUTINE.                                                    RL2024.2
030900     MOVE CCVS-H-1  TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.  RL2024.2
031000     MOVE CCVS-H-2A TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.  RL2024.2
031100     MOVE CCVS-H-2B TO DUMMY-RECORD. PERFORM WRITE-LINE 3 TIMES.  RL2024.2
031200     MOVE CCVS-H-3  TO DUMMY-RECORD. PERFORM WRITE-LINE 3 TIMES.  RL2024.2
031300 COLUMN-NAMES-ROUTINE.                                            RL2024.2
031400     MOVE CCVS-C-1 TO DUMMY-RECORD. PERFORM WRITE-LINE.           RL2024.2
031500     MOVE CCVS-C-2 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   RL2024.2
031600     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE.        RL2024.2
031700 END-ROUTINE.                                                     RL2024.2
031800     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE 5 TIMES.RL2024.2
031900 END-RTN-EXIT.                                                    RL2024.2
032000     MOVE CCVS-E-1 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   RL2024.2
032100 END-ROUTINE-1.                                                   RL2024.2
032200      ADD ERROR-COUNTER TO ERROR-HOLD ADD INSPECT-COUNTER TO      RL2024.2
032300      ERROR-HOLD. ADD DELETE-COUNTER TO ERROR-HOLD.               RL2024.2
032400      ADD PASS-COUNTER TO ERROR-HOLD.                             RL2024.2
032500*     IF PASS-COUNTER EQUAL TO ERROR-HOLD GO TO END-ROUTINE-12.   RL2024.2
032600      MOVE PASS-COUNTER TO CCVS-E-4-1.                            RL2024.2
032700      MOVE ERROR-HOLD TO CCVS-E-4-2.                              RL2024.2
032800      MOVE CCVS-E-4 TO CCVS-E-2-2.                                RL2024.2
032900      MOVE CCVS-E-2 TO DUMMY-RECORD PERFORM WRITE-LINE.           RL2024.2
033000  END-ROUTINE-12.                                                 RL2024.2
033100      MOVE "TEST(S) FAILED" TO ENDER-DESC.                        RL2024.2
033200     IF       ERROR-COUNTER IS EQUAL TO ZERO                      RL2024.2
033300         MOVE "NO " TO ERROR-TOTAL                                RL2024.2
033400         ELSE                                                     RL2024.2
033500         MOVE ERROR-COUNTER TO ERROR-TOTAL.                       RL2024.2
033600     MOVE     CCVS-E-2 TO DUMMY-RECORD.                           RL2024.2
033700     PERFORM WRITE-LINE.                                          RL2024.2
033800 END-ROUTINE-13.                                                  RL2024.2
033900     IF DELETE-COUNTER IS EQUAL TO ZERO                           RL2024.2
034000         MOVE "NO " TO ERROR-TOTAL  ELSE                          RL2024.2
034100         MOVE DELETE-COUNTER TO ERROR-TOTAL.                      RL2024.2
034200     MOVE "TEST(S) DELETED     " TO ENDER-DESC.                   RL2024.2
034300     MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.           RL2024.2
034400      IF   INSPECT-COUNTER EQUAL TO ZERO                          RL2024.2
034500          MOVE "NO " TO ERROR-TOTAL                               RL2024.2
034600      ELSE MOVE INSPECT-COUNTER TO ERROR-TOTAL.                   RL2024.2
034700      MOVE "TEST(S) REQUIRE INSPECTION" TO ENDER-DESC.            RL2024.2
034800      MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.          RL2024.2
034900     MOVE CCVS-E-3 TO DUMMY-RECORD. PERFORM WRITE-LINE.           RL2024.2
035000 WRITE-LINE.                                                      RL2024.2
035100     ADD 1 TO RECORD-COUNT.                                       RL2024.2
035200Y    IF RECORD-COUNT GREATER 50                                   RL2024.2
035300Y        MOVE DUMMY-RECORD TO DUMMY-HOLD                          RL2024.2
035400Y        MOVE SPACE TO DUMMY-RECORD                               RL2024.2
035500Y        WRITE DUMMY-RECORD AFTER ADVANCING PAGE                  RL2024.2
035600Y        MOVE CCVS-C-1 TO DUMMY-RECORD PERFORM WRT-LN             RL2024.2
035700Y        MOVE CCVS-C-2 TO DUMMY-RECORD PERFORM WRT-LN 2 TIMES     RL2024.2
035800Y        MOVE HYPHEN-LINE TO DUMMY-RECORD PERFORM WRT-LN          RL2024.2
035900Y        MOVE DUMMY-HOLD TO DUMMY-RECORD                          RL2024.2
036000Y        MOVE ZERO TO RECORD-COUNT.                               RL2024.2
036100     PERFORM WRT-LN.                                              RL2024.2
036200 WRT-LN.                                                          RL2024.2
036300     WRITE    DUMMY-RECORD AFTER ADVANCING 1 LINES.               RL2024.2
036400     MOVE SPACE TO DUMMY-RECORD.                                  RL2024.2
036500 BLANK-LINE-PRINT.                                                RL2024.2
036600     PERFORM WRT-LN.                                              RL2024.2
036700 FAIL-ROUTINE.                                                    RL2024.2
036800     IF     COMPUTED-X NOT EQUAL TO SPACE                         RL2024.2
036900            GO TO   FAIL-ROUTINE-WRITE.                           RL2024.2
037000     IF     CORRECT-X NOT EQUAL TO SPACE GO TO FAIL-ROUTINE-WRITE.RL2024.2
037100     MOVE   ANSI-REFERENCE TO INF-ANSI-REFERENCE.                 RL2024.2
037200     MOVE  "NO FURTHER INFORMATION, SEE PROGRAM." TO INFO-TEXT.   RL2024.2
037300     MOVE   XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   RL2024.2
037400     MOVE   SPACES TO INF-ANSI-REFERENCE.                         RL2024.2
037500     GO TO  FAIL-ROUTINE-EX.                                      RL2024.2
037600 FAIL-ROUTINE-WRITE.                                              RL2024.2
037700     MOVE   TEST-COMPUTED TO PRINT-REC PERFORM WRITE-LINE         RL2024.2
037800     MOVE   ANSI-REFERENCE TO COR-ANSI-REFERENCE.                 RL2024.2
037900     MOVE   TEST-CORRECT TO PRINT-REC PERFORM WRITE-LINE 2 TIMES. RL2024.2
038000     MOVE   SPACES TO COR-ANSI-REFERENCE.                         RL2024.2
038100 FAIL-ROUTINE-EX. EXIT.                                           RL2024.2
038200 BAIL-OUT.                                                        RL2024.2
038300     IF     COMPUTED-A NOT EQUAL TO SPACE GO TO BAIL-OUT-WRITE.   RL2024.2
038400     IF     CORRECT-A EQUAL TO SPACE GO TO BAIL-OUT-EX.           RL2024.2
038500 BAIL-OUT-WRITE.                                                  RL2024.2
038600     MOVE CORRECT-A TO XXCORRECT. MOVE COMPUTED-A TO XXCOMPUTED.  RL2024.2
038700     MOVE   ANSI-REFERENCE TO INF-ANSI-REFERENCE.                 RL2024.2
038800     MOVE   XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   RL2024.2
038900     MOVE   SPACES TO INF-ANSI-REFERENCE.                         RL2024.2
039000 BAIL-OUT-EX. EXIT.                                               RL2024.2
039100 CCVS1-EXIT.                                                      RL2024.2
039200     EXIT.                                                        RL2024.2
039300 SECT-RL202-001 SECTION.                                          RL2024.2
039400 REL-INIT-003.                                                    RL2024.2
039500     OPEN     INPUT  RL-FD1.                                      RL2024.2
039600     MOVE     "REL-TEST-003"   TO PAR-NAME.                       RL2024.2
039700     MOVE     ZERO TO   RL-FD1-KEY.                               RL2024.2
039800     MOVE     ZERO TO   WRK-CS-09V00-002                          RL2024.2
039900     MOVE     ZERO  TO  WRK-CS-09V00-003                          RL2024.2
040000*                                                                 RL2024.2
040100     MOVE     01 TO REC-CT.                                       RL2024.2
040200     MOVE    "READ RANDOM"  TO FEATURE.                           RL2024.2
040300 REL-TEST-003-R.                                                  RL2024.2
040400     ADD      1 TO WRK-CS-09V00-003                               RL2024.2
040500     MOVE     WRK-CS-09V00-003 TO RL-FD1-KEY.                     RL2024.2
040600     IF       RL-FD1-KEY GREATER +501                             RL2024.2
040700              MOVE "INVALID KEY NOT TAKEN" TO COMPUTED-A          RL2024.2
040800              MOVE RL-FD1-KEY TO CORRECT-18V0                     RL2024.2
040900              PERFORM FAIL                                        RL2024.2
041000              PERFORM PRINT-DETAIL                                RL2024.2
041100              ADD 1 TO REC-CT                                     RL2024.2
041200              GO TO REL-WRITE-003.                                RL2024.2
041300     READ     RL-FD1                                              RL2024.2
041400             INVALID KEY GO TO REL-WRITE-003.                     RL2024.2
041500     MOVE     RL-FD1R1-F-G-120 TO FILE-RECORD-INFO-P1-120 (1).    RL2024.2
041600     IF       XRECORD-NUMBER (1) EQUAL TO RL-FD1-KEY              RL2024.2
041700              GO TO REL-TEST-003-R.                               RL2024.2
041800     MOVE    "YES" TO I-O-ERROR-RL-FD1.                           RL2024.2
041900     ADD      1 TO WRK-CS-09V00-002                               RL2024.2
042000     GO TO    REL-TEST-003-R.                                     RL2024.2
042100 REL-WRITE-003.                                                   RL2024.2
042200     IF      RL-FD1-KEY NOT EQUAL TO 501                          RL2024.2
042300              MOVE "WRONG KEY/NOT 500" TO CORRECT-A               RL2024.2
042400              MOVE  RL-FD1-KEY TO COMPUTED-18V0                   RL2024.2
042500              PERFORM FAIL                                        RL2024.2
042600              ELSE                                                RL2024.2
042700              PERFORM PASS.                                       RL2024.2
042800     PERFORM PRINT-DETAIL.                                        RL2024.2
042900*                                                                 RL2024.2
043000*01                                                               RL2024.2
043100*                                                                 RL2024.2
043200     ADD      1 TO REC-CT.                                        RL2024.2
043300     IF       XRECORD-NUMBER (1) NOT EQUAL TO 500                 RL2024.2
043400              MOVE "WRONG RECORD/NOT 500" TO CORRECT-A            RL2024.2
043500              MOVE  XRECORD-NUMBER (1) TO COMPUTED-18V0           RL2024.2
043600              PERFORM FAIL                                        RL2024.2
043700              ELSE                                                RL2024.2
043800              PERFORM PASS.                                       RL2024.2
043900     PERFORM PRINT-DETAIL.                                        RL2024.2
044000*                                                                 RL2024.2
044100*02                                                               RL2024.2
044200*                                                                 RL2024.2
044300     ADD      1 TO REC-CT.                                        RL2024.2
044400     IF      WRK-CS-09V00-003 NOT EQUAL TO 501                    RL2024.2
044500              MOVE "INCORRECT RECORD COUNT" TO RE-MARK            RL2024.2
044600              MOVE  WRK-CS-09V00-003 TO COMPUTED-18V0             RL2024.2
044700             MOVE 501  TO CORRECT-18V0                            RL2024.2
044800              PERFORM FAIL                                        RL2024.2
044900              ELSE                                                RL2024.2
045000              PERFORM PASS.                                       RL2024.2
045100     PERFORM PRINT-DETAIL.                                        RL2024.2
045200*                                                                 RL2024.2
045300*03                                                               RL2024.2
045400*                                                                 RL2024.2
045500     ADD      1 TO REC-CT.                                        RL2024.2
045600     IF       I-O-ERROR-RL-FD1 EQUAL TO "YES"                     RL2024.2
045700              MOVE WRK-CS-09V00-002 TO COMPUTED-18V0              RL2024.2
045800              MOVE "RECORDS DID NOT COMPARE" TO RE-MARK           RL2024.2
045900              PERFORM FAIL                                        RL2024.2
046000              ELSE                                                RL2024.2
046100              PERFORM PASS.                                       RL2024.2
046200     PERFORM PRINT-DETAIL.                                        RL2024.2
046300*                                                                 RL2024.2
046400*04                                                               RL2024.2
046500*                                                                 RL2024.2
046600     ADD      1 TO REC-CT.                                        RL2024.2
046700     CLOSE    RL-FD1.                                             RL2024.2
046800 REL-INIT-004-R .                                                 RL2024.2
046900     MOVE     "REL-TEST-004" TO PAR-NAME.                         RL2024.2
047000     OPEN I-O RL-FD1.                                             RL2024.2
047100     MOVE     ZERO TO RL-FD1-KEY.                                 RL2024.2
047200     MOVE     ZERO TO WRK-CS-09V00-002.                           RL2024.2
047300     MOVE     ZERO TO WRK-CS-09V00-003.                           RL2024.2
047400      MOVE    ZERO TO WRK-CS-09V00-004.                           RL2024.2
047500      MOVE    ZERO TO WRK-CS-09V00-005.                           RL2024.2
047600*                                                                 RL2024.2
047700     MOVE     01 TO REC-CT.                                       RL2024.2
047800     MOVE     SPACE TO  FILE-RECORD-INFO-P1-120 (1).              RL2024.2
047900     MOVE    "REWRITE"  TO FEATURE.                               RL2024.2
048000 REL-TEST-004-R.                                                  RL2024.2
048100     ADD      5 TO  WRK-CS-09V00-003.                             RL2024.2
048200     MOVE     WRK-CS-09V00-003 TO RL-FD1-KEY.                     RL2024.2
048300      IF     RL-FD1-KEY GREATER 505                               RL2024.2
048400              MOVE "INVALID KEY/NOT TAKEN" TO COMPUTED-A          RL2024.2
048500              MOVE  RL-FD1-KEY TO CORRECT-18V0                    RL2024.2
048600              PERFORM FAIL                                        RL2024.2
048700              PERFORM PRINT-DETAIL                                RL2024.2
048800              ADD 1 TO REC-CT                                     RL2024.2
048900              GO TO REL-TEST-004-3.                               RL2024.2
049000     READ     RL-FD1                                              RL2024.2
049100              INVALID KEY GO TO REL-TEST-004-1.                   RL2024.2
049200     MOVE    RL-FD1R1-F-G-120 TO FILE-RECORD-INFO-P1-120 (1)      RL2024.2
049300     ADD      01 TO UPDATE-NUMBER (1).                            RL2024.2
049400     MOVE CCVS-PGM-ID       TO XPROGRAM-NAME (1).                 RL2024.2
049500     MOVE     FILE-RECORD-INFO-P1-120 (1) TO RL-FD1R1-F-G-120.    RL2024.2
049600     REWRITE    RL-FD1R1-F-G-120                                  RL2024.2
049700              INVALID KEY GO TO REL-TEST-004-2.                   RL2024.2
049800     GO TO    REL-TEST-004-R.                                     RL2024.2
049900 REL-TEST-004-1.                                                  RL2024.2
050000     IF       RL-FD1-KEY LESS THAN 501                            RL2024.2
050100              ADD 1 TO  WRK-CS-09V00-004                          RL2024.2
050200              GO TO REL-TEST-004-R.                               RL2024.2
050300     PERFORM   PASS.                                              RL2024.2
050400     PERFORM   PRINT-DETAIL.                                      RL2024.2
050500*                                                                 RL2024.2
050600*01                                                               RL2024.2
050700*                                                                 RL2024.2
050800     ADD     1  TO REC-CT.                                        RL2024.2
050900     GO TO    REL-TEST-004-3.                                     RL2024.2
051000 REL-TEST-004-2.                                                  RL2024.2
051100     ADD      1 TO WRK-CS-09V00-005.                              RL2024.2
051200     IF       RL-FD1-KEY LESS 501                                 RL2024.2
051300              GO TO REL-TEST-004-R.                               RL2024.2
051400 REL-TEST-004-3.                                                  RL2024.2
051500     IF       WRK-CS-09V00-004 NOT EQUAL TO ZERO                  RL2024.2
051600              MOVE "INVALID KEY ON READ" TO COMPUTED-A            RL2024.2
051700              MOVE WRK-CS-09V00-004 TO CORRECT-18V0               RL2024.2
051800              PERFORM FAIL                                        RL2024.2
051900              ELSE                                                RL2024.2
052000              PERFORM PASS.                                       RL2024.2
052100     PERFORM PRINT-DETAIL.                                        RL2024.2
052200*                                                                 RL2024.2
052300*02                                                               RL2024.2
052400*                                                                 RL2024.2
052500     ADD      1 TO REC-CT.                                        RL2024.2
052600     IF       WRK-CS-09V00-005 NOT EQUAL TO ZERO                  RL2024.2
052700              MOVE "INVALID KEY ON REWRITE" TO COMPUTED-A         RL2024.2
052800              MOVE  WRK-CS-09V00-005 TO CORRECT-18V0              RL2024.2
052900              PERFORM FAIL                                        RL2024.2
053000              ELSE                                                RL2024.2
053100              PERFORM PASS.                                       RL2024.2
053200     PERFORM PRINT-DETAIL.                                        RL2024.2
053300*                                                                 RL2024.2
053400*03                                                               RL2024.2
053500*                                                                 RL2024.2
053600     ADD      1 TO REC-CT.                                        RL2024.2
053700     CLOSE    RL-FD1.                                             RL2024.2
053800 REL-INIT-005.                                                    RL2024.2
053900     MOVE     "REL-TEST-005" TO PAR-NAME.                         RL2024.2
054000     OPEN     INPUT  RL-FD1.                                      RL2024.2
054100     MOVE     501  TO WRK-CS-09V00-003.                           RL2024.2
054200     MOVE    ZERO TO WRK-CS-09V00-004.                            RL2024.2
054300     MOVE    ZERO TO WRK-CS-09V00-005.                            RL2024.2
054400     MOVE    ZERO TO WRK-CS-09V00-002.                            RL2024.2
054500     MOVE     SPACE TO  FILE-RECORD-INFO-P1-120 (1).              RL2024.2
054600     MOVE     01 TO REC-CT.                                       RL2024.2
054700*                                                                 RL2024.2
054800     MOVE    "READ RANDOM"  TO FEATURE.                           RL2024.2
054900 REL-TEST-005-R.                                                  RL2024.2
055000     SUBTRACT 1 FROM    WRK-CS-09V00-003.                         RL2024.2
055100     MOVE     WRK-CS-09V00-003 TO RL-FD1-KEY.                     RL2024.2
055200     IF      WRK-CS-09V00-003 LESS THAN ZERO                      RL2024.2
055300             MOVE    "INVALID KEY/NOT TAKEN"  TO RE-MARK          RL2024.2
055400             MOVE   WRK-CS-09V00-003  TO COMPUTED-18V0            RL2024.2
055500             MOVE   ZERO TO CORRECT-18V0                          RL2024.2
055600              PERFORM FAIL                                        RL2024.2
055700              PERFORM PRINT-DETAIL                                RL2024.2
055800              ADD 1 TO REC-CT                                     RL2024.2
055900              GO TO REL-TEST-005-3.                               RL2024.2
056000     READ     RL-FD1                                              RL2024.2
056100              INVALID KEY  GO TO REL-TEST-005-1.                  RL2024.2
056200     MOVE     RL-FD1R1-F-G-120 TO FILE-RECORD-INFO-P1-120 (1).    RL2024.2
056300     IF     UPDATE-NUMBER (1) EQUAL TO 00                         RL2024.2
056400              ADD 1 TO WRK-CS-09V00-004.                          RL2024.2
056500     IF     UPDATE-NUMBER (1) EQUAL TO 01                         RL2024.2
056600              ADD 1 TO WRK-CS-09V00-005.                          RL2024.2
056700     GO TO    REL-TEST-005-R.                                     RL2024.2
056800 REL-TEST-005-1.                                                  RL2024.2
056900     IF       RL-FD1-KEY GREATER ZERO                             RL2024.2
057000             ADD 1 TO WRK-CS-09V00-002                            RL2024.2
057100              GO TO REL-TEST-005-R.                               RL2024.2
057200     PERFORM   PASS.                                              RL2024.2
057300     PERFORM   PRINT-DETAIL.                                      RL2024.2
057400     ADD      1  TO REC-CT.                                       RL2024.2
057500*01                                                               RL2024.2
057600     GO TO    REL-TEST-005-3.                                     RL2024.2
057700 REL-TEST-005-3.                                                  RL2024.2
057800     IF       WRK-CS-09V00-004 NOT EQUAL TO 400                   RL2024.2
057900              MOVE "NON-UPDATED RECORDS" TO COMPUTED-A            RL2024.2
058000              MOVE WRK-CS-09V00-004 TO CORRECT-18V0               RL2024.2
058100              MOVE "SHOULD BE 400" TO RE-MARK                     RL2024.2
058200              PERFORM FAIL                                        RL2024.2
058300              ELSE                                                RL2024.2
058400              PERFORM PASS.                                       RL2024.2
058500     PERFORM PRINT-DETAIL.                                        RL2024.2
058600*                                                                 RL2024.2
058700*                                                                 RL2024.2
058800*02                                                               RL2024.2
058900*                                                                 RL2024.2
059000     ADD      1 TO REC-CT.                                        RL2024.2
059100     IF       WRK-CS-09V00-005 NOT EQUAL TO 100                   RL2024.2
059200              MOVE "UPDATED RECORDS" TO COMPUTED-A                RL2024.2
059300              MOVE WRK-CS-09V00-005 TO CORRECT-18V0               RL2024.2
059400              MOVE "SHOULD BE 100" TO RE-MARK                     RL2024.2
059500              PERFORM FAIL                                        RL2024.2
059600              ELSE                                                RL2024.2
059700              PERFORM PASS.                                       RL2024.2
059800     PERFORM PRINT-DETAIL.                                        RL2024.2
059900*                                                                 RL2024.2
060000*03                                                               RL2024.2
060100*                                                                 RL2024.2
060200     ADD      1 TO REC-CT.                                        RL2024.2
060300     IF      WRK-CS-09V00-002 GREATER 1                           RL2024.2
060400             MOVE WRK-CS-09V00-002 TO COMPUTED-N                  RL2024.2
060500             MOVE  "INVALID KEY/READS" TO CORRECT-A               RL2024.2
060600             PERFORM FAIL                                         RL2024.2
060700              ELSE                                                RL2024.2
060800              PERFORM PASS.                                       RL2024.2
060900     PERFORM PRINT-DETAIL.                                        RL2024.2
061000*                                                                 RL2024.2
061100*04                                                               RL2024.2
061200*                                                                 RL2024.2
061300     ADD      1 TO REC-CT.                                        RL2024.2
061400     CLOSE    RL-FD1.                                             RL2024.2
061500 CCVS-EXIT SECTION.                                               RL2024.2
061600 CCVS-999999.                                                     RL2024.2
061700     GO TO CLOSE-FILES.                                           RL2024.2
       END PROGRAM RL202A.
000100 IDENTIFICATION DIVISION.                                         RL2034.2
000200 PROGRAM-ID.                                                      RL2034.2
000300     RL203A.                                                      RL2034.2
000400****************************************************************  RL2034.2
000500*                                                              *  RL2034.2
000600*    VALIDATION FOR:-                                          *  RL2034.2
000700*                                                              *  RL2034.2
000800*    "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".RL2034.2
000900*                                                              *  RL2034.2
001000*    "COBOL 85 VERSION 4.2, Apr  1993 SSVG                      ".RL2034.2
001100*                                                              *  RL2034.2
001200****************************************************************  RL2034.2
001300*GENERAL:    THIS PROGRAM IS THE THIRD OF A SERIES.  THE FUNCTION RL2034.2
001400*            OF THIS PROGRAM IS TO PROCESS THE FILE SEQUENTIALLY  RL2034.2
001500*            (ACCESS MODE IS DYNAMIC).    THE FILE USED IS THAT   RL2034.2
001600*            RESULTING FROM RL102.                                RL2034.2
001700*                                                                 RL2034.2
001800*            FIRST, THE FILE IS VERIFIED FOR ACCURACY OF ITS 500  RL2034.2
001900*            RECORDS.  SECONDLY, RECORDS OF THER FILE ARE         RL2034.2
002000*            SELECTIVELY DELETED AND THIRDLY THE ACCURACY OF EACH RL2034.2
002100*            RECORD IN THE FILE IS AGAIN VERIFIED.                RL2034.2
002200*                                                                 RL2034.2
002300*            X-CARD PARAMETERS WHICH MUST BE SUPPLIED FOR THIS    RL2034.2
002400*            PROGRAM ARE:                                         RL2034.2
002500*                                                                 RL2034.2
002600*                 X-21   IMPLEMENTOR-NAME IN ASSIGN TO CLAUSE FOR RL2034.2
002700*                         RELATIVE  I-O DATA FILE                 RL2034.2
002800*                 X-55   SYSTEM PRINTER                           RL2034.2
002900*                 X-69   ADDITIONAL VALUE OF CLAUSES              RL2034.2
003000*                 X-74   VALUE OF IMPLEMENTOR-NAME                RL2034.2
003100*                 X-75   OBJECT OF VALUE OF CLAUSE                RL2034.2
003200*                 X-82   SOURCE-COMPUTER                          RL2034.2
003300*                 X-83   OBJECT-COMPUTER.                         RL2034.2
003400*                                                                 RL2034.2
003500***************************************************               RL2034.2
003600 ENVIRONMENT DIVISION.                                            RL2034.2
003700 CONFIGURATION SECTION.                                           RL2034.2
003800 SOURCE-COMPUTER.                                                 RL2034.2
003900     XXXXX082.                                                    RL2034.2
004000 OBJECT-COMPUTER.                                                 RL2034.2
004100     XXXXX083.                                                    RL2034.2
004200 INPUT-OUTPUT SECTION.                                            RL2034.2
004300 FILE-CONTROL.                                                    RL2034.2
004400     SELECT PRINT-FILE ASSIGN TO                                  RL2034.2
004500     XXXXX055.                                                    RL2034.2
004600     SELECT   RL-FD1 ASSIGN TO                                    RL2034.2
004700     XXXXD021                                                     RL2034.2
004800       ACCESS MODE IS DYNAMIC                                     RL2034.2
004900             RELATIVE KEY IS RL-FD1-KEY                           RL2034.2
005000       ORGANIZATION IS RELATIVE.                                  RL2034.2
005100 DATA DIVISION.                                                   RL2034.2
005200 FILE SECTION.                                                    RL2034.2
005300 FD  PRINT-FILE.                                                  RL2034.2
005400 01  PRINT-REC PICTURE X(120).                                    RL2034.2
005500 01  DUMMY-RECORD PICTURE X(120).                                 RL2034.2
005600 FD  RL-FD1                                                       RL2034.2
005700     LABEL RECORDS STANDARD                                       RL2034.2
005800C    VALUE OF                                                     RL2034.2
005900C    XXXXX074                                                     RL2034.2
006000C    IS                                                           RL2034.2
006100C    XXXXX075                                                     RL2034.2
006200G    XXXXX069                                                     RL2034.2
006300     BLOCK CONTAINS 01 RECORDS                                    RL2034.2
006400     RECORD CONTAINS 120.                                         RL2034.2
006500 01  RL-FD1R1-F-G-120.                                            RL2034.2
006600     02 RL-WRK-120 PIC X(120).                                    RL2034.2
006700 WORKING-STORAGE SECTION.                                         RL2034.2
006800 01  RL-FD1-KEY        PIC 9(08)  USAGE COMP VALUE ZERO.          RL2034.2
006900 01  WRK-CS-09V00-006 PIC S9(09) USAGE COMP VALUE ZERO.           RL2034.2
007000 01  WRK-CS-09V00-007 PIC S9(09) USAGE COMP VALUE ZERO.           RL2034.2
007100 01  WRK-CS-09V00-008 PIC S9(09) USAGE COMP VALUE ZERO.           RL2034.2
007200 01  WRK-CS-09V00-009 PIC S9(09) USAGE COMP VALUE ZERO.           RL2034.2
007300 01  WRK-CS-09V00-010 PIC S9(09) USAGE COMP VALUE ZERO.           RL2034.2
007400 01  WRK-CS-09V00-011 PIC S9(09) USAGE COMP VALUE ZERO.           RL2034.2
007500 01  I-O-ERROR-RL-FD1 PIC X(3) VALUE "NO ".                       RL2034.2
007600 01  FILE-RECORD-INFORMATION-REC.                                 RL2034.2
007700     03 FILE-RECORD-INFO-SKELETON.                                RL2034.2
007800        05 FILLER                 PICTURE X(48)       VALUE       RL2034.2
007900             "FILE=      ,RECORD=      /0,RECNO=000000,UPDT=00".  RL2034.2
008000        05 FILLER                 PICTURE X(46)       VALUE       RL2034.2
008100             ",ODO=0000,PGM=     ,LRECL=000000,BLKSIZ  =0000".    RL2034.2
008200        05 FILLER                 PICTURE X(26)       VALUE       RL2034.2
008300             ",LFIL=000000,ORG=  ,LBLR= ".                        RL2034.2
008400        05 FILLER                 PICTURE X(37)       VALUE       RL2034.2
008500             ",RECKEY=                             ".             RL2034.2
008600        05 FILLER                 PICTURE X(38)       VALUE       RL2034.2
008700             ",ALTKEY1=                             ".            RL2034.2
008800        05 FILLER                 PICTURE X(38)       VALUE       RL2034.2
008900             ",ALTKEY2=                             ".            RL2034.2
009000        05 FILLER                 PICTURE X(7)        VALUE SPACE.RL2034.2
009100     03 FILE-RECORD-INFO          OCCURS  10  TIMES.              RL2034.2
009200        05 FILE-RECORD-INFO-P1-120.                               RL2034.2
009300           07 FILLER              PIC X(5).                       RL2034.2
009400           07 XFILE-NAME           PIC X(6).                      RL2034.2
009500           07 FILLER              PIC X(8).                       RL2034.2
009600           07 XRECORD-NAME         PIC X(6).                      RL2034.2
009700           07 FILLER              PIC X(1).                       RL2034.2
009800           07 REELUNIT-NUMBER     PIC 9(1).                       RL2034.2
009900           07 FILLER              PIC X(7).                       RL2034.2
010000           07 XRECORD-NUMBER       PIC 9(6).                      RL2034.2
010100           07 FILLER              PIC X(6).                       RL2034.2
010200           07 UPDATE-NUMBER       PIC 9(2).                       RL2034.2
010300           07 FILLER              PIC X(5).                       RL2034.2
010400           07 ODO-NUMBER          PIC 9(4).                       RL2034.2
010500           07 FILLER              PIC X(5).                       RL2034.2
010600           07 XPROGRAM-NAME        PIC X(5).                      RL2034.2
010700           07 FILLER              PIC X(7).                       RL2034.2
010800           07 XRECORD-LENGTH       PIC 9(6).                      RL2034.2
010900           07 FILLER              PIC X(7).                       RL2034.2
011000           07 CHARS-OR-RECORDS    PIC X(2).                       RL2034.2
011100           07 FILLER              PIC X(1).                       RL2034.2
011200           07 XBLOCK-SIZE          PIC 9(4).                      RL2034.2
011300           07 FILLER              PIC X(6).                       RL2034.2
011400           07 RECORDS-IN-FILE     PIC 9(6).                       RL2034.2
011500           07 FILLER              PIC X(5).                       RL2034.2
011600           07 XFILE-ORGANIZATION   PIC X(2).                      RL2034.2
011700           07 FILLER              PIC X(6).                       RL2034.2
011800           07 XLABEL-TYPE          PIC X(1).                      RL2034.2
011900        05 FILE-RECORD-INFO-P121-240.                             RL2034.2
012000           07 FILLER              PIC X(8).                       RL2034.2
012100           07 XRECORD-KEY          PIC X(29).                     RL2034.2
012200           07 FILLER              PIC X(9).                       RL2034.2
012300           07 ALTERNATE-KEY1      PIC X(29).                      RL2034.2
012400           07 FILLER              PIC X(9).                       RL2034.2
012500           07 ALTERNATE-KEY2      PIC X(29).                      RL2034.2
012600           07 FILLER              PIC X(7).                       RL2034.2
012700 01  TEST-RESULTS.                                                RL2034.2
012800     02 FILLER                   PIC X      VALUE SPACE.          RL2034.2
012900     02 FEATURE                  PIC X(20)  VALUE SPACE.          RL2034.2
013000     02 FILLER                   PIC X      VALUE SPACE.          RL2034.2
013100     02 P-OR-F                   PIC X(5)   VALUE SPACE.          RL2034.2
013200     02 FILLER                   PIC X      VALUE SPACE.          RL2034.2
013300     02  PAR-NAME.                                                RL2034.2
013400       03 FILLER                 PIC X(19)  VALUE SPACE.          RL2034.2
013500       03  PARDOT-X              PIC X      VALUE SPACE.          RL2034.2
013600       03 DOTVALUE               PIC 99     VALUE ZERO.           RL2034.2
013700     02 FILLER                   PIC X(8)   VALUE SPACE.          RL2034.2
013800     02 RE-MARK                  PIC X(61).                       RL2034.2
013900 01  TEST-COMPUTED.                                               RL2034.2
014000     02 FILLER                   PIC X(30)  VALUE SPACE.          RL2034.2
014100     02 FILLER                   PIC X(17)  VALUE                 RL2034.2
014200            "       COMPUTED=".                                   RL2034.2
014300     02 COMPUTED-X.                                               RL2034.2
014400     03 COMPUTED-A               PIC X(20)  VALUE SPACE.          RL2034.2
014500     03 COMPUTED-N               REDEFINES COMPUTED-A             RL2034.2
014600                                 PIC -9(9).9(9).                  RL2034.2
014700     03 COMPUTED-0V18 REDEFINES COMPUTED-A   PIC -.9(18).         RL2034.2
014800     03 COMPUTED-4V14 REDEFINES COMPUTED-A   PIC -9(4).9(14).     RL2034.2
014900     03 COMPUTED-14V4 REDEFINES COMPUTED-A   PIC -9(14).9(4).     RL2034.2
015000     03       CM-18V0 REDEFINES COMPUTED-A.                       RL2034.2
015100         04 COMPUTED-18V0                    PIC -9(18).          RL2034.2
015200         04 FILLER                           PIC X.               RL2034.2
015300     03 FILLER PIC X(50) VALUE SPACE.                             RL2034.2
015400 01  TEST-CORRECT.                                                RL2034.2
015500     02 FILLER PIC X(30) VALUE SPACE.                             RL2034.2
015600     02 FILLER PIC X(17) VALUE "       CORRECT =".                RL2034.2
015700     02 CORRECT-X.                                                RL2034.2
015800     03 CORRECT-A                  PIC X(20) VALUE SPACE.         RL2034.2
015900     03 CORRECT-N    REDEFINES CORRECT-A     PIC -9(9).9(9).      RL2034.2
016000     03 CORRECT-0V18 REDEFINES CORRECT-A     PIC -.9(18).         RL2034.2
016100     03 CORRECT-4V14 REDEFINES CORRECT-A     PIC -9(4).9(14).     RL2034.2
016200     03 CORRECT-14V4 REDEFINES CORRECT-A     PIC -9(14).9(4).     RL2034.2
016300     03      CR-18V0 REDEFINES CORRECT-A.                         RL2034.2
016400         04 CORRECT-18V0                     PIC -9(18).          RL2034.2
016500         04 FILLER                           PIC X.               RL2034.2
016600     03 FILLER PIC X(2) VALUE SPACE.                              RL2034.2
016700     03 COR-ANSI-REFERENCE             PIC X(48) VALUE SPACE.     RL2034.2
016800 01  CCVS-C-1.                                                    RL2034.2
016900     02 FILLER  PIC IS X(99)    VALUE IS " FEATURE              PARL2034.2
017000-    "SS  PARAGRAPH-NAME                                          RL2034.2
017100-    "       REMARKS".                                            RL2034.2
017200     02 FILLER                     PIC X(20)    VALUE SPACE.      RL2034.2
017300 01  CCVS-C-2.                                                    RL2034.2
017400     02 FILLER                     PIC X        VALUE SPACE.      RL2034.2
017500     02 FILLER                     PIC X(6)     VALUE "TESTED".   RL2034.2
017600     02 FILLER                     PIC X(15)    VALUE SPACE.      RL2034.2
017700     02 FILLER                     PIC X(4)     VALUE "FAIL".     RL2034.2
017800     02 FILLER                     PIC X(94)    VALUE SPACE.      RL2034.2
017900 01  REC-SKL-SUB                   PIC 9(2)     VALUE ZERO.       RL2034.2
018000 01  REC-CT                        PIC 99       VALUE ZERO.       RL2034.2
018100 01  DELETE-COUNTER                PIC 999      VALUE ZERO.       RL2034.2
018200 01  ERROR-COUNTER                 PIC 999      VALUE ZERO.       RL2034.2
018300 01  INSPECT-COUNTER               PIC 999      VALUE ZERO.       RL2034.2
018400 01  PASS-COUNTER                  PIC 999      VALUE ZERO.       RL2034.2
018500 01  TOTAL-ERROR                   PIC 999      VALUE ZERO.       RL2034.2
018600 01  ERROR-HOLD                    PIC 999      VALUE ZERO.       RL2034.2
018700 01  DUMMY-HOLD                    PIC X(120)   VALUE SPACE.      RL2034.2
018800 01  RECORD-COUNT                  PIC 9(5)     VALUE ZERO.       RL2034.2
018900 01  ANSI-REFERENCE                PIC X(48)    VALUE SPACES.     RL2034.2
019000 01  CCVS-H-1.                                                    RL2034.2
019100     02  FILLER                    PIC X(39)    VALUE SPACES.     RL2034.2
019200     02  FILLER                    PIC X(42)    VALUE             RL2034.2
019300     "OFFICIAL COBOL COMPILER VALIDATION SYSTEM".                 RL2034.2
019400     02  FILLER                    PIC X(39)    VALUE SPACES.     RL2034.2
019500 01  CCVS-H-2A.                                                   RL2034.2
019600   02  FILLER                        PIC X(40)  VALUE SPACE.      RL2034.2
019700   02  FILLER                        PIC X(7)   VALUE "CCVS85 ".  RL2034.2
019800   02  FILLER                        PIC XXXX   VALUE             RL2034.2
019900     "4.2 ".                                                      RL2034.2
020000   02  FILLER                        PIC X(28)  VALUE             RL2034.2
020100            " COPY - NOT FOR DISTRIBUTION".                       RL2034.2
020200   02  FILLER                        PIC X(41)  VALUE SPACE.      RL2034.2
020300                                                                  RL2034.2
020400 01  CCVS-H-2B.                                                   RL2034.2
020500   02  FILLER                        PIC X(15)  VALUE             RL2034.2
020600            "TEST RESULT OF ".                                    RL2034.2
020700   02  TEST-ID                       PIC X(9).                    RL2034.2
020800   02  FILLER                        PIC X(4)   VALUE             RL2034.2
020900            " IN ".                                               RL2034.2
021000   02  FILLER                        PIC X(12)  VALUE             RL2034.2
021100     " HIGH       ".                                              RL2034.2
021200   02  FILLER                        PIC X(22)  VALUE             RL2034.2
021300            " LEVEL VALIDATION FOR ".                             RL2034.2
021400   02  FILLER                        PIC X(58)  VALUE             RL2034.2
021500     "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".RL2034.2
021600 01  CCVS-H-3.                                                    RL2034.2
021700     02  FILLER                      PIC X(34)  VALUE             RL2034.2
021800            " FOR OFFICIAL USE ONLY    ".                         RL2034.2
021900     02  FILLER                      PIC X(58)  VALUE             RL2034.2
022000     "COBOL 85 VERSION 4.2, Apr  1993 SSVG                      ".RL2034.2
022100     02  FILLER                      PIC X(28)  VALUE             RL2034.2
022200            "  COPYRIGHT   1985 ".                                RL2034.2
022300 01  CCVS-E-1.                                                    RL2034.2
022400     02 FILLER                       PIC X(52)  VALUE SPACE.      RL2034.2
022500     02 FILLER  PIC X(14) VALUE IS "END OF TEST-  ".              RL2034.2
022600     02 ID-AGAIN                     PIC X(9).                    RL2034.2
022700     02 FILLER                       PIC X(45)  VALUE SPACES.     RL2034.2
022800 01  CCVS-E-2.                                                    RL2034.2
022900     02  FILLER                      PIC X(31)  VALUE SPACE.      RL2034.2
023000     02  FILLER                      PIC X(21)  VALUE SPACE.      RL2034.2
023100     02 CCVS-E-2-2.                                               RL2034.2
023200         03 ERROR-TOTAL              PIC XXX    VALUE SPACE.      RL2034.2
023300         03 FILLER                   PIC X      VALUE SPACE.      RL2034.2
023400         03 ENDER-DESC               PIC X(44)  VALUE             RL2034.2
023500            "ERRORS ENCOUNTERED".                                 RL2034.2
023600 01  CCVS-E-3.                                                    RL2034.2
023700     02  FILLER                      PIC X(22)  VALUE             RL2034.2
023800            " FOR OFFICIAL USE ONLY".                             RL2034.2
023900     02  FILLER                      PIC X(12)  VALUE SPACE.      RL2034.2
024000     02  FILLER                      PIC X(58)  VALUE             RL2034.2
024100     "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".RL2034.2
024200     02  FILLER                      PIC X(13)  VALUE SPACE.      RL2034.2
024300     02 FILLER                       PIC X(15)  VALUE             RL2034.2
024400             " COPYRIGHT 1985".                                   RL2034.2
024500 01  CCVS-E-4.                                                    RL2034.2
024600     02 CCVS-E-4-1                   PIC XXX    VALUE SPACE.      RL2034.2
024700     02 FILLER                       PIC X(4)   VALUE " OF ".     RL2034.2
024800     02 CCVS-E-4-2                   PIC XXX    VALUE SPACE.      RL2034.2
024900     02 FILLER                       PIC X(40)  VALUE             RL2034.2
025000      "  TESTS WERE EXECUTED SUCCESSFULLY".                       RL2034.2
025100 01  XXINFO.                                                      RL2034.2
025200     02 FILLER                       PIC X(19)  VALUE             RL2034.2
025300            "*** INFORMATION ***".                                RL2034.2
025400     02 INFO-TEXT.                                                RL2034.2
025500       04 FILLER                     PIC X(8)   VALUE SPACE.      RL2034.2
025600       04 XXCOMPUTED                 PIC X(20).                   RL2034.2
025700       04 FILLER                     PIC X(5)   VALUE SPACE.      RL2034.2
025800       04 XXCORRECT                  PIC X(20).                   RL2034.2
025900     02 INF-ANSI-REFERENCE           PIC X(48).                   RL2034.2
026000 01  HYPHEN-LINE.                                                 RL2034.2
026100     02 FILLER  PIC IS X VALUE IS SPACE.                          RL2034.2
026200     02 FILLER  PIC IS X(65)    VALUE IS "************************RL2034.2
026300-    "*****************************************".                 RL2034.2
026400     02 FILLER  PIC IS X(54)    VALUE IS "************************RL2034.2
026500-    "******************************".                            RL2034.2
026600 01  CCVS-PGM-ID                     PIC X(9)   VALUE             RL2034.2
026700     "RL203A".                                                    RL2034.2
026800 PROCEDURE DIVISION.                                              RL2034.2
026900 CCVS1 SECTION.                                                   RL2034.2
027000 OPEN-FILES.                                                      RL2034.2
027100     OPEN    OUTPUT PRINT-FILE.                                   RL2034.2
027200     MOVE  CCVS-PGM-ID TO TEST-ID. MOVE CCVS-PGM-ID TO ID-AGAIN.  RL2034.2
027300     MOVE    SPACE TO TEST-RESULTS.                               RL2034.2
027400     PERFORM HEAD-ROUTINE THRU COLUMN-NAMES-ROUTINE.              RL2034.2
027500     MOVE    ZERO TO REC-SKL-SUB.                                 RL2034.2
027600     PERFORM CCVS-INIT-FILE 9 TIMES.                              RL2034.2
027700 CCVS-INIT-FILE.                                                  RL2034.2
027800     ADD     1 TO REC-SKL-SUB.                                    RL2034.2
027900     MOVE    FILE-RECORD-INFO-SKELETON                            RL2034.2
028000          TO FILE-RECORD-INFO (REC-SKL-SUB).                      RL2034.2
028100 CCVS-INIT-EXIT.                                                  RL2034.2
028200     GO TO CCVS1-EXIT.                                            RL2034.2
028300 CLOSE-FILES.                                                     RL2034.2
028400     PERFORM END-ROUTINE THRU END-ROUTINE-13. CLOSE PRINT-FILE.   RL2034.2
028500 TERMINATE-CCVS.                                                  RL2034.2
028600S    EXIT PROGRAM.                                                RL2034.2
028700STERMINATE-CALL.                                                  RL2034.2
028800     STOP     RUN.                                                RL2034.2
028900 INSPT. MOVE "INSPT" TO P-OR-F. ADD 1 TO INSPECT-COUNTER.         RL2034.2
029000 PASS.  MOVE "PASS " TO P-OR-F.  ADD 1 TO PASS-COUNTER.           RL2034.2
029100 FAIL.  MOVE "FAIL*" TO P-OR-F.  ADD 1 TO ERROR-COUNTER.          RL2034.2
029200 DE-LETE.  MOVE "*****" TO P-OR-F.  ADD 1 TO DELETE-COUNTER.      RL2034.2
029300     MOVE "****TEST DELETED****" TO RE-MARK.                      RL2034.2
029400 PRINT-DETAIL.                                                    RL2034.2
029500     IF REC-CT NOT EQUAL TO ZERO                                  RL2034.2
029600             MOVE "." TO PARDOT-X                                 RL2034.2
029700             MOVE REC-CT TO DOTVALUE.                             RL2034.2
029800     MOVE     TEST-RESULTS TO PRINT-REC. PERFORM WRITE-LINE.      RL2034.2
029900     IF P-OR-F EQUAL TO "FAIL*"  PERFORM WRITE-LINE               RL2034.2
030000        PERFORM FAIL-ROUTINE THRU FAIL-ROUTINE-EX                 RL2034.2
030100          ELSE PERFORM BAIL-OUT THRU BAIL-OUT-EX.                 RL2034.2
030200     MOVE SPACE TO P-OR-F. MOVE SPACE TO COMPUTED-X.              RL2034.2
030300     MOVE SPACE TO CORRECT-X.                                     RL2034.2
030400     IF     REC-CT EQUAL TO ZERO  MOVE SPACE TO PAR-NAME.         RL2034.2
030500     MOVE     SPACE TO RE-MARK.                                   RL2034.2
030600 HEAD-ROUTINE.                                                    RL2034.2
030700     MOVE CCVS-H-1  TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.  RL2034.2
030800     MOVE CCVS-H-2A TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.  RL2034.2
030900     MOVE CCVS-H-2B TO DUMMY-RECORD. PERFORM WRITE-LINE 3 TIMES.  RL2034.2
031000     MOVE CCVS-H-3  TO DUMMY-RECORD. PERFORM WRITE-LINE 3 TIMES.  RL2034.2
031100 COLUMN-NAMES-ROUTINE.                                            RL2034.2
031200     MOVE CCVS-C-1 TO DUMMY-RECORD. PERFORM WRITE-LINE.           RL2034.2
031300     MOVE CCVS-C-2 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   RL2034.2
031400     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE.        RL2034.2
031500 END-ROUTINE.                                                     RL2034.2
031600     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE 5 TIMES.RL2034.2
031700 END-RTN-EXIT.                                                    RL2034.2
031800     MOVE CCVS-E-1 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   RL2034.2
031900 END-ROUTINE-1.                                                   RL2034.2
032000      ADD ERROR-COUNTER TO ERROR-HOLD ADD INSPECT-COUNTER TO      RL2034.2
032100      ERROR-HOLD. ADD DELETE-COUNTER TO ERROR-HOLD.               RL2034.2
032200      ADD PASS-COUNTER TO ERROR-HOLD.                             RL2034.2
032300*     IF PASS-COUNTER EQUAL TO ERROR-HOLD GO TO END-ROUTINE-12.   RL2034.2
032400      MOVE PASS-COUNTER TO CCVS-E-4-1.                            RL2034.2
032500      MOVE ERROR-HOLD TO CCVS-E-4-2.                              RL2034.2
032600      MOVE CCVS-E-4 TO CCVS-E-2-2.                                RL2034.2
032700      MOVE CCVS-E-2 TO DUMMY-RECORD PERFORM WRITE-LINE.           RL2034.2
032800  END-ROUTINE-12.                                                 RL2034.2
032900      MOVE "TEST(S) FAILED" TO ENDER-DESC.                        RL2034.2
033000     IF       ERROR-COUNTER IS EQUAL TO ZERO                      RL2034.2
033100         MOVE "NO " TO ERROR-TOTAL                                RL2034.2
033200         ELSE                                                     RL2034.2
033300         MOVE ERROR-COUNTER TO ERROR-TOTAL.                       RL2034.2
033400     MOVE     CCVS-E-2 TO DUMMY-RECORD.                           RL2034.2
033500     PERFORM WRITE-LINE.                                          RL2034.2
033600 END-ROUTINE-13.                                                  RL2034.2
033700     IF DELETE-COUNTER IS EQUAL TO ZERO                           RL2034.2
033800         MOVE "NO " TO ERROR-TOTAL  ELSE                          RL2034.2
033900         MOVE DELETE-COUNTER TO ERROR-TOTAL.                      RL2034.2
034000     MOVE "TEST(S) DELETED     " TO ENDER-DESC.                   RL2034.2
034100     MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.           RL2034.2
034200      IF   INSPECT-COUNTER EQUAL TO ZERO                          RL2034.2
034300          MOVE "NO " TO ERROR-TOTAL                               RL2034.2
034400      ELSE MOVE INSPECT-COUNTER TO ERROR-TOTAL.                   RL2034.2
034500      MOVE "TEST(S) REQUIRE INSPECTION" TO ENDER-DESC.            RL2034.2
034600      MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.          RL2034.2
034700     MOVE CCVS-E-3 TO DUMMY-RECORD. PERFORM WRITE-LINE.           RL2034.2
034800 WRITE-LINE.                                                      RL2034.2
034900     ADD 1 TO RECORD-COUNT.                                       RL2034.2
035000Y    IF RECORD-COUNT GREATER 50                                   RL2034.2
035100Y        MOVE DUMMY-RECORD TO DUMMY-HOLD                          RL2034.2
035200Y        MOVE SPACE TO DUMMY-RECORD                               RL2034.2
035300Y        WRITE DUMMY-RECORD AFTER ADVANCING PAGE                  RL2034.2
035400Y        MOVE CCVS-C-1 TO DUMMY-RECORD PERFORM WRT-LN             RL2034.2
035500Y        MOVE CCVS-C-2 TO DUMMY-RECORD PERFORM WRT-LN 2 TIMES     RL2034.2
035600Y        MOVE HYPHEN-LINE TO DUMMY-RECORD PERFORM WRT-LN          RL2034.2
035700Y        MOVE DUMMY-HOLD TO DUMMY-RECORD                          RL2034.2
035800Y        MOVE ZERO TO RECORD-COUNT.                               RL2034.2
035900     PERFORM WRT-LN.                                              RL2034.2
036000 WRT-LN.                                                          RL2034.2
036100     WRITE    DUMMY-RECORD AFTER ADVANCING 1 LINES.               RL2034.2
036200     MOVE SPACE TO DUMMY-RECORD.                                  RL2034.2
036300 BLANK-LINE-PRINT.                                                RL2034.2
036400     PERFORM WRT-LN.                                              RL2034.2
036500 FAIL-ROUTINE.                                                    RL2034.2
036600     IF     COMPUTED-X NOT EQUAL TO SPACE                         RL2034.2
036700            GO TO   FAIL-ROUTINE-WRITE.                           RL2034.2
036800     IF     CORRECT-X NOT EQUAL TO SPACE GO TO FAIL-ROUTINE-WRITE.RL2034.2
036900     MOVE   ANSI-REFERENCE TO INF-ANSI-REFERENCE.                 RL2034.2
037000     MOVE  "NO FURTHER INFORMATION, SEE PROGRAM." TO INFO-TEXT.   RL2034.2
037100     MOVE   XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   RL2034.2
037200     MOVE   SPACES TO INF-ANSI-REFERENCE.                         RL2034.2
037300     GO TO  FAIL-ROUTINE-EX.                                      RL2034.2
037400 FAIL-ROUTINE-WRITE.                                              RL2034.2
037500     MOVE   TEST-COMPUTED TO PRINT-REC PERFORM WRITE-LINE         RL2034.2
037600     MOVE   ANSI-REFERENCE TO COR-ANSI-REFERENCE.                 RL2034.2
037700     MOVE   TEST-CORRECT TO PRINT-REC PERFORM WRITE-LINE 2 TIMES. RL2034.2
037800     MOVE   SPACES TO COR-ANSI-REFERENCE.                         RL2034.2
037900 FAIL-ROUTINE-EX. EXIT.                                           RL2034.2
038000 BAIL-OUT.                                                        RL2034.2
038100     IF     COMPUTED-A NOT EQUAL TO SPACE GO TO BAIL-OUT-WRITE.   RL2034.2
038200     IF     CORRECT-A EQUAL TO SPACE GO TO BAIL-OUT-EX.           RL2034.2
038300 BAIL-OUT-WRITE.                                                  RL2034.2
038400     MOVE CORRECT-A TO XXCORRECT. MOVE COMPUTED-A TO XXCOMPUTED.  RL2034.2
038500     MOVE   ANSI-REFERENCE TO INF-ANSI-REFERENCE.                 RL2034.2
038600     MOVE   XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   RL2034.2
038700     MOVE   SPACES TO INF-ANSI-REFERENCE.                         RL2034.2
038800 BAIL-OUT-EX. EXIT.                                               RL2034.2
038900 CCVS1-EXIT.                                                      RL2034.2
039000     EXIT.                                                        RL2034.2
039100 SECT-RL-03-001 SECTION.                                          RL2034.2
039200 REL-INIT-006.                                                    RL2034.2
039300     MOVE  99 TO RL-FD1-KEY.                                      RL2034.2
039400*    CONTAIN THE NUMBER OF THE RECORD PREVIOUSLY READ.            RL2034.2
039500     OPEN INPUT RL-FD1.                                           RL2034.2
039600     MOVE     "REL-TEST-006" TO   PAR-NAME.                       RL2034.2
039700     MOVE     ZERO TO             WRK-CS-09V00-006.               RL2034.2
039800     MOVE     ZERO TO             WRK-CS-09V00-007.               RL2034.2
039900     MOVE     ZERO TO             WRK-CS-09V00-008.               RL2034.2
040000     MOVE     ZERO TO             WRK-CS-09V00-009.               RL2034.2
040100     MOVE     ZERO TO             WRK-CS-09V00-010.               RL2034.2
040200     MOVE     ZERO TO             WRK-CS-09V00-011.               RL2034.2
040300     MOVE     SPACE TO  FILE-RECORD-INFO-P1-120 (1).              RL2034.2
040400     MOVE    RL-FD1-KEY TO WRK-CS-09V00-011.                      RL2034.2
040500     MOVE     01 TO REC-CT.                                       RL2034.2
040600     MOVE    "READ SEQUENTIAL"  TO FEATURE.                       RL2034.2
040700 REL-TEST-006-R.                                                  RL2034.2
040800     ADD      1 TO WRK-CS-09V00-006.                              RL2034.2
040900     READ     RL-FD1  NEXT RECORD                                 RL2034.2
041000              AT END GO TO REL-TEST-006-3.                        RL2034.2
041100     MOVE     RL-FD1R1-F-G-120    TO FILE-RECORD-INFO-P1-120 (1). RL2034.2
041200     IF       UPDATE-NUMBER (1) EQUAL TO 00                       RL2034.2
041300             ADD 1 TO WRK-CS-09V00-007                            RL2034.2
041400              GO TO REL-TEST-006-2.                               RL2034.2
041500     IF       UPDATE-NUMBER (1) EQUAL TO 01                       RL2034.2
041600              ADD 1 TO WRK-CS-09V00-008                           RL2034.2
041700              GO TO REL-TEST-006-2.                               RL2034.2
041800     ADD      1 TO WRK-CS-09V00-009.                              RL2034.2
041900 REL-TEST-006-2.                                                  RL2034.2
042000     IF       RL-FD1-KEY NOT EQUAL TO XRECORD-NUMBER (1)          RL2034.2
042100              ADD 1 TO  WRK-CS-09V00-010.                         RL2034.2
042200     IF       WRK-CS-09V00-006  GREATER 501                       RL2034.2
042300              GO TO REL-TEST-006-3.                               RL2034.2
042400     GO TO    REL-TEST-006-R.                                     RL2034.2
042500 REL-TEST-006-3.                                                  RL2034.2
042600     IF       WRK-CS-09V00-006 NOT EQUAL TO 501                   RL2034.2
042700              MOVE "INCORRECT RECORD COUNT"  TO RE-MARK           RL2034.2
042800              MOVE  WRK-CS-09V00-006 TO COMPUTED-18V0             RL2034.2
042900              MOVE  501  TO             CORRECT-18V0              RL2034.2
043000              PERFORM FAIL                                        RL2034.2
043100              ELSE                                                RL2034.2
043200              PERFORM PASS.                                       RL2034.2
043300     PERFORM  PRINT-DETAIL.                                       RL2034.2
043400*    .01                                                          RL2034.2
043500     ADD      1 TO REC-CT.                                        RL2034.2
043600     IF       WRK-CS-09V00-007 EQUAL TO 400                       RL2034.2
043700              PERFORM PASS                                        RL2034.2
043800              ELSE                                                RL2034.2
043900              MOVE "NON-UPDATED RECORDS" TO COMPUTED-A            RL2034.2
044000              MOVE  WRK-CS-09V00-007 TO CORRECT-18V0              RL2034.2
044100              MOVE "SHOULD BE 400" TO RE-MARK                     RL2034.2
044200              PERFORM FAIL.                                       RL2034.2
044300     PERFORM  PRINT-DETAIL.                                       RL2034.2
044400     ADD      1 TO REC-CT.                                        RL2034.2
044500*    .02                                                          RL2034.2
044600     IF      WRK-CS-09V00-008 EQUAL TO 100                        RL2034.2
044700              PERFORM PASS                                        RL2034.2
044800              ELSE                                                RL2034.2
044900             MOVE WRK-CS-09V00-008 TO COMPUTED-18V0               RL2034.2
045000             MOVE 100             TO  CORRECT-18V0                RL2034.2
045100             MOVE "UPDATED RECORDS" TO RE-MARK                    RL2034.2
045200              PERFORM FAIL.                                       RL2034.2
045300     PERFORM  PRINT-DETAIL.                                       RL2034.2
045400     ADD      1 TO REC-CT.                                        RL2034.2
045500*    .03                                                          RL2034.2
045600     IF      WRK-CS-09V00-009 EQUAL TO ZERO                       RL2034.2
045700             PERFORM PASS                                         RL2034.2
045800             ELSE                                                 RL2034.2
045900             MOVE WRK-CS-09V00-009 TO COMPUTED-18V0               RL2034.2
046000             MOVE  ZERO            TO CORRECT-18V0                RL2034.2
046100             MOVE "BAD-UPDATES" TO RE-MARK                        RL2034.2
046200             PERFORM FAIL.                                        RL2034.2
046300     PERFORM PRINT-DETAIL.                                        RL2034.2
046400     ADD     01 TO REC-CT.                                        RL2034.2
046500*    .04                                                          RL2034.2
046600     IF      WRK-CS-09V00-010 EQUAL TO ZERO                       RL2034.2
046700             PERFORM PASS                                         RL2034.2
046800             ELSE                                                 RL2034.2
046900             MOVE WRK-CS-09V00-010 TO COMPUTED-18V0               RL2034.2
047000             MOVE ZERO             TO CORRECT-18V0                RL2034.2
047100             MOVE "KEY VS RECORD" TO RE-MARK                      RL2034.2
047200             PERFORM FAIL.                                        RL2034.2
047300     PERFORM PRINT-DETAIL.                                        RL2034.2
047400     ADD     01 TO REC-CT.                                        RL2034.2
047500*    .05                                                          RL2034.2
047600     MOVE    WRK-CS-09V00-011 TO RL-FD1-KEY.                      RL2034.2
047700     MOVE RL-FD1-KEY TO COMPUTED-18V0.                            RL2034.2
047800     MOVE    "INFORMATION" TO CORRECT-A.                          RL2034.2
047900     MOVE    "STATUS AFTER OPEN" TO RE-MARK.                      RL2034.2
048000     PERFORM PRINT-DETAIL.                                        RL2034.2
048100     ADD     01 TO REC-CT.                                        RL2034.2
048200*    .06                                                          RL2034.2
048300     CLOSE    RL-FD1.                                             RL2034.2
048400 REL-INIT-007.                                                    RL2034.2
048500     MOVE     "REL-TEST-007" TO PAR-NAME                          RL2034.2
048600     OPEN     I-O RL-FD1.                                         RL2034.2
048700     MOVE     ZERO TO WRK-CS-09V00-006                            RL2034.2
048800     MOVE     ZERO TO WRK-CS-09V00-007                            RL2034.2
048900     MOVE     ZERO TO WRK-CS-09V00-008                            RL2034.2
049000     MOVE     ZERO TO WRK-CS-09V00-009                            RL2034.2
049100     MOVE     ZERO TO WRK-CS-09V00-010                            RL2034.2
049200     MOVE     ZERO TO WRK-CS-09V00-011                            RL2034.2
049300     MOVE     01 TO REC-CT.                                       RL2034.2
049400     MOVE     SPACE TO  FILE-RECORD-INFO-P1-120 (1).              RL2034.2
049500     MOVE    "DELETE"  TO FEATURE.                                RL2034.2
049600 REL-TEST-007-R.                                                  RL2034.2
049700     ADD      1 TO WRK-CS-09V00-006                               RL2034.2
049800     ADD      1 TO WRK-CS-09V00-007.                              RL2034.2
049900     READ     RL-FD1  NEXT RECORD                                 RL2034.2
050000              AT END                                              RL2034.2
050100              MOVE "AT END PATH TAKEN " TO RE-MARK                RL2034.2
050200             GO TO  REL-TEST-007-3.                               RL2034.2
050300     MOVE     RL-FD1R1-F-G-120 TO FILE-RECORD-INFO-P1-120 (1).    RL2034.2
050400     IF       WRK-CS-09V00-007  EQUAL TO 4                        RL2034.2
050500              GO TO REL-TEST-007-2.                               RL2034.2
050600     IF       WRK-CS-09V00-006 GREATER 501                        RL2034.2
050700              MOVE  "AT END NOT TAKEN"  TO RE-MARK                RL2034.2
050800              GO TO REL-TEST-007-3.                               RL2034.2
050900     GO TO    REL-TEST-007-R.                                     RL2034.2
051000 REL-TEST-007-2.                                                  RL2034.2
051100     MOVE CCVS-PGM-ID       TO  XPROGRAM-NAME (1).                RL2034.2
051200     MOVE  99 TO UPDATE-NUMBER (1).                               RL2034.2
051300     MOVE     FILE-RECORD-INFO-P1-120 (1) TO RL-FD1R1-F-G-120.    RL2034.2
051400     DELETE    RL-FD1                                             RL2034.2
051500             INVALID KEY GO TO REL-TEST-007-3.                    RL2034.2
051600     MOVE     ZERO TO  WRK-CS-09V00-007.                          RL2034.2
051700     ADD      1 TO  WRK-CS-09V00-008                              RL2034.2
051800     GO TO    REL-TEST-007-R.                                     RL2034.2
051900 REL-TEST-007-3.                                                  RL2034.2
052000     IF       WRK-CS-09V00-006 NOT EQUAL TO 501                   RL2034.2
052100              MOVE WRK-CS-09V00-006 TO COMPUTED-18V0              RL2034.2
052200              MOVE              501 TO CORRECT-18V0               RL2034.2
052300              PERFORM FAIL                                        RL2034.2
052400              ELSE                                                RL2034.2
052500              PERFORM PASS.                                       RL2034.2
052600     PERFORM  PRINT-DETAIL.                                       RL2034.2
052700     ADD      01 TO REC-CT.                                       RL2034.2
052800*    .01                                                          RL2034.2
052900     IF       WRK-CS-09V00-008 NOT EQUAL TO 125                   RL2034.2
053000              MOVE WRK-CS-09V00-008 TO COMPUTED-18V0              RL2034.2
053100              MOVE 125              TO CORRECT-18V0               RL2034.2
053200              MOVE "DELETED RECORDS" TO RE-MARK                   RL2034.2
053300              PERFORM FAIL                                        RL2034.2
053400              ELSE                                                RL2034.2
053500              PERFORM PASS.                                       RL2034.2
053600     PERFORM  PRINT-DETAIL.                                       RL2034.2
053700     ADD      01 TO REC-CT.                                       RL2034.2
053800*    .02                                                          RL2034.2
053900     CLOSE    RL-FD1.                                             RL2034.2
054000 REL-INIT-008.                                                    RL2034.2
054100     MOVE     "REL-TEST-008" TO PAR-NAME.                         RL2034.2
054200     MOVE     ZERO TO   WRK-CS-09V00-006                          RL2034.2
054300     MOVE     ZERO TO   WRK-CS-09V00-007                          RL2034.2
054400     MOVE     ZERO TO   WRK-CS-09V00-008                          RL2034.2
054500     MOVE     ZERO TO   WRK-CS-09V00-009                          RL2034.2
054600     MOVE     ZERO TO   WRK-CS-09V00-010                          RL2034.2
054700     MOVE     ZERO TO   WRK-CS-09V00-011                          RL2034.2
054800     MOVE     01 TO REC-CT.                                       RL2034.2
054900     MOVE     SPACE  TO  FILE-RECORD-INFO-P1-120 (1).             RL2034.2
055000     MOVE     ZERO TO RL-FD1-KEY.                                 RL2034.2
055100     OPEN     INPUT  RL-FD1.                                      RL2034.2
055200     MOVE    "READ UPDATED FILE"  TO FEATURE.                     RL2034.2
055300 REL-TEST-008-R.                                                  RL2034.2
055400     ADD      1 TO WRK-CS-09V00-006.                              RL2034.2
055500     ADD      1 TO WRK-CS-09V00-007.                              RL2034.2
055600     ADD      1 TO WRK-CS-09V00-008.                              RL2034.2
055700     READ      RL-FD1 NEXT RECORD  AT END  GO TO REL-TEST-008-3.  RL2034.2
055800     MOVE     RL-FD1R1-F-G-120 TO FILE-RECORD-INFO-P1-120 (1).    RL2034.2
055900     IF       UPDATE-NUMBER (1) EQUAL TO 99                       RL2034.2
056000              ADD  1 TO WRK-CS-09V00-009.                         RL2034.2
056100     IF       WRK-CS-09V00-007  EQUAL TO 4                        RL2034.2
056200              MOVE 01 TO WRK-CS-09V00-007                         RL2034.2
056300              ADD 1 TO WRK-CS-09V00-008.                          RL2034.2
056400     IF       RL-FD1-KEY EQUAL TO  XRECORD-NUMBER (1)             RL2034.2
056500              ADD 1 TO  WRK-CS-09V00-010.                         RL2034.2
056600     IF       XRECORD-NUMBER (1) EQUAL TO  WRK-CS-09V00-008       RL2034.2
056700              ADD 1 TO  WRK-CS-09V00-011.                         RL2034.2
056800     IF       WRK-CS-09V00-006 GREATER  501                       RL2034.2
056900              GO TO REL-TEST-008-3.                               RL2034.2
057000     GO TO    REL-TEST-008-R.                                     RL2034.2
057100 REL-TEST-008-3.                                                  RL2034.2
057200     IF       WRK-CS-09V00-006 NOT EQUAL TO 376                   RL2034.2
057300              MOVE "INCORRECT RECORD COUNT"  TO RE-MARK           RL2034.2
057400              MOVE WRK-CS-09V00-006 TO COMPUTED-18V0              RL2034.2
057500              MOVE 376 TO CORRECT-18V0                            RL2034.2
057600              PERFORM  FAIL                                       RL2034.2
057700              ELSE                                                RL2034.2
057800              PERFORM  PASS.                                      RL2034.2
057900     PERFORM  PRINT-DETAIL.                                       RL2034.2
058000     ADD      01 TO REC-CT.                                       RL2034.2
058100*    .01                                                          RL2034.2
058200     IF       WRK-CS-09V00-009 NOT EQUAL TO ZERO                  RL2034.2
058300              MOVE WRK-CS-09V00-009 TO COMPUTED-18V0              RL2034.2
058400             MOVE   ZERO TO CORRECT-18V0                          RL2034.2
058500              MOVE "DELETED RECORDS" TO RE-MARK                   RL2034.2
058600              PERFORM FAIL                                        RL2034.2
058700              ELSE                                                RL2034.2
058800              PERFORM PASS.                                       RL2034.2
058900     PERFORM  PRINT-DETAIL.                                       RL2034.2
059000     ADD      01  TO  REC-CT.                                     RL2034.2
059100*    .02                                                          RL2034.2
059200     IF       WRK-CS-09V00-010 NOT EQUAL TO 375                   RL2034.2
059300              MOVE "KEY MISMATCH" TO RE-MARK                      RL2034.2
059400             MOVE 375 TO CORRECT-18V0                             RL2034.2
059500              MOVE WRK-CS-09V00-010 TO COMPUTED-18V0              RL2034.2
059600              PERFORM FAIL                                        RL2034.2
059700              ELSE                                                RL2034.2
059800              PERFORM PASS.                                       RL2034.2
059900     PERFORM  PRINT-DETAIL.                                       RL2034.2
060000     ADD      01 TO REC-CT.                                       RL2034.2
060100*    .03                                                          RL2034.2
060200     IF      WRK-CS-09V00-011  NOT EQUAL TO 375                   RL2034.2
060300             MOVE   375  TO CORRECT-18V0                          RL2034.2
060400             MOVE  "INCORRECT RECORD FOUND"  TO RE-MARK           RL2034.2
060500             MOVE   WRK-CS-09V00-011 TO COMPUTED-18V0             RL2034.2
060600             PERFORM   FAIL                                       RL2034.2
060700             ELSE                                                 RL2034.2
060800             PERFORM  PASS.                                       RL2034.2
060900     PERFORM   PRINT-DETAIL.                                      RL2034.2
061000*04                                                               RL2034.2
061100     CLOSE    RL-FD1.                                             RL2034.2
061200 CCVS-EXIT SECTION.                                               RL2034.2
061300 CCVS-999999.                                                     RL2034.2
061400     GO TO CLOSE-FILES.                                           RL2034.2
