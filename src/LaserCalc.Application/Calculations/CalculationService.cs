using LaserCalc.Domain.Calculation;

namespace LaserCalc.Application.Calculations;

public sealed class CalculationService(CalculationEngine engine)
{
    public CalculationResult Calculate(CalculationInputs inputs) => engine.Calculate(inputs);
}
