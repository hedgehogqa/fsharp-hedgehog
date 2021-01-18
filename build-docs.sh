#!/bin/bash

VERSION=$1

if [ -z "$VERSION" ]; then
    echo "Expected a version as the first argument."
    exit 1
fi

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
REPO_URL="git@github.com:hedgehogqa/fsharp-hedgehog.git"
DOCS_DIR="temp/$REPO_BRANCH"

# Remove temp directory.

rm -rf $DOCS_DIR
mkdir -p $DOCS_DIR

# Clone our repo's `gh-pages` branch into the temp directory.

git clone -b $REPO_BRANCH $REPO_URL $DOCS_DIR

# Remove everything but the .git folder.

pushd $DOCS_DIR

rm -rf !(.git)

popd

# Copy all artifacts into the temp directory, and commit.

cp -r $OUTPUT_DIR $DOCS_DIR

pushd $DOCS_DIR

git add .
git commit -m "Update generated documentation for version $VERSION"
git push # `gh-pages` should be a tracking branch for origin/gh-pages.

popd
