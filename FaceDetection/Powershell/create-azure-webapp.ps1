# How to run the script
# create-azure-website-env.ps1 -Name yourwebsitename

# Define input parameters
Param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9]*$")]
    [String]$Name,                             # required    needs to be alphanumeric

    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9]*$")]
    [String]$ResourceGroupName,                             # required    needs to be alphanumeric

    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[a-z0-9]*$")]
    [String]$AppServicePlanName,

    [String]$Location = "East Asia",            # optional    default to "West US", needs to be a location which all the services created here are available
    [String]$WebAppTier = "Free"
    )

# Begin - Helper functions --------------------------------------------------------------------------------------------------------------------------

# Generate environment xml file, which will be used by the deploy script later.
Function Generate-EnvironmentXml
{
    Param(
        [String]$EnvironmentName,
        [String]$WebsiteName,
        [Object]$Storage
    )

    [String]$template = Get-Content ("{0}\website-environment.template" -f $scriptPath)

    $xml = $template -f $EnvironmentName, $WebsiteName, `
                        $Storage.AccountName, $Storage.AccessKey, $Storage.ConnectionString, `
    
    $xml | Out-File -Encoding utf8 ("{0}\website-environment.xml" -f $scriptPath)
}


# End - Helper funtions -----------------------------------------------------------------------------------------------------------------------------


# Begin - Actual script -----------------------------------------------------------------------------------------------------------------------------
# Select-AzureSubscription -Current -
# Set the output level to verbose and make the script stop on error
$VerbosePreference = "Continue"
$ErrorActionPreference = "Stop"

# Mark the start time of the script execution
$startTime = Get-Date

Write-Verbose ("[Start] creating Windows Azure website environment {0}" -f $Name)

# Get the directory of the current script
$scriptPath = Split-Path -parent $PSCommandPath

# Define the names of website, storage account
$Name = $Name.ToLower()
$websiteName = $Name
$storageAccountName = "{0}storage" -f $Name

#Login-AzureRmAccount
Try
{
	$CurrentResourceGroup = Get-AzureRmResourceGroup -Name $ResourceGroupName
	if ($CurrentResourceGroup)
	{
		Write-Output "The resource group already exists."    
	}
	else
	{
		New-AzureRmResourceGroup -Name $ResourceGroupName -Location $Location
		Write-Verbose ("[Finish] creating ResourceGroup {0} in location {1}" -f $ResourceGroupName, $Location)
	}
}
Catch
{
		New-AzureRmResourceGroup -Name $ResourceGroupName -Location $Location
		Write-Verbose ("[Finish] creating ResourceGroup {0} in location {1}" -f $ResourceGroupName, $Location)
}

Try
{
	$CurrentAppServicePlan = Get-AzureRMAppServicePlan -ResourceGroupName $ResourceGroupName  -Name $AppServicePlanName
	if ($CurrentAppServicePlan)
	{
		Write-Output "The App service already exists."   
	}
	else
	{
		New-AzureRMAppServicePlan -ResourceGroupName $ResourceGroupName -Name $AppServicePlanName -Location $Location  -Tier $WebAppTier
		Write-Verbose ("[Finish] creating App Service Plan {0} in location {1}" -f $AppServicePlan, $Location)
	}
}
Catch
{
	New-AzureRMAppServicePlan -ResourceGroupName $ResourceGroupName -Name $AppServicePlanName -Location $Location  -Tier $WebAppTier
	Write-Verbose ("[Finish] creating App Service Plan {0} in location {1}" -f $AppServicePlan, $Location)
}

# Create a new website
Write-Verbose ("[Start] creating website {0} in location {1}" -f $websiteName, $Location)
New-AzureRmWebApp -ResourceGroupName $ResourceGroupName -AppServicePlan $AppServicePlanName -Name $Name  -Verbose -Location $Location
Write-Verbose ("[Finish] creating website {0} in location {1}" -f $websiteName, $Location)

# Create a new storage account
$storage = & "$scriptPath\create-azure-storage.ps1" `
    -Name $storageAccountName `
    -ResourceGroupName $ResourceGroupName `
    -Location $Location

Write-Verbose ("[Finish] creating Windows Azure environment {0}" -f $Name)

# Write the environment info to an xml file so that the deploy script can consume
Write-Verbose "[Begin] writing environment info to website-environment.xml"
Generate-EnvironmentXml -EnvironmentName $Name -WebsiteName $websiteName -Storage $storage
Write-Verbose ("{0}\website-environment.xml" -f $scriptPath)
Write-Verbose "[Finish] writing environment info to website-environment.xml"

Get-AzureRMWebAppPublishingProfile -ResourceGroupName $ResourceGroupName -Name $websiteName -OutputFile ("{0}\{1}.PublishSettings" -f $scriptPath, $websiteName)

# Mark the finish time of the script execution
$finishTime = Get-Date
# Output the time consumed in seconds
Write-Output ("Total time used (seconds): {0}" -f ($finishTime - $startTime).TotalSeconds)

# End - Actual script -------------------------------------------------------------------------------------------------------------------------------