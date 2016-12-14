#
# Test.ps1
#
Login-AzureRmAccount
$scriptPath = Split-Path -parent $PSCommandPath
# Create Web and Storage
#."$scriptPath\create-azure-webapp.ps1" -Name WebSiteName -ResourceGroupName ResourceGroupName -AppServicePlanName AppServicePlanName

# Publish code to Web
#."$scriptPath\deploy-azure-website-devbox.ps1" -ProjectFile [ProjectPath]\IoTFaceDetectionBackendDNX5\src\IoTFaceDetectionBackendDNX5\IoTFaceDetectionBackendDNX5.xproj
