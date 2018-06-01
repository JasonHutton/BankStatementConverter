using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Globalization;

using BankStatementConverter.Transactions;

namespace BankStatementConverter.Statements
{
    public class RBCStatement : Statement
    {
        private static string dataStart = "TRANSACTION POSTING\nACTIVITY DESCRIPTION AMOUNT ($)\nDATE DATE\n";
        private static string startEndStart = "STATEMENTFROM";

        public override void GetRecords(String data)
        {
            int recordHeaderIndex = 0;
            RecordHeaderIndex = data.IndexOf(dataStart, recordHeaderIndex);
            RecordHeader = data.Substring(RecordHeaderIndex, dataStart.Length);
            FirstRecordIndex = RecordHeaderIndex + RecordHeader.Length;
            LastRecordIndex = 0;

            // This is a hack due to issues with whitespace detection. We're just going to read this pretty fixedly.
            int ses = data.IndexOf(startEndStart);
            int ses2 = data.IndexOf("\n", ses + startEndStart.Length + 1);
            string datex = data.Substring(ses, ses2 - ses);

            HackGetYears(datex);

            int nextRecordIndex = FirstRecordIndex;

            while (nextRecordIndex >= 0 && nextRecordIndex < data.Length)
            {
                int dollarIndex = 0;
                dollarIndex = data.IndexOf('$', nextRecordIndex);

                if (dollarIndex == -1)
                    break;

                if (data.Substring(nextRecordIndex, dollarIndex - nextRecordIndex).EndsWith("AMOUNT ("))
                {
                    // Okay, we're hitting a "continue" delimiter, between pages. We're seeing the data header again. Advance this a bit.
                    dollarIndex = data.IndexOf("\n", dollarIndex + 3);
                }

                // Redundant, but recheck anyway.
                if (dollarIndex == -1)
                    break;

                int recordEndIndex = 0;
                recordEndIndex = data.IndexOf("\n", dollarIndex);

                if (recordEndIndex == -1)
                    break;

                // If this is wrapping to the next page without a delimiter, trim it.
                bool bTrimRBC = false;
                if (data.Substring(recordEndIndex - 3, 3) == "RBC")
                {
                    recordEndIndex = recordEndIndex - 3;
                    bTrimRBC = true;
                }

                // Redundant, but recheck anyway.
                if (recordEndIndex == -1)
                    break;

                string Record = data.Substring(nextRecordIndex, recordEndIndex - nextRecordIndex);

                if (FilterRecord(Record))
                {
                    records.Add(Record);
                    LastRecordIndex = nextRecordIndex;
                }

                // Since in the lacking-delimiter scenario we don't need to advance over it.
                if (bTrimRBC)
                    nextRecordIndex = nextRecordIndex + Record.Length;
                else
                    nextRecordIndex = nextRecordIndex + Record.Length + 1;
            }
        }

        protected bool FilterRecord(string record)
        {
            if (record.Contains(dataStart))
                return false;

            if (record.StartsWith("NEWBALANCE $"))
                return false;

            if (record.StartsWith("TimetoPay\n"))
                return false;

            if (record.StartsWith("RBC"))
                return false;

            if (record.StartsWith("**") || record.StartsWith("* *"))
                return false;

            return true;
        }

        protected void HackGetYears(string dates)
        {
            // This is pretty nasty, could use some cleanup.
            StringBuilder startYear = new StringBuilder();
            StringBuilder startDay = new StringBuilder();
            StringBuilder startMonth = new StringBuilder();

            StringBuilder endYear = new StringBuilder();
            StringBuilder endDay = new StringBuilder();
            StringBuilder endMonth = new StringBuilder();

            int beforeYear = -1;
            int i = startEndStart.Length;
            HackGetMonth(dates, ref i, startMonth);
            HackSkipSpaces(dates, ref i);
            HackGetDay(dates, ref i, startDay);
            HackSkipSpaces(dates, ref i);
            beforeYear = i;
            HackGetYear(dates, ref i, startYear);
            if (startYear.Length != 4)
                i = beforeYear;
            HackSkipSpaces(dates, ref i);

            i = dates.IndexOf("TO", i) + 2; // Length of "TO"
            HackGetMonth(dates, ref i, endMonth);
            HackSkipSpaces(dates, ref i);
            HackGetDay(dates, ref i, endDay);
            HackSkipSpaces(dates, ref i);
            beforeYear = i;
            HackGetYear(dates, ref i, endYear);
            if (endYear.Length != 4)
                i = beforeYear;
            HackSkipSpaces(dates, ref i);

            if (startYear.Length != 4 && endYear.Length != 4)
                throw new Exception("FATAL ERROR: startyear AND endYear BOTH don't have a length of 4!");

            // If one's missing a year, assume we're in the same year.
            if (startYear.Length != 4)
                startYear = endYear;
            if (endYear.Length != 4)
                endYear = startYear;

            StartDate = new DateTime(int.Parse(startYear.ToString()), DateTime.ParseExact(startMonth.ToString(), "MMM", CultureInfo.InvariantCulture).Month, int.Parse(startDay.ToString()));
            EndDate = new DateTime(int.Parse(endYear.ToString()), DateTime.ParseExact(endMonth.ToString(), "MMM", CultureInfo.InvariantCulture).Month, int.Parse(endDay.ToString()));
        }

        protected static void HackGetMonth(string dates, ref int i, StringBuilder sb)
        {
            int start = i;
            int count = 0;
            // Assume we've got 3 letters, always.
            for (i = start; i < dates.Length; i++)
            {
                if (char.IsLetter(dates[i]))
                {
                    sb.Append(dates[i]);
                    count++;
                }

                if (count == 3)
                {
                    i++;
                    break;
                }
            }
        }

        protected static void HackSkipSpaces(string dates, ref int i)
        {
            // Advance over any spaces
            while (i < dates.Length && !char.IsLetterOrDigit(dates[i]))
            {
                i++;
            }
        }

        protected static void HackGetDay(string dates, ref int i, StringBuilder sb)
        {
            int start = i;
            int count = 0;
            // Get any numeric digits
            for (i = start; i < dates.Length; i++)
            {
                if (char.IsDigit(dates[i]))
                {
                    sb.Append(dates[i]);
                    count++;
                }
                else if (count > 0 && !char.IsDigit(dates[i]))
                {
                    break;
                }

                if (count == 2)
                {
                    i++;
                    break;
                }
            }
        }

        protected static void HackGetYear(string dates, ref int i, StringBuilder sb)
        {
            int start = i;
            int count = 0;
            // Get any numeric digits
            for (i = start; i < dates.Length; i++)
            {
                if (char.IsDigit(dates[i]))
                {
                    sb.Append(dates[i]);
                    count++;
                }
                else if (count > 0 && !char.IsDigit(dates[i]))
                {
                    break;
                }

                if (count == 4)
                    break;
            }
        }

        // This assumes we don't have more than 1 month difference on the same statement.
        protected DateTime GetBiasedYearDate(string Month, string Day)
        {
            int iMonth = DateTime.ParseExact(Month.ToString(), "MMM", CultureInfo.InvariantCulture).Month;

            if (!(DateTime.Compare(StartDate, EndDate) > 0)) // startDate is NOT LATER than endDate (IS EARLIER OR SAME)
            {
                if (iMonth == StartDate.Month)
                    return new DateTime(StartDate.Year, iMonth, int.Parse(Day.ToString()));
                else if (iMonth == EndDate.Month)
                    return new DateTime(EndDate.Year, iMonth, int.Parse(Day.ToString()));
                else
                    throw new Exception("FATAL ERRROR: Date range spans more than 1 month on statement!");
            }
            else
            {
                throw new Exception("FATAL ERRROR: startDate is NOT earlier than endDate!");
            }
        }

        public override void GetTransactions(List<Transaction> transactions)
        {
            foreach (string record in records)
            {
                string amt = record.Substring(record.LastIndexOf("$"));
                decimal amount = decimal.Parse(amt, NumberStyles.Currency);

                // This is a hack due to issues with whitespace detection. We're just going to read 3 letters, and 1-2 numbers for each date, with an indeterminate number of spaces between.
                string dates = record.Substring(0, 13);

                StringBuilder tMonth = new StringBuilder();
                StringBuilder tDay = new StringBuilder();
                StringBuilder pMonth = new StringBuilder();
                StringBuilder pDay = new StringBuilder();

                int index = 0;

                HackSkipSpaces(dates, ref index);
                HackGetMonth(dates, ref index, tMonth);
                HackSkipSpaces(dates, ref index);
                HackGetDay(dates, ref index, tDay);
                HackSkipSpaces(dates, ref index);
                HackGetMonth(dates, ref index, pMonth);
                HackSkipSpaces(dates, ref index);
                HackGetDay(dates, ref index, pDay);
                HackSkipSpaces(dates, ref index);

                string description = record.Substring(index + 2, record.Length - (index + 2 + amt.Length));

                Transaction t = new Transaction(GetBiasedYearDate(tMonth.ToString(), tDay.ToString()),
                    GetBiasedYearDate(pMonth.ToString(), pDay.ToString()), amount, description);

                transactions.Add(t);
            }
        }
    }
}
