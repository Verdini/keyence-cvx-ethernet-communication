using System;

namespace CVXCommunication
{
    public class Program
    {
        /// <summary>
        /// Example program to test the CVX communication.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            CVX cvx;
            CVXResponseCode res;
            bool isRunmode;
            int readSdcard;
            int readProgram;
            int readExecNo;
            double[] resultData;

            int targetSdcard = 1;
            int targetProgram = 0;
            int targetExecNo = 0;


            Console.WriteLine("Starting CVX Ethernet communication test...");

            cvx = new CVX();
            if(!cvx.Connect("192.168.0.10", 8500, 2000))
            {
                Console.WriteLine("Error: Couldn't connect to CVX!");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Connected to CVX!");

            // Check if the controller mode and set the run mode if it's not in it.
            res = cvx.ReadRunSetupMode(out isRunmode);
            if (!isRunmode)
                res = cvx.SetRunMode();

            // Check the current program and changed it if needed
            res = cvx.ReadProgram(out readSdcard, out readProgram);
            if (readSdcard != targetSdcard || readProgram != targetProgram)
                res = cvx.ChangeProgram(targetSdcard, targetProgram);

            // Check the current Execution Number and change it if needed
            res = cvx.ReadExecNo(out readExecNo);
            if (readExecNo != targetExecNo)
                res = cvx.WriteExecNo(targetExecNo);

            // Reset the program
            res = cvx.Reset();

            // Trigger the cameras and get the result
            res = cvx.Trigger(out resultData);
            Console.WriteLine("Cameras have been triggered. Results:");
            if (res == CVXResponseCode.Timeout || resultData == null)
                Console.WriteLine("No data received");
            else
                foreach (double data in resultData)
                    Console.WriteLine(data);

            Console.WriteLine("CVX communication test ended.");
            Console.ReadKey();
        }
    }
}