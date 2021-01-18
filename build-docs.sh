#!/bin/bash

CONFIGURATION=Release
FRAMEWORK=netstandard2.0

dotnet build -c $CONFIGURATION -f $FRAMEWORK ./src/Hedgehog

dotnet fsdocs build \
    --input ./doc \
    --property Configuration=$CONFIGURATION,TargetFramework=$FRAMEWORK
