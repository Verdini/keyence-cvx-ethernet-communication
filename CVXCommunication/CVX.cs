using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace CVXCommunication
{
    /// <summary>
    /// Defines the communication return codes from CVX.
    /// </summary>
    public enum CVXResponseCode
    {
        OK = 0, CommandError = 2, CommandDisabled = 3, ParameterError = 22, Timeout = 99
    }

    /// <summary>
    /// This class is responsible for controlling the CVX
    /// </summary>
    public class CVX: IDisposable
    {
        /// <summary>
        /// Socket that handles the TCP communication.
        /// </summary>
        private Socket client;

        /// <summary>
        /// Data buffer for incoming data.
        /// </summary>
        byte[] bytes = new byte[1024];

        /// <summary>
        /// Separator characters for the incoming messages.
        /// Configured in the CVX controller (Network Settings >> Non-Procedural).
        /// </summary>
        char[] separator = { ',', '\r' };


        /// <summary>
        /// Encodes the messages and decodes the responses.
        /// </summary>
        /// <param name="req">The sending message</param>
        /// <returns>The incoming message</returns>
        private string sendMessage(string req)
        {
            byte[] byteData = Encoding.UTF8.GetBytes(req);
            int bytesRead = client.Send(byteData);
            int bytesRec = client.Receive(bytes);
            return Encoding.ASCII.GetString(bytes, 0, bytesRec);
        }


        #region Interface

        #region Connection
        /// <summary>
        /// Starts the connection with the CVX.
        /// The controller IP and Port are configured in Network Settings (Non-Procedural).
        /// </summary>
        /// <param name="ip">Controller IP address</param>
        /// <param name="port">Controller port</param>
        /// /// <param name="timeout">Communication timeout in milliseconds</param>
        /// <returns></returns>
        public bool Connect(string ip, int port, int timeout)
        {
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(ip), port);
                client = new Socket(SocketType.Stream, ProtocolType.Tcp);
                client.ReceiveTimeout = timeout;
                client.Connect(remoteEP);
                return true;
            }
            catch
            {
                return false;
            }          
        }


        /// <summary>
        /// Release the socket when the object is destroyed.
        /// </summary>
        public void Dispose()
        { 
            client?.Shutdown(SocketShutdown.Both);
            client?.Close();
        }
        #endregion


        /// <summary>
        /// Checks if the controller is in the setup or run mode.
        /// Request message = "RM\r"
        /// Response message = "RM,n\r" [0 = setup; 1 = run] when message OK
        /// Response message = "ER,RM,nn\r" [nn = error code] when message error
        /// This method works both in Setup or Run Mode
        /// </summary>
        /// <param name="isRunMode">Output response</param>
        /// <returns>Error code</returns>
        public CVXResponseCode ReadRunSetupMode(out bool isRunMode)
        {
            isRunMode = false;
            string res = sendMessage("RM\r");

            string[] resSplit = res.Split(separator);
            if (String.Compare(resSplit[0], "RM") == 0)
            {
                isRunMode = ( String.Compare(resSplit[1], "1") == 0 ? true : false);
                return CVXResponseCode.OK;
            }
                
            return (CVXResponseCode)Convert.ToInt32(resSplit[2]);   // Return error code
        }

        /// <summary>
        /// Set the controller in run mode.
        /// Request message = "R0\r"
        /// Response message = "R0\r" when message ok
        /// Response message = "ER,R0,nn\r" [nn = error code] when message error
        /// This method works both in Setup or Run Mode
        /// </summary>
        /// <returns>Error code</returns>
        public CVXResponseCode SetRunMode()
        {
            string res = sendMessage("R0\r");  

            string[] resSplit = res.Split(separator);
            if (String.Compare(resSplit[0], "R0") == 0)
                return CVXResponseCode.OK;

            return (CVXResponseCode)Convert.ToInt32(resSplit[2]);
        }

        /// <summary>
        /// Read the current program.
        /// Request message = "PR\r"
        /// Response message = "PR,d,nnn\r" [d = sd card number (1 or 2); nnn = program number (0 to 999)] when message ok
        /// Response message = "ER,PR,nn\r" [nn = error code] when message error
        /// This method only works in Run Mode
        /// </summary>
        /// <param name="sdcardNumber">output current sdcard</param>
        /// <param name="programNumber">output current program</param>
        /// <returns>Error code</returns>
        public CVXResponseCode ReadProgram(out int sdcardNumber, out int programNumber)
        {
            sdcardNumber = -1;
            programNumber = -1;
            string res = sendMessage("PR\r");

            string[] resSplit = res.Split(separator);
            if (String.Compare(resSplit[0], "PR") == 0)
            {
                sdcardNumber = Convert.ToInt32(resSplit[1]);
                programNumber = Convert.ToInt32(resSplit[2]);
                return CVXResponseCode.OK;
            }

            return (CVXResponseCode)Convert.ToInt32(resSplit[2]);   // Return error code
        }

        /// <summary>
        /// Sets the program in the controller
        /// Request message = "PW,d,nnn\r" [d = sd card number (1 or 2); nnn = program number (0 to 999)]
        /// Response message = "PW\r"  when message ok
        /// Response message = "ER,PW,nn\r" [nn = error code] when message error
        /// This method only works in Run Mode
        /// </summary>
        /// <param name="sdcardNumber"></param>
        /// <param name="programNumber"></param>
        /// <returns>Error code</returns>
        public CVXResponseCode ChangeProgram(int sdcardNumber, int programNumber)
        {
            
            string msg = "PW," + sdcardNumber + "," + programNumber.ToString("000") + "\r";
            string res = sendMessage(msg);

            string[] resSplit = res.Split(separator);
            if (String.Compare(resSplit[0], "PW") == 0)
                return CVXResponseCode.OK;

            return (CVXResponseCode)Convert.ToInt32(resSplit[2]);   // Return error code
        }


        /// <summary>
        /// Reads the current condition execution number.
        /// Request message = "EXR\r"
        /// Response message = "EXR,nn\r" [nn = execute condition number (0 to 99)] when message ok
        /// Response message = "ER,EXR,nn\r" [nn = error code] when message error
        /// This method only works in Run Mode
        /// </summary>
        /// <param name="execNo">output the current execution number</param>
        /// <returns>Error code</returns>
        public CVXResponseCode ReadExecNo(out int execNo)
        {
           
            execNo = -1;
            string res = sendMessage("EXR\r");

            string[] resSplit = res.Split(separator);
            if (String.Compare(resSplit[0], "EXR") == 0)
            {
                execNo = Convert.ToInt32(resSplit[1]);
                return CVXResponseCode.OK;
            }

            return (CVXResponseCode)Convert.ToInt32(resSplit[2]);   // Return error code
        }

        /// <summary>
        /// Sets the condition execution number.
        /// Request message = "EXW,n\r" [n = execute condition number (0 to 99)]
        /// Response message = "EXW\r"  when message ok
        /// Response message = "ER,EXW,nn\r" [nn = error code] when message error
        /// This method only works in Run Mode
        /// </summary>
        /// <param name="execNo"></param>
        /// <returns>Error code</returns>
        public CVXResponseCode WriteExecNo(int execNo)
        {
            string res = sendMessage("EXW," + execNo + "\r");

            string[] resSplit = res.Split(separator);
            if (String.Compare(resSplit[0], "EXW") == 0)
                return CVXResponseCode.OK;

            return (CVXResponseCode)Convert.ToInt32(resSplit[2]);   // Return error code
        }

        /// <summary>
        /// Reset the current program.
        /// Request message = "RS\r" 
        /// Response message = "RS\r"  when message ok
        /// Response message = "ER,RS,nn\r" [nn = error code] when message error
        /// This method only works in Run Mode
        /// </summary>
        /// <returns>Error code</returns>
        public CVXResponseCode Reset()
        {
           
            string res = sendMessage("RS\r");

            string[] resSplit = res.Split(separator);
            if (String.Compare(resSplit[0], "RS") == 0)
                return CVXResponseCode.OK;
            else
                return (CVXResponseCode)Convert.ToInt32(resSplit[2]);
        }

        /// <summary>
        /// Triggers the cameras and get the result data from the controller.
        /// The data must be configured in the controller (Output >> Ethernet (Non-procedural))
        /// Request message = "EXW,n\r" [n = execute condition number (0 to 99)]
        /// Response message = "EXW\r"  when message ok
        /// Response message = "ER,EXW,nn\r" [nn = error code] when message error
        /// This method only works in Run Mode
        /// </summary>
        /// <param name="resultData">output the data received from the controller</param>
        /// <returns>Error code</returns>
        public CVXResponseCode Trigger(out double[] resultData)
        {
            
            string res = sendMessage("TA\r");

            string[] resSplit = res.Split(separator);
            if (String.Compare(resSplit[0], "TA") == 0)
            {
                try
                {
                    int bytesRec = client.Receive(bytes);
                    string result = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    string[] resultSplit = result.Split(separator);
                    resultData = new double[resultSplit.Length - 1];
                    for (int i = 0; i < resultData.Length; i++)
                        resultData[i] = Convert.ToDouble(resultSplit[i], CultureInfo.InvariantCulture);

                    return CVXResponseCode.OK;
                }
                catch(SocketException scktEx)
                {
                    if(scktEx.SocketErrorCode == SocketError.TimedOut)
                    {
                        resultData = null;
                        return CVXResponseCode.Timeout;
                    }
                }  
            }

            resultData = null;
            return (CVXResponseCode)Convert.ToInt32(resSplit[2]);   // Return error code
        }


        /// <summary>
        /// Saves the current image as a reference image.
        /// Request message = "BS,c,nnn\r" [c = camera number (1 to 4), nnn = reference image number (0 to 999)]
        /// Response message = "BS\r"  when message ok
        /// Response message = "ER,BS,nn\r" [nn = error code] when message error
        /// This method only works in Run Mode
        /// </summary>
        /// <param name="cameraNo">The camera number (1 to 4)</param>
        /// <param name="referenceNo">The image reference number (0 to 999)</param>
        /// <returns>Error code</returns>
        public CVXResponseCode ImageRegistration(int cameraNo, int referenceNo)
        {
            string res = sendMessage("BS," + cameraNo + "," + referenceNo + "\r");

            string[] resSplit = res.Split(separator);
            if (String.Compare(resSplit[0], "BS") == 0)
                return CVXResponseCode.OK;
            else
                return (CVXResponseCode)Convert.ToInt32(resSplit[2]);
        }
        #endregion
    }
}
