using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RecordTransaction.Models
{
    public class Transaction
    {
        public string MsgCenter { get; set; }

        public string MsgSource { get; set; }

        public string TransactionType { get; set; }

        public string Project { get; set; }

        public string Amount { get; set; }

        public string PaidTo { get; set; }

        public string Payee { get; set; }

        public string Timestamp { get; set; }

        public string ToCSV()
        {
            return MsgCenter + "," + MsgSource + "," + TransactionType + "," + Project + "," + Amount + "," + PaidTo + "," + Payee + "," + Timestamp;
        }
    }
}
