﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreadTest
{
    class Program
    {
        static void Main(string[] args)
        {
            TestThread test = new TestThread();
            test.initAndStart();

        }
    }
}
