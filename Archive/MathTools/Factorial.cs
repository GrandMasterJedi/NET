﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OKOSW.MathTools
{
    /// <summary>
    /// Implements cached factorial computation.
    /// </summary>
    public static class Factorial
    {
        //Constants
        public const uint MAX_BASE = 20;
        //Attributes
        private static readonly ulong[] m_cache;

        //Constructor
        static Factorial()
        {
            //Preparation of the precomputed factorial cache
            m_cache = new ulong[MAX_BASE + 1];
            m_cache[0] = 1;
            for(uint n = 1; n <= MAX_BASE; n++)
            {
                m_cache[n] = m_cache[n - 1] * n;
            }
            return;
        }

        /// <summary>
        /// Returns n!. Product of all positive integers less than or equal to n (n has to be LE to Factorial.MAX_BASE).
        /// </summary>
        /// <param name="n"></param>
        public static ulong Get(uint n)
        {
            if(n > MAX_BASE)
            {
                throw (new ApplicationException("Argument is bigger than Factorial.MAX_BASE"));
            }
            return m_cache[n];
        }

        /// <summary>
        /// Computes product of integers LE than n and GE than ((n - count) + 1)
        /// Example: n = 50 and count = 3 computes 50*49*48
        /// </summary>
        /// <param name="n"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static ulong PartialReversal(uint n, uint count)
        {
            if (count < 1) return 0;
            if (n == 0) return 1;
            if (count > n) count = n;
            ulong result = n;
            double ctrlResult = n;
            --n;
            for (count = count - 1; count > 0; count--, n--)
            {
                result *= n;
                ctrlResult *= n;
                if(Math.Floor(ctrlResult) > result)
                {
                    throw (new ApplicationException("Result is too big"));
                }
                ctrlResult = result;
            }
            return result;
        }

    }//Factorial
} //Namespace
