<#
This example demonstrates a software build process using the 'ZeroFailed.Build.DotNet' extension
to provide the features needed when building a .NET solutions.
#>

# We only need the Container build functionality from this extension, but we need to guarantee that the ZeroFailed.Build.DotNet extension
# overrides the overlapping build features. Workaround for https://github.com/zerofailed/ZeroFailed/issues/8
$zerofailedExtensions = @(
    @{
        Name = "Endjin.RecommendedPractices.Build"
        Version = "1.5.14"
    }
)
. ZeroFailed.tasks -ZfPath $here/.zf

$zerofailedExtensions = @(
    @{
        # References the extension from its GitHub repository. If not already installed, use latest version from 'main' will be downloaded.
        Name = "ZeroFailed.Build.DotNet"
        GitRepository = "https://github.com/zerofailed/ZeroFailed.Build.DotNet"
        GitRef = "main"
    }
)

# Load the tasks and process
. ZeroFailed.tasks -ZfPath $here/.zf


#
# Build process control options
#
$SkipInit = $false
$SkipVersion = $false
$SkipBuild = $false
$CleanBuild = $Clean
$SkipTest = $false
$SkipTestReport = $false
$SkipAnalysis = $false
$SkipPackage = $false

#
# Build process configuration
#
$SolutionToBuild = (Resolve-Path (Join-Path $here ".\Solutions\Marain.Tenancy.sln")).Path
$ProjectsToPublish = @(
    "Solutions/Marain.Tenancy.Api/Marain.Tenancy.Api.csproj"
    "Solutions/Marain.Tenancy.Cli/Marain.Tenancy.Cli.csproj"
)
$NuSpecFilesToPackage = @()
$NugetPublishSource = property ZF_NUGET_PUBLISH_SOURCE "$here/_local-nuget-feed"
$IncludeAssembliesInCodeCoverage = "Marain.Tenancy*"
$ExcludeAssembliesInCodeCoverage = ""

# Container-related build configuration
$ContainersToBuild = @(
    @{
        Dockerfile = "$here/Solutions/Marain.Tenancy.Api/Dockerfile"
        ImageName = "marain/tenancy-service"
        ContextDir = './_packages/Marain.Tenancy.Api'
        Arguments  = @{
            # Arguments with a scriptblock value are evaluated at runtime, rather than when the script is
            # loaded. This means any runtime variable overrides will be available.
            Configuration = { $Configuration }
            BaseImage = { $Configuration -eq 'Debug' ? 'sdk' : 'aspnet' }
            SrcDir = '.'
        }
    }
)
$ContainerRegistryType = 'ghcr'
$ContainerRegistryPublishPrefix = ""
$ContainerImageVersionOverride = "dev"     # ensure a static tag for local builds (overridden on build server)

# Bicep-related build configuration
$MinimumBicepCliVersion = "0.37.4"


# Customise the build process

task . FullBuild


#
# Build Process Extensibility Points - uncomment and implement as required
#

# Required until container functionality has been migrated to ZF
task ApplyEnvironmentVariableOverridesWrapper -Before PreInit ApplyEnvironmentVariableOverrides
task BuildContainerWrapper -After PackageCore BuildContainerImages
task PublishContainerWrapper -After PublishCore PublishContainerImages

# task RunFirst {}
# task PreInit {}
# task PostInit {}
# task PreVersion {}
# task PostVersion {}
# task PreBuild {}
# task PostBuild {}
# task PreTest Init {}
# task PostTest {}
# task PreTestReport {}
# task PostTestReport {}
# task PreAnalysis {}
# task PostAnalysis {}
# task PrePackage {}
# task PostPackage {}
# task PrePublish {}
# task PostPublish {}
# task RunLast {}
