using Serilog;
using static QB_CoA_Lib.ChartOfAccount;

namespace QB_CoA_Lib
{
    public class CoAComparator
    {
        public static List<ChartOfAccount> CompareAccounts(List<ChartOfAccount> companyAccounts)
        {
            Log.Information("CoAComparator Initialized.");

            // Read Chart of Accounts from QuickBooks
            List<ChartOfAccount> qbAccounts = CoAReader.QueryAllCoAs();

            // Create dictionaries using Name for faster lookup (case-insensitive)
            var qbAccountDict = qbAccounts.ToDictionary(a => a.Name.Trim().ToLower(), a => a);
            var companyAccountDict = companyAccounts.ToDictionary(a => a.Name.Trim().ToLower(), a => a);

            var qbAccountNumberSet = new HashSet<string>(qbAccounts.Select(a => a.AccountNumber));

            List<ChartOfAccount> newAccountsToAdd = new List<ChartOfAccount>();



            foreach (var companyAccount in companyAccounts.ToList())
            {
                if (string.IsNullOrEmpty(companyAccount.Name))
                {
                    Log.Warning($"Company Name missing for company account with Account Number {companyAccount.AccountNumber}, skipping.");
                    continue;
                }

                companyAccount.Status = ChartOfAccountStatus.Unchanged;

                if (qbAccountDict.TryGetValue(companyAccount.Name.Trim().ToLower(), out var qbAccount))
                {
                    // Account exists in both, you can compare further if needed (e.g., Name or AccountNumber)
                    if (qbAccount.Name.Trim().ToLower() == companyAccount.Name.Trim().ToLower() && qbAccount.AccountType == companyAccount.AccountType)
                    {
                        companyAccount.Status = ChartOfAccountStatus.Unchanged;
                        Log.Information($"Account {companyAccount.Name} is Unchanged.");
                    }
                    else
                    {
                        Console.WriteLine($"qbaccount name :{qbAccount.Name.Trim().ToLower()}|  qbaccount type:{qbAccount.AccountType}");
                        Console.WriteLine($"companyaccount name :{companyAccount.Name.Trim().ToLower()}|  companyAccount type:{companyAccount.AccountType}");

                        companyAccount.Status = ChartOfAccountStatus.Different;
                        Log.Information($"Account {companyAccount.Name} is Different.");
                    }
                }
                else
                {
                    // Before adding, check if account number already exists in QB
                    if (!qbAccountNumberSet.Contains(companyAccount.AccountNumber))
                    {
                        companyAccount.Status = ChartOfAccountStatus.Added;
                        newAccountsToAdd.Add(companyAccount);
                        Log.Information($"Account {companyAccount.Name} is New and will be added.");
                        continue;

                    }


                    Log.Warning($"Account Number {companyAccount.AccountNumber} for {companyAccount.Name} already exists in QB, skipping addition.");

                    continue;


                }
            }


            // Check for accounts that exist in QB but missing from Company data
            foreach (var qbAccount in qbAccounts)
            {
                if (string.IsNullOrEmpty(qbAccount.Name))
                {
                    Log.Warning($"Name missing for QB account with AccountNumber {qbAccount.AccountNumber}, skipping.");
                    continue;
                }

                var nameKey = qbAccount.Name.Trim().ToLower();

                if (!companyAccountDict.ContainsKey(nameKey))
                {
                    qbAccount.Status = ChartOfAccountStatus.Missing;
                    Log.Information($"Account '{qbAccount.Name}' exists in QB but missing from Company Data.");
                }
            }

            // Add new accounts to QuickBooks
            if (newAccountsToAdd.Count > 0)
            {
                CoAAdder.AddCustomers(newAccountsToAdd);
                foreach (var addedTerm in newAccountsToAdd)
                {
                    if (companyAccountDict.TryGetValue(addedTerm.Name.Trim().ToLower(), out var companyTerm))
                    {
                        companyTerm.Status = addedTerm.Status;
                        Log.Information($"Term {companyTerm.Name} is Added.");
                    }
                }
            }

            Log.Information("CoAComparator Completed.");

            // Merge results: prioritize company accounts
            Dictionary<string, ChartOfAccount> mergedAccountsDict = new Dictionary<string, ChartOfAccount>();

            foreach (var acct in qbAccountDict.Values)
                mergedAccountsDict[acct.Name.Trim().ToLower()] = acct; // Add all QB accounts

            foreach (var acct in companyAccountDict.Values)
                mergedAccountsDict[acct.Name.Trim().ToLower()] = acct; // Overwrite with company accounts

            return mergedAccountsDict.Values.ToList();
        }
    }
}

