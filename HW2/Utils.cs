﻿// <copyright file="Utils.cs" company="Boxiang Lin - WSU 011601661">
// Copyright (c) Boxiang Lin - WSU 011601661. All rights reserved.
// </copyright>

namespace HW2
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// This the class of the collection of static methods.
    /// </summary>
    public class Utils
    {
        /// <summary>
        /// The method to generate and return a random list with boundary of element value and size of the list specified.
        /// </summary>
        /// <param name="min"> min boundary. </param>
        /// <param name="max"> max boundary. </param>
        /// <param name="size"> size of the list. </param>
        /// <returns> return a int type random list. </returns>
        public static List<int> GetGenerateRandomList(int min, int max, int size)
        {
            Random rd = new Random();
            List<int> rdList = new List<int>();
            for (int i = 0; i < size; i++)
            {
                rdList.Add(rd.Next(min, max + 1)); // [min,max+1) = [min,max] based on integer min diff = 1
            }

            return rdList;
        }

        /// <summary>
        /// The method that calculate the numbers of distinct value through hashset.
        /// </summary>
        /// <param name="rdList"> pass in a random list. </param>
        /// <returns> return a number of distinct value. </returns>
        public static int GetByHashSetDistinct(List<int> rdList)
        {
            HashSet<int> hash = new HashSet<int>();
            foreach (int val in rdList)
            {
                hash.Add(val); // We know that build-in hashset will do its determination whether add or not the value by the value distinction.
            }

            return hash.Count;
        }

        /// <summary>
        /// This is a method to calculate numbers of distinct value by constant space.
        /// This should be a O(N^2) worst time compleixty and O(1) space.
        /// </summary>
        /// <param name="rdList"> pass in a random list. </param>
        /// <returns> return a number of distinct value. </returns>
        public static int GetByConstantSpaceDistinct(List<int> rdList)
        {
            if (rdList.Count < 2)
            {
                return rdList.Count; // empty or 1 size of list, just return list size.
            }

            // About worstly O(N*N*1) = O(N^2) time complexity.
            int res = rdList.Count;
            for (int i = 0; i < rdList.Count - 1; i++)
            {// O(N) is a must in best,average,worst cases.
                for (int j = i + 1; j < rdList.Count; j++)
                {// O(N) happens in worst case "no duplicates".
                    if (rdList[i] == rdList[j])
                    {// O(1) Cosntant
                        /* When we found a duplicate in j(fast pointer) we decrease the res by 1;
                         * We want to break and start i(slow pointer) next step because we sure that we
                         * will eventually encounter this duplicate again.
                         */
                        res--;
                        break;
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// This is a method to calculate numbers of distinct value by sorting technique.
        /// </summary>
        /// <param name="rdList">  pass in a random list. </param>
        /// <returns> return a number of distinct value. </returns>
        public static int GetBySortDisdinct(List<int> rdList)
        {
            // sort the list
            rdList.Sort();
            int res = rdList.Count;

            // Handle list.count >= 2
            /*
             * For any E_i in rdList, each E_i <= E_i+1 <=> E_i-1 <= E_i then we see, each E_i-1 = E_i indicates one duplicates.
             * By starting i from 1 to N (size of list), at worst suppose each E_i the same, we will get N-1 duplicates which satified our purpose.
             */
            for (int i = 1; i < rdList.Count; i++)
            {
                if (rdList[i] == rdList[i - 1])
                {
                    res--;
                }
            }

            // if list.count < 2, for loop condition i inequality not satified because not valid for i = 1 < 1("list.Count") so just return the list.count.
            return res;
        }
    }
}