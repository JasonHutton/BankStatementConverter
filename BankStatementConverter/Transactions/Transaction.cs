using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankStatementConverter.Transactions
{
    public class Transaction
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
}
