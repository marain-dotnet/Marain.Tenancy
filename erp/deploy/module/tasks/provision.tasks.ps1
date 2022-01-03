task armDeployment {

    $script:ArmDeploymentOutputs = Invoke-CorvusArmTemplateDeployment `
                                        -BicepVersion "0.4.1124" `
                                        -DeploymentScope $armDeployment.Scope `
                                        -Location $armDeployment.Location `
                                        -ArmTemplatePath $armDeployment.TemplatePath `
                                        -TemplateParameters $armDeployment.TemplateParameters `
                                        -NoArtifacts `
                                        -Verbose
}