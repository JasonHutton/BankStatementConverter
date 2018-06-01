using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Globalization;

using BankStatementConverter.Transactions;

namespace BankStatementConverter.Statements
{
    public abstract class Statement
    {
        public int RecordHeaderIndex;
        public String RecordHeader;
        public int FirstRecordIndex;
        public int LastRecordIndex;
        public DateTime StartDate;
        public DateTime EndDate;

        public List<String> records;

        public Statement()
        {
            this.records = new List<String>();
        }

        public abstract void GetRecords(String data);
        public abstract void GetTransactions(List<Transaction> transactions);
    }
}
