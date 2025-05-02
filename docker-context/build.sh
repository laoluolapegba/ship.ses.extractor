#!/bin/bash

# Navigate into build context (if needed)
cd "$(dirname "$0")"

# Step 1: Get current Git commit hash
COMMIT_HASH=$(git rev-parse --short HEAD)

echo "🔧 Using commit hash: $COMMIT_HASH"

# Step 2: Replace commitHash in appsettings.Production.json
sed -i "s/\"commitHash\": \".*\"/\"commitHash\": \"$COMMIT_HASH\"/" appsettings.Production.json

# Step 3: Build Docker image
docker build -t ship-extractor:uat .

echo "✅ Docker image built: ship-extractor:uat"
