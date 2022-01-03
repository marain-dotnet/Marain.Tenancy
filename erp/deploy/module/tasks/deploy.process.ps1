task RunFirst

task EnsurePreReqs

task PreAzureAd
task AzureAdCore DeployAzureAdApplications
task PostAzureAd
task AzureAd PreAzureAd,AzureAdCore,PostAzureAd

task PreProvision
task ProvisionCore armDeployment
task PostProvision
task Provision PreProvision,ProvisionCore,PostProvision

task PreDeploy
task DeployCore
task PostDeploy
task Deploy PreDeploy,DeployCore,PostDeploy

task RunLast

task FullDeploy RunFirst,EnsurePreReqs,AzureAd,Provision,Deploy,RunLast
