namespace AttendVisionReportsApi.Helpers
{
    public static class NumberToWordsHelper
    {
        private static readonly string[] Ones =
        {
            "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
            "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen"
        };

        private static readonly string[] Tens =
        {
            "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"
        };

        /// <summary>
        /// Spells out a monetary amount, e.g. 15050.00 -> "Fifteen Thousand and Fifty Rand Only".
        /// Used for the "Amount in Words" line on printed invoices.
        /// </summary>
        public static string ToWords(decimal amount, string currencySingular = "Rand", string currencyPlural = "Rand")
        {
            var whole = (long)Math.Floor(amount);
            var cents = (int)Math.Round((amount - whole) * 100, MidpointRounding.AwayFromZero);

            var result = whole == 0 ? "Zero" : ConvertWhole(whole);
            result += $" {(whole == 1 ? currencySingular : currencyPlural)}";

            if (cents > 0)
            {
                result += $" and {ConvertWhole(cents)} Cent{(cents == 1 ? "" : "s")}";
            }

            return result + " Only";
        }

        private static string ConvertWhole(long number)
        {
            if (number == 0) return "";
            if (number < 20) return Ones[number];
            if (number < 100) return Tens[number / 10] + (number % 10 > 0 ? " " + Ones[number % 10] : "");
            if (number < 1_000) return Ones[number / 100] + " Hundred" + (number % 100 > 0 ? " and " + ConvertWhole(number % 100) : "");
            if (number < 1_000_000) return ConvertWhole(number / 1_000) + " Thousand" + (number % 1_000 > 0 ? " " + ConvertWhole(number % 1_000) : "");
            if (number < 1_000_000_000) return ConvertWhole(number / 1_000_000) + " Million" + (number % 1_000_000 > 0 ? " " + ConvertWhole(number % 1_000_000) : "");
            return ConvertWhole(number / 1_000_000_000) + " Billion" + (number % 1_000_000_000 > 0 ? " " + ConvertWhole(number % 1_000_000_000) : "");
        }
    }
}
