#! /bin/bash

SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )
ROOT_DIR=${SCRIPT_DIR}/../..

${ROOT_DIR}/publish/kiota generate --language Java \
  --openapi ${ROOT_DIR}/tests/Kiota.Builder.IntegrationTests/InheritingErrors.yaml \
  --output ${ROOT_DIR}/it/java/src

${ROOT_DIR}/publish/kiota generate --language Java \
  --openapi ${ROOT_DIR}/tests/Kiota.Builder.IntegrationTests/NoUnderscoresInModel.yaml \
  --output ${ROOT_DIR}/it/java/src \
  --namespace-name "no.underscores"
