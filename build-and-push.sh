#!/bin/bash
set -e

REGISTRY="localhost:5000"
SERVICES=(
  "PaymentGateway.Identity.Api:paygate-identity-api"
  "PaymentGateway.Billing.Api:paygate-billing-api"
  "PaymentGateway.Gateway.Api:paygate-gateway-api"
  "PaymentGateway.Backoffice.Api:paygate-backoffice-api"
)

cd /home/ubuntu/paygate

for entry in "${SERVICES[@]}"; do
  SERVICE_NAME="${entry%%:*}"
  IMAGE_NAME="${entry##*:}"
  DLL_NAME="${SERVICE_NAME}.dll"

  echo "========================================="
  echo "Building ${IMAGE_NAME} (${SERVICE_NAME})"
  echo "========================================="

  docker build \
    --build-arg SERVICE_NAME="${SERVICE_NAME}" \
    --build-arg SERVICE_PATH="src/services/${SERVICE_NAME}" \
    -f Dockerfile \
    -t "${REGISTRY}/${IMAGE_NAME}:latest" \
    .

  # Fix the ENTRYPOINT CMD to use the correct DLL name
  # We'll create a tagged version with the correct CMD
  echo "Pushing ${REGISTRY}/${IMAGE_NAME}:latest"
  docker push "${REGISTRY}/${IMAGE_NAME}:latest"

  echo "Done: ${IMAGE_NAME}"
  echo ""
done

echo "All images built and pushed successfully!"
