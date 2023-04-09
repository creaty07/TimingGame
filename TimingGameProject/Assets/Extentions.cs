using System;
public static class ArrayExtentions
{
    public static bool Exists<T>(this T[] array, T value) where T : struct
    {
        if (array == null) return false;

        bool exists = false;

        for (int i = 0; i < array.Length; i++)
        {
            if (array[i].Equals(value))
            {
                exists = true; break;
            }
        }

        return exists;
    }

    public static T[] AppendItem<T>(this T[] array, T value)
    {
        if (array == null) array = new T[1];

        int addIndex = array.Length;

        return array.AppendAtItem(value, addIndex);
    }

    public static T[] AppendAtItem<T>(this T[] array, T value, int index)
    {
        if (index < 0 || index > array.Length)
        {
            index = array.Length;
        }

        T[] newArray = new T[array.Length + 1];
        for (int i = 0; i < index; i++)
        {
            newArray[i] = array[i];
        }

        newArray[index] = value;

        for (int i = index + 1; i < newArray.Length; i++)
        {
            newArray[i] = array[i - 1];
        }

        return newArray;
    }

    public static T[] RemoveItem<T>(this T[] array, T value)
    {
        int index = FindIndex(array, value);

        return RemoveAtItem(array, index);
    }

    public static T[] RemoveAtItem<T>(this T[] array, int index)
    {
        if (index >= 0)
        {
            T[] newArray = new T[array.Length - 1];
            Array.Copy(array, 0, newArray, 0, index);
            Array.Copy(array, index + 1, newArray, index, array.Length - index - 1);

            return newArray;
        }
        else
        {
            return array;
        }
    }

    public static int FindIndex<T>(this T[] array, T value)
    {
        int index = 0;

        if(array != null)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].Equals(value))
                {
                    index = i;
                    break;
                }
            }
        }

        return index;
    }

    public static string ToArrayString<T>(this T[] array)
    {
        string value = "";

        for(int i = 0; i < array.Length; i++)
        {
            if (value.Length > 0) value += ", ";

            value += array[i].ToString();
        }

        return value;
    }

    public static int GetMinInt(this int[] array)
    {
        int min = int.MaxValue;

        for(int i = 0; i < array.Length; i++)
        {
            if(min > array[i]) min = array[i];
        }

        return min;
    }

    public static void SortArray(this int[] arr)
    {
        int temp;
        for (int i = 0; i < arr.Length - 1; i++)
        {
            for (int j = 0; j < arr.Length - i - 1; j++)
            {
                if (arr[j] > arr[j + 1])
                {
                    // Swap elements if they are in the wrong order
                    temp = arr[j];
                    arr[j] = arr[j + 1];
                    arr[j + 1] = temp;
                }
            }
        }
    }
}
