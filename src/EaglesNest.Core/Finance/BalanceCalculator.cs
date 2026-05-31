using EaglesNest.Core.Domain;

namespace EaglesNest.Core.Finance;

public static class BalanceCalculator
{
    public static decimal GetOutstandingBalance(FinancialAssessment assessment)
    {
        var paid = assessment.Payments.Sum(payment => payment.Amount);
        return assessment.Amount - paid;
    }
}
