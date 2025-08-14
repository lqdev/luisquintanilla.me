---
title: "Deploy Owncast to Azure Container Apps with Persistent Storage"
language: "PowerShell"
tags: "owncast,fediverse,azure,livestream"
created_date: "2025-08-14 13:35 -05:00"
---

## Description 

This guide shows how to deploy Owncast to Azure Container Apps with persistent storage and scale-to-zero capability to minimize costs.

## Prerequisites

- Azure CLI installed and logged in
- An Azure subscription
- A resource group created

## Step 1: Create Required Resources

### Create Storage Account for Persistent Data

```bash
# Set variables (choose cheapest regions)
RESOURCE_GROUP="your-resource-group"
LOCATION="centralus"  # Often cheaper than eastus
STORAGE_ACCOUNT="owncaststorage$(date +%s)"  # Must be globally unique
CONTAINER_APP_ENV="owncast-env"
CONTAINER_APP_NAME="owncast-app"

# Create MINIMAL cost storage account
az storage account create \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS \
  --kind StorageV2 \
  --access-tier Cool \
  --allow-blob-public-access false \
  --https-only true \
  --min-tls-version TLS1_2

# Create file share with minimal provisioned size
az storage share create \
  --name "owncast-data" \
  --account-name $STORAGE_ACCOUNT \
  --quota 1  # Start with 1GB, scales automatically
```

### Create Container Apps Environment

```bash
# Create the Container Apps environment
az containerapp env create \
  --name $CONTAINER_APP_ENV \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION
```

## Step 2: Configure Storage Mount

Get the storage account key and create the storage mount:

```bash
# Get storage account key
STORAGE_KEY=$(az storage account keys list \
  --account-name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --query "[0].value" -o tsv)

# Create storage mount in the environment
az containerapp env storage set \
  --name $CONTAINER_APP_ENV \
  --resource-group $RESOURCE_GROUP \
  --storage-name "owncast-storage" \
  --azure-file-account-name $STORAGE_ACCOUNT \
  --azure-file-account-key $STORAGE_KEY \
  --azure-file-share-name "owncast-data" \
  --access-mode ReadWrite
```

## Step 3: Deploy Owncast Container App

Create the container app with persistent storage:

```bash
az containerapp create \
  --name $CONTAINER_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --environment $CONTAINER_APP_ENV \
  --image "owncast/owncast:latest" \
  --target-port 8080 \
  --ingress external \
  --min-replicas 0 \
  --max-replicas 1 \
  --cpu 0.5 \
  --memory 1Gi \
  --volume-mount "data:/app/data" \
  --volume-name "data" \
  --volume-storage-name "owncast-storage" \
  --volume-storage-type AzureFile
```

## Step 4: Configure Dual-Port Ingress (HTTP + RTMP)

For Owncast to work properly, you need both HTTP (8080) and RTMP (1935) ports. This requires a **Virtual Network (VNet)** integration:

### Create VNet and Subnet
```bash
# Create MINIMAL virtual network (smallest possible address space)
az network vnet create \
  --name "owncast-vnet" \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --address-prefix "10.0.0.0/24"  # Smaller than default /16

# Create minimal subnet
az network vnet subnet create \
  --name "container-apps-subnet" \
  --resource-group $RESOURCE_GROUP \
  --vnet-name "owncast-vnet" \
  --address-prefix "10.0.0.0/27"  # Only 32 IPs instead of /23 (512 IPs)
```

### Recreate Container Apps Environment with VNet
```bash
# Get subnet ID
SUBNET_ID=$(az network vnet subnet show \
  --name "container-apps-subnet" \
  --vnet-name "owncast-vnet" \
  --resource-group $RESOURCE_GROUP \
  --query id -o tsv)

# Delete existing environment and recreate with VNet
az containerapp env delete \
  --name $CONTAINER_APP_ENV \
  --resource-group $RESOURCE_GROUP \
  --yes

# Create Container Apps environment with workload profiles DISABLED (cheapest option)
az containerapp env create \
  --name $CONTAINER_APP_ENV \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --infrastructure-subnet-resource-id $SUBNET_ID \
  --enable-workload-profiles false  # Forces consumption-only pricing
```

### Deploy Container App with MINIMAL Resources
```bash
az containerapp create \
  --name $CONTAINER_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --environment $CONTAINER_APP_ENV \
  --image "owncast/owncast:latest" \
  --target-port 8080 \
  --exposed-port 1935 \
  --ingress external \
  --transport auto \
  --min-replicas 0 \
  --max-replicas 1 \
  --cpu 0.25 \
  --memory 0.5Gi \
  --volume-mount "data:/app/data" \
  --volume-name "data" \
  --volume-storage-name "owncast-storage" \
  --volume-storage-type AzureFile
```

**Cost-Optimized Resource Allocation:**
- **CPU**: 0.25 cores (minimum allowed, sufficient for small streams)
- **Memory**: 0.5Gi (minimum allowed, will work for basic streaming)
- **Scaling**: Aggressive scale-to-zero with max 1 replica

## Ultra Low-Cost Alternative YAML Configuration

For maximum cost optimization, use this YAML approach with the smallest possible resource allocation:

```yaml
# owncast-minimal-cost.yaml
properties:
  managedEnvironmentId: /subscriptions/{subscription-id}/resourceGroups/{resource-group}/providers/Microsoft.App/managedEnvironments/{environment-name}
  configuration:
    ingress:
      external: true
      targetPort: 8080
      additionalPortMappings:
      - external: true
        targetPort: 1935
        exposedPort: 1935
    secrets: []
  template:
    containers:
    - image: owncast/owncast:latest
      name: owncast
      resources:
        cpu: 0.25
        memory: 0.5Gi
      volumeMounts:
      - mountPath: /app/data
        volumeName: data
      env:
      - name: OWNCAST_RTMP_PORT
        value: "1935"
      - name: OWNCAST_WEBSERVER_PORT  
        value: "8080"
    scale:
      minReplicas: 0
      maxReplicas: 1
      rules:
      - name: "http-rule"
        http:
          metadata:
            concurrentRequests: "10"  # Scale up quickly but keep minimal
    volumes:
    - name: data
      storageType: AzureFile
      storageName: owncast-storage
```

Deploy with:
```bash
az containerapp create \
  --name $CONTAINER_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --yaml owncast-minimal-cost.yaml
```

## Cost Optimization Features

### Scale-to-Zero Configuration
- **Min Replicas**: Set to 0 to completely scale down when not in use
- **Max Replicas**: Set to 1 (Owncast doesn't need horizontal scaling)
- **Scale Rules**: Container Apps will automatically scale up when requests arrive

### Resource Limits (Ultra Cost-Optimized)
- **CPU**: 0.25 cores (absolute minimum, sufficient for 1-2 viewer streams)  
- **Memory**: 0.5Gi (minimum allowed by Azure Container Apps)
- **Storage**: Cool tier with 1GB initial quota (auto-scales as needed)
- **Network**: Minimal VNet addressing to reduce overhead

## OBS Configuration

After deployment, configure OBS for streaming:

1. **Server Settings**: Use `rtmp://your-app-url:1935/live` (note: `rtmp://` not `https://`)
2. **Stream Key**: Use the key from Owncast admin panel (Configuration > Server Setup > Stream Keys)
3. **Owncast Web Interface**: Access at `https://your-app-url` (port 8080 is handled automatically by ingress)

1. **Persistent Data**: All Owncast configuration, database, and uploaded files are stored in Azure Files and persist across container restarts and scale-to-zero events.

2. **Cold Start**: When scaling from zero, there will be a brief cold start delay as the container initializes.

3. **VNet Requirement**: For dual-port access (HTTP + RTMP), you **must** use a Virtual Network integration. This is a requirement for exposing additional TCP ports in Azure Container Apps.

4. **Security Configuration**: After deployment, immediately change the default admin credentials:
   - Navigate to `https://your-app-url/admin`
   - Default login: `admin` / `abc123`
   - Go to Configuration > Server Setup and change the admin password
   - Create/copy stream keys from Configuration > Server Setup > Stream Keys tab

4. **Custom Domain**: You can configure a custom domain using:
   ```bash
   az containerapp hostname add \
     --name $CONTAINER_APP_NAME \
     --resource-group $RESOURCE_GROUP \
     --hostname "your-domain.com"
   ```

5. **SSL Certificate**: Azure Container Apps provides automatic SSL certificates for custom domains.

## Monitoring and Troubleshooting

Check your deployment:
```bash
# Get the URL
az containerapp show \
  --name $CONTAINER_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query properties.configuration.ingress.fqdn

# View logs
az containerapp logs show \
  --name $CONTAINER_APP_NAME \
  --resource-group $RESOURCE_GROUP
```

## Cost Estimation (Ultra-Optimized)

With these optimizations, your monthly costs should be:

**When Streaming (4 hours/month example):**
- **Compute**: ~$0.50/month (0.25 CPU + 0.5Gi RAM × 4 hours)
- **Container Apps Environment**: ~$0.00 (consumption plan, no dedicated resources)
- **Networking**: ~$0.05/month (minimal VNet overhead)

**When Idle (Scale-to-Zero):**
- **Compute**: $0.00 (scaled to zero)
- **Environment**: $0.00 (consumption plan)

**Always-On Costs:**
- **Storage**: ~$0.05-0.10/month (1-2GB in Cool tier)
- **VNet**: ~$0.00 (no gateways or dedicated resources)

**Total Monthly Cost: ~$0.60-0.65/month** (assuming 4 hours of streaming)

### Performance Expectations at Minimal Resources:
- **0.25 CPU + 0.5Gi RAM**: Suitable for 480p-720p streams with 1-5 concurrent viewers
- **Scale-up Path**: Monitor performance and increase to 0.5 CPU + 1Gi if needed
- **Cold Start**: ~10-15 seconds when scaling from zero (acceptable for personal streaming)