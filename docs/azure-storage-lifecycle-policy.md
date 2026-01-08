# Azure Storage Lifecycle Policy Configuration

This document describes the required Azure Storage lifecycle policy for automatic cleanup of temporary preview images.

## Overview

The partner advertisement system uploads preview images to the `partner-ads` container under the `preview/` folder prefix. These images are temporary and should be automatically deleted after 90 days to prevent unnecessary storage costs.

## Required Lifecycle Policy

Configure the following lifecycle management rule in Azure Storage for the `partner-ads` container:

### Rule Configuration

- **Rule name**: `delete-preview-images`
- **Rule type**: Delete blobs
- **Blob type**: Block blobs
- **Blob prefix**: `preview/`
- **Days after last modification**: 90

### Azure Portal Configuration

1. Navigate to your Azure Storage Account
2. Go to **Data management** > **Lifecycle management**
3. Click **Add rule**
4. Configure the rule with the following settings:
   - Rule name: `delete-preview-images`
   - Rule scope: Limit blobs with filters
   - Blob type: Block blobs
   - Blob subtype: Base blobs
   - **Filters**:
     - Blob prefix: `preview/`
   - **Actions**:
     - Delete blob: 90 days after last modification

### Azure CLI Configuration

```bash
# Create lifecycle policy JSON
cat > lifecycle-policy.json << 'EOF'
{
  "rules": [
    {
      "enabled": true,
      "name": "delete-preview-images",
      "type": "Lifecycle",
      "definition": {
        "actions": {
          "baseBlob": {
            "delete": {
              "daysAfterModificationGreaterThan": 90
            }
          }
        },
        "filters": {
          "blobTypes": ["blockBlob"],
          "prefixMatch": ["partner-ads/preview/"]
        }
      }
    }
  ]
}
EOF

# Apply the policy
az storage account management-policy create \
  --account-name <your-storage-account-name> \
  --policy @lifecycle-policy.json \
  --resource-group <your-resource-group-name>
```

### Terraform Configuration

```hcl
resource "azurerm_storage_management_policy" "example" {
  storage_account_id = azurerm_storage_account.example.id

  rule {
    name    = "delete-preview-images"
    enabled = true
    filters {
      prefix_match = ["partner-ads/preview/"]
      blob_types   = ["blockBlob"]
    }
    actions {
      base_blob {
        delete_after_days_since_modification_greater_than = 90
      }
    }
  }
}
```

## Implementation Details

### Code Changes

As of the latest implementation, the application no longer manages temporary file expiration in code. Instead:

- Preview images are uploaded using the standard `UploadImageAsync` method
- Images are stored under the `preview/{partnerId}/` path in the `partner-ads` container
- Azure Storage lifecycle policy handles automatic deletion after 90 days
- No metadata is required on the blobs themselves

See:
- [ImageService.cs](../src/web/Jordnaer/Features/Images/ImageService.cs) - Image upload service
- [PartnerService.cs](../src/web/Jordnaer/Features/Partners/PartnerService.cs) - Partner preview image upload (line 228)

## Verification

To verify the lifecycle policy is working:

```bash
# List lifecycle policies
az storage account management-policy show \
  --account-name <your-storage-account-name> \
  --resource-group <your-resource-group-name>

# Check blob last modified dates in preview folder
az storage blob list \
  --account-name <your-storage-account-name> \
  --container-name partner-ads \
  --prefix preview/ \
  --query "[].{name:name, lastModified:properties.lastModified}"
```

## Notes

- The lifecycle policy runs once per day, so deletion is not immediate
- Blobs are marked for deletion and removed in the next policy run cycle
- This approach is more cost-effective and scalable than managing expiration in application code
- No manual cleanup or background jobs are required
