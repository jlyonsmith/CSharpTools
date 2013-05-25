mcs GenSpacerTestFiles.cs
mono GenSpacerTestFiles.exe
SPACER="../bin/Debug/Spacer.exe"
mono $SPACER -h
hexdump test.txt
mono $SPACER test.txt -m:t -o:test_tabs.txt
hexdump test_tabs.txt
mono $SPACER test.txt -m:s -o:test_spaces.txt
hexdump test_spaces.txt
# rm GenSpacerTestFiles.exe
# rm *.txt
