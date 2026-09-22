using Tests.Shared.Attributes;
using Tests.Shared.Base;

namespace Tests.Domain;

[Unit]
public sealed class SanityTests : UnitTestsBase
{
    [Test]
    public void Domain_test_infrastructure_is_working() =>
        Assert.Pass("Sanity check: domain test project is correctly configured.");
}
