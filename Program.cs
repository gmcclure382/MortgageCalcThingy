using System;
using System.Globalization;

namespace MortgageThingy
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.Clear();
            Console.WriteLine("======================================");
            Console.WriteLine("        Mortgage Strategy Calculator        ");
            Console.WriteLine("======================================");
            Console.WriteLine();

            // Default Values
            double defaultHousePrice = 350000;
            double defaultDepositCash = 200000;
            double defaultMortgageRateFrom = 0.065; // 6.5%
            double defaultMortgageRateTo = 0.079;   // 7.9%
            double defaultInvestmentReturn = 0.06;  // 8% annual
            int defaultMonthlyContribution = 1000;
            int defaultPropertyTax = 2000;
            int defaultHomeInsurance = 1500;
            int defaultPrincipalPayment = 0;
            int defaultForcedDown = 0;
            double defaultPMIRate = 0.005; // 0.5% of loan amount annually
            double defaultClosingCosts = 5000; // Example: $5,000 for closing costs
            int defaultLoanTermYears = 30;

            // Get User Inputs using helper methods, passing default values
            double housePrice = GetIntInput("House Price", (int)defaultHousePrice);
            double depositCash = GetIntInput("Deposit Cash Available", (int)defaultDepositCash);
            double closingCosts = GetIntInput("Estimated Closing Costs", (int)defaultClosingCosts);

            // Adjust depositCash by subtracting closing costs upfront
            depositCash -= closingCosts;
            if (depositCash < 0)
            {
                Console.WriteLine("Error: Deposit cash available is less than estimated closing costs. Please adjust your inputs.");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                return;
            }

            double fromPercent = GetDoubleInput("Mortgage Rate From (6.5)", defaultMortgageRateFrom, true);
            double toPercent = GetDoubleInput("Mortgage Rate To (7.9)", defaultMortgageRateTo, true);
            double investmentReturnAnnual = GetDoubleInput("Annual Investment Return (6)", defaultInvestmentReturn, true);
            int monthlyContribution = GetIntInput("Monthly Contribution from Paycheck (towards housing)", defaultMonthlyContribution);
            int propertyTax = GetIntInput("Yearly Property Tax (2000)", defaultPropertyTax);
            int homeInsurance = GetIntInput("Yearly Home Insurance (1500)", defaultHomeInsurance);
            int principalPayment = GetIntInput("Extra Monthly Principal Payment (0)", defaultPrincipalPayment);
            int forcedDown = GetIntInput("Forced Down Payment Amount (0 for no forced down payment)", defaultForcedDown);
            double pmiRateAnnual = GetDoubleInput("Annual PMI Rate (e.g., 0.5 for 0.5%)", defaultPMIRate, true);
            int loanTermYears = GetIntInput("Loan Term in Years (30)", defaultLoanTermYears);

            Console.Clear();
            Console.WriteLine("============SUMMARY OF INPUTS============");
            Console.WriteLine($"House Price: {housePrice.ToString("C", CultureInfo.CurrentCulture)}");
            Console.WriteLine($"Deposit Cash Available (Before Closing Costs): {(depositCash + closingCosts).ToString("C", CultureInfo.CurrentCulture)}");
            Console.WriteLine($"Estimated Closing Costs: {closingCosts.ToString("C", CultureInfo.CurrentCulture)}");
            Console.WriteLine($"Deposit Cash Remaining (After Closing Costs): {depositCash.ToString("C", CultureInfo.CurrentCulture)}");
            Console.WriteLine($"Mortgage Rate Range: {Math.Round(fromPercent * 100, 2)}% to {Math.Round(toPercent * 100, 2)}%");
            Console.WriteLine($"Annual Investment Return: {Math.Round(investmentReturnAnnual * 100, 2)}%");
            Console.WriteLine($"Monthly Contribution from Paycheck: {monthlyContribution.ToString("C", CultureInfo.CurrentCulture)}");
            Console.WriteLine($"Yearly Property Tax: {propertyTax.ToString("C", CultureInfo.CurrentCulture)}");
            Console.WriteLine($"Yearly Home Insurance: {homeInsurance.ToString("C", CultureInfo.CurrentCulture)}");
            Console.WriteLine($"Extra Monthly Principal Payment: {principalPayment.ToString("C", CultureInfo.CurrentCulture)}");
            Console.WriteLine($"Total Paycheck Contribution Per Month: {(monthlyContribution + principalPayment).ToString("C", CultureInfo.CurrentCulture)}");
            Console.WriteLine($"Forced Down Payment Amount: {forcedDown.ToString("C", CultureInfo.CurrentCulture)}");
            Console.WriteLine($"Annual PMI Rate: {Math.Round(pmiRateAnnual * 100, 2)}%");
            Console.WriteLine($"Loan Term: {loanTermYears} years");
            Console.WriteLine("=========================================");
            Console.WriteLine("\nCalculating Best Strategies...\n");

            // Loop through mortgage rates
            for (double currentMortgageRate = fromPercent; currentMortgageRate <= toPercent; currentMortgageRate += 0.001)
            {
                var strategy = MortgageCalculator.GetOptimalMortgageStrategy(
                    housePrice: housePrice,
                    depositCash: depositCash,
                    mortgageRateAnnual: currentMortgageRate,
                    investmentReturnAnnual: investmentReturnAnnual,
                    userMonthlyContribution: monthlyContribution,
                    yearlyPropertyTax: propertyTax,
                    yearlyHomeInsurance: homeInsurance,
                    pmiRateAnnual: pmiRateAnnual,
                    closingCosts: 0, // Already deducted from depositCash, so pass 0 here
                    extraPrincipalPerMonth: principalPayment,
                    forcedDown: forcedDown,
                    loanTermYears: loanTermYears
                );

                Console.WriteLine($"------------------- Mortgage Rate: {Math.Round(currentMortgageRate * 100, 2)}% -----------------");
                if (strategy.MonthsCovered == 0 && strategy.InitialLoanAmount > 0)
                {
                    // This scenario means the investment fund was insufficient to cover even the first month's draw
                    // given the user's contribution, or there was a very small investment amount that quickly depleted.
                    Console.WriteLine("No viable strategy found for this mortgage rate, or investment ran out immediately.");
                    Console.WriteLine($"Initial Loan Amount: {strategy.InitialLoanAmount.ToString("C", CultureInfo.CurrentCulture)}");
                    Console.WriteLine($"Initial Investment: {strategy.InitialInvestmentAmount.ToString("C", CultureInfo.CurrentCulture)}");
                    Console.WriteLine($"Mortgage Payment (P&I): {strategy.mortgagePerMonth.ToString("C", CultureInfo.CurrentCulture)}");
                    Console.WriteLine($"Required Monthly Draw (approx): {Math.Round(strategy.totalMonthlyCost - monthlyContribution, 2).ToString("C", CultureInfo.CurrentCulture)}");
                }
                else if (strategy.InitialLoanAmount <= 0) // Case where down payment covers house price or more
                {
                    Console.WriteLine("House purchased fully with cash. No mortgage required.");
                    Console.WriteLine($"Initial Investment: {strategy.InitialInvestmentAmount.ToString("C", CultureInfo.CurrentCulture)}");
                    Console.WriteLine($"Remaining Investment: {strategy.RemainingInvestmentBalance.ToString("C", CultureInfo.CurrentCulture)}");
                }
                else
                {
                    double initialDownPaymentAmount = housePrice * (strategy.BestDownPaymentPercent / 100.0);
                    // Ensure the initial down payment shown matches the forcedDown if applicable, otherwise use calculated percent.
                    if (forcedDown > 0) initialDownPaymentAmount = forcedDown;


                    Console.WriteLine($"Best Down Payment: {strategy.BestDownPaymentPercent}% ({initialDownPaymentAmount.ToString("C", CultureInfo.CurrentCulture)})");
                    Console.WriteLine($"Initial Loan Amount: {strategy.InitialLoanAmount.ToString("C", CultureInfo.CurrentCulture)}");
                    Console.WriteLine($"Initial Investment Amount: {strategy.InitialInvestmentAmount.ToString("C", CultureInfo.CurrentCulture)}");
                    Console.WriteLine($"Monthly Mortgage (P&I): {Math.Round(strategy.mortgagePerMonth, 2).ToString("C", CultureInfo.CurrentCulture)}");
                    Console.WriteLine($"Total Housing Cost Per Month (incl. P&I, Tax, Ins, PMI if applicable): {Math.Round(strategy.totalMonthlyCost, 2).ToString("C", CultureInfo.CurrentCulture)}");
                    Console.WriteLine($"Monthly Draw from Investment: {Math.Round(strategy.MonthlyDrawFromInvestment, 2).ToString("C", CultureInfo.CurrentCulture)}");
                    Console.WriteLine($"Monthly Contribution from Paycheck (towards housing): {monthlyContribution.ToString("C", CultureInfo.CurrentCulture)}");
                    Console.WriteLine($"Extra Principal Payment Per Month: {principalPayment.ToString("C", CultureInfo.CurrentCulture)}");
                    Console.WriteLine($"Investment Covered for: {strategy.MonthsCovered / 12} years and {strategy.MonthsCovered % 12} months");
                    Console.WriteLine($"Total PMI Paid: {strategy.PMITotalPaid.ToString("C", CultureInfo.CurrentCulture)}");
                    Console.WriteLine($"Remaining Mortgage Balance: {strategy.RemainingMortgageBalance.ToString("C", CultureInfo.CurrentCulture)}");
                    Console.WriteLine($"Remaining Investment Balance: {strategy.RemainingInvestmentBalance.ToString("C", CultureInfo.CurrentCulture)}");

                    if (strategy.MortgagePaidOffMonth.HasValue)
                    {
                        int yr = strategy.MortgagePaidOffMonth.Value / 12;
                        int mo = strategy.MortgagePaidOffMonth.Value % 12;
                        Console.WriteLine($"Mortgage paid off early after {yr} years and {mo} months");
                    }
                    else
                    {
                        Console.WriteLine("Mortgage not paid off within the loan term.");
                    }
                }
                Console.WriteLine($"------------------------------------------------------------------");
                Console.WriteLine("\n");
            }
            Console.WriteLine("Calculation Complete. Press any key to exit.");
            Console.ReadKey();
        }

        // Helper method for robust integer input
        private static int GetIntInput(string prompt, int defaultValue)
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
        private static double GetDoubleInput(string prompt, double defaultValue, bool isPercentage = false)
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