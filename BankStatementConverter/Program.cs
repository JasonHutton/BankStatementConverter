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

using BankStatementConverter.Statements;
using BankStatementConverter.Transactions;

namespace BankStatementConverter
{

    class Program
    {
        static void Main(string[] args)
        {
            List<Transaction> transactions = new List<Transaction>();
            
            string[] files = Directory.GetFiles(".", "*.pdf", SearchOption.TopDirectoryOnly);

            foreach (string file in files)
            {
                RBCStatement statement = new RBCStatement();
                List<String> records = new List<String>();
                string data = ReadPdfFile(file);
                statement.GetRecords(data);
                statement.GetTransactions(transactions);
            }

            using (StreamWriter file = new StreamWriter("./Output.txt"))
            {
                foreach (Transaction t in transactions)
                {
                    file.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}", t.transactionDate, t.postingDate, Regex.Replace(t.description, @"\t|\n|\r", ""), t.amount));
                }
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
