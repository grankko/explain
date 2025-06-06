#!/bin/bash

set -e

# Color outputs
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Clear out existing publish folder if it exists
if [ -d "publish" ]; then
    echo -e "${YELLOW}Cleaning existing publish directory...${NC}"
    rm -rf publish
fi

echo -e "${GREEN}Creating publish directory...${NC}"
mkdir -p publish

# Publish the application
echo -e "${GREEN}Publishing Explain as a self-contained single-file application...${NC}"

dotnet publish src/Explain.Cli/Explain.Cli.csproj \
    -c Release \
    -r linux-x64 \
    -p:PublishSingleFile=true \
    -p:AssemblyName=explain \
    --self-contained \
    -o publish

# Check if publish was successful
if [ ! -f "publish/explain" ]; then
    echo -e "${RED}Error: Publishing failed - explain executable not found in publish directory${NC}"
    exit 1
fi

# Make the published file executable
chmod +x publish/explain

# Check if appsettings.json exists in the publish directory
if [ ! -f "publish/appsettings.json" ]; then
    # Check if source appsettings.json exists
    if [ -f "src/Explain.Cli/appsettings.json" ]; then
        echo -e "${YELLOW}Found appsettings.json in source directory.${NC}"
        read -p "Would you like to use these settings? (y/n): " choice
        case "$choice" in 
          y|Y ) 
            echo -e "${GREEN}Copying your existing settings file...${NC}"
            cp src/Explain.Cli/appsettings.json publish/appsettings.json
            echo -e "${GREEN}Settings copied successfully.${NC}"
            ;;
          * ) 
            echo -e "${YELLOW}Using template settings instead...${NC}"
            cp src/Explain.Cli/appsettings.example.json publish/appsettings.json
            echo -e "${YELLOW}Created appsettings.json from template.${NC}"
            echo -e "${YELLOW}Please update it with valid API key and settings for the application to function properly.${NC}"
            ;;
        esac
    else
        echo -e "${YELLOW}No appsettings.json found in source directory. Creating one from example...${NC}"
        if [ -f "src/Explain.Cli/appsettings.example.json" ]; then
            cp src/Explain.Cli/appsettings.example.json publish/appsettings.json
            echo -e "${YELLOW}Created appsettings.json from template.${NC}"
        else
            echo -e "${YELLOW}No example found, creating default appsettings.json...${NC}"
            create_default_appsettings
        fi
        echo -e "${RED}⚠️  IMPORTANT: Please update appsettings.json with your OpenAI API key before running!${NC}"
    fi
else
    echo -e "${GREEN}Existing appsettings.json found in publish directory, keeping it intact.${NC}"
fi

# Function to create default appsettings.json if example doesn't exist
create_default_appsettings() {
    cat > publish/appsettings.json << 'EOF'
{
  "OpenAi": {
    "ApiKey": "your-openai-api-key-here",
    "ModelName": "gpt-4",
    "SmartModelName": "o1-mini"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  }
}
EOF
}

echo -e "${GREEN}=== Build Complete ===${NC}"
echo -e "The Explain application has been published to the ${YELLOW}./publish${NC} directory."
echo -e ""
echo -e "Usage examples:"
echo -e "  ${YELLOW}./publish/explain \"Your question here\"${NC}"
echo -e "  ${YELLOW}./publish/explain \"Your question here\" --verbose${NC}"
echo -e "  ${YELLOW}./publish/explain \"Your question here\" --think${NC}"
echo -e "  ${YELLOW}cat file.txt | ./publish/explain${NC}"
echo -e "  ${YELLOW}cat file.txt | ./publish/explain \"What does this code do?\" --verbose${NC}"
echo -e ""
if [ ! -f "publish/appsettings.json" ] || grep -q "your-openai-api-key-here" publish/appsettings.json 2>/dev/null; then
    echo -e "${RED}⚠️  REMINDER: Update ${YELLOW}./publish/appsettings.json${RED} with your OpenAI API key!${NC}"
fi
