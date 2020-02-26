using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AsyncLockAndFixes
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }


        private async void button1_Click(object sender, EventArgs e)
        {
            //1. calling GetContentAsync
            var task = GetContentAsync();

            //fix 2: var task = Task<string>.Run(GetContentAsync);

            Debug.WriteLine($"Continuation:{Environment.CurrentManagedThreadId}");

            //4. .Result(or GetAwait().GetResult()) which waiting for GetContentAsync to complete. 
            //OOPS: DEADLOCK!!!
            //task.Result waiting the http.GetStringAsync complete and return;
            //GetContentAsync wait button1_Click release the synchronization context;
            var content = await task;

            this.label1.Text = content;
        }

        public async Task<string> GetContentAsync()
        {
            //fix 1: var syncContext = WindowsFormsSynchronizationContext.Current;
            //fix 1: WindowsFormsSynchronizationContext.SetSynchronizationContext(null);

            var http = new HttpClient();
            //2. automatic capture synchronization context   :: auto capture caused the issue.
            //3. due to await applied, yield thread to caller(button1_Click)
            var result = await http.GetStringAsync("http://www.imzjy.com");
            //fix 3: var result  = await http.GetStringAsync("http://www.imzjy.com").ConfigureAwait(continueOnCapturedContext: false);

            //fix 1: WindowsFormsSynchronizationContext.SetSynchronizationContext(syncContext);

            var first50 = result.Substring(0, 50);
            return first50;


            //solution1: do not let http.GetStringAsync capture the synchronization context, set to null then restore. cons: UI unresponsive
            //solution2: run GetContentAsync in a thread pool with Task.Run, so that it will capture thread pool syncrhonization context, instead of UI sychronization context. cons: UI unresponsive
            //solution3: do not capture synchronmization context with .ConfigureAwait(continueOnCapturedContext: false)
            //solution4: async calling chain all the way.
        }
    }
}
