using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.TestManagement.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.TestManagement.Common;

namespace AccessTFS
{
    class Program
    {
        static void Main(string[] args)
        {
            string serverurl = "http://vstfpg03:8080/tfs/MSLP";
            string project = "MLX";
            var proj = GetProject(serverurl, project);

            var testPlan = proj.TestPlans.Query("select * from TestPlan where planname = 'Sprint 9' ").First();
            var testRuns = proj.TestRuns.Query("select * from TestRun").Where(
                    tr=>tr.TestPlanId==testPlan.Id 
                    && tr.IsAutomated 
                    && tr.Title.Contains("MlxTestFramework_")
                    && tr.DateStarted.AddDays(3)>DateTime.Now);

            var results = testRuns.First().QueryResults(true);
            var failed = results.Where(r => r.Outcome == TestOutcome.Failed);
        }
        static ITestManagementTeamProject GetProject(string serverUrl,
          string project)
        {
            TfsTeamProjectCollection tfs = new TfsTeamProjectCollection(TfsTeamProjectCollection.GetFullyQualifiedUriForName(serverUrl),
                new System.Net.NetworkCredential("Redmond\\elserv", "Lsgtfs!thirteen!@"));
            tfs.EnsureAuthenticated();
             ITestManagementService tms = tfs.GetService<ITestManagementService>();
            return tms.GetTeamProject(project);
        }
    }
}
