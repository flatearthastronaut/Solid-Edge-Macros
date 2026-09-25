// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.

// Copyright (c) Siemens Product Lifecycle Management Software Inc. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace SolidEdge.Automation
{
    /// <summary>
    /// Console application demonstrating basic Solid Edge automation.
    /// </summary>
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            SolidEdgeFramework.Application application = null;
            SolidEdgeFramework.Documents documents = null;
            SolidEdgeFramework.SolidEdgeDocument document = null;

            try
            {
                Console.WriteLine("Registering OleMessageFilter.");

                // Register with OLE to handle concurrency issues on the current thread.
                OleMessageFilter.Register();

                Console.WriteLine("Connecting to Solid Edge.");

                // Connect to or start Solid Edge.
                application = ConnectToSolidEdge(true);

                // Ensure Solid Edge GUI is visible.
                application.Visible = true;

                // Bring Solid Edge to the foreground.
                application.Activate();

                // Get a reference to the documents collection.
                documents = application.Documents;

                Console.WriteLine("Creating a new part document.  No template specified.");
                document = (SolidEdgeFramework.SolidEdgeDocument)documents.Add("SolidEdge.PartDocument");

                //string template = "your template.par";
                //Console.WriteLine("Creating a new part document.  Template '{0}' specified.", template);
                //document = (SolidEdgeFramework.SolidEdgeDocument)documents.Add("SolidEdge.PartDocument", template);

                //Console.WriteLine("Quitting Solid Edge.");

                // Quit Solid Edge.
                //application.Quit();
            }
            catch (System.Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debugger.Break();
#endif
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Console.WriteLine("Unregistering OleMessageFilter.");
                OleMessageFilter.Revoke();
            }
        }

        /// <summary>
        /// Connects to a running instance of Solid Edge.
        /// </summary>
        public static SolidEdgeFramework.Application ConnectToSolidEdge()
        {
            return ConnectToSolidEdge(false);
        }

        /// <summary>
        /// Connects to a running instance of Solid Edge with an option to start if not running.
        /// </summary>
        public static SolidEdgeFramework.Application ConnectToSolidEdge(bool startIfNotRunning)
        {
            try
            {
                // Attempt to connect to a running instance of Solid Edge.
                return (SolidEdgeFramework.Application)
                    Marshal.GetActiveObject("SolidEdge.Application");
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                // Failed to connect.
                if (ex.ErrorCode == -2147221021 /* MK_E_UNAVAILABLE */)
                {
                    if (startIfNotRunning)
                    {
                        // Start Solid Edge.
                        return (SolidEdgeFramework.Application)
                            Activator.CreateInstance(Type.GetTypeFromProgID("SolidEdge.Application"));
                    }
                    else
                    {
                        throw new System.Exception("Solid Edge is not running.");
                    }
                }
                else
                {
                    throw;
                }
            }
            catch
            {
                throw;
            }
        }
    }
}


