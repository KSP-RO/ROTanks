import os, argparse, sys, json, glob, re

class DefaultHelpParser(argparse.ArgumentParser):
    def error(self, message):
        sys.stderr.write('error: %s\n' % message)
        self.print_help()
        sys.exit(2)

HELP_DESC = "Creates neccesary metadata files"
parser = DefaultHelpParser(description=HELP_DESC)
parser.add_argument('tag', metavar='tag', type=str, nargs=1,
                   help='tag of release (e.g. v0.4.6.0')

args = parser.parse_args()

if not args.tag or len(args.tag) < 1:
    print("ERROR: git tag must be specified and must be in the format major.minor.patch.build-configuration.e.g. 0.4.6.0")
    sys.exit(2)

version = args.tag[0]

if version.startswith('v'):
    version = version.split('v')[1]

major = int(version.split(".")[0])
minor = int(version.split(".")[1])
patch = int(version.split(".")[2])
build = int(version.split(".")[3])
if len(version) == 4:
	build = int(version[3])
# create AVC .version file
avc = {
    "NAME"     : "ROTanks",
    "URL"      : "https://raw.githubusercontent.com/KSP-RO/ROTanks/master/GameData/ROTanks/ROTanks.version",
    "DOWNLOAD" : "https://github.com/KSP-RO/ROTanks/releases",
    "HOMEPAGE" : "https://github.com/KSP-RO/ROTanks/",
	"GITHUB":
	{
		"USERNAME":"KSP-RO",
		"REPOSITORY":"ROTanks",
		"ALLOW_PRE_RELEASE": False
	},
	"VERSION" :
	{
		"MAJOR" : major,
		"MINOR" : minor,
		"PATCH" : patch,
		"BUILD" : build
	},
	"KSP_VERSION" :
	{
		"MAJOR" : 1,
		"MINOR" : 12,
		"PATCH" : 3
	},
	"KSP_VERSION_MIN":
	{
		"MAJOR": "1",
		"MINOR": "12",
		"PATCH": "0"
	},
	"KSP_VERSION_MAX":
	{
		"MAJOR": "1",
		"MINOR": "12",
		"PATCH": "99"
	}
}

with open("ROTanks.version", "w") as f:
	f.write(json.dumps(avc, indent=4))
