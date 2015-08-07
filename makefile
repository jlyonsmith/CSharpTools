CONFIG?=Release
PREFIX?=prefix
PREFIX:=$(abspath $(PREFIX))
VERSION=3.0.10807
PROJECT=CSharpTools
SCRATCH=scratch
tools=Doozer Ender Lindex Spacer Strapper Vamper Cleaner Projector Popper
otherfiles=makefile template.sh csharptools
markdown=README.md LICENSE.md
lc=$(shell echo $(1) | tr A-Z a-z)
zipfile=$(PROJECT)-$(VERSION).tar.gz

define copyrule
$(1): $(2)
	cp $$< $$@

endef

define mkdirrule
$(1):
	mkdir -p $$@

endef

define mkscriptrule
$$(PREFIX)/bin/$(call lc,$(1)): $$(PREFIX)/lib/$(1) \
								$$(PREFIX)/lib/$(1)/$(1).exe \
								$$(PREFIX)/lib/$(1)/ToolBelt.dll
	sed -e 's,_TOOL_,$(1),g' -e 's,_PREFIX_,$$(PREFIX),g' template.sh > $$@
	chmod u+x $$@

$$(PREFIX)/lib/$(1):
	mkdir -p $$@

$$(PREFIX)/lib/$(1)/$(1).exe: $(1)/$(1).exe
	cp $$< $$@

$$(PREFIX)/lib/$(1)/ToolBelt.dll: $(1)/ToolBelt.dll
	cp $$< $$@

endef

.PHONY: default
default:
	$(error Specify clean, dist or install)

.PHONY: dist
dist: $(SCRATCH) $(foreach X,$(tools),$(SCRATCH)/$(X)) $(zipfile)

$(SCRATCH):
	mkdir -p $(SCRATCH)

$(foreach X,$(tools),$(eval $(call mkdirrule,$(SCRATCH)/$(X))))

$(zipfile): $(foreach X,$(tools),$(SCRATCH)/$(X)/$(X).exe) \
			$(foreach X,$(tools),$(SCRATCH)/$(X)/ToolBelt.dll) \
			$(foreach X,$(otherfiles),$(SCRATCH)/$(X)) \
			$(foreach X,$(markdown),$(SCRATCH)/$(X))
	tar -cvz -C $(SCRATCH) -f $(zipfile) ./
	openssl sha1 $(zipfile)
	@echo "aws s3 cp $(zipfile) s3://jlyonsmith/ --profile jamoki --acl public-read"

$(foreach X,$(tools),$(eval $(call copyrule,$(SCRATCH)/$(X)/$(X).exe,$(X)/bin/$(CONFIG)/$(X).exe)))
$(foreach X,$(tools),$(eval $(call copyrule,$(SCRATCH)/$(X)/ToolBelt.dll,$(X)/bin/$(CONFIG)/ToolBelt.dll)))
$(foreach X,$(otherfiles),$(eval $(call copyrule,$(SCRATCH)/$(X),$(X))))
$(foreach X,$(markdown),$(eval $(call copyrule,$(SCRATCH)/$(X),$(X))))

# NOTE: Test this by going to scratch dir and running there!

.PHONY: install
install: $(PREFIX)/bin \
		 $(PREFIX)/lib \
		 $(PREFIX)/bin/csharptools \
		 $(foreach X,$(tools),$(PREFIX)/bin/$(call lc, $(X)))
ifdef HOMEBREW
install: $(foreach X,$(markdown),$(PREFIX)/$(X))
endif

$(PREFIX)/bin $(PREFIX)/lib:
	mkdir -p $@

$(foreach X,$(tools),$(eval $(call mkscriptrule,$(X))))

$(PREFIX)/bin/csharptools: csharptools
	cp csharptools $@
	chmod u+x $@

ifdef HOMEBREW
$(foreach X,$(markdown),$(eval $(call copyrule,$(PREFIX)/$(X),$(X))))
endif

.PHONY: clean
clean:
	-@rm *.gz
	-@rm -rf $(SCRATCH)
