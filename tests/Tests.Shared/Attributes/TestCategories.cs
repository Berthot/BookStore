namespace Tests.Shared.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class UnitAttribute() : CategoryAttribute("Unit");

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class IntegrationAttribute() : CategoryAttribute("Integration");

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SlowAttribute() : CategoryAttribute("Slow");

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class CriticalAttribute() : CategoryAttribute("Critical");
