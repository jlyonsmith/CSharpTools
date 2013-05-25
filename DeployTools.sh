function deploy {
	APPNAME=$1
	LAPPNAME=$(tr '[:upper:]' '[:lower:]' <<< ${APPNAME:0:1})${APPNAME:1}
	BINDIR=~/bin/$APPNAME.app
	if [ ! -d "$BINDIR" ]; then
	    mkdir $BINDIR
	fi
	cp $APPNAME/bin/Release/$APPNAME.exe $BINDIR
	cp RunTool.sh ~/bin/$LAPPNAME
	chmod u+x ~/bin/$LAPPNAME
}

deploy Ender
deploy Spacer
deploy Vamper
deploy Strapper
