#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
VERSION_FILE="$ROOT_DIR/VERSION"
PUBLISH_DIR="$ROOT_DIR/publish"

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

usage() {
    echo "Usage: ./publish.sh (--bump-patch|--bump-minor|--bump-major)"
    echo
    echo "Publishing always requires exactly one semantic-version bump:"
    echo "  --bump-patch  0.2.0 -> 0.2.1"
    echo "  --bump-minor  0.2.0 -> 0.3.0"
    echo "  --bump-major  0.2.0 -> 1.0.0"
}

fail() {
    echo -e "${RED}Error: $1${NC}" >&2
    usage >&2
    exit 2
}

create_default_appsettings() {
    cat > "$PUBLISH_DIR/appsettings.json" << 'EOF'
{
  "OpenAi": {
    "ApiKey": "your-openai-api-key-here",
    "ModelName": "gpt-5.6-terra",
    "SmartModelName": "gpt-5.6-terra"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  }
}
EOF
}

for argument in "$@"; do
    if [[ "$argument" == "--help" || "$argument" == "-h" ]]; then
        usage
        exit 0
    fi
done

bump_kind=""
for argument in "$@"; do
    case "$argument" in
        --bump-patch|--bump-minor|--bump-major)
            if [[ -n "$bump_kind" ]]; then
                fail "Pass exactly one version bump flag."
            fi
            bump_kind="${argument#--bump-}"
            ;;
        *)
            fail "Unknown argument: $argument"
            ;;
    esac
done

if [[ -z "$bump_kind" ]]; then
    fail "A version bump flag is required."
fi

if [[ ! -f "$VERSION_FILE" ]]; then
    fail "VERSION file not found at $VERSION_FILE"
fi

current_version="$(tr -d '[:space:]' < "$VERSION_FILE")"
if [[ ! "$current_version" =~ ^([0-9]+)\.([0-9]+)\.([0-9]+)$ ]]; then
    fail "VERSION must contain a semantic version such as 0.2.0."
fi

major=$((10#${BASH_REMATCH[1]}))
minor=$((10#${BASH_REMATCH[2]}))
patch=$((10#${BASH_REMATCH[3]}))

case "$bump_kind" in
    patch)
        patch=$((patch + 1))
        ;;
    minor)
        minor=$((minor + 1))
        patch=0
        ;;
    major)
        major=$((major + 1))
        minor=0
        patch=0
        ;;
esac

new_version="$major.$minor.$patch"
echo -e "${GREEN}Preparing Explain $new_version ($bump_kind bump from $current_version)...${NC}"

cd "$ROOT_DIR"

if [[ -d "$PUBLISH_DIR" ]]; then
    echo -e "${YELLOW}Cleaning existing publish directory...${NC}"
    rm -rf -- "$PUBLISH_DIR"
fi

echo -e "${GREEN}Creating publish directory...${NC}"
mkdir -p "$PUBLISH_DIR"

echo -e "${GREEN}Publishing Explain as a self-contained single-file application...${NC}"
dotnet publish src/Explain.Cli/Explain.Cli.csproj \
    -c Release \
    -r linux-x64 \
    -p:PublishSingleFile=true \
    -p:AssemblyName=explain \
    -p:Version="$new_version" \
    -p:AssemblyVersion="$new_version.0" \
    -p:FileVersion="$new_version.0" \
    -p:InformationalVersion="$new_version" \
    --self-contained \
    -o "$PUBLISH_DIR"

if [[ ! -f "$PUBLISH_DIR/explain" ]]; then
    echo -e "${RED}Error: Publishing failed - explain executable not found in publish directory${NC}"
    exit 1
fi

chmod +x "$PUBLISH_DIR/explain"

if [[ ! -f "$PUBLISH_DIR/appsettings.json" ]]; then
    if [[ -f "$ROOT_DIR/src/Explain.Cli/appsettings.json" ]]; then
        echo -e "${YELLOW}Found appsettings.json in source directory.${NC}"
        choice=""
        read -r -p "Would you like to use these settings? (y/n): " choice || true
        case "$choice" in
            y|Y)
                echo -e "${GREEN}Copying your existing settings file...${NC}"
                cp "$ROOT_DIR/src/Explain.Cli/appsettings.json" "$PUBLISH_DIR/appsettings.json"
                ;;
            *)
                echo -e "${YELLOW}Using template settings instead...${NC}"
                cp "$ROOT_DIR/src/Explain.Cli/appsettings.example.json" "$PUBLISH_DIR/appsettings.json"
                echo -e "${YELLOW}Please update it with a valid API key before using application-directory configuration.${NC}"
                ;;
        esac
    elif [[ -f "$ROOT_DIR/src/Explain.Cli/appsettings.example.json" ]]; then
        echo -e "${YELLOW}No appsettings.json found in source directory. Creating one from example...${NC}"
        cp "$ROOT_DIR/src/Explain.Cli/appsettings.example.json" "$PUBLISH_DIR/appsettings.json"
    else
        echo -e "${YELLOW}No settings example found. Creating default settings...${NC}"
        create_default_appsettings
    fi
fi

version_temp="$(mktemp "$ROOT_DIR/.VERSION.tmp.XXXXXX")"
printf '%s\n' "$new_version" > "$version_temp"
mv -- "$version_temp" "$VERSION_FILE"

echo -e "${GREEN}=== Publish Complete: Explain $new_version ===${NC}"
echo -e "The executable is available at ${YELLOW}$PUBLISH_DIR/explain${NC}."
echo
echo "Usage examples:"
echo -e "  ${YELLOW}explain \"Your question here\"${NC}"
echo -e "  ${YELLOW}explain --version${NC}"
echo -e "  ${YELLOW}cat file.txt | explain \"What does this code do?\"${NC}"
echo

if grep -q "your-openai-api-key-here" "$PUBLISH_DIR/appsettings.json" 2>/dev/null; then
    echo -e "${RED}Reminder: configure an OpenAI API key before using application-directory configuration.${NC}"
fi
