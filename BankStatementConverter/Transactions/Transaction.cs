using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankStatementConverter.Transactions
{
    public class Transaction
    {
        public DateTime transactionDate;
        public DateTime postingDate;
        public String description;
        public decimal amount;
        //private String extraData;

        public Transaction(DateTime tDate, DateTime pDate, decimal amount, String description)
        {
            this.transactionDate = tDate;
            this.postingDate = pDate;
            this.amount = amount;
            this.description = description;
        }
    }
}
