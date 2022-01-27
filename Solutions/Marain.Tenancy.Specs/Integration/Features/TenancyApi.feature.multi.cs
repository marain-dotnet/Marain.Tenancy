using Marain.Tenancy.Specs.MultiHost;

namespace Marain.Tenancy.Specs.Integration.Features
{
    [MultiHostTest]

    public partial class TenancyApiFeature : MultiTestHostBase
    {
        public TenancyApiFeature(TestHostModes hostMode)
    : base(hostMode)
        {
        }
    }
}