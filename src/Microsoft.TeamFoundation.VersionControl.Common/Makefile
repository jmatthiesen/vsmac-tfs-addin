thisdir = class/Microsoft.TeamFoundation.VersionControl.Common
include ../../build/rules.make
SUBDIRS =

LIBRARY = Microsoft.TeamFoundation.VersionControl.Common.dll

LIB_MCS_FLAGS = /r:$(corlib) /r:System.dll
TEST_MCS_FLAGS =  $(LIB_MCS_FLAGS) /r:Microsoft.TeamFoundation.Common.dll

include ../../build/library.make

