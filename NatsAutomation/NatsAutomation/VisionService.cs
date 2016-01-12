using System;
using System.Runtime.InteropServices;
using BMDSwitcherAPI;

namespace NatsAutomation
{
    public class VisionService
    {
        private IBMDSwitcher Switcher;
        private IBMDSwitcherInputIterator InputIterator;

        public VisionService(String IP)
        {
            if (!MainForm.IGNORE_VISION)
            {
                _BMDSwitcherConnectToFailure failReason = 0;

                CBMDSwitcherDiscovery switcher_discovery = new CBMDSwitcherDiscovery();
                if (switcher_discovery == null)
                {
                    throw new Exception("ATEM Switcher Software not installed");
                }

                try
                {
                    switcher_discovery.ConnectTo(IP, out this.Switcher, out failReason);
                }
                catch (COMException)
                {
                    switch (failReason)
                    {
                        case _BMDSwitcherConnectToFailure.bmdSwitcherConnectToFailureNoResponse:
                            throw new Exception("No response from Switcher at " + IP);
                        case _BMDSwitcherConnectToFailure.bmdSwitcherConnectToFailureIncompatibleFirmware:
                            throw new Exception("Switcher at " + IP + " has incompatible firmware");
                        default:
                            throw new Exception("Connection to " + IP + " failed for unknown reason");
                    }
                }

                if (this.Switcher != null)
                {
                    IntPtr input_iterator_ptr;
                    this.Switcher.CreateIterator(typeof(IBMDSwitcherInputIterator).GUID, out input_iterator_ptr);
                    if (input_iterator_ptr != null)
                    {
                        this.InputIterator = (IBMDSwitcherInputIterator)Marshal.GetObjectForIUnknown(input_iterator_ptr);
                    }
                    else
                    {
                        this.Switcher = null;
                    }
                }
            }
        }

        public void RunMacro(int index)
        {
            if (Switcher != null)
            {
                IBMDSwitcherMacroControl macroControl = (IBMDSwitcherMacroControl)Switcher;
                macroControl.Run(Convert.ToUInt32(index));
            }
        }
    }
}
