using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Text.RegularExpressions;

using iTextSharp;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using iTextSharp.text.pdf.interfaces;

using System.Globalization;

namespace BankStatementConverter
{
    class Transaction
    {
        private DateTime transactionDate;
        private DateTime postingDate;
        private String description;
        private decimal amount;
        //private String extraData;

        public Transaction(DateTime tDate, DateTime pDate, decimal amount, String description)
        {
            this.transactionDate = tDate;
            this.postingDate = pDate;
            this.amount = amount;
            this.description = description;
        }
    }

    class Statement
    {
        public int RecordHeaderIndex;
        public String RecordHeader;
        public int FirstRecordIndex;
        public int LastRecordIndex;
        public DateTime StartDate;
        public DateTime EndDate;
    }

    class Program
    {
        static string dataStart = "TRANSACTION POSTING\nACTIVITY DESCRIPTION AMOUNT ($)\nDATE DATE\n";
        static string startEndStart = "STATEMENTFROM";


        public static void GetRecords(ref Statement statement, ref List<String> records, String data)
        {
            int recordHeaderIndex = 0;
            statement.RecordHeaderIndex = data.IndexOf(dataStart, recordHeaderIndex);
            statement.RecordHeader = data.Substring(statement.RecordHeaderIndex, dataStart.Length);
            statement.FirstRecordIndex = statement.RecordHeaderIndex + statement.RecordHeader.Length;
            statement.LastRecordIndex = 0;

            // This is a hack due to issues with whitespace detection. We're just going to read this pretty fixedly.
            int ses = data.IndexOf(startEndStart);
            int ses2 = data.IndexOf("\n", ses + startEndStart.Length + 1);
            string datex = data.Substring(ses, ses2 - ses);

            HackGetYears(datex, ref statement.StartDate, ref statement.EndDate);

            int nextRecordIndex = statement.FirstRecordIndex;
            
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
                    statement.LastRecordIndex = nextRecordIndex;
                }

                // Since in the lacking-delimiter scenario we don't need to advance over it.
                if (bTrimRBC)
                    nextRecordIndex = nextRecordIndex + Record.Length;
                else
                    nextRecordIndex = nextRecordIndex + Record.Length + 1;
            }
        }

        public static bool FilterRecord(string record)
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

        private static void HackGetYears(string dates, ref DateTime start, ref DateTime end)
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
            HackGetMonth(dates, ref i, ref startMonth);
            HackSkipSpaces(dates, ref i);
            HackGetDay(dates, ref i, ref startDay);
            HackSkipSpaces(dates, ref i);
            beforeYear = i;
            HackGetYear(dates, ref i, ref startYear);
            if (startYear.Length != 4)
                i = beforeYear;
            HackSkipSpaces(dates, ref i);

            i = dates.IndexOf("TO", i) + 2; // Length of "TO"
            HackGetMonth(dates, ref i, ref endMonth);
            HackSkipSpaces(dates, ref i);
            HackGetDay(dates, ref i, ref endDay);
            HackSkipSpaces(dates, ref i);
            beforeYear = i;
            HackGetYear(dates, ref i, ref endYear);
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

            start = new DateTime(int.Parse(startYear.ToString()), DateTime.ParseExact(startMonth.ToString(), "MMM", CultureInfo.InvariantCulture).Month, int.Parse(startDay.ToString()));
            end = new DateTime(int.Parse(endYear.ToString()), DateTime.ParseExact(endMonth.ToString(), "MMM", CultureInfo.InvariantCulture).Month, int.Parse(endDay.ToString()));
        }

        private static void HackGetMonth(string dates, ref int i, ref StringBuilder sb)
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

        private static void HackSkipSpaces(string dates, ref int i)
        {
            // Advance over any spaces
            while (i < dates.Length && !char.IsLetterOrDigit(dates[i]))
            {
                i++;
            }
        }

        private static void HackGetDay(string dates, ref int i, ref StringBuilder sb)
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

        private static void HackGetYear(string dates, ref int i, ref StringBuilder sb)
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

        public static void GetTransactions(Statement statement, ref List<Transaction> transactions, ref List<String> records)
        {
            foreach(string record in records)
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
                HackGetMonth(dates, ref index, ref tMonth);
                HackSkipSpaces(dates, ref index);
                HackGetDay(dates, ref index, ref tDay);
                HackSkipSpaces(dates, ref index);
                HackGetMonth(dates, ref index, ref pMonth);
                HackSkipSpaces(dates, ref index);
                HackGetDay(dates, ref index, ref pDay);
                HackSkipSpaces(dates, ref index);

                string description = record.Substring(index + 2, record.Length - (index + 2 + amt.Length));

                Transaction t = new Transaction(GetBiasedYearDate(statement.StartDate, statement.EndDate, tMonth.ToString(), tDay.ToString()),
                    GetBiasedYearDate(statement.StartDate, statement.EndDate, pMonth.ToString(), pDay.ToString()), amount, description);

                transactions.Add(t);
            }
        }

        // This assumes we don't have more than 1 month difference on the same statement.
        private static DateTime GetBiasedYearDate(DateTime startDate, DateTime endDate, string Month, string Day)
        {
            int iMonth = DateTime.ParseExact(Month.ToString(), "MMM", CultureInfo.InvariantCulture).Month;

            if (!(DateTime.Compare(startDate, endDate) > 0)) // startDate is NOT LATER than endDate (IS EARLIER OR SAME)
            {
                if (iMonth == startDate.Month)
                    return new DateTime(startDate.Year, iMonth, int.Parse(Day.ToString()));
                else if (iMonth == endDate.Month)
                    return new DateTime(endDate.Year, iMonth, int.Parse(Day.ToString()));
                else
                    throw new Exception("FATAL ERRROR: Date range spans more than 1 month on statement!");
            }
            else
            {
                throw new Exception("FATAL ERRROR: startDate is NOT earlier than endDate!");
            }
        }

        static void Main(string[] args)
        {
            List<Transaction> transactions = new List<Transaction>();
            
            string[] files = Directory.GetFiles(".", "*.pdf", SearchOption.TopDirectoryOnly);

            foreach (string file in files)
            {
                Statement statement = new Statement();
                List<String> records = new List<String>();
                string data = ReadPdfFile(file);
                GetRecords(ref statement, ref records, data);
                GetTransactions(statement, ref transactions, ref records);
            }

            Console.Write("Press Any Key to Continue...");
            Console.ReadKey();
        }

        public static string ReadPdfFile(string fileName)
        {
            StringBuilder text = new StringBuilder();

            if (File.Exists(fileName))
            {
                PdfReader pdfReader = new PdfReader(fileName);

                for (int page = 1; page <= pdfReader.NumberOfPages; page++)
                {
                    // Todo: Try LocationTextExtractionStrategy instead, or see about overriding SimpleTextExtractionStrategy() with different space between characters.
                    ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                    string currentText = PdfTextExtractor.GetTextFromPage(pdfReader, page, strategy);

                    currentText = Encoding.UTF8.GetString(ASCIIEncoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(currentText)));
                    text.Append(currentText);
                }
                pdfReader.Close();
            }
            return text.ToString();
        }
    }
}
