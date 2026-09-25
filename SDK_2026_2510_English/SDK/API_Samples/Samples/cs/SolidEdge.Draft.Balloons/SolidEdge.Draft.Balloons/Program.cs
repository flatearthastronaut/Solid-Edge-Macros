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

namespace SolidEdge.Draft.Balloons
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            SolidEdgeFramework.Application application = null;
            SolidEdgeFramework.Documents documents = null;
            SolidEdgeDraft.DraftDocument draftDocument = null;
            SolidEdgeDraft.Sheet sheet = null;
            SolidEdgeFrameworkSupport.Balloons balloons = null;
            SolidEdgeFrameworkSupport.Balloon balloon = null;
            SolidEdgeFrameworkSupport.DimStyle dimStype = null;

            try
            {
                Console.WriteLine("Registering OleMessageFilter.");

                // Register with OLE to handle concurrency issues on the current thread.
                OleMessageFilter.Register();

                Console.WriteLine("Connecting to Solid Edge.");

                // Connect to or start Solid Edge.
                application = ConnectToSolidEdge(true);

                // Make sure user can see the GUI.
                application.Visible = true;

                // Bring Solid Edge to the foreground.
                application.Activate();

                // Get a reference to the documents collection.
                documents = application.Documents;

                // Create a new draft document.
                draftDocument = (SolidEdgeDraft.DraftDocument)documents.Add("SolidEdge.DraftDocument");

                // Get a reference to the active sheet.
                sheet = draftDocument.ActiveSheet;

                // Get a reference to the balloons collection.
                balloons = (SolidEdgeFrameworkSupport.Balloons)sheet.Balloons;

                balloon = balloons.Add(0.05, 0.05, 0);
                balloon.TextScale = 1.0;
                balloon.BalloonText = "B";
                balloon.Leader = true;
                balloon.BreakLine = true;
                balloon.BalloonSize = 3;
                balloon.SetTerminator(balloon, 0, 0, 0, false);
                balloon.BalloonType = SolidEdgeFrameworkSupport.DimBalloonTypeConstants.igDimBalloonCircle;

                dimStype = balloon.Style;
                dimStype.TerminatorType = SolidEdgeFrameworkSupport.DimTermTypeConstants.igDimStyleTermFilled;

                balloon = balloons.Add(0.1, 0.1, 0);
                balloon.Callout = 1;
                balloon.TextScale = 1.0;
                balloon.BalloonText = "This is a callout";
                balloon.Leader = true;
                balloon.BreakLine = true;
                balloon.BalloonSize = 3;
                balloon.SetTerminator(balloon, 0, 0, 0, false);
                balloon.BalloonType = SolidEdgeFrameworkSupport.DimBalloonTypeConstants.igDimBalloonCircle;

                dimStype = balloon.Style;
                dimStype.TerminatorType = SolidEdgeFrameworkSupport.DimTermTypeConstants.igDimStyleTermFilled;
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
        static SolidEdgeFramework.Application ConnectToSolidEdge()
        {
            return ConnectToSolidEdge(false);
        }

        /// <summary>
        /// Connects to a running instance of Solid Edge with an option to start if not running.
        /// </summary>
        static SolidEdgeFramework.Application ConnectToSolidEdge(bool startIfNotRunning)
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


