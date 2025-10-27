using System;
namespace Targil0
{

    partial class Program
    {
        private static void Main(string[] args)
        {
            Welcome3851();
            Welcome0181();
            Console.ReadKey();
        }

        private static void Welcome3851()
        {
            Console.WriteLine("Enter your name: ");
            string userName = Console.ReadLine();
            Console.WriteLine("{0}, welcome to my first console appliacation", userName);
        }
        static partial void Welcome0181();
    }
}