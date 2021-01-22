#!/bin/bash -ex

VERSION=$1

if [ -z "$VERSION" ]; then
    echo "Expected a version as the first argument."
    exit 1
fi

# Build doc files, they appear in ./output.

CONFIGURATION=Release
FRAMEWORK=netstandard2.0
INPUT_DIR="./doc"
OUTPUT_DIR="./output"

dotnet build -c $CONFIGURATION -f $FRAMEWORK ./src/Hedgehog

dotnet fsdocs build \
    --input $INPUT_DIR \
    --property Configuration=$CONFIGURATION,TargetFramework=$FRAMEWORK \
    --strict

# Get the artifacts into the `gh-pages` branch.

REPO_BRANCH="gh-pages"
REPO_URL="git@github.com:adam-becker/fsharp-hedgehog.git"
TEMP_DIR="temp/$REPO_BRANCH"

# Remove temp directory.

rm -rf $TEMP_DIR
mkdir -p $TEMP_DIR

# Clone our repo's `gh-pages` branch into the temp directory.

pushd $TEMP_DIR

git clone -b $REPO_BRANCH $REPO_URL .

find . -maxdepth 1 ! -path '*.git*' ! -path . -exec rm -rf {} \;

popd

# Copy all artifacts into the temp directory, and commit.

cp -r $OUTPUT_DIR/* $TEMP_DIR

pushd $TEMP_DIR

git add .
git commit -m "Update generated documentation for version $VERSION"
git push # `gh-pages` should be a tracking branch for origin/gh-pages.

popd
