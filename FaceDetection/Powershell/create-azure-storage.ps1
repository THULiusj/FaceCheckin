Param(
    [Parameter(Mandatory = $true)]
    [String]$Name,
    [Parameter(Mandatory = $true)]
    [String]$ResourceGroupName,
    [String]$Location = "East Asia"
)
$CurrentResourceGroup = Get-AzureRmResourceGroup -Name $ResourceGroupName
if ($CurrentResourceGroup)
{
    Write-Output "The resource group already exists."
    
}
else
{
    New-AzureRmResourceGroup -Name $ResourceGroupName -Location "East Asia"
}
# Create a new storage account
Write-Verbose ("[Start] creating storage account {0} in location {1}" -f $Name, $Location)
New-AzureRmStorageAccount -ResourceGroupName $ResourceGroupName -StorageAccountName $Name -Location $Location -Type Standard_LRS -Verbose
Write-Verbose ("[Finish] creating storage account {0} in location {1}" -f $Name, $Location)

# Get the access key of the storage account
$key = (Get-AzureRmStorageAccountKey -ResourceGroupName $ResourceGroupName -StorageAccountName $Name).Key1

# Generate the connection string of the storage account
$connectionString = "BlobEndpoint=http://{0}.blob.core.windows.net/;QueueEndpoint=http://{0}.queue.core.windows.net/;TableEndpoint=http://{0}.table.core.windows.net/;AccountName={0};AccountKey={1}" -f $Name, $key.Primary

Return @{AccountName = $Name; AccessKey = $key; ConnectionString = $connectionString}