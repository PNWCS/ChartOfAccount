using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;
using QBFC16Lib;                 // QuickBooks session and QBFC API
using Serilog;
using QB_CoA_Lib;               // <-- Replace with your actual library namespace
using static QB_CoA_Test.CommonMethods;  // Helpers (EnsureLogFileClosed, etc.)
using QB_CoA_Test;

namespace QB_CoA_Test
{
    [Collection("Sequential Tests")]
    public class CoAAdderTests
    {
        [Fact]
        public void AddMultipleCoAs_ThenQueryByListID_TheyShouldExistInQuickBooks()
        {
            // 1) Ensure Serilog has released file access before deleting old logs.
            EnsureLogFileClosed();
            DeleteOldLogFiles();
            ResetLogger();

            // 2) Build a list of random ChartOfAccount objects to add.
            const int ACCOUNTS_TO_ADD = 5;
            var accountsToAdd = new List<ChartOfAccount>();
            for (int i = 0; i < ACCOUNTS_TO_ADD; i++)
            {
                string randomName = "TestAccount_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                string randomAccountNumber = "ACC-" + Random.Shared.Next(100, 999);
                string accountType = "Expense";  // Could be Bank, Income, etc.
                string randomCompanyID = new Random().Next(1000, 9999).ToString(); // Some fake "Company ID"

                var newCoA = new ChartOfAccount(accountType, randomAccountNumber, randomName)
                {
                    // We store the "Company ID" in the .CompanyID property
                    CompanyID = randomCompanyID
                };

                accountsToAdd.Add(newCoA);
            }

            // 3) Call the CoAAdder under test to add those CoAs into QuickBooks.
            using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
            {
                //var coAAdder = new CoAAdder(qbSession); // Adjust ctor if needed
                //coAAdder.AddCoAs(accountsToAdd);
                CoAAdder.AddCustomers(accountsToAdd);

            }

            // 4) Verify each added CoA has a QB_ID. Fail if any are missing.
            foreach (var acct in accountsToAdd)
            {
                Assert.False(
                    string.IsNullOrWhiteSpace(acct.QB_ID),
                    $"CoA named '{acct.Name}' did not receive a QB_ID after AddCoAs call."
                );
            }

            // 5) Open a QB session again to query each newly added account by its QB_ID.
            using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
            {
                foreach (var acct in accountsToAdd)
                {
                    var qbRet = QueryAccountByListID(qbSession, acct.QB_ID);
                    Assert.NotNull(qbRet);  // If null, we failed to find it in QB.
<<<<<<< HEAD

=======
                    
>>>>>>> bd06d5c776c93d6ab706cd6c0708af6ebbb42ba4
                    // Optionally, you can also assert that fields match what was sent:
                    Assert.Equal(acct.Name, qbRet.Name.GetValue());
                    Assert.Equal(acct.AccountNumber, qbRet.AccountNumber?.GetValue());
                    Assert.Equal(ConvertStringToAccountType(acct.AccountType), qbRet.AccountType.GetValue());
                    Assert.Equal(acct.CompanyID, qbRet.Desc?.GetValue());
                }

                // 6) Cleanup: delete test accounts from QuickBooks so we don't clutter the company file.
                foreach (var acct in accountsToAdd.Where(a => !string.IsNullOrWhiteSpace(a.QB_ID)))
                {
                    DeleteAccount(qbSession, acct.QB_ID);
                }
            }

            // Give QB a small breather to finalize changes if needed.
            Thread.Sleep(3000);

            // 7) Ensure logs are flushed and verify them if desired.
            EnsureLogFileClosed();
            string latestLogFile = GetLatestLogFile();
            EnsureLogFileExists(latestLogFile);

            string logContents = File.ReadAllText(latestLogFile);
            Assert.Contains("CoAAdder Initialized", logContents); // Example expected log text
            Assert.Contains("CoAAdder Completed", logContents);   // Example expected log text
        }

        /// <summary>
        /// Queries QuickBooks directly by ListID (i.e., the QB's unique ListID).
        /// This does NOT use the Reader code—it's a direct QBFC query.
        /// Returns an IAccountRet if found, or null if not found.
        /// </summary>
        private IAccountRet? QueryAccountByListID(QuickBooksSession qbSession, string listID)
        {
            IMsgSetRequest requestMsgSet = qbSession.CreateRequestSet();
            requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

            // Build the AccountQueryRq
            IAccountQuery accountQueryRq = requestMsgSet.AppendAccountQueryRq();
            accountQueryRq.ORAccountListQuery.ListIDList.Add(listID);

            IMsgSetResponse responseMsgSet = qbSession.SendRequest(requestMsgSet);
            IResponseList responseList = responseMsgSet.ResponseList;
            if (responseList == null || responseList.Count == 0)
            {
                return null; // No response
            }

            IResponse response = responseList.GetAt(0);
            if (response.StatusCode != 0)
            {
                // If the response status is not 0, QB might not have found anything or had an error
                Debug.WriteLine($"AccountQuery status code: {response.StatusCode}, message: {response.StatusMessage}");
                return null;
            }
<<<<<<< HEAD

=======
>>>>>>> bd06d5c776c93d6ab706cd6c0708af6ebbb42ba4
            if (response.Detail is IAccountRet accountRet)
            {
                return accountRet;
            }
            else if (response.Detail is IAccountRetList accountRetList && accountRetList.Count > 0)
            {
                return accountRetList.GetAt(0);
            }

            return null;

            // Extract the AccountRet object if present
            //IAccountRetList accountRetList = response.Detail as IAccountRetList;
            //if (accountRetList == null) return null;

            //// We expect exactly one match if the ListID is correct
            //return (IAccountRet?)accountRetList;
<<<<<<< HEAD
=======

>>>>>>> bd06d5c776c93d6ab706cd6c0708af6ebbb42ba4
        }

        /// <summary>
        /// Converts a string-based account type into a QBFC ENAccountType.
        /// Adjust your mapping as needed (Bank, Expense, Income, etc.).
        /// </summary>
        private ENAccountType ConvertStringToAccountType(string accountType)
        {
            return accountType.ToLower() switch
            {
<<<<<<< HEAD
                "bank" => ENAccountType.atBank,
                "expense" => ENAccountType.atExpense,
                "income" => ENAccountType.atIncome,
                _ => ENAccountType.atOtherAsset
=======
                "bank"    => ENAccountType.atBank,
                "expense" => ENAccountType.atExpense,
                "income"  => ENAccountType.atIncome,
                _         => ENAccountType.atOtherAsset
>>>>>>> bd06d5c776c93d6ab706cd6c0708af6ebbb42ba4
            };
        }

        /// <summary>
        /// Deletes an account from QuickBooks by ListID.
        /// </summary>
        private void DeleteAccount(QuickBooksSession qbSession, string listID)
        {
            IMsgSetRequest requestMsgSet = qbSession.CreateRequestSet();
            requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

            IListDel listDelRq = requestMsgSet.AppendListDelRq();
            listDelRq.ListDelType.SetValue(ENListDelType.ldtAccount);
            listDelRq.ListID.SetValue(listID);

            IMsgSetResponse responseMsgSet = qbSession.SendRequest(requestMsgSet);
            WalkListDelResponse(responseMsgSet, listID);
        }

        /// <summary>
        /// Handles the response of the Delete request, throwing if not successful.
        /// </summary>
        private void WalkListDelResponse(IMsgSetResponse responseMsgSet, string listID)
        {
            IResponseList responseList = responseMsgSet.ResponseList;
            if (responseList == null || responseList.Count == 0) return;

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
