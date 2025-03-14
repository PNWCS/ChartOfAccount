using System.Diagnostics;
using Serilog;
using QB_CoA_Lib;  // <-- Replace with your actual library namespace
using QBFC16Lib;              // QuickBooks session and API interaction
using static QB_CoA_Test.CommonMethods;  // <-- Adjust if you store test helpers in a different namespace
using QB_CoA_Test;
using System.ComponentModel.Design;

namespace QB_CoA_Test
{
    [Collection("Sequential Tests")]
    public class CoAReaderTests
    {
        [Fact]
        public void AddAndReadMultipleAccounts_FromQuickBooks_And_Verify_Logs()
        {
            const int ACCOUNTS_COUNT = 5;
            var accountsToAdd = new List<ChartOfAccount>();

            // 1) Ensure Serilog has released file access before deleting old logs.
            EnsureLogFileClosed();
            DeleteOldLogFiles();
            ResetLogger();

            // 2) Build a list of random ChartOfAccount objects.
            //    We’ll use "Description" to store the company’s ID and "AccountNumber" for account numbering.
            for (int i = 0; i < ACCOUNTS_COUNT; i++)
            {
                string randomName = "TestAccount_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                string randomAccountNumber = "ACC-" + new Random().Next(1000, 9999);
                int randomCompanyID = new Random().Next(1, 999);
                string accountType = "Bank"; // or "Expense", "Income", etc.

                var account = new ChartOfAccount
                {
                    Name = randomName,
                    AccountType = accountType,
                    AccountNumber = randomAccountNumber,
                    CompanyID = randomCompanyID // to store the company’s ID
                };

                accountsToAdd.Add(account);
            }

            // 3) Add these accounts directly to QuickBooks.
            using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
            {
                foreach (var acct in accountsToAdd)
                {
                    string qbID = AddAccount(
                        qbSession,
                        acct.Name,
                        acct.AccountType,
                        acct.AccountNumber,
                        acct.CompanyID
                    );
                    acct.QB_ID = qbID; // Store the returned QB ListID.
                }
            }

            // 4) Query QuickBooks to retrieve all accounts.
            var allQBAccounts = CoAReader.QueryAllCoAs();

            // 5) Verify that all added accounts are present in QuickBooks and match expected fields.
            foreach (var acct in accountsToAdd)
            {
                var matchingAccount = allQBAccounts.FirstOrDefault(a => a.QB_ID == acct.QB_ID);
                Assert.NotNull(matchingAccount);
                Assert.Equal(acct.Name, matchingAccount.Name);
                Assert.Equal(acct.AccountType, matchingAccount.AccountType);
                Assert.Equal(acct.AccountNumber, matchingAccount.AccountNumber);
                Assert.Equal(acct.CompanyID, matchingAccount.CompanyID);
                

            // 6) Cleanup: Delete the added accounts from QuickBooks.
            using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
            {
                foreach (var acct in accountsToAdd.Where(a => !string.IsNullOrEmpty(a.QB_ID)))
                {
                    DeleteAccount(qbSession, acct.QB_ID);
                }
            }

            // 7) Ensure logs are fully flushed before accessing them.
            EnsureLogFileClosed();

            // 8) Verify that a new log file exists.
            string logFile = GetLatestLogFile();
            EnsureLogFileExists(logFile);

            // 9) Read the log file content.
            string logContents = File.ReadAllText(logFile);

            // 10) Assert expected log messages exist.
            Assert.Contains("ChartOfAccountReader Initialized", logContents);
            Assert.Contains("ChartOfAccountReader Completed", logContents);

            // 11) Verify that each retrieved account was logged properly.
            foreach (var acct in accountsToAdd)
            {
                string expectedLogMessage = $"Successfully retrieved {acct.Name} from QB";
                Assert.Contains(expectedLogMessage, logContents);
            }
        }

        /// <summary>
        /// Adds a new account to QuickBooks via QBFC.
        /// </summary>
        private string AddAccount(
            QuickBooksSession qbSession,
            string name,
            string accountType,
            string accountNumber,
            int companyID
        )
        {
            IMsgSetRequest requestMsgSet = qbSession.CreateRequestSet();
            IAccountAddRq accountAddRq = requestMsgSet.AppendAccountAddRq();

            accountAddRq.Name.SetValue(name);
            accountAddRq.AccountType.SetValue(ConvertStringToAccountType(accountType));
            accountAddRq.AccountNumber.SetValue(accountNumber);
            accountAddRq.Desc.SetValue(companyID);

            IMsgSetResponse responseMsgSet = qbSession.SendRequest(requestMsgSet);
            return ExtractListIDFromResponse(responseMsgSet);
        }

        /// <summary>
        /// Converts a string-based account type (e.g., "Bank") into the appropriate QB enum.
        /// Adjust or expand for your environment.
        /// </summary>
        private ENAccountType ConvertStringToAccountType(string accountType)
        {
            return accountType.ToLower() switch
            {
                "bank" => ENAccountType.atBank,
                "expense" => ENAccountType.atExpense,
                "income" => ENAccountType.atIncome,
                _ => ENAccountType.atOtherAsset
            };
        }

        /// <summary>
        /// Extracts the newly created account's ListID from the QBFC response.
        /// </summary>
        private string ExtractListIDFromResponse(IMsgSetResponse responseMsgSet)
        {
            IResponseList responseList = responseMsgSet.ResponseList;
            if (responseList == null || responseList.Count == 0)
                throw new Exception("No response from AccountAddRq.");

            IResponse response = responseList.GetAt(0);
            if (response.StatusCode != 0)
                throw new Exception($"AccountAdd failed: {response.StatusMessage}");

            IAccountRet? accountRet = response.Detail as IAccountRet;
            if (accountRet == null)
                throw new Exception("No IAccountRet returned after adding Account.");

            return accountRet.ListID?.GetValue()
                ?? throw new Exception("ListID is missing in QuickBooks response.");
        }

        /// <summary>
        /// Deletes an account from QuickBooks by ListID.
        /// </summary>
        private void DeleteAccount(QuickBooksSession qbSession, string listID)
        {
            IMsgSetRequest requestMsgSet = qbSession.CreateRequestSet();
            IListDel listDelRq = requestMsgSet.AppendListDelRq();
            listDelRq.ListDelType.SetValue(ENListDelType.ldtAccount);
            listDelRq.ListID.SetValue(listID);

            IMsgSetResponse responseMsgSet = qbSession.SendRequest(requestMsgSet);
            WalkListDelResponse(responseMsgSet, listID);
        }

        private void WalkListDelResponse(IMsgSetResponse responseMsgSet, string listID)
        {
            IResponseList responseList = responseMsgSet.ResponseList;
            if (responseList == null || responseList.Count == 0)
                return;

            IResponse response = responseList.GetAt(0);
            if (response.StatusCode == 0 && response.Detail != null)
            {
                Debug.WriteLine($"Successfully deleted ChartOfAccount (ListID: {listID}).");
            }
            else
            {
                throw new Exception(
                    $"Error Deleting Account (ListID: {listID}): {response.StatusMessage}. " +
                    $"Status code: {response.StatusCode}"
                );
            }
        }
    }
}
