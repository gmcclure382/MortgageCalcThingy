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
            double defaultHousePrice = 340000;
            double defaultDepositCash = 200000;
            double defaultMortgageRateFrom = 0.05875; // 6.5%
            double defaultMortgageRateTo = 0.05875;   // 7.9%
            double defaultInvestmentReturn = 0.04;  // 8% annual
            int defaultMonthlyContribution = 1000;
            int defaultPropertyTax = 1865;
            int defaultHomeInsurance = 2160;
            int defaultPrincipalPayment = 0;
            int defaultForcedDown = 68000;
            double defaultPMIRate = 0.000; // 0.5% of loan amount annually
            int defaultClosingCosts = 1384; // Example: $5,000 for closing costs
            int defaultLoanTermYears = 30;

            var fields = BuildInputFields(
                defaultHousePrice,
                defaultDepositCash,
                defaultClosingCosts,
                defaultMortgageRateFrom,
                defaultMortgageRateTo,
                defaultInvestmentReturn,
                defaultMonthlyContribution,
                defaultPrincipalPayment,
                defaultPropertyTax,
                defaultHomeInsurance,
                defaultForcedDown,
                defaultPMIRate,
                defaultLoanTermYears
            );

            while (true)
            {
                bool continueRun = RunInputForm(fields);
                if (!continueRun) return;

                double housePrice = GetFieldDouble(fields, "HousePrice");
                double depositCash = GetFieldDouble(fields, "DepositCash");
                double closingCosts = GetFieldInt(fields, "ClosingCosts");

                // Adjust depositCash by subtracting closing costs upfront
                depositCash -= closingCosts;
                if (depositCash < 0)
                {
                    Console.WriteLine("Error: Deposit cash available is less than estimated closing costs. Please adjust your inputs.");
                    Console.WriteLine("Press any key to return to the form.");
                    Console.ReadKey();
                    continue;
                }

                double fromPercent = GetFieldPercent(fields, "MortgageRateFrom");
                double toPercent = GetFieldPercent(fields, "MortgageRateTo");
                double investmentReturnAnnual = GetFieldPercent(fields, "InvestmentReturn");
                int monthlyContribution = GetFieldInt(fields, "MonthlyContribution");
                int principalPayment = GetFieldInt(fields, "PrincipalPayment");
                int propertyTax = GetFieldInt(fields, "PropertyTax");
                int homeInsurance = GetFieldInt(fields, "HomeInsurance");
                int forcedDown = GetFieldInt(fields, "ForcedDown");
                double pmiRateAnnual = GetFieldPercent(fields, "PMIRate");
                int loanTermYears = GetFieldInt(fields, "LoanTermYears");

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
                            Console.WriteLine($"Investment Balance at Mortgage Payoff: {strategy.InvestmentBalanceAtPayoff.Value.ToString("C", CultureInfo.CurrentCulture)}");
                        }
                        else
                        {
                            Console.WriteLine("Mortgage not paid off within the loan term.");
                        }
                    }
                    Console.WriteLine($"------------------------------------------------------------------");
                    Console.WriteLine("\n");
                }

                Console.WriteLine("Calculation Complete. Press Enter to return to the form, or Esc to exit.");
                ConsoleKeyInfo endKey = Console.ReadKey(true);
                if (endKey.Key == ConsoleKey.Escape)
                {
                    return;
                }
            }
        }
        

        private enum FieldType
        {
            Int,
            Double,
            Percent
        }

        private sealed class InputField
        {
            public string Key { get; }
            public string Label { get; }
            public FieldType Type { get; }
            public int DefaultInt { get; }
            public double DefaultDouble { get; }
            public bool HasValue { get; set; }
            public int CurrentInt { get; set; }
            public double CurrentDouble { get; set; }

            public InputField(string key, string label, int defaultValue)
            {
                Key = key;
                Label = label;
                Type = FieldType.Int;
                DefaultInt = defaultValue;
            }

            public InputField(string key, string label, double defaultValue, FieldType type)
            {
                Key = key;
                Label = label;
                Type = type;
                DefaultDouble = defaultValue;
            }
        }

        private static System.Collections.Generic.List<InputField> BuildInputFields(
            double defaultHousePrice,
            double defaultDepositCash,
            int defaultClosingCosts,
            double defaultMortgageRateFrom,
            double defaultMortgageRateTo,
            double defaultInvestmentReturn,
            int defaultMonthlyContribution,
            int defaultPrincipalPayment,
            int defaultPropertyTax,
            int defaultHomeInsurance,
            int defaultForcedDown,
            double defaultPMIRate,
            int defaultLoanTermYears)
        {
            return new System.Collections.Generic.List<InputField>
            {
                new InputField("HousePrice", "House Price", (int)defaultHousePrice),
                new InputField("DepositCash", "Deposit Cash Available", (int)defaultDepositCash),
                new InputField("ClosingCosts", "Estimated Closing Costs", defaultClosingCosts),
                new InputField("MortgageRateFrom", "Mortgage Rate From", defaultMortgageRateFrom, FieldType.Percent),
                new InputField("MortgageRateTo", "Mortgage Rate To", defaultMortgageRateTo, FieldType.Percent),
                new InputField("InvestmentReturn", "Annual Investment Return", defaultInvestmentReturn, FieldType.Percent),
                new InputField("MonthlyContribution", "Monthly Contribution from Paycheck", defaultMonthlyContribution),
                new InputField("PrincipalPayment", "Extra Monthly Principal Payment", defaultPrincipalPayment),
                new InputField("PropertyTax", "Yearly Property Tax", defaultPropertyTax),
                new InputField("HomeInsurance", "Yearly Home Insurance", defaultHomeInsurance),
                new InputField("ForcedDown", "Forced Down Payment Amount", defaultForcedDown),
                new InputField("PMIRate", "Annual PMI Rate", defaultPMIRate, FieldType.Percent),
                new InputField("LoanTermYears", "Loan Term in Years", defaultLoanTermYears)
            };
        }

        private static bool RunInputForm(System.Collections.Generic.List<InputField> fields)
        {
            int selectedIndex = 0;
            bool isEditing = false;
            string editBuffer = string.Empty;
            string errorMessage = string.Empty;

            ConsoleKeyInfo keyInfo;
            while (true)
            {
                Console.Clear();
                Console.WriteLine("======================================");
                Console.WriteLine("        Mortgage Strategy Calculator        ");
                Console.WriteLine("======================================");
                Console.WriteLine("Tab/Shift+Tab or Up/Down to move. Enter to edit. F5 to calculate. Esc to quit.");
                Console.WriteLine();

                int labelWidth = 38;
                int valueWidth = 14;
                int columnPadding = 4;
                int columnWidth = 2 + labelWidth + 1 + valueWidth + columnPadding;
                int leftColumnCount = (fields.Count + 1) / 2;
                for (int row = 0; row < leftColumnCount; row++)
                {
                    string leftText = RenderFieldLine(fields, row, selectedIndex, isEditing, editBuffer, labelWidth, valueWidth);
                    string rightText = RenderFieldLine(fields, row + leftColumnCount, selectedIndex, isEditing, editBuffer, labelWidth, valueWidth);
                    Console.WriteLine(leftText.PadRight(columnWidth) + rightText);
                }

                Console.WriteLine();
                if (!string.IsNullOrWhiteSpace(errorMessage))
                {
                    Console.WriteLine(errorMessage);
                    Console.WriteLine();
                }

                if (isEditing)
                {
                    InputField field = fields[selectedIndex];
                    string defaultHint = GetDefaultHint(field);
                    Console.WriteLine($"Editing: {field.Label} (default {defaultHint}). Type a value then press Enter.");
                }

                keyInfo = Console.ReadKey(true);
                errorMessage = string.Empty;

                if (isEditing)
                {
                    if (keyInfo.Key == ConsoleKey.Enter)
                    {
                        if (!TryCommitEdit(fields[selectedIndex], editBuffer, out string commitError))
                        {
                            errorMessage = commitError;
                        }
                        else
                        {
                            isEditing = false;
                            editBuffer = string.Empty;
                            selectedIndex = (selectedIndex + 1) % fields.Count;
                        }
                        continue;
                    }

                    if (keyInfo.Key == ConsoleKey.Escape)
                    {
                        isEditing = false;
                        editBuffer = string.Empty;
                        continue;
                    }

                    if (keyInfo.Key == ConsoleKey.Backspace)
                    {
                        if (editBuffer.Length > 0)
                        {
                            editBuffer = editBuffer.Substring(0, editBuffer.Length - 1);
                        }
                        continue;
                    }

                    if (!char.IsControl(keyInfo.KeyChar))
                    {
                        editBuffer += keyInfo.KeyChar;
                    }
                    continue;
                }

                if (keyInfo.Key == ConsoleKey.Escape)
                {
                    return false;
                }

                if (keyInfo.Key == ConsoleKey.F5)
                {
                    return true;
                }

                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    isEditing = true;
                    editBuffer = string.Empty;
                    continue;
                }

                if (keyInfo.Key == ConsoleKey.Tab)
                {
                    if ((keyInfo.Modifiers & ConsoleModifiers.Shift) != 0)
                    {
                        selectedIndex = (selectedIndex - 1 + fields.Count) % fields.Count;
                    }
                    else
                    {
                        selectedIndex = (selectedIndex + 1) % fields.Count;
                    }
                    continue;
                }

                if (keyInfo.Key == ConsoleKey.DownArrow)
                {
                    selectedIndex = (selectedIndex + 1) % fields.Count;
                    continue;
                }

                if (keyInfo.Key == ConsoleKey.UpArrow)
                {
                    selectedIndex = (selectedIndex - 1 + fields.Count) % fields.Count;
                    continue;
                }
            }
        }

        private static string RenderFieldLine(
            System.Collections.Generic.List<InputField> fields,
            int index,
            int selectedIndex,
            bool isEditing,
            string editBuffer,
            int labelWidth,
            int valueWidth)
        {
            if (index < 0 || index >= fields.Count)
            {
                return string.Empty;
            }

            InputField field = fields[index];
            string selector = index == selectedIndex ? ">" : " ";
            string label = field.Label.PadRight(labelWidth);
            string valueText = FormatFieldValue(field).PadLeft(valueWidth);

            if (index == selectedIndex && isEditing)
            {
                string editText = editBuffer.PadLeft(valueWidth);
                return $"{selector} {label} {editText}";
            }

            return $"{selector} {label} {valueText}";
        }

        private static string FormatFieldValue(InputField field)
        {
            switch (field.Type)
            {
                case FieldType.Int:
                    int intValue = field.HasValue ? field.CurrentInt : field.DefaultInt;
                    return intValue.ToString("N0", CultureInfo.CurrentCulture);
                case FieldType.Percent:
                    double pctValue = field.HasValue ? field.CurrentDouble : field.DefaultDouble;
                    return $"{(pctValue * 100).ToString("0.###", CultureInfo.CurrentCulture)}%";
                case FieldType.Double:
                default:
                    double dblValue = field.HasValue ? field.CurrentDouble : field.DefaultDouble;
                    return dblValue.ToString("N2", CultureInfo.CurrentCulture);
            }
        }

        private static string GetDefaultHint(InputField field)
        {
            switch (field.Type)
            {
                case FieldType.Int:
                    return field.DefaultInt.ToString("N0", CultureInfo.CurrentCulture);
                case FieldType.Percent:
                    return $"{(field.DefaultDouble * 100).ToString("0.###", CultureInfo.CurrentCulture)}%";
                case FieldType.Double:
                default:
                    return field.DefaultDouble.ToString("N2", CultureInfo.CurrentCulture);
            }
        }

        private static bool TryCommitEdit(InputField field, string input, out string error)
        {
            error = string.Empty;
            string trimmed = (input ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                field.HasValue = false;
                return true;
            }

            trimmed = trimmed.Replace("%", string.Empty).Trim();

            if (field.Type == FieldType.Int)
            {
                if (!int.TryParse(trimmed, NumberStyles.Number, CultureInfo.CurrentCulture, out int intValue))
                {
                    error = "Invalid input. Please enter a non-negative whole number.";
                    return false;
                }
                if (intValue < 0)
                {
                    error = "Invalid input. Please enter a non-negative whole number.";
                    return false;
                }
                field.CurrentInt = intValue;
                field.HasValue = true;
                return true;
            }

            if (!double.TryParse(trimmed, NumberStyles.Number, CultureInfo.CurrentCulture, out double doubleValue))
            {
                error = "Invalid input. Please enter a non-negative number.";
                return false;
            }
            if (doubleValue < 0)
            {
                error = "Invalid input. Please enter a non-negative number.";
                return false;
            }
            if (field.Type == FieldType.Percent && doubleValue > 1.0)
            {
                doubleValue /= 100.0;
            }

            field.CurrentDouble = doubleValue;
            field.HasValue = true;
            return true;
        }

        private static int GetFieldInt(System.Collections.Generic.List<InputField> fields, string key)
        {
            InputField field = fields.Find(f => f.Key == key);
            return field.HasValue ? field.CurrentInt : field.DefaultInt;
        }

        private static double GetFieldDouble(System.Collections.Generic.List<InputField> fields, string key)
        {
            InputField field = fields.Find(f => f.Key == key);
            if (field.Type == FieldType.Int)
            {
                return field.HasValue ? field.CurrentInt : field.DefaultInt;
            }
            return field.HasValue ? field.CurrentDouble : field.DefaultDouble;
        }

        private static double GetFieldPercent(System.Collections.Generic.List<InputField> fields, string key)
        {
            InputField field = fields.Find(f => f.Key == key);
            return field.HasValue ? field.CurrentDouble : field.DefaultDouble;
        }
    }
}
