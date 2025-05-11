namespace QB_CoA_Lib
{
    public class ChartOfAccount
    {
        public string AccountType { get; set; }
        public string AccountNumber { get; set; }
        public string Name { get; set; }
        public string? CompanyID { get; set; }
        public string QB_ID { get; set; }

        public ChartOfAccountStatus Status { get; set; }

        public ChartOfAccount(string accountType, string accountNumber, string name)
        {
            AccountType = accountType;
            AccountNumber = accountNumber;
            Name = name;
            Status = ChartOfAccountStatus.Unknown;


        }

        public enum ChartOfAccountStatus
        {
            Unchanged,// Exists in both but no changes
            Unknown, // When first read from the company excel or QB
            Different, // Exists in both but name is different
            Added,     // Newly added to QB
            FailedToAdd, // If adding to QB failed
            Missing    // Exists in QB but not in the company file
        }
    }
}
