using EaglesNest.Core.Domain;
using EaglesNest.Core.Finance;

namespace EaglesNest.Tests.Finance;

public class BalanceCalculatorTests
{
    [Fact]
    public void GetOutstandingBalance_ReturnsAssessmentAmount_WhenNoPaymentsExist()
    {
        var assessment = new FinancialAssessment
        {
            Amount = 100m
        };

        var balance = BalanceCalculator.GetOutstandingBalance(assessment);

        Assert.Equal(100m, balance);
    }

    [Fact]
    public void GetOutstandingBalance_SubtractsAllPayments()
    {
        var assessment = new FinancialAssessment
        {
            Amount = 100m,
            Payments =
            [
                new FinancialPayment { Amount = 25m },
                new FinancialPayment { Amount = 10m }
            ]
        };

        var balance = BalanceCalculator.GetOutstandingBalance(assessment);

        Assert.Equal(65m, balance);
    }
}
