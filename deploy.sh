#!/bin/bash

set -e

RELEASE="webzcan-release"
NAMESPACE="webzcan"

export KUBECONFIG=/etc/rancher/k3s/k3s.yaml

if [ $# -eq 1 ]; then
  NAMESPACE="$1"
fi

DOMAIN="webzcan.local"

echo "Starting MyApp deployment in namespace: $NAMESPACE..."

echo "Creating namespace..."
kubectl create namespace $NAMESPACE 2>/dev/null || echo "Namespace already exists"

echo "Building Docker images..."

echo "Building frontend..."
docker build -f .docker/prod/prodfrontend.Dockerfile -t myapp/frontend:latest .
docker build -f .docker/prod/prodbackend.Dockerfile -t myapp/backend:latest .

echo "Importing images to k3s..."
docker save myapp/frontend:latest > frontend.tar
docker save myapp/backend:latest > backend.tar
sudo k3s ctr images import frontend.tar
sudo k3s ctr images import backend.tar
rm frontend.tar backend.tar

echo "Deploying with Helm..."
helm upgrade $RELEASE ./.helm/dev \
    --install \
    --namespace "$NAMESPACE" \
    --create-namespace \
    --set global.domain="$DOMAIN"

echo "Waiting for pods to be ready..."
# Fixed: Use correct app labels
kubectl wait --for=condition=ready pod -l app=webzcan-frontend -n $NAMESPACE --timeout=600s
kubectl wait --for=condition=ready pod -l app=webzcan-backend -n $NAMESPACE --timeout=700s
kubectl wait --for=condition=ready pod -l app=webzcan-zap -n $NAMESPACE --timeout=300s

echo "Configuring $DOMAIN domain in /etc/hosts..."

# Get the correct node IP
NODE_IP=$(kubectl get node -o jsonpath='{.items[0].status.addresses[0].address}' 2>/dev/null || echo "127.0.0.1")
echo "Using Node IP: $NODE_IP"

# Remove existing entries first
sudo sed -i "/.*webzcan\.local/d" /etc/hosts

# Add new entries
echo "$NODE_IP webzcan.local" | sudo tee -a /etc/hosts
echo "$NODE_IP www.webzcan.local" | sudo tee -a /etc/hosts

echo "Domain configuration completed"
echo "Added to /etc/hosts:"
grep "webzcan.local" /etc/hosts

echo "Deployment complete!"
echo "Access app at:"
echo "  Main App: http://$DOMAIN/"
echo "  With www: http://www.$DOMAIN/"
echo ""

echo "Checking deployment status..."
kubectl get pods -n $NAMESPACE
kubectl get svc -n $NAMESPACE  
kubectl get ingress -n $NAMESPACE

echo ""
echo "To remove everything, run: ./rollback.sh $NAMESPACE"