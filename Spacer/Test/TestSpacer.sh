mcs GenSpacerTestFiles.cs
mono GenSpacerTestFiles.exe
SPACER="../Spacer.exe"
mono $SPACER -?
echo -----------------
echo Test.cs:
hexdump Test.cs
echo -----------------
mono $SPACER Test.cs -m:t -o:TestTabs.cs
mono $SPACER TestTabs.cs
echo TestTabs.cs:
hexdump TestTabs.cs
echo -----------------
mono $SPACER Test.cs -m:t -o:TestRoundedTabs.cs -r
mono $SPACER TestRoundedTabs.cs
echo TestRoundedTabs.cs:
hexdump TestRoundedTabs.cs
echo -----------------
mono $SPACER Test.cs -m:s -o:TestSpaces.cs
mono $SPACER TestSpaces.cs
echo TestSpaces.cs:
hexdump TestSpaces.cs
echo -----------------
echo Test.tson:
hexdump Test.tson
echo -----------------
mono $SPACER Test.tson -m:t -o:TestTabs.tson
mono $SPACER TestTabs.tson
echo TestTabs.tson:
hexdump TestTabs.tson
echo -----------------
mono $SPACER Test.tson -m:t -o:TestRoundedTabs.tson -r -t:2
mono $SPACER TestRoundedTabs.tson
echo TestRoundedTabs.tson:
hexdump TestRoundedTabs.tson
echo -----------------
mono $SPACER Test.tson -m:s -o:TestSpaces.tson
mono $SPACER TestSpaces.tson
echo TestSpaces.tson:
hexdump TestSpaces.tson
echo -----------------
# rm GenSpacerTestFiles.exe
# rm *.txt
