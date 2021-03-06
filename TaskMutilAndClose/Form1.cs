﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Windows.Forms;

namespace TaskMutilAndClose
{
    public partial class Form1 : Form
    {
        CancellationTokenSource tokenSource;
        CancellationToken token;

        public Form1()
        {
            InitializeComponent();
        
            tokenSource = new CancellationTokenSource();
            token = tokenSource.Token;

        }



        private void button1_Click(object sender, EventArgs ev)
        {
            

            // Store references to the tasks so that we can wait on them and  
            // observe their status after cancellation. 
            Task t;
            var tasks = new ConcurrentBag<Task>();

            //Console.WriteLine("Press any key to begin tasks...");
            //Console.ReadKey(true);
            //Console.WriteLine("To terminate the example, press 'c' to cancel and exit...");
            //Console.WriteLine();

            // Request cancellation of a single task when the token source is canceled. 
            // Pass the token to the user delegate, and also to the task so it can  
            // handle the exception correctly.
            t = Task.Factory.StartNew(() => DoSomeWork(1, token), token);
            Console.WriteLine("Task {0} executing", t.Id);
            tasks.Add(t);

            // Request cancellation of a task and its children. Note the token is passed 
            // to (1) the user delegate and (2) as the second argument to StartNew, so  
            // that the task instance can correctly handle the OperationCanceledException.
            t = Task.Factory.StartNew(() =>
            {
                // Create some cancelable child tasks.  
                Task tc;
                for (int i = 3; i <= 10; i++)
                {
                    // For each child task, pass the same token 
                    // to each user delegate and to StartNew.
                    tc = Task.Factory.StartNew(iteration => DoSomeWork((int)iteration, token), i, token);
                    Console.WriteLine("Task {0} executing", tc.Id);
                    tasks.Add(tc);
                    // Pass the same token again to do work on the parent task.  
                    // All will be signaled by the call to tokenSource.Cancel below.
                    DoSomeWork(2, token);
                }
            },
                                        token);

            Console.WriteLine("Task {0} executing", t.Id);
            tasks.Add(t);

            //// Request cancellation from the UI thread. 
            //char ch = Console.ReadKey().KeyChar;
            //if (ch == 'c' || ch == 'C')
            //{
            //    tokenSource.Cancel();
            //    Console.WriteLine("\nTask cancellation requested.");

            //    // Optional: Observe the change in the Status property on the task. 
            //    // It is not necessary to wait on tasks that have canceled. However, 
            //    // if you do wait, you must enclose the call in a try-catch block to 
            //    // catch the TaskCanceledExceptions that are thrown. If you do  
            //    // not wait, no exception is thrown if the token that was passed to the  
            //    // StartNew method is the same token that requested the cancellation. 
            //}
            Application.DoEvents();


            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch (AggregateException e)
            {
                Console.WriteLine("\nAggregateException thrown with the following inner exceptions:");
                // Display information about each exception. 
                foreach (var v in e.InnerExceptions)
                {
                    if (v is TaskCanceledException)
                        Console.WriteLine("   TaskCanceledException: Task {0}",
                                          ((TaskCanceledException)v).Task.Id);
                    else
                        Console.WriteLine("   Exception: {0}", v.GetType().Name);
                }
                Console.WriteLine();
            }
            finally
            {
                tokenSource.Dispose();
            }

            // Display status of all tasks. 
            foreach (var task in tasks)
                Console.WriteLine("Task {0} status is now {1}", task.Id, task.Status);
        }



        static void DoSomeWork(int taskNum, CancellationToken ct)
        {
            // Was cancellation already requested? 
            if (ct.IsCancellationRequested == true)
            {
                Console.WriteLine("Task {0} was cancelled before it got started.",
                                  taskNum);
                ct.ThrowIfCancellationRequested();
            }

            int maxIterations = 100;

            // NOTE!!! A "TaskCanceledException was unhandled 
            // by user code" error will be raised here if "Just My Code" 
            // is enabled on your computer. On Express editions JMC is 
            // enabled and cannot be disabled. The exception is benign. 
            // Just press F5 to continue executing your code. 
            for (int i = 0; i <= maxIterations; i++)
            {
                // Do a bit of work. Not too much. 
                var sw = new SpinWait();
                for (int j = 0; j <= 100; j++)
                    sw.SpinOnce();

                if (ct.IsCancellationRequested)
                {
                    Console.WriteLine("Task {0} cancelled", taskNum);
                    ct.ThrowIfCancellationRequested();
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            tokenSource.Cancel();
        }

        List<Thread> pool = new List<Thread>();
        ManualResetEvent mreStop = new ManualResetEvent(false);

        private void button3_Click(object sender, EventArgs e)
        {
            mreStop.Reset();

            for (int i = 0; i < 100; i++)
            {
                Thread tmpThread = new Thread(new ThreadStart(() =>
                {
                    while (!mreStop.WaitOne(1000))
                    {
                        Thread.Sleep(1000);
                        Console.WriteLine("Running {0}", Thread.CurrentThread.ManagedThreadId);
                    }
                }));
                pool.Add(tmpThread);

                tmpThread.Start();
            }

        }

        public void Dispose1()
        {
            mreStop.Set();
            pool.ForEach(t => { if (t.IsAlive) t.Join(); } );
            pool.Clear();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            mreStop.Set();
            pool.ForEach(t => { if (t.IsAlive) t.Abort(); });
            pool.Clear();
        }
    }
}
// The example displays output like the following:
//       Press any key to begin tasks...
//    To terminate the example, press 'c' to cancel and exit...
//    
//    Task 1 executing
//    Task 2 executing
//    Task 3 executing
//    Task 4 executing
//    Task 5 executing
//    Task 6 executing
//    Task 7 executing
//    Task 8 executing
//    c
//    Task cancellation requested.
//    Task 2 cancelled
//    Task 7 cancelled
//    
//    AggregateException thrown with the following inner exceptions:
//       TaskCanceledException: Task 2
//       TaskCanceledException: Task 8
//       TaskCanceledException: Task 7
//    
//    Task 2 status is now Canceled
//    Task 1 status is now RanToCompletion
//    Task 8 status is now Canceled
//    Task 7 status is now Canceled
//    Task 6 status is now RanToCompletion
//    Task 5 status is now RanToCompletion
//    Task 4 status is now RanToCompletion
//    Task 3 status is now RanToCompletion