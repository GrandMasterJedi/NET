﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OKOSW.Extensions
{
    /// <summary>Useful extensions of Double class.</summary>
    public static class DoubleClassExtensions
    {
        /// <summary>
        /// Checks if this is computable double value.
        /// </summary>
        public static bool IsValid(this double x)
        {
            if (Double.IsInfinity(x) || Double.IsNaN(x))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Bounds given value.
        /// </summary>
        public static double Bound(this double x, double min = -1.0E20, double max = 1.0E20)
        {
            if(x < min)
            {
                return min;
            }
            if(x > max)
            {
                return max;
            }
            return x;
        }

        /// <summary>
        /// Computes power (faster way than standard Math)
        /// </summary>
        /// <param name="exponent"></param>
        public static double Power(this double x, uint exponent)
        {
            //Faster than Math.Pow
            switch (exponent)
            {
                case 2: return x * x;
                case 1: return x;
                case 0: return 1;
                default:
                    {
                        double result = x;
                        for (uint level = 2; level <= exponent; level++)
                        {
                            result *= result;
                        }
                        return result;
                    }
            }
        }
    }//DoubleClassExtensions
}//Namespace
