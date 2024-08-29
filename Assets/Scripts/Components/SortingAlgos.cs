using System.Collections.Generic;
using System.Linq;

namespace Components
{
    public static class SortingAlgos
    {
        public static List<Tile> MergeSort(List<Tile> unsorted, int check)
        {
            if (unsorted.Count <= 1)
                return unsorted; // Base case: return the list if it has only one element

            // Divide the unsorted list into two halves
            List<Tile> left = new List<Tile>();
            List<Tile> right = new List<Tile>();

            int middle = unsorted.Count / 2;
            for (int i = 0; i < middle; i++) // Split the unsorted list into left
            {
                left.Add(unsorted[i]);
            }

            for (int i = middle; i < unsorted.Count; i++) // Split the unsorted list into right
            {
                right.Add(unsorted[i]);
            }

            // Recursively perform Merge Sort on the divided lists
            left = MergeSort(left, check);
            right = MergeSort(right, check);

            // Merge the sorted halves
            return Merge(left, right, check);
        }

        // Method to merge two sorted lists
        private static List<Tile> Merge(List<Tile> left, List<Tile> right, int check)
        {
            List<Tile> result = new List<Tile>();

            // Compare elements from both lists and merge them in sorted order
            while (left.Count > 0 || right.Count > 0)
            {
                if (left.Count > 0 && right.Count > 0)
                {
                    if (check == 1)
                    {
                        if (left.First().Coords.x >= right.First().Coords.x) // Compare the first elements of both lists
                        {
                            result.Add(left.First());
                            left.Remove(left.First()); // Remove the added element from the list
                        }
                    }

                    
                        else if (left.First().Coords.y <= right.First().Coords.y)
                        {
                            result.Add(left.First());
                            left.Remove(left.First());
                        }
                        else
                        {
                            result.Add(right.First());
                            right.Remove(right.First()); // Remove the added element from the list
                        }
                }
                else if (left.Count > 0)
                {
                    result.Add(left.First());
                    left.Remove(left.First());
                }
                else if (right.Count > 0)
                {
                    result.Add(right.First());
                    right.Remove(right.First());
                }
            }

            return result; // Return the merged and sorted list
        }
    }
}