using System;
using System.Globalization; // Make sure this is at the top of your file

namespace MortgageThingy
{
    public class MortgageStrategy
    {
        public double BestDownPaymentPercent { get; set; }
        public double MonthlyDrawFromInvestment { get; set; }
        public int MonthsCovered { get; set; }
        public double mortgagePerMonth { get; set; }
        public double totalMonthlyCost { get; set; }
        public double RemainingMortgageBalance { get; set; }
        public double RemainingInvestmentBalance { get; set; }
        public int? MortgagePaidOffMonth { get; set; } // null if not paid off early
        public double InitialInvestmentAmount { get; set; } // Added for clarity
        public double InitialLoanAmount { get; set; } // Added for clarity
        public double PMITotalPaid { get; set; } // Added to track PMI cost
    }

    public class MortgageCalculator
    {
        public static MortgageStrategy GetOptimalMortgageStrategy(
            double housePrice,
            double depositCash,
            double mortgageRateAnnual,
            double investmentReturnAnnual, // New: Investment return
            double userMonthlyContribution,
            double yearlyPropertyTax,
            double yearlyHomeInsurance,
            double pmiRateAnnual,        // New: PMI rate
            double closingCosts,         // New: Closing costs
            double extraPrincipalPerMonth = 0,
            double forcedDown = 0,
            int loanTermYears = 30)      // New: Loan Term
        {
            double bestPercent = 0;
            double bestDraw = 0;
            int bestMonthsCovered = 0;
            double bestRemainingLoanBalance = 0;
            double bestRemainingInvestment = 0;
            double bestInitialInvestment = 0; // Track initial investment for the best strategy
            double bestInitialLoanAmount = 0; // Track initial loan amount for the best strategy
            double bestPMITotalPaid = 0; // Track PMI total paid for the best strategy
            double bestMonthlyMortgage = 0;
            double bestTotalMonthlyCost = 0;
            int? bestPayoffMonth = null; // This variable stores the payoff month for the *overall best* strategy

            double monthlyRate = mortgageRateAnnual / 12;
            int loanTermMonths = loanTermYears * 12;
            double investmentReturnMonthly = Math.Pow(1 + investmentReturnAnnual, 1.0 / 12) - 1;

            double monthlyPropertyTax = yearlyPropertyTax / 12.0;
            double monthlyInsurance = yearlyHomeInsurance / 12.0;

            // Adjust deposit cash for closing costs
            depositCash -= closingCosts;
            if (depositCash < 0)
            {
                // Not enough cash for closing costs, this strategy is not viable.
                return new MortgageStrategy { MonthsCovered = 0 };
            }

            // Determine the range for down payment percentages
            int startPercent = 5; // Start from 5% for more flexibility
            int endPercent = 50; // Max 50% down

            // If forcedDown is used, we only want to calculate for that specific down payment
            if (forcedDown > 0)
            {
                startPercent = (int)Math.Round((forcedDown / housePrice) * 100, 0);
                endPercent = startPercent; // Only iterate for this specific percentage
            }

            for (int percent = startPercent; percent <= endPercent; percent += 1)
            {
                double downPayment = housePrice * (percent / 100.0);

                // If forcedDown is active, ensure we use that value
                if (forcedDown > 0)
                {
                    downPayment = forcedDown;
                    // Recalculate percent based on forcedDown for accurate reporting
                    percent = (int)Math.Round((downPayment * 100) / housePrice, 0);
                }

                // Ensure we have enough cash for this down payment
                if (downPayment > depositCash) continue;

                double loanAmount = housePrice - downPayment;
                double invested = depositCash - downPayment;

                // Cannot take a loan if down payment covers everything or more
                if (loanAmount <= 0)
                {
                    // If the house is fully paid with cash, there's no mortgage strategy.
                    // This scenario is handled separately in the Main method's output.
                    continue; // Skip this iteration as it's not a mortgage scenario
                }

                // Calculate monthly mortgage payment
                double monthlyMortgage;
                if (monthlyRate == 0) // Handle zero interest rate case to avoid division by zero
                {
                    monthlyMortgage = loanAmount / loanTermMonths;
                }
                else
                {
                    monthlyMortgage = loanAmount * (monthlyRate * Math.Pow(1 + monthlyRate, loanTermMonths)) /
                                      (Math.Pow(1 + monthlyRate, loanTermMonths) - 1);
                }

                double investmentBalance = invested;
                double remainingLoanBalance = loanAmount;
                int monthsCovered = 0;
                bool mortgagePaidOff = false;
                double currentPMIMonthly = 0;
                double totalPMIPaid = 0;
                int? currentStrategyPayoffMonth = null; // This variable stores payoff month for *this specific* down payment strategy

                // Calculate initial PMI if applicable
                if (downPayment / housePrice < 0.20)
                {
                    currentPMIMonthly = (loanAmount * pmiRateAnnual) / 12.0;
                }

                for (int month = 0; month < loanTermMonths; month++)
                {
                    // Apply investment growth before drawing
                    investmentBalance *= (1 + investmentReturnMonthly);

                    double interestPortion = 0;
                    double principalPortion = 0;

                    if (!mortgagePaidOff)
                    {
                        interestPortion = remainingLoanBalance * monthlyRate;
                        principalPortion = monthlyMortgage - interestPortion;

                        // Ensure principal portion doesn't exceed remaining balance
                        if (principalPortion > remainingLoanBalance)
                        {
                            principalPortion = remainingLoanBalance;
                        }

                        remainingLoanBalance -= principalPortion;

                        // Apply extra principal payment
                        if (remainingLoanBalance > 0)
                        {
                            double extraPrincipalToApply = Math.Min(extraPrincipalPerMonth, remainingLoanBalance);
                            remainingLoanBalance -= extraPrincipalToApply;
                        }

                        if (remainingLoanBalance <= 0)
                        {
                            mortgagePaidOff = true;
                            remainingLoanBalance = 0;
                            if (!currentStrategyPayoffMonth.HasValue) // Record payoff month only once for THIS strategy
                                currentStrategyPayoffMonth = month + 1;
                        }

                        // Check for PMI removal based on original LTV
                        // Note: This is a simplified LTV check. Real LTV can include appreciation.
                        // PMI is removed if the loan balance drops to 80% or less of the original loan amount.
                        if (currentPMIMonthly > 0 && remainingLoanBalance / loanAmount <= 0.80)
                        {
                            currentPMIMonthly = 0; // PMI is removed
                        }

                        totalPMIPaid += currentPMIMonthly; // Accumulate PMI
                    }


                    double currentHousingCost = monthlyPropertyTax + monthlyInsurance;
                    if (!mortgagePaidOff)
                    {
                        currentHousingCost += monthlyMortgage + currentPMIMonthly;
                    }

                    double drawThisMonth = currentHousingCost - userMonthlyContribution;

                    if (drawThisMonth > investmentBalance)
                    {
                        break; // Investment ran out
                    }

                    investmentBalance -= drawThisMonth;
                    monthsCovered++;
                }

                // We want the strategy that keeps the investment alive the longest.
                // If there's a tie in monthsCovered, you might add another criteria
                // e.g., higher remaining investment balance, lower total cost, etc.
                if (monthsCovered > bestMonthsCovered)
                {
                    bestPercent = percent;
                    bestMonthsCovered = monthsCovered;
                    // Recalculate total monthly cost for the best strategy, assuming PMI is still active if applicable
                    bestTotalMonthlyCost = monthlyMortgage + monthlyPropertyTax + monthlyInsurance + ((downPayment / housePrice < 0.20 && currentPMIMonthly > 0) ? pmiRateAnnual * loanAmount / 12 : 0);
                    bestMonthlyMortgage = monthlyMortgage;
                    bestRemainingLoanBalance = remainingLoanBalance;
                    bestRemainingInvestment = investmentBalance;
                    bestInitialInvestment = invested;
                    bestInitialLoanAmount = loanAmount;
                    bestPMITotalPaid = totalPMIPaid;
                    bestPayoffMonth = currentStrategyPayoffMonth; // <<< THIS IS THE CORRECTED LINE

                    // Report actual draw based on the cost during the period investment was active
                    // This needs to be calculated based on the *initial* monthly cost for that strategy
                    double initialPMIForBestStrategy = (downPayment / housePrice < 0.20) ? (loanAmount * pmiRateAnnual) / 12.0 : 0;
                    bestDraw = Math.Round(monthlyMortgage + monthlyPropertyTax + monthlyInsurance + initialPMIForBestStrategy - userMonthlyContribution, 2);
                }

                // If forcedDown was used, we only want to run one iteration
                if (forcedDown > 0)
                {
                    break;
                }
            }

            return new MortgageStrategy
            {
                BestDownPaymentPercent = bestPercent,
                MonthlyDrawFromInvestment = bestDraw,
                MonthsCovered = bestMonthsCovered,
                totalMonthlyCost = bestTotalMonthlyCost,
                mortgagePerMonth = bestMonthlyMortgage,
                RemainingMortgageBalance = Math.Round(bestRemainingLoanBalance, 2),
                RemainingInvestmentBalance = Math.Round(bestRemainingInvestment, 2),
                MortgagePaidOffMonth = bestPayoffMonth,
                InitialInvestmentAmount = Math.Round(bestInitialInvestment, 2),
                InitialLoanAmount = Math.Round(bestInitialLoanAmount, 2),
                PMITotalPaid = Math.Round(bestPMITotalPaid, 2)
            };
        }

        // Helper method for robust integer input
        // (These helper methods would typically be in the Program class or a separate Utilities class)
        public static int GetIntInput(string prompt, int defaultValue)
        {
            int value;
            while (true)
            {
                Console.Write($"{prompt} (default {defaultValue.ToString("N0", CultureInfo.CurrentCulture)}): ");
                string input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    return defaultValue;
                }
                if (int.TryParse(input, out value) && value >= 0)
                {
                    return value;
                }
                Console.WriteLine("Invalid input. Please enter a non-negative whole number.");
            }
        }

        // Helper method for robust double input
        public static double GetDoubleInput(string prompt, double defaultValue, bool isPercentage = false)
        {
            double value;
            while (true)
            {
                string defaultString = isPercentage ? $"{defaultValue * 100}%" : defaultValue.ToString("N2", CultureInfo.CurrentCulture);
                Console.Write($"{prompt} (default {defaultString}): ");
                string input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    return defaultValue;
                }
                if (double.TryParse(input, out value))
                {
                    if (isPercentage)
                    {
                        // If input is like "7.5", treat as 7.5%, convert to 0.075
                        // If input is already like "0.075", use as is.
                        // Simple check: if value is > 1.0 and user intended percent (e.g., entered 7.5 instead of 0.075)
                        if (value > 1.0) value /= 100.0;
                    }

                    if (value >= 0) return value;
                }
                Console.WriteLine("Invalid input. Please enter a non-negative number.");
            }
        }
    }
}