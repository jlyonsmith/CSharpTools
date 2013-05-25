mcs GenEnderTestFiles.cs
mono GenEnderTestFiles.exe
ENDER="../bin/Debug/Ender.exe"
mono $ENDER -h
mono $ENDER cr.txt
mono $ENDER lf.txt
mono $ENDER crlf.txt
mono $ENDER mixed1.txt
mono $ENDER mixed2.txt
mono $ENDER mixed3.txt
mono $ENDER mixed4.txt
mono $ENDER -f:lf -o:cr2lf.txt cr.txt
mono $ENDER -f:cr -o:lf2cr.txt lf.txt
mono $ENDER -f:lf -o:crlf2lf.txt crlf.txt
mono $ENDER -f:cr -o:crlf2cr.txt crlf.txt
mono $ENDER -f:auto mixed1.txt
mono $ENDER -f:auto mixed2.txt
mono $ENDER -f:auto mixed3.txt
mono $ENDER -f:auto mixed4.txt
#rm GenEnderTestFiles.exe
#rm *.txt