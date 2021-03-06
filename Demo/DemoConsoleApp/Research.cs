﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCNet.DemoConsoleApp.Log;
using RCNet.Neural.Activation;
using RCNet.Extensions;
using RCNet.MathTools;
using RCNet.Neural.Network.SM;
using RCNet.MathTools.Differential;
using RCNet.MathTools.VectorMath;

namespace RCNet.DemoConsoleApp
{
    class Research
    {
        //Attributes

        //Constructor
        public Research()
        {
            return;
        }

        //Methods
        public void Run()
        {
            /*
            IActivationFunction af = new SimpleIF(15,
                                                  0.1,
                                                  5,
                                                  20,
                                                  0,
                                                  1
                                                  );
            IActivationFunction af = new AdSimpleIF(15,
                                                    0.1,
                                                    5,
                                                    20,
                                                    1
                                                    );
            IActivationFunction af = new LeakyIF(8,
                                                 10,
                                                 -70,
                                                 -65,
                                                 -50,
                                                 0,
                                                 5.5
                                                 );
            IActivationFunction af = new ExpIF(12,
                                               20,
                                               -65,
                                               -60,
                                               -55,
                                               -30,
                                               2,
                                               0,
                                               4.5
                                               );
            IActivationFunction af = new AdExpIF(5,
                                                 500,
                                                 -70,
                                                 -51,
                                                 -50,
                                                 -30,
                                                 2,
                                                 0.5,
                                                 100,
                                                 7,
                                                 200
                                                 );
            */
            //TestDEq();
            IActivationFunction af = new SimpleIF(15,
                                                  0.1,
                                                  5,
                                                  20,
                                                  0,
                                                  1
                                                  );
            TestActivation(af, 800, double.NaN, 10, 190);
            return;
        }

        private void TestActivation(IActivationFunction af, int simLength, double constCurrent, int from, int count)
        {
            Random rand = new Random();
            for (int i = 1; i <= simLength; i++)
            {
                double signal = 0;
                if (i >= from && i < from + count)
                {
                    double input = double.IsNaN(constCurrent) ? rand.NextDouble(0, 1, false, RandomClassExtensions.DistributionType.Uniform) : constCurrent;
                    signal = af.Compute(input);
                }
                else
                {
                    signal = af.Compute(0);
                }
                Console.WriteLine($"{i}, State {af.InternalState} signal {signal}");
            }
            Console.ReadLine();

            return;
        }

        private void TestDEq()
        {
            Vector v = new Vector(1);
            v[0] = 5;
            double time = 0;
            double timeStep = 0.001d;
            for (int i = 0; i < 100; i++)
            {
                foreach(ODENumSolver.Estimation subResult in ODENumSolver.Solve(DEq, time, v, timeStep, 10, ODENumSolver.Method.Euler))
                {
                    v = subResult.V;
                }
                time += timeStep;
                Console.WriteLine($"{time}, v = {v[0]}");
            }
            Console.ReadLine();
            return;
        }


        private Vector DEq(double t, Vector v)
        {
            return v;
        }


    }//Research
}
