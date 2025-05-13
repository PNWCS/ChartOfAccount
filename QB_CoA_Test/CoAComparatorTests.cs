using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;
using Serilog;
using QB_CoA_Lib;               // ChartOfAccount, ChartOfAccountComparator, etc.
using QBFC16Lib;                // QuickBooks Desktop SDK
using static QB_CoA_Test.CommonMethods;
using static QB_CoA_Lib.ChartOfAccount;


namespace QB_CoA_Test
{
    /// <summary>
    /// Tests must run sequentially because QuickBooks Desktop
    /// allows only one session at a time.
    /// </summary>
    [Collection("Sequential Tests")]
    public class ChartOfAccountComparatorTests
    {
        [Fact]
        public void CompareChartOfAccounts_InMemoryScenario_And_Verify_Logs()
        {
            //------------------------------------------------------------------
            // ①  Build five entirely new in-memory Chart-of-Account objects
            //------------------------------------------------------------------
            const string DEFAULT_ACCOUNT_TYPE = "Expense";
            var initialAccounts = new List<ChartOfAccount>();
            var rand = new Random();

            EnsureLogFileClosed();
            DeleteOldLogFiles();
            ResetLogger();

            for (int i = 0; i < 5; i++)
            {
                string acctNumber = rand.Next(10000, 99999).ToString();
                string acctName = $"TestCoA_{Guid.NewGuid():N}".Substring(0, 16);
                string companyId = $"CID_{Guid.NewGuid():N}".Substring(0, 8);

                initialAccounts.Add(new ChartOfAccount(DEFAULT_ACCOUNT_TYPE, acctNumber, acctName)
                {
                    CompanyID = companyId   //  Used as the business-key when comparing
                });
            }

            List<ChartOfAccount>? firstCompareResult = null;
            List<ChartOfAccount>? secondCompareResult = null;

            try
            {
                //------------------------------------------------------------------
                // ②  First compare → every account should be *Added* to QB
                //------------------------------------------------------------------

                firstCompareResult = CoAComparator.CompareAccounts(initialAccounts);

                foreach (var acct in firstCompareResult
                                     .Where(a => initialAccounts.Any(x => x.Name.Trim().ToLower() == a.Name.Trim().ToLower())))

                {
                    Assert.Equal(ChartOfAccountStatus.Added, acct.Status);
                }

                //------------------------------------------------------------------
                // ③  Mutate list to trigger *Missing* & *Different*
                //------------------------------------------------------------------
                var updatedAccounts = new List<ChartOfAccount>(initialAccounts);

                var acctToRemove = updatedAccounts[0];           // → Missing
                var acctToRename = updatedAccounts[1];           // → Different

                updatedAccounts.Remove(acctToRemove);
                //acctToRename.Name += "_Mod";
                acctToRename.AccountType = "Income";  // Different from original "Expense"


                //------------------------------------------------------------------
                // ④  Second compare → expect Missing, Different, Unchanged
                //------------------------------------------------------------------

                secondCompareResult = CoAComparator.CompareAccounts(updatedAccounts);
                // !--------change here
                //var secondDict      = secondCompareResult.ToDictionary(a => a.CompanyID);
                var secondDict = secondCompareResult.ToDictionary(a => a.Name.Trim().ToLower());

                // Missing
                Assert.True(secondDict.ContainsKey(acctToRemove.Name.Trim().ToLower()));
                Assert.Equal(ChartOfAccountStatus.Missing, secondDict[acctToRemove.Name.Trim().ToLower()].Status);

                // Different
                Assert.True(secondDict.ContainsKey(acctToRename.Name.Trim().ToLower()));
                Assert.Equal(ChartOfAccountStatus.Different, secondDict[acctToRename.Name.Trim().ToLower()].Status);

                // Unchanged
                var unaffectedIds = updatedAccounts
                                    .Select(a => a.Name.Trim().ToLower())
                                    .Except(new[] { acctToRename.Name.Trim().ToLower() });

                foreach (var id in unaffectedIds)
                {
                    Assert.Equal(ChartOfAccountStatus.Unchanged, secondDict[id].Status);
                }
            }
            finally
            {
                //------------------------------------------------------------------
                // ⑤  Clean up – delete everything we added to QuickBooks
                //------------------------------------------------------------------
                var allAddedAccounts = firstCompareResult?
                                       .Where(a => !string.IsNullOrWhiteSpace(a.QB_ID))
                                       .ToList();

                if (allAddedAccounts is { Count: > 0 })
                {
                    using var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME);

                    foreach (var acct in allAddedAccounts)
                    {
                        DeleteAccount(qbSession, acct.QB_ID);
                    }
                }
            }

            //----------------------------------------------------------------------
            // ⑥  Log verification – make sure our comparator logged what we expect
            //----------------------------------------------------------------------
            EnsureLogFileClosed();
            string logFile = GetLatestLogFile();
            EnsureLogFileExists(logFile);

            string logs = File.ReadAllText(logFile);

            Assert.Contains("ChartOfAccountComparator Initialized", logs);
            Assert.Contains("ChartOfAccountComparator Completed", logs);

            void AssertLogsFor(IEnumerable<ChartOfAccount>? accts)
            {
                if (accts == null) return;

                foreach (var acct in accts)
                {
                    string expected = $"Account {acct.Name} is {acct.Status}.";
                    Assert.Contains(expected, logs);
                }
            }

            AssertLogsFor(firstCompareResult);
            AssertLogsFor(secondCompareResult);
        }

        // ---------------------------------------------------------------------
        // QuickBooks-specific helper to delete an Account list element cleanly
        // ---------------------------------------------------------------------
        private static void DeleteAccount(QuickBooksSession qbSession, string listID)
        {
            IMsgSetRequest req = qbSession.CreateRequestSet();
            IListDel delRq = req.AppendListDelRq();

            delRq.ListDelType.SetValue(ENListDelType.ldtAccount);
            delRq.ListID.SetValue(listID);

            IMsgSetResponse rsp = qbSession.SendRequest(req);
            WalkListDelResponse(rsp, listID);
        }

        private static void WalkListDelResponse(IMsgSetResponse rsp, string listID)
        {
            IResponseList list = rsp.ResponseList;
            if (list == null || list.Count == 0) return;

            IResponse resp = list.GetAt(0);
            if (resp.StatusCode != 0)
            {
                Debug.WriteLine($"Error deleting account (ListID={listID}): {resp.StatusMessage}");
            }
        }
    }
}
