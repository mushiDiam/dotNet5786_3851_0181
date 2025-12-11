namespace BlTest;
using DalApi;
using BlApi;
using BO;
using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
internal class Program
{
    static readonly BlApi.IBl s_bl = BlApi.Factory.get();
    enum MenuOptions{
        Exit, Admin, Courier, Order
    }
    static void Main(string[] args)
    {
        //Console.WriteLine("Hello, World!");
        while (true)
        {
            try
            {
                if(MainMenu() == 0)
                    break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
    private static MenuOptions MainMenu()
    {
        Console.WriteLine("Main Menu:");
        Console.WriteLine("1. Admin");
        Console.WriteLine("2. Courier");
        Console.WriteLine("3. Order");
        Console.WriteLine("0. Exit");
        Console.Write("Select an option: ");
        int choice = int.Parse(Console.ReadLine() ?? throw new InvalidOperationException("Input cannot be null"));
        return (MenuOptions)choice;
    }
}
